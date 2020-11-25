using System;
using System.IO;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace GzipArchiver
{
    public class CompressionPipeline : IDisposable
    {
        public string SourcePath { get; }
        public string DestinationPath { get; }
        public int WorkersNumber { get; } = Environment.ProcessorCount * 2;
        public long MaxMemoryUsage { get; } = 1000 * 1024 * 1024;   // not strict limitation
        public int WorkTimeoutMinutes { get; } = 30;

        public CompressionPipeline(
            ISourceReader reader, IWorker worker, IResultWriter writer, ILogger logger)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _worker = worker ?? throw new ArgumentNullException(nameof(worker));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public void DoWork()
        {
            StartWorkerThreads();
            StartOutboundQueueHandler();

            FillInboundQueue();
            WaitInboundQueueIsEmpty();
            // After we set it here there is no chance of new tickets in the inbound queue.
            //  This means we don't have race condition here from inbound queue perspective.
            SignalNoMoreInboundData();

            WaitPipelineIsFinished();
        }

        public void Dispose()
        {
            // TODO: reader/writer/threads?

            _writer.Dispose();
        }

        private void StartWorkerThreads()
        {
            if (_workers != null)
                throw new Exception("One instance per operation please!");

            _logger.Log($"Starting {WorkersNumber} worker thread[s]...");

            _workers = new List<Thread>(WorkersNumber);
            _workFinishedEvents = new List<ManualResetEvent>(WorkersNumber);

            for (int i = 0; i < WorkersNumber; ++i)
            {
                var ts = new ParameterizedThreadStart(WorkerFunc);

                var t = new Thread(ts);
                var workFinishedEvent = new ManualResetEvent(false);

                t.Start(workFinishedEvent);

                _workers.Add(t);
                _workFinishedEvents.Add(workFinishedEvent);
            }
        }

        private void WorkerFunc(object param)
        {
            // validate input parameters before the work
            var workFinishedEvent = param as ManualResetEvent ?? throw new ArgumentNullException(nameof(param));

            var threadId = Thread.CurrentThread.ManagedThreadId;
            _logger.Log($"Worker '{threadId}' is started.");

            while (true)
            {
                var haveTicket = _inboundQueue.TryDequeue(out var ticket);
                if (haveTicket)
                {
                    _logger.Log($"Worker '{threadId}' got a next portion with index '{ticket.Index}'.");

                    var handledPortion = _worker.HandlePortion(ticket.Data);

                    _outboundQueue.Enqueue(new PortionTicket(
                        ticket.Index,
                        handledPortion
                    ));

                    ticket.Data.Dispose();
                }
                else
                {
                    if (_noMoreInboundData)
                    {
                        _logger.Log($"Worker '{threadId}' is going to break the loop due to no more input data.");
                        break;
                    }

                    // TODO : makes sense to think about some smarter pause
                    Thread.Sleep(1);
                }
            }

            _logger.Log($"Worker '{threadId}' will signal via event '{workFinishedEvent.SafeWaitHandle.GetHashCode()}' it is finished...");
            workFinishedEvent.Set();

            _logger.Log($"Worker '{threadId}' is finished.");
        }

        private void StartOutboundQueueHandler()
        {
            ThreadStart ts = new ThreadStart(OutboundQueueHandlerFunc);

            _outboundQueueHandler = new Thread(ts);
            _outboundQueueHandler.Start();
        }

        private void OutboundQueueHandlerFunc()
        {
            _logger.Log($"Output queue handler thread '{Thread.CurrentThread.ManagedThreadId}' is started.");

            while (true)
            {
                var haveTicket = _outboundQueue.TryDequeue(out var ticket);
                if (haveTicket)
                {
                    _logger.Log($"Output handler got a next portion with index '{ticket.Index}'.");

                    #warning TODO : sort tickets by indices here before write them

                    _writer.WritePortion(ticket.Data);
                    ticket.Data.Dispose();
                }
                else
                {
                    // TODO : makes sense to think about some smarter pause
                    var finished = WaitWorkersAreFinished(TimeSpan.FromMilliseconds(1));
                    if (_noMoreInboundData && finished && _outboundQueue.IsEmpty)
                    {
                        _logger.Log($"Going to stop output handler...");
                        break;
                    }
                }
            }

            _logger.Log($"Output queue handler thread is finished.");
        }

        private void FillInboundQueue()
        {
            // TODO : handle OutOfMemoryException via exponential retry?

            _logger.Log("Start filling inbound queue...");

            long portionIndex = 0;
            while (true)
            {
                var portionStream = _reader.ReadNextPortion();
                if (portionStream != null)
                {
                    _inboundQueue.Enqueue(new PortionTicket(
                        portionIndex,
                        portionStream
                    ));

                    portionIndex ++;
                }
                else break;
            }

            _logger.Log($"Overall added {portionIndex} portion[s] into inbound queue.");
        }

        private void WaitInboundQueueIsEmpty()
        {
            _logger.Log("Waiting for inbound queue is empty...");

            // super simple way to wait for the queue is empty
            var startedAt = DateTime.UtcNow;
            while (_inboundQueue.Count > 0)
            {
                Thread.Sleep(100);
                if (DateTime.UtcNow.Subtract(startedAt) > TimeSpan.FromMinutes(WorkTimeoutMinutes))
                    throw new TimeoutException("hm operation takes too long today");
            }

            _logger.Log("Inbound queue is empty.");
        }

        private void SignalNoMoreInboundData()
        {
            _logger.Log("Will signal that there is no more input data...");
            _noMoreInboundData = true;
        }

        private bool WaitWorkersAreFinished(TimeSpan timeout)
        {
            // can use just Join() in loop here but I'd like to summarize timeout for all threads.
            var finished = WaitHandle.WaitAll(_workFinishedEvents.ToArray(), timeout);

            return finished;
        }

        private void WaitPipelineIsFinished()
        {
            _logger.Log("Start waiting for whole pipeline is finished...");

            var timeout = TimeSpan.FromMinutes(WorkTimeoutMinutes);

            _logger.Log($"Will wait for {_workFinishedEvents.Count} workers to finish with timeout {timeout}...");
            var finished = WaitWorkersAreFinished(timeout);
            if (!finished)
                throw new TimeoutException("hm operation takes too long today");
            
            _logger.Log($"Will wait for output data handler thread is finished with timeout {timeout}...");
            finished = _outboundQueueHandler.Join(timeout);
            if (!finished)
                throw new TimeoutException("hm operation takes too long today");

            _logger.Log("Whole pipeline is finished.");
        }

        ISourceReader _reader;
        IWorker _worker;
        IResultWriter _writer;

        private class PortionTicket : IDisposable
        {
            public long Index { get; }
            public Stream Data { get; }

            public PortionTicket(long index, Stream data)
            {
                Index = (index >= 0) ?
                    index : throw new ArgumentOutOfRangeException(nameof(index));
                
                Data = data ?? throw new ArgumentNullException(nameof(data));
            }

            public void Dispose()
            {
                Data?.Dispose();
            }
        }

        private bool _noMoreInboundData = false;
        private ConcurrentQueue<PortionTicket> _inboundQueue = new ConcurrentQueue<PortionTicket>();
        private ConcurrentQueue<PortionTicket> _outboundQueue = new ConcurrentQueue<PortionTicket>();

        private List<Thread> _workers;
        private List<ManualResetEvent> _workFinishedEvents;

        private Thread _outboundQueueHandler;

        private ILogger _logger;
    }
}
