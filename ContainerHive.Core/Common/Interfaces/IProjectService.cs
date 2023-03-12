using ContainerHive.Core.Models;

namespace ContainerHive.Core.Common.Interfaces {
    public interface IProjectService {

        // CRUD
        public Task<IEnumerable<Project>> GetProjectsAsync();
        public Task<Project> GetProjectAsync(string projectId);
        public Task<string> AddProjectAsync(Project project);
        public Task<bool> DeleteProjectAsync(string id);

        // Webhook
        public Task<bool> IsWebhookActive(string id);
        public Task<bool> SetWebhookActive(string id, bool active);
        public Task<string> RegenerateToken(string id);

        // Network
        public Task<bool> IsOnCustomNetwork(string id);
        public Task<bool> SetOnCustomNetwork(string id, bool enabled);

        // Repo
        public Task<string> SetRepoUrl(string id, string repoUrl);
    }
}
