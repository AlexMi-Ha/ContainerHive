using ContainerHive.Core.Common.Helper.Result;
using ContainerHive.Core.Models;

namespace ContainerHive.Core.Common.Interfaces {
    public interface IProjectService {

        // CRUD
        public Task<IEnumerable<Project>> GetProjectsAsync();
        public Task<Result<Project>> GetProjectAsync(string projectId);
        public Task<Result<string>> AddProjectAsync(Project project);
        public Task<Result<bool>> DeleteProjectAsync(string id);

        // Webhook
        public Task<Result<bool>> IsWebhookActive(string id);
        public Task<Result<bool>> SetWebhookActive(string id, bool active);
        public Task<Result<string>> RegenerateToken(string id);

        // Network
        public Task<Result<bool>> IsOnCustomNetwork(string id);
        public Task<Result<bool>> SetOnCustomNetwork(string id, bool enabled);

        // Repo
        public Task<Result<string>> SetRepoUrl(string id, string repoUrl);
    }
}
