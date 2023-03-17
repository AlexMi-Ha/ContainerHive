
using ContainerHive.Core.Common.Helper.Result;
using ContainerHive.Core.Models;

namespace ContainerHive.Core.Common.Interfaces {
    public interface IDeploymentService {

        // CRUD
        public Task<Result<Deployment>> GetDeploymentByIdAsync(string deploymentId);
        public Task<IEnumerable<Deployment>> GetDeploymentsAsync();
        public Task<IEnumerable<Deployment>> GetDeploymentsByProjectIdAsync(string projId);
        public Task<string> AddDeploymentAsync(Deployment deployment);
        public Task<string> UpdateDeploymentAsync(Deployment deployment);
        public Task<bool> DeleteDeploymentAsync(string id);

        // Util
        public Task<bool> PortAvailableOnHostAsync(ushort port);
    }
}
