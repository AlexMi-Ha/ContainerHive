
using ContainerHive.Core.Common.Helper.Result;
using ContainerHive.Core.Common.Interfaces;
using ContainerHive.Core.Models;
using Microsoft.Extensions.Configuration;
using System.Diagnostics;

namespace ContainerHive.Core.Services {
    internal class GitService : IGitService {

        private readonly string _repoPath;
        public GitService(IConfiguration configuration) {
            _repoPath = configuration["RepoPath"]!;
        }

        public Task<Result> CloneOrPullProjectRepositoryAsync(Project project, CancellationToken cancelToken) {
            // Does the folder exist? Yes -> pull ;  No -> clone
            if(Directory.Exists(Path.Combine(_repoPath, project.ProjectId, ".git"))) {
                return PullProjectRepositoryAsync(project, cancelToken);
            }
            return CloneProjectRepositoryAsync(project, cancelToken);
        }

        public Task<Result> CloneProjectRepositoryAsync(Project project, CancellationToken cancelToken) {
            var args = $"clone -b {project.Repo.Branch} {project.Repo.Url} {project.ProjectId}";
            var workDir = _repoPath;
            return ExecuteGitCommandAsync(args, workDir, cancelToken);
        }


        public async Task<Result> PullProjectRepositoryAsync(Project project, CancellationToken cancelToken) {
            var args = $"checkout {project.Repo.Branch}";
            var workDir = Path.Combine(_repoPath, project.ProjectId);
            var res = await ExecuteGitCommandAsync(args, workDir, cancelToken);
            if(res.IsFaulted) {
                return res;
            }
            args = $"pull origin";
            return await ExecuteGitCommandAsync(args, workDir, cancelToken);
        }

        public async Task<Result> DeleteProjectRepoAsync(Project project, CancellationToken cancelToken) {
            if (!Directory.Exists(Path.Combine(_repoPath, project.ProjectId)))
                return true; // No dir to delete -> I see it as an absolute win!
            try {
                await Task.Run(() => Directory.Delete(Path.Combine(_repoPath, project.ProjectId), true), cancelToken);
                if (cancelToken.IsCancellationRequested)
                    return new OperationCanceledException();
            } catch (IOException ex) {
                return ex;
            } catch (UnauthorizedAccessException ex) {
                return ex;
            }
            return true;
        }

        private async Task<Result> ExecuteGitCommandAsync(string args, string workDir, CancellationToken cancelToken) {
            using (var process = new Process {
                StartInfo = new ProcessStartInfo {
                    FileName = "git",
                    Arguments = args,
                    WorkingDirectory = workDir,
                    // TODO: maybe add auth for private repos
                }
            }) {
                var res = process.Start();
                if (!res) return false;
                await process.WaitForExitAsync(cancelToken);
                
                return cancelToken.IsCancellationRequested ?
                    new OperationCanceledException(cancelToken)
                    : process.ExitCode == 0;
            }
        }
    }
}
