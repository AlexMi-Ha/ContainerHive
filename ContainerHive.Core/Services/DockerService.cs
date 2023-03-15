using ContainerHive.Core.Common.Helper.Result;
using ContainerHive.Core.Common.Interfaces;
using ContainerHive.Core.Models;
using ContainerHive.Core.Models.Docker;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json.Serialization;
using System.Buffers;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ContainerHive.Core.Services {
    internal class DockerService : IDockerService {

        private readonly IDockerClient _dockerClient;
        private readonly string _imagesPath;

        private const string _logsExtension = ".logs";
        private const string _metaExtension = ".meta";

        private readonly string _repoPath;

        public DockerService(IDockerClient dockerClient, IConfiguration config) {
            _dockerClient = dockerClient;
            _imagesPath = config["ImagePath"]!;
            _repoPath = config["RepoPath"]!;
        }

        public async Task<Result<ImageBuild>> BuildImageAsync(Deployment deployment, CancellationToken cancelToken) {
            var config = new ImageBuildParameters {
                Dockerfile = Path.Combine(_repoPath, deployment.ProjectId ,deployment.DockerPath),
                Tags = new List<string> { $"project={deployment.ProjectId}", $"deployment={deployment.DeploymentId}" },
                Labels = new Dictionary<string, string> { { "project", deployment.ProjectId }, { "deployment", deployment.DeploymentId } }  
            };

            using(var logStream = new StreamWriter(File.Open(Path.Combine(_imagesPath, deployment.DeploymentId, _logsExtension), FileMode.Create, FileAccess.Write)))
            using(var dockerFileStream = File.OpenRead(Path.Combine(_repoPath, deployment.ProjectId, deployment.DockerPath))) {
                try {
                    await _dockerClient.Images.BuildImageFromDockerfileAsync(
                        config, dockerFileStream, null, null,
                        new Progress<JSONMessage>(msg => {
                            logStream.WriteLine($"[{DateTime.Now}] {msg.Stream}");
                        }), cancelToken);
                }catch(DockerApiException ex) {
                    logStream.WriteLine($"[{DateTime.Now}] Exited with Code {ex.StatusCode}\nError Message: {ex.Message}");
                    return ex;
                }
            }

        }

        public async Task<IEnumerable<ContainerListResponse>> GetAllContainersForProjectAsync(string projId, CancellationToken cancelToken) {
            var config = new ContainersListParameters {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>> {
                    {"label", new Dictionary<string ,bool> { { $"project={projId}", true } } }
                }
            };
            return await _dockerClient.Containers.ListContainersAsync(config, cancelToken);
        }

        public async Task<IEnumerable<ImageBuild>> GetAllImagesForProjectAsync(string projId, CancellationToken cancelToken) {
            throw new NotImplementedException(); //Custom implementation based on filesystem metafiles -> Failed images are not stored in docker
        }

        public async Task<List<ContainerLogEntry>> GetContainerLogsAsync(string containerId, CancellationToken cancelToken) {
            List<ContainerLogEntry> logs = new();
            
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

        public Task<string> GetImageLogsAsync(string imageId, CancellationToken cancelToken) {
            throw new NotImplementedException();
        }

        public async Task<bool> StopRunningContainersByProject(string projId, CancellationToken cancelToken) {
            var containers = await GetAllContainersForProjectAsync(projId, cancelToken);
            var config = new ContainerStopParameters {
                WaitBeforeKillSeconds = 5
            };
            bool succ = true;
            foreach(var container in containers) {
                succ = succ && await _dockerClient.Containers.StopContainerAsync(container.ID, config, cancelToken);
            }
            return succ;
        }

        public async Task<bool> PruneImagesAsync(string projId, CancellationToken cancelToken) {
            var config = new ImagesPruneParameters {
                Filters = new Dictionary<string, IDictionary<string, bool>> {
                    { "label", new Dictionary<string, bool> { { $"project={projId}", true } } },
                    { "dangling", new Dictionary<string, bool> { { "true", true} } }
                }
            };
            var res = await _dockerClient.Images.PruneImagesAsync(config, cancelToken);
            return res.ImagesDeleted.Count > 0;
        }

        public async Task<bool> PruneProcessesAsync(string projId, CancellationToken cancelToken) {
            var config = new ContainersPruneParameters {
                Filters = new Dictionary<string, IDictionary<string, bool>> {
                    {"label", new Dictionary<string ,bool> { { $"project={projId}", true } } }
                }
            };
            var res = await _dockerClient.Containers.PruneContainersAsync(config, cancelToken);
            return res.ContainersDeleted.Count > 0;
        }

        public Task<Result<Container>> RunImageAsync(ImageBuild image, Deployment deployment, CancellationToken cancelToken) {
            throw new NotImplementedException();
        }
    }
}
