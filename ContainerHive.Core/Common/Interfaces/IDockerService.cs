
using ContainerHive.Core.Common.Helper.Result;
using ContainerHive.Core.Models;
using ContainerHive.Core.Models.Docker;

namespace ContainerHive.Core.Common.Interfaces {
    public interface IDockerService {

        public Task<IEnumerable<Container>> GetAllContainersForProjectAsync(string projId);
        public Task<IEnumerable<ImageBuild>> GetAllImagesForProjectAsync(string projId);

        public Task<List<ContainerLogEntry>> GetContainerLogsAsync(string containerId);
        public Task<string> GetImageLogsAsync(string imageId);

        public Task<bool> PruneProcessesAsync(string projectId);
        public Task<bool> PruneImagesAsync(string projectId);


        public Task<Result<ImageBuild>> BuildImageAsync(Deployment deployment);
        public Task<Result<Container>> RunImageAsync(ImageBuild image, Deployment deployment);

        public Task<bool> KillRunningContainersByProject(string projId);


    }
}
