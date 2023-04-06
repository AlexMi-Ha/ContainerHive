using ContainerHive.Core.Common.Interfaces;

namespace ContainerHive.Mvc.Workers {
    public class LongRunningServiceWorker : BackgroundService {

        private readonly BackgroundWorkerQueue queue;
        private readonly IServiceScopeFactory serviceScopeFactory;

        public LongRunningServiceWorker(BackgroundWorkerQueue queue, IServiceScopeFactory serviceScopeFactory) {
            this.queue = queue;
            this.serviceScopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while(!stoppingToken.IsCancellationRequested) {
                var workItem = await queue.DequeueAsync(stoppingToken);

                using var scope = serviceScopeFactory.CreateScope();
                var projectService = scope.ServiceProvider.GetRequiredService<IProjectService>();
                await workItem(projectService, stoppingToken);
            }
        }
    }
}
