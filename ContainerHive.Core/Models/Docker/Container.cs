namespace ContainerHive.Core.Models.Docker {

    public enum ContainerStatus {

    }

    public class Container {

        public string ContainerId { get; set; }
        public string Image { get; set; }
        public string Command { get; set; }
        public DateTime Created { get; set; } 

    }
}
