using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace GzipArchiver
{
    public partial class CompressionPipeline : IDisposable
    {
        public string SourcePath { get; }
        public string DestinationPath { get; }
        public int WorkersNumber { get; } = Environment.ProcessorCount * 2;
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

        /// Returns a list of errors happened during work process.
        public IEnumerable<Exception> GetWorkErrors()
        {
            return _internalErrorsReport;
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

        private long FinishPortionHandling(PortionTicket ticket)
        {
            var lastHandledIndex = ticket.Index;
            _writer.WritePortion(ticket.Data);
            ticket.Dispose();

            return lastHandledIndex;
        }

        private long TryHandleTicketsFromCache(long prevPortionIndex, SortedList<long, PortionTicket> ticketsCache)
        {
            while (ticketsCache.Any())
            {
                var nextCachedItem = ticketsCache.First();
                var nextCachedTicket = nextCachedItem.Value;

                var strCache = String.Join(",", ticketsCache.Select(x => x.Key));
                _logger.Log($"OH - current cache with '{ticketsCache.Count}' elements: {strCache}");

                if (nextCachedTicket.Index == prevPortionIndex + 1)
                {
                    ticketsCache.Remove(nextCachedItem.Key);
                    prevPortionIndex = FinishPortionHandling(nextCachedTicket);
                    _logger.Log($"OH has handled cached ticket '{nextCachedTicket.Index}', prev handled index is '{prevPortionIndex}'.");
                }
                else break;
            }

            return prevPortionIndex;
        }

        private void OutboundQueueHandlerFunc()
        {
            // TODO : try-catch - report errors and terminate pipeline if needed

            _logger.Log($"OH handler thread '{Thread.CurrentThread.ManagedThreadId}' is started.");

            var ticketsCache = new SortedList<long, PortionTicket>();
            long prevPortionIndex = -1;

            while (true)
            {
                _logger.Log($"OH - trying handle items from cache, prev handled index is '{prevPortionIndex}'...");
                prevPortionIndex = TryHandleTicketsFromCache(prevPortionIndex, ticketsCache);

                var haveTicket = _outboundQueue.TryDequeue(out var ticket);
                if (haveTicket)
                {
                    _logger.Log($"OH got a next portion with index '{ticket.Index}', prev handled index is '{prevPortionIndex}'.");
                    
                    if (ticket.Index == prevPortionIndex + 1)
                    {
                        prevPortionIndex = FinishPortionHandling(ticket);
                        _logger.Log($"OH has handled a ticket '{ticket.Index}', prev handled index is '{prevPortionIndex}'.");
                    }
                    else
                    {
                        ticketsCache.Add(ticket.Index, ticket);
                    }
                }
                else
                {
                    // TODO : makes sense to think about some smarter pause
                    var finished = WaitWorkersAreFinished(TimeSpan.FromMilliseconds(1));
                    if (_noMoreInboundData && finished && !_outboundQueue.Any() && !ticketsCache.Any())
                    {
                        _logger.Log($"OH - Going to stop handler...");
                        break;
                    }
                }
            }

            _logger.Log("OH thread is finished.");
        }

        private void FillInboundQueue()
        {
            try
            {
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
            catch (Exception ex)
            {
                // All this error-handling-stuff should be a separate class.
                //  I just added it here as an idea of error handling.

                ReportError(ex);

                if (IsCriticalError(ex))
                    TerminateWorkProcess();
            }
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

        private bool IsCriticalError(Exception ex)
        {
            return false;
        }

        /// Stops whole internal work on a critical error for example.
        private void TerminateWorkProcess()
        {
            throw new NotImplementedException();
        }

        private void ReportError(Exception ex)
        {
            _logger.Log($"Reporting an error {ex.Message}");
            _internalErrorsReport.Add(ex);
        }

        ISourceReader _reader;
        IWorker _worker;
        IResultWriter _writer;

        private bool _noMoreInboundData = false;
        private ConcurrentQueue<PortionTicket> _inboundQueue = new ConcurrentQueue<PortionTicket>();
        private ConcurrentQueue<PortionTicket> _outboundQueue = new ConcurrentQueue<PortionTicket>();

        private List<Thread> _workers;
        private List<ManualResetEvent> _workFinishedEvents;
        private Thread _outboundQueueHandler;
        private ConcurrentBag<Exception> _internalErrorsReport = new ConcurrentBag<Exception>();

        private ILogger _logger;
    }
}
