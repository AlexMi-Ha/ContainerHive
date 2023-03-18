using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContainerHive.Core.Models {
    public class Repo {

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public string RepoId { get; set; } = Guid.NewGuid().ToString();

        [Required]
        [DataType(DataType.Url)]
        [MaxLength(512)]
        public required string Url { get; set; }

        [MaxLength(64)]
        public string Branch { get; set; } = "main";

    }
}
