
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContainerHive.Core.Models.Docker {

    public enum Status {
        DONE,
        BUILDING,
        FAILED,
        UNKNOWN
    }

    public class ImageBuild {

        [Key]
        [ForeignKey(nameof(Deployment))]
        public string DeploymentId { get; set; }
        public Deployment? Deployment { get; set; }

        [Required]
        public Status BuidStatus { get; set; } = Status.UNKNOWN;

        public string? ImageId { get; set; }

        public DateTime Created { get; set; }

        [Required]
        public string? Logs { get; set; }


    }
}
