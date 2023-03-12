
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContainerHive.Core.Models {
    public class EnvironmentVar {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string VarId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(64)]
        public required string Key { get; set; }
        [Required]
        [MaxLength(256)]
        public required string Value { get; set; }

        [ForeignKey(nameof(Deployment))]
        [Required]
        public required string DeploymentId { get; set; }
        public Deployment? Deployment { get; set; }
    }
}
