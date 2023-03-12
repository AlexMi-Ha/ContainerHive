
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContainerHive.Core.Models {
    public class Project {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ProjectId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [MaxLength(255)]
        public required string Name { get; set; }

        public bool WebhookActive { get; set; } = false;

        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string ApiToken { get; private set; } = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        [Required]
        public bool CustomNetwork { get; set; } = false;

        [Required]
        [ForeignKey(nameof(Repo))]
        public required string RepoId { get; set; }
        public Repo? Repo { get; set; }


        public IEnumerable<Deployment>? Deployments { get; set; }

        public string RegenerateToken() {
            ApiToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            return ApiToken;
        }
    }
}
