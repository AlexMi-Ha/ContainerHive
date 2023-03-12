
namespace ContainerHive.Core.Common.Exceptions {
    public class RecordNotFoundException : ArgumentException {

        public RecordNotFoundException() : base() { }
        public RecordNotFoundException(string message) : base(message) { }
        public RecordNotFoundException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
