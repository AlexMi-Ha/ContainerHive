
using ContainerHive.Core.Common.Helper.Result;
using ContainerHive.Core.Models;

namespace ContainerHive.Core.Common.Interfaces {
    public interface IGitService {

        public Task<Result> CloneProjectRepositoryAsync(Project project, CancellationToken cancelToken);

        public Task<Result> PullProjectRepositoryAsync(Project project, CancellationToken cancelToken);

        public Task<Result> CloneOrPullProjectRepositoryAsync(Project project, CancellationToken cancelToken);
    }
}
