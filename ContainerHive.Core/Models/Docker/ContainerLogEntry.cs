namespace ContainerHive.Core.Models.Docker {

    public enum LogLevel {
        ERROR,
        STD
    }

    public class ContainerLogEntry {

        public string Log { get; set; }
        public LogLevel Level { get; set; }
    }
}
