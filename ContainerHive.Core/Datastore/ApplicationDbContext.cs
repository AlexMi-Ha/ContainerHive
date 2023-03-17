
using ContainerHive.Core.Models;
using ContainerHive.Core.Models.Docker;
using Microsoft.EntityFrameworkCore;

namespace ContainerHive.Core.Datastore {
    public class ApplicationDbContext : DbContext {

        public DbSet<Project> Projects { get; set; }
        public DbSet<Repo> Repos { get; set; }
        public DbSet<Deployment> Deployments { get; set; }
        public DbSet<EnvironmentVar> EnvironmentVars { get; set; }
        public DbSet<Mount> Mounts { get; set; }

        public DbSet<ImageBuild> ImageBuilds { get; set; }     

        public ApplicationDbContext(DbContextOptions options) : base(options) {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Deployment>().Navigation(e => e.EnvironmentVars).AutoInclude();
            modelBuilder.Entity<Deployment>().Navigation(e => e.Mounts).AutoInclude();

            modelBuilder.Entity<Project>().Navigation(e => e.Repo).AutoInclude();
        }
    }
}
