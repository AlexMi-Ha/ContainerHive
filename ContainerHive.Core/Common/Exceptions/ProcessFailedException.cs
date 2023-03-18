using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContainerHive.Core.Common.Exceptions {
    public class ProcessFailedException : Exception {
        public ProcessFailedException() : base() { }
        public ProcessFailedException(string message) : base(message) { }
        public ProcessFailedException(string message, Exception innerException)
            : base(message, innerException) { }
    }
}
