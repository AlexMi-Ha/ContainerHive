using ContainerHive.Core.Common.Helper.Result;
using ContainerHive.Core.Common.Interfaces;
using ContainerHive.Core.Models;
using ContainerHive.Core.Models.Docker;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Logging.Abstractions;
using System.Buffers;
using System.Text;

namespace ContainerHive.Core.Services {
    internal class DockerService : IDockerService {

        private readonly IDockerClient _dockerClient;

        public DockerService(IDockerClient dockerClient) {
            _dockerClient = dockerClient;
        }

        public Task<Result<ImageBuild>> BuildImageAsync(Deployment deployment) {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<Container>> GetAllContainersForProjectAsync(string projId) {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<ImageBuild>> GetAllImagesForProjectAsync(string projId) {
            throw new NotImplementedException();
        }

        public async Task<List<ContainerLogEntry>> GetContainerLogsAsync(string containerId) {
            List<ContainerLogEntry> logs = new();
            CancellationToken cancelToken = new();
            
            var config = new ContainerLogsParameters {
                Timestamps = true,
                Tail = "500",
                ShowStderr = true,
                ShowStdout = true
            };

            var muxStream = await _dockerClient.Containers.GetContainerLogsAsync(containerId, false, config);
            var buffer = ArrayPool<byte>.Shared.Rent(81920);
            try {
                using (MemoryStream m = new())
                using (StreamReader reader = new(m)) {
                    while (!cancelToken.IsCancellationRequested) {
                        var result = await muxStream.ReadOutputAsync(buffer, 0, buffer.Length, cancelToken);
                        if (result.EOF)
                            break;

                        logs.Add(new ContainerLogEntry {
                            Log = Encoding.Default.GetString(buffer),
                            Level = result.Target == MultiplexedStream.TargetStream.StandardError ? LogLevel.ERROR : LogLevel.STD
                        });

                    }
                }
            }finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return logs;
        }

        public Task<string> GetImageLogsAsync(string imageId) {
            throw new NotImplementedException();
        }

        public Task<bool> KillRunningContainersByProject(string projId) {
            throw new NotImplementedException();
        }

        public Task<bool> PruneImagesAsync(string projectId) {
            throw new NotImplementedException();
        }

        public Task<bool> PruneProcessesAsync(string projectId) {
            throw new NotImplementedException();
        }

        public Task<Result<Container>> RunImageAsync(ImageBuild image, Deployment deployment) {
            throw new NotImplementedException();
        }

        private static Stream MuxStreamToSingleStream(MultiplexedStream s) {
            
        }
    }
}
