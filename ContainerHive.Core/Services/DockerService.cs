﻿using ContainerHive.Core.Common.Helper.Result;
using ContainerHive.Core.Common.Interfaces;
using ContainerHive.Core.Datastore;
using ContainerHive.Core.Models;
using ContainerHive.Core.Models.Docker;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Buffers;
using System.Text;

namespace ContainerHive.Core.Services {
    internal class DockerService : IDockerService {

        private readonly IDockerClient _dockerClient;

        private readonly string _repoPath;

        private readonly ApplicationDbContext _dbContext;

        public DockerService(IDockerClient dockerClient, IConfiguration config, ApplicationDbContext dbContext) {
            _dockerClient = dockerClient!;
            _repoPath = config["RepoPath"]!;
            _dbContext = dbContext!;
        }

        public async Task<Result<ImageBuild>> BuildImageAsync(Deployment deployment, CancellationToken cancelToken) {
            var config = new ImageBuildParameters {
                Dockerfile = Path.Combine(_repoPath, deployment.ProjectId ,deployment.DockerPath),
                Tags = new List<string> { $"project={deployment.ProjectId}", $"deployment={deployment.DeploymentId}" },
                Labels = new Dictionary<string, string> { { "project", deployment.ProjectId }, { "deployment", deployment.DeploymentId } }  
            };
            var image = new ImageBuild {
                Deployment = deployment,
                DeploymentId = deployment.DeploymentId,
                BuidStatus = Status.BUILDING,
                Created = DateTime.Now
            };
            _dbContext.ImageBuilds.Update(image);
            await _dbContext.SaveChangesAsync(cancelToken);
            if (cancelToken.IsCancellationRequested) return new OperationCanceledException();
            try {
                using (var dockerFileStream = File.OpenRead(Path.Combine(_repoPath, deployment.ProjectId, deployment.DockerPath))) {
                    await _dockerClient.Images.BuildImageFromDockerfileAsync(
                        config, dockerFileStream, null, null,
                        new Progress<JSONMessage>(msg => {
                            image.Logs += $"[{DateTime.Now}] {msg.Stream}\n";
                        }), cancelToken);
                    if (cancelToken.IsCancellationRequested) return new OperationCanceledException(); 
                }
            } catch (Exception ex) {
                if (ex is not DockerApiException or IOException)
                    throw;
                if(ex is IOException)
                    image.Logs += $"[{DateTime.Now}] Dockerfile could not be found at ${Path.Combine(_repoPath, deployment.ProjectId, deployment.DockerPath)}\n";
                else
                    image.Logs += $"[{DateTime.Now}] Exited with an Error!\nError Message: {ex.Message}\n";
                image.BuidStatus = Status.FAILED;
                _dbContext.ImageBuilds.Update(image);
                await _dbContext.SaveChangesAsync(cancelToken);
                return ex;
            }

            var listConfig = new ImagesListParameters {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>> {
                    {"label", new Dictionary<string ,bool> { { $"deployment={deployment.DeploymentId}", true } } }
                }
            };
            IList<ImagesListResponse> res;
            try {
                res = await _dockerClient.Images.ListImagesAsync(listConfig, cancelToken);
            }catch (DockerApiException ex) {
                return ex;
            }
            if (cancelToken.IsCancellationRequested) return new OperationCanceledException();
            if (res.Count != 1) {
                image.BuidStatus = Status.FAILED;
                await _dbContext.SaveChangesAsync();
                return new DockerImageNotFoundException(System.Net.HttpStatusCode.BadRequest, "Found multiple or no Images after build. Did the Build fail or was the old image not pruned?");
            }

            image.BuidStatus = Status.DONE;
            image.ImageId = res.First().ID;
            image.Created = res.First().Created;
            _dbContext.ImageBuilds.Update(image);
            await _dbContext.SaveChangesAsync(cancelToken);
            return cancelToken.IsCancellationRequested ? new OperationCanceledException() : image;
        }

        public async Task<Result<IEnumerable<ContainerListResponse>>> GetAllContainersForProjectAsync(string projId, CancellationToken cancelToken) {
            var config = new ContainersListParameters {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>> {
                    {"label", new Dictionary<string ,bool> { { $"project={projId}", true } } }
                }
            };
            try {
                var res = await _dockerClient.Containers.ListContainersAsync(config, cancelToken);
                return new Result<IEnumerable<ContainerListResponse>>(res);
            }catch(DockerApiException ex) {
                return ex;
            }
        }

        public async Task<IEnumerable<ImageBuild>> GetAllImagesForProjectAsync(string projId) {
            return await _dbContext.ImageBuilds
                .Include(e => e.Deployment)
                .Where(e => e.Deployment.ProjectId.Equals(projId))
                .ToListAsync();
        }

        public async Task<List<ContainerLogEntry>> GetContainerLogsAsync(string containerId, CancellationToken cancelToken) {
            List<ContainerLogEntry> logs = new();
            
            var config = new ContainerLogsParameters {
                Timestamps = true,
                Tail = "500",
                ShowStderr = true,
                ShowStdout = true
            };

            var buffer = ArrayPool<byte>.Shared.Rent(81920);
            try {
                var muxStream = await _dockerClient.Containers.GetContainerLogsAsync(containerId, false, config);
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
            }catch(DockerApiException ex) {
                return Enumerable.Empty<ContainerLogEntry>().ToList();
            }finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return logs;
        }

        public async Task<string?> GetImageLogsAsync(string imageId) {
            return await _dbContext.ImageBuilds
                .Where(e => e.ImageId.Equals(imageId))
                .Select(e => e.Logs)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> StopRunningContainersByProjectAsync(string projId, CancellationToken cancelToken) {
            var containersResult = (await GetAllContainersForProjectAsync(projId, cancelToken));
            if (containersResult.IsFaulted)
                return false;
            if (cancelToken.IsCancellationRequested)
                return false;
            var config = new ContainerStopParameters {
                WaitBeforeKillSeconds = 5
            };
            bool succ = true;
            try {
                foreach (var container in containersResult.Value) {
                    succ = succ && await _dockerClient.Containers.StopContainerAsync(container.ID, config, cancelToken);
                    if (cancelToken.IsCancellationRequested)
                        return false;
                }
            }catch(DockerApiException) {
                return false;
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
            ImagesPruneResponse dockerRes;
            try {
                dockerRes = await _dockerClient.Images.PruneImagesAsync(config, cancelToken);
            }catch(DockerApiException) { 
                return false; 
            }
            if (cancelToken.IsCancellationRequested) return false;
            var dbRes = await _dbContext.ImageBuilds.Include(e => e.Deployment).Where(e => e.Deployment.ProjectId.Equals(projId)).ExecuteDeleteAsync();
            await _dbContext.SaveChangesAsync();
            return dockerRes != null && dockerRes.ImagesDeleted.Count > 0 && dbRes > 0;
        }

        public async Task<bool> PruneProcessesAsync(string projId, CancellationToken cancelToken) {
            var config = new ContainersPruneParameters {
                Filters = new Dictionary<string, IDictionary<string, bool>> {
                    {"label", new Dictionary<string ,bool> { { $"project={projId}", true } } }
                }
            };
            ContainersPruneResponse res;
            try {
                res = await _dockerClient.Containers.PruneContainersAsync(config, cancelToken);
            }catch(DockerApiException) { 
                return false; 
            }
            return res != null && res.ContainersDeleted.Count > 0;
        }

        public async Task<Result<string>> RunImageAsync(ImageBuild image, Deployment deployment, CancellationToken cancelToken) {
            if(image.ImageId == null) {
                return new ArgumentNullException("Image Id can't be null");
            }

            var config = new CreateContainerParameters {
                Image = image.ImageId,
                HostConfig = new HostConfig {
                    PortBindings = new Dictionary<string, IList<PortBinding>> {
                        { deployment.EnvironmentPort.ToString(), new List<PortBinding> { new PortBinding { HostPort = deployment.HostPort.ToString() } } }
                    },
                    Binds = deployment.Mounts.Select(e => $"{Path.Combine(_repoPath,deployment.ProjectId,e.HostPath)}:{e.EnvironmentPath}").ToList()
                },
                Name = deployment.DeploymentId,
                Env = deployment.EnvironmentVars.Select(e => $"{e.Key}={e.Value}").ToList(),
                Labels = new Dictionary<string, string> { { "project", deployment.ProjectId }, { "deployment", deployment.DeploymentId } },
            };
            try {
                var res = await _dockerClient.Containers.CreateContainerAsync(config, cancelToken);
                if (cancelToken.IsCancellationRequested)
                    return new OperationCanceledException();
                return res.ID;
            }catch(DockerApiException ex) {
                return ex;
            }
        }
    }
}
