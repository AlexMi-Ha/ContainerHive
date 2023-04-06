using ContainerHive.Core.Common.Exceptions;
using ContainerHive.Core.Common.Helper.Result;
using ContainerHive.Core.Common.Interfaces;
using ContainerHive.Core.Datastore;
using ContainerHive.Core.Models;
using ContainerHive.Core.Models.Docker;
using Docker.DotNet;
using Docker.DotNet.Models;
using ICSharpCode.SharpZipLib.Tar;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;

namespace ContainerHive.Core.Services {
    internal class DockerService : IDockerService {

        private readonly IDockerClient _dockerClient;

        private readonly ILogger logger;

        private readonly string _repoPath;

        private readonly ApplicationDbContext _dbContext;

        public DockerService(IDockerClient dockerClient, IConfiguration config, ApplicationDbContext dbContext, ILogger<DockerService> logger) {
            _dockerClient = dockerClient!;
            _repoPath = config["RepoPath"]!;
            _dbContext = dbContext!;
            this.logger = logger;
        }

        public async Task<Result<ImageBuild>> BuildImageAsync(Deployment deployment, CancellationToken cancelToken) {
            var config = new ImageBuildParameters {
                Tags = new List<string> { $"project:{deployment.ProjectId.ToLower()}", $"deployment:{deployment.DeploymentId.ToLower()}" },
                Labels = new Dictionary<string, string> { { "project", deployment.ProjectId.ToLower() }, { "deployment", deployment.DeploymentId.ToLower() } } 
            };
            var image = new ImageBuild {
                Deployment = deployment,
                DeploymentId = deployment.DeploymentId,
                BuidStatus = Status.BUILDING,
                Created = DateTime.Now,
                Logs = string.Empty
            };
            await _dbContext.ImageBuilds.AddAsync(image);
            await _dbContext.SaveChangesAsync(cancelToken);
            if (cancelToken.IsCancellationRequested) return new OperationCanceledException();
            try {
                using (var tarStream = CreateTarFileForDockerfileDirectory(Path.Combine(_repoPath, deployment.ProjectId))) {
                    using (var responseStream = await _dockerClient.Images.BuildImageFromDockerfileAsync(tarStream, config, cancelToken))
                    using (var responseReader = new StreamReader(responseStream)) {
                        while(!responseReader.EndOfStream && !cancelToken.IsCancellationRequested) {
                            string? output = await responseReader.ReadLineAsync();
                            if(!String.IsNullOrEmpty(output)) {
                                image.Logs += Regex.Replace(Regex.Replace(output, @"[{}\\n]+", ""), @"\\(.)", "$1").Replace(@"\u003e", ">") + "\n";
                            }
                        }
                    }
                    if (cancelToken.IsCancellationRequested) return new OperationCanceledException();

                    image.Logs += $"[{DateTime.Now}] Exiting Building\n";
                    _dbContext.ImageBuilds.Update(image);
                    await _dbContext.SaveChangesAsync(cancelToken);
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
                    {"label", new Dictionary<string ,bool> { { $"deployment={deployment.DeploymentId.ToLower()}", true } } }
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
                image.Logs += $"[{DateTime.Now}] Could not find the corresponding image\n";
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

        private Stream CreateTarFileForDockerfileDirectory(string directory) {
            var stream = new MemoryStream();
            var filePaths = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
            using (var archive = TarArchive.CreateOutputTarArchive(stream)) {
                archive.IsStreamOwner = false;

                foreach (var file in filePaths) {
                    logger.LogInformation(file);

                    var entry = TarEntry.CreateEntryFromFile(file);
                    archive.WriteEntry(entry, true);

                }
                archive.Close();

                stream.Position = 0;
                return stream;
            }
        }

        public async Task<Result<IEnumerable<ContainerListResponse>>> GetAllContainersForProjectAsync(string projId, CancellationToken cancelToken) {
            var config = new ContainersListParameters {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>> {
                    {"label", new Dictionary<string ,bool> { { $"project={projId.ToLower()}", true } } }
                }
            };
            try {
                var res = await _dockerClient.Containers.ListContainersAsync(config, cancelToken);
                if (cancelToken.IsCancellationRequested)
                    return new OperationCanceledException();
                return new Result<IEnumerable<ContainerListResponse>>(res);
            }catch(DockerApiException ex) {
                return ex;
            }
        }

        public async Task<Result<IEnumerable<ContainerListResponse>>> GetAllContainersForDeploymentAsync(string deploymentId, CancellationToken cancelToken) {
            var config = new ContainersListParameters {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>> {
                    {"label", new Dictionary<string ,bool> { { $"deployment={deploymentId.ToLower()}", true } } }
                }
            };
            try {
                var res = await _dockerClient.Containers.ListContainersAsync(config, cancelToken);
                if(cancelToken.IsCancellationRequested)
                    return new OperationCanceledException();
                return new Result<IEnumerable<ContainerListResponse>>(res);
            } catch (DockerApiException ex) {
                return ex;
            }
        }

        public async Task<IEnumerable<ImageBuild>> GetAllImagesForProjectAsync(string projId) {
            return await _dbContext.ImageBuilds
                .Include(e => e.Deployment)
                .Where(e => e.Deployment.ProjectId.Equals(projId))
                .ToListAsync();
        }

        public async Task<Result<string>> GetContainerLogsAsync(string deploymentId, CancellationToken cancelToken) {

            var containerResult = await GetAllContainersForDeploymentAsync(deploymentId, cancelToken);
            if(cancelToken.IsCancellationRequested) {
                return new OperationCanceledException();
            }
            if(containerResult.IsFaulted) {
                return new Result<string>(containerResult);
            }
            if(containerResult.Value?.Count() != 1) {
                return new RecordNotFoundException($"Could not find Container with DeploymentId {deploymentId}");
            }

            StringBuilder logsBuilder = new();
            
            var config = new ContainerLogsParameters {
                Timestamps = true,
                Tail = "500",
                ShowStderr = true,
                ShowStdout = true
            };

            var buffer = ArrayPool<byte>.Shared.Rent(81920);
            try {
                var muxStream = await _dockerClient.Containers.GetContainerLogsAsync(containerResult.Value.First().ID, false, config);
                using (MemoryStream m = new())
                using (StreamReader reader = new(m)) {
                    while (!cancelToken.IsCancellationRequested) {
                        var result = await muxStream.ReadOutputAsync(buffer, 0, buffer.Length, cancelToken);
                        if (result.EOF)
                            break;
                        logsBuilder.AppendLine(Encoding.Default.GetString(buffer));
                    }
                }
            }catch(DockerApiException ex) {
                return ex;
            }finally {
                ArrayPool<byte>.Shared.Return(buffer);
            }
            return logsBuilder.ToString();
        }

        public async Task<Result<ImageBuild>> GetImageByIdAsync(string deploymentId) {
            var res = await _dbContext.ImageBuilds
                .Include(e => e.Deployment)
                .Where(e => e.DeploymentId.Equals(deploymentId))
                .FirstOrDefaultAsync();
            return res != null ? res : new RecordNotFoundException($"Could not find Image with DeploymentId {deploymentId}");
        }

        public async Task<bool> StopRunningContainersByProjectAsync(string projId, CancellationToken cancelToken) {
            var containersResult = (await GetAllContainersForProjectAsync(projId, cancelToken));
            if (containersResult.IsFaulted)
                return false;
            if (cancelToken.IsCancellationRequested)
                return false;
            return await StopContainersAsync(containersResult.Value, cancelToken);
        }

        public async Task<bool> StopRunningContainersByDeploymentIdAsync(string deploymentId, CancellationToken cancelToken) {
            var containerResult = await GetAllContainersForDeploymentAsync(deploymentId, cancelToken);
            if (containerResult.IsFaulted)
                return false;
            if (cancelToken.IsCancellationRequested)
                return false;
            return await StopContainersAsync(containerResult.Value, cancelToken);
        }

        private async Task<bool> StopContainersAsync(IEnumerable<ContainerListResponse> containers, CancellationToken cancelToken) {
            var config = new ContainerStopParameters {
                WaitBeforeKillSeconds = 5
            };
            bool succ = true;
            try {
                foreach (var container in containers) {
                    succ = succ && await _dockerClient.Containers.StopContainerAsync(container.ID, config, cancelToken);
                    if (cancelToken.IsCancellationRequested)
                        return false;
                }
            } catch (DockerApiException) {
                return false;
            }
            return succ;
        }

        public async Task<bool> PruneImagesAsync(string projId, CancellationToken cancelToken) {
            var config = new ImagesPruneParameters {
                Filters = new Dictionary<string, IDictionary<string, bool>> {
                    { "label", new Dictionary<string, bool> { { $"project={projId.ToLower()}", true } } },
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
            var dbRes = await _dbContext.ImageBuilds.Include(e => e.Deployment).Where(e => e.Deployment.ProjectId.Equals(projId)).ToListAsync();
            _dbContext.ImageBuilds.RemoveRange(dbRes);
            await _dbContext.SaveChangesAsync();
            return dockerRes != null;
        }

        public async Task<bool> PruneProcessesAsync(string projId, CancellationToken cancelToken) {
            var config = new ContainersPruneParameters {
                Filters = new Dictionary<string, IDictionary<string, bool>> {
                    {"label", new Dictionary<string ,bool> { { $"project={projId.ToLower()}", true } } }
                }
            };
            ContainersPruneResponse res;
            try {
                res = await _dockerClient.Containers.PruneContainersAsync(config, cancelToken);
            }catch(DockerApiException) { 
                return false; 
            }
            return res != null;
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
                Labels = new Dictionary<string, string> { { "project", deployment.ProjectId.ToLower() }, { "deployment", deployment.DeploymentId.ToLower() } },
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
