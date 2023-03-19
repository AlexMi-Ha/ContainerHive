﻿
using ContainerHive.Core.Common.Helper.Result;
using ContainerHive.Core.Models;
using ContainerHive.Core.Models.Docker;
using Docker.DotNet.Models;

namespace ContainerHive.Core.Common.Interfaces {
    public interface IDockerService {

        public Task<Result<IEnumerable<ContainerListResponse>>> GetAllContainersForProjectAsync(string projId, CancellationToken cancelToken);
        public Task<IEnumerable<ImageBuild>> GetAllImagesForProjectAsync(string projId);

        public Task<List<ContainerLogEntry>> GetContainerLogsAsync(string containerId, CancellationToken cancelToken);
        public Task<string?> GetImageLogsAsync(string imageId);

        public Task<bool> PruneProcessesAsync(string projectId, CancellationToken cancelToken);
        public Task<bool> PruneImagesAsync(string projectId, CancellationToken cancelToken);


        public Task<Result<ImageBuild>> BuildImageAsync(Deployment deployment, CancellationToken cancelToken);
        public Task<Result<string>> RunImageAsync(ImageBuild image, Deployment deployment, CancellationToken cancelToken);

        public Task<bool> StopRunningContainersByProjectAsync(string projId, CancellationToken cancelToken);


    }
}