
using ContainerHive.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace ContainerHive.Core.Datastore {
    public class ApplicationDbContext : DbContext {

        public DbSet<Project> Projects { get; set; }
        public DbSet<Repo> Repos { get; set; }
        public DbSet<Deployment> Deployments { get; set; }
        public DbSet<EnvironmentVar> EnvironmentVars { get; set; }
        public DbSet<Mount> Mounts { get; set; }


        public ApplicationDbContext(DbContextOptions options) : base(options) {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

        }
    }
}
