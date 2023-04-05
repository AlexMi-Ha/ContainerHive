using System.Collections.Concurrent;

namespace ContainerHive.Mvc.Workers {
    public class BackgroundWorkerQueue {

        private ConcurrentQueue<Func<CancellationToken, Task>> _workItems = new();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);

        public async Task<Func<CancellationToken, Task>> DequeueAsync(CancellationToken cancelToken) {
            await _signal.WaitAsync(cancelToken);
            _workItems.TryDequeue(out var workItem);

            return workItem ?? (token => Task.CompletedTask);
        }

        public void QueueBackgroundItem(Func<CancellationToken, Task> item) {
            ArgumentNullException.ThrowIfNull(item, nameof(item));

            _workItems.Enqueue(item);
            _signal.Release();
        }
    }
}
