using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContainerHive.Core.Models {
    public class Mount {

        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string MountId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(128)]
        public required string HostPath { get; set; }
        [Required]
        [MaxLength(128)]
        public required string EnvironmentPath { get; set; }

        [ForeignKey(nameof(Deployment))]
        [Required]
        public required string DeploymentId { get; set; }
        public Deployment? Deployment { get; set; }
    }
}
