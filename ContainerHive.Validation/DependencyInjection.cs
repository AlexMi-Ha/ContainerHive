
using ContainerHive.Validation.Validators;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ContainerHive.Validation {
    public static class DependencyInjection {

        public static IServiceCollection AddValidationServices(this IServiceCollection services) => 
            services.AddValidatorsFromAssemblyContaining<DeploymentValidator>(ServiceLifetime.Singleton);
    }
}
