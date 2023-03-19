using ContainerHive.Core.Common.Helper.Result;
using ContainerHive.Core.Models;

namespace ContainerHive.Core.Common.Interfaces {
    public interface IProjectService {

        // CRUD
        public Task<IEnumerable<Project>> GetProjectsAsync();
        public Task<Result<Project>> GetProjectAsync(string projectId);
        public Task<Result<string>> AddProjectAsync(Project project);
        public Task<Result<string>> UpdateProjectAsync(Project project);
        public Task<Result<bool>> DeleteProjectAsync(string id, CancellationToken cancelToken);

        // Webhook
        public Task<Result<bool>> IsWebhookActiveAsync(string id);
        public Task<Result<bool>> SetWebhookActiveAsync(string id, bool active);
        public Task<Result<string>> RegenerateTokenAsync(string id);

        // Network
        public Task<Result<bool>> IsOnCustomNetworkAsync(string id);
        public Task<Result<bool>> SetOnCustomNetworkAsync(string id, bool enabled);

        // Repo
        public Task<Result<string>> SetRepoUrlAsync(string id, string repoUrl);

        // Deployment
        public Task<Result<bool>> DeployAllAsync(string id, CancellationToken cancelToken);
        public Task<Result<bool>> KillAllContainersAsync(string id, CancellationToken cancelToken);
    }
}
