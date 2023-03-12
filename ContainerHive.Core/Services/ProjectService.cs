using ContainerHive.Core.Common.Exceptions;
using ContainerHive.Core.Common.Helper.Result;
using ContainerHive.Core.Common.Interfaces;
using ContainerHive.Core.Datastore;
using ContainerHive.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;

namespace ContainerHive.Core.Services {
    internal class ProjectService : IProjectService {

        private readonly ApplicationDbContext _dbContext;

        public ProjectService(ApplicationDbContext dbContext) {
            _dbContext = dbContext;
        }

        public async Task<Result<string>> AddProjectAsync(Project project) {
            await _dbContext.Projects.AddAsync(project);
            if (await _dbContext.SaveChangesAsync() <= 0)
                return new ApplicationException("Failed to add Project!");
            return project.ProjectId;
            
        }

        public async Task<Result<bool>> DeleteProjectAsync(string id) {
            var proj = await _dbContext.Projects.FindAsync(id);
            if (proj == null)
                return new RecordNotFoundException($"Project with id {id} not found!");
            _dbContext.Projects.Remove(proj);
            if (await _dbContext.SaveChangesAsync() <= 0)
                return new ApplicationException("Failed to delete Project!");

            return true;
        }

        public async Task<Result<Project>> GetProjectAsync(string projectId) {
            var proj = await _dbContext.Projects.FindAsync(projectId);
            if (proj == null)
                return new RecordNotFoundException("Requested Ressource was not found!");
            return proj;
        }

        public async Task<IEnumerable<Project>> GetProjectsAsync() {
            return await _dbContext.Projects.ToListAsync();
        }

        public async Task<Result<bool>> IsOnCustomNetwork(string id) {
            var proj = await GetProjectAsync(id);
            return proj.Map(e => e.CustomNetwork);
        }

        public async Task<Result<bool>> IsWebhookActive(string id) {
            var proj = await GetProjectAsync(id);
            return proj.Map(e => e.WebhookActive);
        }

        public async Task<Result<string>> RegenerateToken(string id) {
            var proj = await GetProjectAsync(id);
            if(proj.IsSuccess && !proj.Value.WebhookActive) {
                return new ArgumentException($"The specified Project with id {id} has Webhooks disabled. Enable it to regenerate your Token!");            
            }
            if(proj.IsSuccess) {
                proj.Value.RegenerateToken();
                var updated = await _dbContext.SaveChangesAsync();
                if(updated <= 0) {
                    return new ApplicationException($"Error when updating Token for Project with id {id}");
                }
            }
            return proj.Map(e => e.ApiToken);
        }

        public async Task<Result<bool>> SetOnCustomNetwork(string id, bool enabled) {
            var proj = await GetProjectAsync(id);
            if (proj.IsSuccess) {
                proj.Value.CustomNetwork = enabled;
                var updated = await _dbContext.SaveChangesAsync();
                if (updated <= 0) {
                    return new ApplicationException($"Error when updating the custom Network state for Project with id {id}");
                }
            }
            return proj.Map(e => e.CustomNetwork);
        }

        public async Task<Result<string>> SetRepoUrl(string id, string repoUrl) {
            var proj = await _dbContext.Projects
                .Include(e => e.Repo)
                .Where(e => e.ProjectId.Equals(id))
                .FirstOrDefaultAsync();

            if(proj == null) {
                return new RecordNotFoundException("Requested Ressource was not found!");
            }
            if (proj.Repo == null) {
                proj.Repo = new Repo { Url = repoUrl };
                proj.RepoId = proj.Repo.RepoId;
            }else {
                proj.Repo.Url = repoUrl;
            }
            var updated = await _dbContext.SaveChangesAsync();
            if (updated <= 0) {
                return new ApplicationException($"Error when updating Repo of Project with id {id}");
            }
            return proj.Repo.Url;
        }

        public async Task<Result<bool>> SetWebhookActive(string id, bool active) {
            var proj = await GetProjectAsync(id);
            if (proj.IsSuccess) {
                proj.Value.WebhookActive = active;
                var updated = await _dbContext.SaveChangesAsync();
                if (updated <= 0) {
                    return new ApplicationException($"Error when updating Webhook state for Project with id {id}");
                }
            }
            return proj.Map(e => e.WebhookActive);
        }
    }
}
