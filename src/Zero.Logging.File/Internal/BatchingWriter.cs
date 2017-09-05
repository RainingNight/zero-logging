using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zero.Logging.File.Internal
{
    public class BatchingWriter : IMessageWriter, IDisposable
    {
        private TimeSpan _interval;
        private int? _queueSize;
        private int? _batchSize;

        private BlockingCollection<string> _messageQueue;
        private Task _outputTask;
        private CancellationTokenSource _cancellationTokenSource;

        private readonly IMessageWriter _writer;

        public BatchingWriter(IMessageWriter writer, TimeSpan interval, int? batchSize, int? queueSize)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _interval = interval;
            _batchSize = batchSize;
            _queueSize = queueSize;
            Start();
        }

        private void Start()
        {
            _messageQueue = _queueSize == null ?
                new BlockingCollection<string>(new ConcurrentQueue<string>()) :
                new BlockingCollection<string>(new ConcurrentQueue<string>(), _queueSize.Value);

            _cancellationTokenSource = new CancellationTokenSource();
            _outputTask = Task.Factory.StartNew<Task>(
                ProcessLogQueue,
                null,
                TaskCreationOptions.LongRunning);
        }

        private async Task ProcessLogQueue(object state)
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                var limit = _batchSize ?? int.MaxValue;
                StringBuilder currentBatch = new StringBuilder();
                while (limit > 0 && _messageQueue.TryTake(out var message))
                {
                    currentBatch.Append(message);
                    limit--;
                }
                if (currentBatch.Length > 0)
                {
                    try
                    {
                        await _writer.WriteMessagesAsync(currentBatch.ToString(), _cancellationTokenSource.Token);
                    }
                    catch
                    {
                        // ignored
                    }
                }
                await IntervalAsync(_interval, _cancellationTokenSource.Token);
            }
        }

        protected virtual Task IntervalAsync(TimeSpan interval, CancellationToken cancellationToken)
        {
            return Task.Delay(interval, cancellationToken);
        }

        private void Stop()
        {
            _cancellationTokenSource.Cancel();
            _messageQueue.CompleteAdding();
            try
            {
                _outputTask.Wait(_interval);
            }
            catch (TaskCanceledException)
            {
            }
            catch (AggregateException ex) when (ex.InnerExceptions.Count == 1 && ex.InnerExceptions[0] is TaskCanceledException)
            {
            }
        }

        public Task WriteMessagesAsync(string message, CancellationToken cancellationToken)
        {
            if (!_messageQueue.IsAddingCompleted)
            {
                try
                {
                    _messageQueue.Add(message, _cancellationTokenSource.Token);
                }
                catch
                {
                    //cancellation token canceled or CompleteAdding called
                }
            }
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
