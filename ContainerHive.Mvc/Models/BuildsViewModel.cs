using ContainerHive.Core.Models.Docker;
using Docker.DotNet.Models;

namespace ContainerHive.Mvc.Models {
    public class BuildsViewModel {

        public List<ImageBuild> Builds { get; set; } = new();

        public List<ContainerListResponse> ContainerList { get; set; } = new();

    }
}
