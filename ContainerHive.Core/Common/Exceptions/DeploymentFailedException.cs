
namespace ContainerHive.Core.Common.Exceptions {
    public class DeploymentFailedException : Exception {

        public DeploymentFailedException() : base() { }
        public DeploymentFailedException(string message) : base(message) { }
        public DeploymentFailedException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
