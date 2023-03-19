﻿using ContainerHive.Core.Common.Exceptions;
using ContainerHive.Core.Common.Helper.Result;
using ContainerHive.Core.Common.Interfaces;
using ContainerHive.Core.Datastore;
using ContainerHive.Core.Models;
using ContainerHive.Core.Models.Docker;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ContainerHive.Core.Services {
    internal class ProjectService : IProjectService {

        private readonly ApplicationDbContext _dbContext;
        private readonly IDeploymentService _deploymentService;
        private readonly IDockerService _dockerService;
        private readonly IGitService _gitService;

        private readonly IValidator<Project> _projectValidator;

        public ProjectService(ApplicationDbContext dbContext, IDeploymentService deploymentService, IDockerService dockerService, IGitService gitService, IValidator<Project> projectValidator) {
            _dbContext = dbContext ?? 
                throw new ArgumentNullException(nameof(dbContext), "Injected Service can't be null");
            _deploymentService = deploymentService ?? 
                throw new ArgumentNullException(nameof(deploymentService),"Injected Service can't be null");
            _dockerService = dockerService ?? 
                throw new ArgumentNullException(nameof(dockerService), "Injected Service can't be null");
            _gitService = gitService ?? 
                throw new ArgumentNullException(nameof(gitService),"Injected Service can't be null");
            _projectValidator = projectValidator ?? 
                throw new ArgumentNullException(nameof(projectValidator),"Injected Validator can't be null");
        }

        public async Task<Result<string>> AddProjectAsync(Project project) {
            var validationRes = _projectValidator.Validate(project);
            if (!validationRes.IsValid)
                return new ValidationException(validationRes.Errors);

            await _dbContext.Projects.AddAsync(project);
            if (await _dbContext.SaveChangesAsync() <= 0)
                return new ApplicationException("Failed to add Project!");
            return project.ProjectId;
        }

        public async Task<Result<string>> UpdateProjectAsync(Project project) {
            var validationRes = _projectValidator.Validate(project);
            if (!validationRes.IsValid)
                return new ValidationException(validationRes.Errors);

            _dbContext.Projects.Update(project);
            if (await _dbContext.SaveChangesAsync() <= 0)
                return new ApplicationException("Failed to update Project!");
            return project.ProjectId;

        }

        public async Task<Result<bool>> DeleteProjectAsync(string id, CancellationToken cancelToken) {
            var proj = await _dbContext.Projects.FindAsync(id);
            if (proj == null)
                return new RecordNotFoundException($"Project with id {id} not found!");

            var pruneRes = await _dockerService.StopRunningContainersByProjectAsync(id, cancelToken);
            if (!pruneRes)
                return new ProcessFailedException("Failed stopping the containers");

            var dirDeleteRes = await _gitService.DeleteProjectRepoAsync(proj, cancelToken);
            if (dirDeleteRes.IsFaulted)
                return new ProcessFailedException("Failed to delete project Repo",dirDeleteRes);

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

        public async Task<Result<bool>> IsOnCustomNetworkAsync(string id) {
            var proj = await GetProjectAsync(id);
            return proj.Map(e => e.CustomNetwork);
        }

        public async Task<Result<bool>> IsWebhookActiveAsync(string id) {
            var proj = await GetProjectAsync(id);
            return proj.Map(e => e.WebhookActive);
        }

        public async Task<Result<string>> RegenerateTokenAsync(string id) {
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

        public async Task<Result<bool>> SetOnCustomNetworkAsync(string id, bool enabled) {
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

        public async Task<Result<string>> SetRepoUrlAsync(string id, string repoUrl) {
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

        public async Task<Result<bool>> SetWebhookActiveAsync(string id, bool active) {
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


        public async Task<Result<bool>> DeployAllAsync(string id, CancellationToken cancelToken) {
            var killResult = await KillAllContainersAsync(id, cancelToken);
            if(killResult.IsFaulted || !killResult.Value) {
                return new DeploymentFailedException("Failed stopping and killing old Deployments", killResult);
            }

            var projectResult = await GetProjectAsync(id);
            if (projectResult.IsFaulted || projectResult.Value == null)
                return new RecordNotFoundException("Did not find the project with the id " + id, projectResult);

            var gitResult = await _gitService.CloneOrPullProjectRepositoryAsync(projectResult.Value, cancelToken);
            if (cancelToken.IsCancellationRequested)
                return new OperationCanceledException(cancelToken);
            if (gitResult.IsFaulted)
                return new DeploymentFailedException("Unable to pull from Git Repo " + projectResult.Value.Repo.Url, gitResult);

            var deployments = await _deploymentService.GetDeploymentsByProjectIdAsync(id);
            if (deployments.Count() == 0)
                return new RecordNotFoundException("Could not find deployments in the project with id " + id);

            List<Task<Result<ImageBuild>>> buildTasks = new();
            foreach (var deployment in deployments) {
                buildTasks.Add(_dockerService.BuildImageAsync(deployment, cancelToken));
                if (cancelToken.IsCancellationRequested)
                    return new OperationCanceledException();

            }
            var buildResults = await Task.WhenAll(buildTasks);

            foreach(var result in buildResults) {
                if(result.IsFaulted || result.Value?.BuidStatus != Status.DONE || result.Value?.ImageId == null || result.Value?.Deployment == null) {
                    return new DeploymentFailedException("Some Builds failed!", result);
                }
            }
            // All builds where successful
            List<Task<Result<string>>> runTasks = new();
            foreach(var image in buildResults.Select(e => e.Value)) {
                runTasks.Add(_dockerService.RunImageAsync(image, image.Deployment!, cancelToken));
                if (cancelToken.IsCancellationRequested)
                    return new OperationCanceledException();
            }
            var runResults = await Task.WhenAll(runTasks);
            foreach(var result in runResults) {
                if(result.IsFaulted) {
                    return new DeploymentFailedException("Failed Starting some containers!", result);
                }
            }
            return true;
        }
        public async Task<Result<bool>> KillAllContainersAsync(string id, CancellationToken cancelToken) {
            var res = await _dockerService.StopRunningContainersByProjectAsync(id, cancelToken);
            if(cancelToken.IsCancellationRequested)
                return new OperationCanceledException();
            if (!res) 
                return new ProcessFailedException($"Failed to stop containers with projectID {id}!");

            res = await _dockerService.PruneProcessesAsync(id, cancelToken);
            if (cancelToken.IsCancellationRequested)
                return new OperationCanceledException();
            if (!res)
                return new ProcessFailedException($"Failed to prune containers with projectID {id}!");

            res = await _dockerService.PruneImagesAsync(id, cancelToken);
            if (cancelToken.IsCancellationRequested)
                return new OperationCanceledException();
            if(!res)
                return new ProcessFailedException($"Failed to prune images with projectID {id}!");

            return true;
        }
    }
}