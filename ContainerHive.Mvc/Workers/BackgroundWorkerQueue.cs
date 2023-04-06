using ContainerHive.Core.Common.Interfaces;
using System.Collections.Concurrent;

namespace ContainerHive.Mvc.Workers {
    public class BackgroundWorkerQueue {

        private ConcurrentQueue<Func<IProjectService, CancellationToken, Task>> _workItems = new();
        private SemaphoreSlim _signal = new SemaphoreSlim(0);

        public async Task<Func<IProjectService, CancellationToken, Task>> DequeueAsync(CancellationToken cancelToken) {
            await _signal.WaitAsync(cancelToken);
            _workItems.TryDequeue(out var workItem);

            return workItem ?? ((prjService,token) => Task.CompletedTask);
        }

        public void QueueBackgroundItem(Func<IProjectService, CancellationToken, Task> item) {
            ArgumentNullException.ThrowIfNull(item, nameof(item));

            _workItems.Enqueue(item);
            _signal.Release();
        }
    }
}
