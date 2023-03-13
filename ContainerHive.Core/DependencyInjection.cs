
using ContainerHive.Core.Datastore;
using Docker.DotNet;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ContainerHive.Core {
    public static class DependencyInjection {

        public static IServiceCollection AddCoreServices(this IServiceCollection services, IConfiguration configuration) {

            services.AddDbContext<ApplicationDbContext>(
                options => options.UseMySql(configuration.GetConnectionString("DefaultConnection")!,
                new MariaDbServerVersion(new Version(configuration["DatabaseConfig:Version"]!)))
            );

            services.AddSingleton<IDockerClient>(
                new DockerClientConfiguration(new Uri(configuration["DockerDaemonSocket"]!))
                .CreateClient()
            );

            return services;
        }
    }
}
