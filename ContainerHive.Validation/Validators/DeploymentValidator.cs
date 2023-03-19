using ContainerHive.Core.Models;
using FluentValidation;

namespace ContainerHive.Validation.Validators {
    internal class DeploymentValidator : AbstractValidator<Deployment> {

        public DeploymentValidator() {
            RuleFor(e => e.DeploymentId)
                .NotEmpty()
                .WithMessage("DeploymentId cannot be empty");

            RuleFor(e => e.DockerPath)
                .Matches(@"^(?:\.\.?(?:/[^\n""?:*<>|]+)*/)?Dockerfile$")
                .WithMessage("The Dockerpath must be a valid relative Path in your project");

            RuleFor(e => e.HostPort)
                .InclusiveBetween((ushort)0, (ushort)65535)
                .WithMessage("Host Port is out of range. Must be between 0 and 65536");

            RuleFor(e => e.EnvironmentPort)
                .InclusiveBetween((ushort)0, (ushort)65535)
                .WithMessage("Environment Port is out of range. Must be between 0 and 65536");

            RuleFor(e => e.EnvironmentVars)
                .NotNull()
                .WithMessage("Environment Vars cannot be null")
                .ForEach(envL => {
                    envL.ChildRules(env => {
                        env.RuleFor(e => e.Key)
                            .NotEmpty()
                            .WithMessage("Environment Variable Key cannot be empty")
                            .Matches(@"^[a-zA-Z_][a-zA-Z0-9_]*$")
                            .WithMessage("Environment Variable Key must start with a letter or underscore and only contain letters, digits or underscores.");
                        env.RuleFor(e => e.Value)
                            .NotEmpty()
                            .WithMessage("Environment Variable Value cannot be empty");
                    });
                });

            RuleFor(e => e.Mounts)
                .NotNull()
                .WithMessage("Mounts cannot be null")
                .ForEach(mountL => {
                    mountL.ChildRules(mount => {
                        mount.RuleFor(e => e.HostPath)
                            .NotEmpty()
                            .WithMessage("Host Path cannot be empty")
                            .Matches(@"^(?:/[^\n""?:*<>|]+)*/?$")
                            .WithMessage("Host Path must be an absolute Path relative to the Project root");

                        mount.RuleFor(e => e.EnvironmentPath)
                            .NotEmpty()
                            .WithMessage("Environment Path cannot be empty")
                            .Matches(@"^(?:/[^\n""?:*<>|]+)*/?$")
                            .WithMessage("Environment Path must be an absolute Path relative to the container root");
                    });
                });

        }
    }
}
