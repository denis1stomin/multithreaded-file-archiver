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
            ISourceReader reader, IWorker worker, IResultWriter writer)
        {
            _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            _worker = worker ?? throw new ArgumentNullException(nameof(worker));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
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

            WaitWorkIsFinished();
        }

        public void Dispose()
        {
            // TODO: reader/writer/threads?
        }

        private void StartWorkerThreads()
        {
            if (_workers != null)
                throw new Exception("One instance per operation please!");

            _workers = new List<Thread>(WorkersNumber);
            _workFinishedEvents = new List<ManualResetEvent>(WorkersNumber);

            for (int i = 0; i < WorkersNumber; ++i)
            {
                ThreadStart ts = new ThreadStart(WorkerFunc);

                var t = new Thread(ts);
                t.Start();

                _workers.Add(t);
                _workFinishedEvents.Add(new ManualResetEvent(false));
            }
        }

        private void WorkerFunc()
        {
            while (true)
            {
                var haveTicket = _inboundQueue.TryDequeue(out var ticket);
                if (haveTicket)
                {
                    var handledPortion = _worker.HandlePortion(ticket.Data);

                    _outboundQueue.Enqueue(new PortionTicket(
                        ticket.Index,
                        handledPortion
                    ));
                }
                else
                {
                    if (_noMoreInboundData)
                        break;

                    // TODO : makes sense to think about some smarter pause
                    Thread.Sleep(1);
                }
            }

            MarkThreadFinished();
        }

        private void MarkThreadFinished()
        {
            // TODO : it is better to merge workers collection and its events collection

            var idx = _workers.FindIndex(
                el => el.ManagedThreadId == Thread.CurrentThread.ManagedThreadId);
            _workFinishedEvents[idx].Set();
        }

        private void StartOutboundQueueHandler()
        {
            ThreadStart ts = new ThreadStart(WorkerFunc);

            _outboundQueueHandler = new Thread(ts);
            _outboundQueueHandler.Start();
        }

        private void OutboundQueueHandlerFunc()
        {
            while (true)
            {
                var haveTicket = _outboundQueue.TryDequeue(out var ticket);
                if (haveTicket)
                {
                    // TODO : sort tickets by indices here before write them

                    _writer.WritePortion(ticket.Data);
                }
                else
                {
                    // TODO : check that all Workers are finished their work
                    //          Wait all manual reset events with a small timeout here.

                    // TODO : makes sense to think about some smarter pause
                    Thread.Sleep(1);
                }
            }
        }

        private void FillInboundQueue()
        {
            // TODO : handle OutOfMemoryException and exponential retry?

            long portionIndex = 0;
            while (true)
            {
                using (var portionStream = _reader.ReadNextPortion())
                {
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
            }
        }

        private void WaitInboundQueueIsEmpty()
        {
            // super simple way to wait for the queue is empty

            var startedAt = DateTime.UtcNow;
            while (_inboundQueue.Count > 0)
            {
                Thread.Sleep(100);
                if (DateTime.UtcNow.Subtract(startedAt) > TimeSpan.FromMinutes(WorkTimeoutMinutes))
                    throw new TimeoutException("hm operation takes too long today");
            }
        }

        private void SignalNoMoreInboundData()
        {
            _noMoreInboundData = true;
        }

        private void WaitWorkIsFinished()
        {
            var timeout = TimeSpan.FromMinutes(WorkTimeoutMinutes);
            // can use just Join() in loop here but I'd like to summarize timeout for all threads.
            var finished = WaitHandle.WaitAll(_workFinishedEvents.ToArray(), timeout);
            if (!finished)
                throw new TimeoutException("hm operation takes too long today");
            
            finished = _outboundQueueHandler.Join(timeout);
            if (!finished)
                throw new TimeoutException("hm operation takes too long today");
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
    }
}
