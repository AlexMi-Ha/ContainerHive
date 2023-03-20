using ContainerHive.Core.Common.Exceptions;
using ContainerHive.Core.Common.Helper.Result;
using ContainerHive.Core.Common.Interfaces;
using ContainerHive.Core.Datastore;
using ContainerHive.Core.Models;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ContainerHive.Core.Services {
    internal class DeploymentService : IDeploymentService {

        private readonly ApplicationDbContext _dbContext;
        private readonly IDockerService _dockerService;

        private IValidator<Deployment> _deploymentValidator;

        public DeploymentService(ApplicationDbContext dbContext, IValidator<Deployment> deploymentValidator, IDockerService dockerService) {
            _dbContext = dbContext!;
            _deploymentValidator = deploymentValidator!;
            _dockerService = dockerService!;
        }

        public async Task<Result<string>> AddDeploymentAsync(Deployment deployment) {
            var validationRes = _deploymentValidator.Validate(deployment);
            if (!validationRes.IsValid)
                return new ValidationException(validationRes.Errors);
            var proj = await _dbContext.Projects.Where(e => e.ProjectId.Equals(deployment.ProjectId)).FirstOrDefaultAsync();
            if(proj == null)
                return new ValidationException($"Project with id {deployment.ProjectId} does not exist");

            await _dbContext.Deployments.AddAsync(deployment);
            if(await _dbContext.SaveChangesAsync() <= 0)
                return new ApplicationException("Failed to add Deployment!");
            return deployment.DeploymentId;
        }

        public async Task<Result<string>> UpdateDeploymentAsync(Deployment deployment) {
            var validationRes = _deploymentValidator.Validate(deployment);
            if (!validationRes.IsValid)
                return new ValidationException(validationRes.Errors);
            var proj = await _dbContext.Projects.Where(e => e.ProjectId.Equals(deployment.ProjectId)).FirstOrDefaultAsync();
            if (proj == null)
                return new ValidationException($"Project with id {deployment.ProjectId} does not exist");

            _dbContext.Deployments.Update(deployment);
            if (await _dbContext.SaveChangesAsync() <= 0)
                return new ApplicationException("Failed to update Deployment!");
            return deployment.DeploymentId;
        }

        public async Task<Result<bool>> DeleteDeploymentAsync(string id, CancellationToken cancelToken) {
            var deployment = await _dbContext.Deployments.FindAsync(id);
            if(deployment == null) 
                return new RecordNotFoundException($"Could not find Deployment with id {id}");

            var stopResult = await _dockerService.StopRunningContainersByDeploymentIdAsync(id, cancelToken);
            if(!stopResult)
                return new ProcessFailedException($"Failed trying to Stop Running Containers");

            await _dbContext.ImageBuilds.Where(e => e.DeploymentId == deployment.DeploymentId).ExecuteDeleteAsync();
            _dbContext.Deployments.Remove(deployment);
            return await _dbContext.SaveChangesAsync() > 0;
        }

        public async Task<Result<Deployment>> GetDeploymentByIdAsync(string deploymentId) {
            var res = await _dbContext.Deployments.Include(e => e.Project).Where(e => e.DeploymentId.Equals(deploymentId)).SingleOrDefaultAsync();
            return res != null ? res : new RecordNotFoundException($"Could not find Deployment with id {deploymentId}");
        }

        public async Task<IEnumerable<Deployment>> GetDeploymentsAsync() {
            return await _dbContext.Deployments.ToListAsync();
        }

        public async Task<IEnumerable<Deployment>> GetDeploymentsByProjectIdAsync(string projId) {
            return await _dbContext.Deployments.Where(e => e.ProjectId.Equals(projId)).ToListAsync();
        }

    }
}
