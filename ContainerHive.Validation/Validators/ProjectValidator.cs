using ContainerHive.Core.Models;
using FluentValidation;

namespace ContainerHive.Validation.Validators
{
    internal class ProjectValidator : AbstractValidator<Project>
    {

        public ProjectValidator()
        {
            RuleFor(p => p.ProjectId)
                .NotEmpty()
                .WithMessage("ProjectId cannot be empty");

            RuleFor(p => p.Name)
                .NotEmpty()
                .WithMessage("Name cannot be empty")
                .MaximumLength(255)
                .WithMessage("Name can only be up to 255 characters long")
                .Matches(@"^[a-zA-Z0-9]+(\s?[a-zA-Z0-9]+)*$")
                .WithMessage("No special characters are allowed in the Name");

            /*RuleFor(p => p.RepoId)
                .NotNull()
                .NotEmpty()
                .WithMessage("RepoId cannot be empty");
            */

            RuleFor(p => p.Repo)
                .NotNull()
                .WithMessage("Repo cannot be null")
                .ChildRules(e => {
                    e.RuleFor(r => r.RepoId)
                        .NotEmpty()
                        .WithMessage("RepoId cannot be empty");

                    e.RuleFor(r => r.Url)
                        .NotEmpty()
                        .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                            && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps))
                        .WithMessage("Invalid Repo URL format");

                    e.RuleFor(r => r.Branch)
                        .Matches(@"^(?!\/|.*(?:[\/.]\.|\/\/|@\{|\\))[^\040\177 ~^:?*[]+(?<!\.lock)(?<![\/.])$")
                        .WithMessage("Branch name must be valid");
                });
        }
    }
}
