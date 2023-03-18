
namespace ContainerHive.Core.Common.Exceptions {
    public class DeploymentFailedException : ProcessFailedException {

        public DeploymentFailedException() : base() { }
        public DeploymentFailedException(string message) : base(message) { }
        public DeploymentFailedException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
