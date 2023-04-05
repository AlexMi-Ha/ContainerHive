namespace ContainerHive.Mvc.Workers {
    public class LongRunningServiceWorker : BackgroundService {

        private readonly BackgroundWorkerQueue queue;

        public LongRunningServiceWorker(BackgroundWorkerQueue queue) {
            this.queue = queue;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while(!stoppingToken.IsCancellationRequested) {
                var workItem = await queue.DequeueAsync(stoppingToken);

                await workItem(stoppingToken);
            }
        }
    }
}
