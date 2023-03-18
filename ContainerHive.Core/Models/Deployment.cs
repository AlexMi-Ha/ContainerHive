
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContainerHive.Core.Models {
    public class Deployment {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string DeploymentId { get; set; } = Guid.NewGuid().ToString();

        [MaxLength(512)]
        public string DockerPath { get; set; } = ".";

        [Required]
        [Range(0, 65536)]
        public ushort HostPort { get; set; }   

        [Required]
        [Range(0, 65536)]
        public ushort EnvironmentPort { get; set; }

        [Required]
        [ForeignKey(nameof(Project))]
        public required string ProjectId { get; set; }
        public Project? Project { get; set; }

        // Auto included by context
        public IEnumerable<EnvironmentVar> EnvironmentVars { get; set; }
        public IEnumerable<Mount> Mounts { get; set; }

    }
}
