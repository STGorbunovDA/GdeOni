using FluentValidation;
using GdeOni.Application.Users.GetAll.Model;

namespace GdeOni.Application.Users.GetAll.Validation;

public sealed class GetAllUsersQueryValidator : AbstractValidator<GetAllUsersQuery>
{
    public GetAllUsersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithMessage("Page must be greater than 0.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(x => x.Search)
            .MaximumLength(200)
            .WithMessage("Search must be at most 200 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.Search));

        RuleFor(x => x.Role)
            .IsInEnum()
            .When(x => x.Role.HasValue)
            .WithMessage("Invalid role.");
        
        RuleFor(x => x.RegisteredAtUtc)
            .LessThanOrEqualTo(_ => DateTime.UtcNow)
            .When(x => x.RegisteredAtUtc.HasValue)
            .WithMessage("RegisteredAtUtc cannot be in the future.");

        RuleFor(x => x.LastLoginAtUtc)
            .LessThanOrEqualTo(_ => DateTime.UtcNow)
            .When(x => x.LastLoginAtUtc.HasValue)
            .WithMessage("LastLoginAtUtc cannot be in the future.");
    }
}