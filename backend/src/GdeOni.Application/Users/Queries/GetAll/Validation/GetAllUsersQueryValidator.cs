using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Users.Queries.GetAll.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetAll.Validation;

public sealed class GetAllUsersQueryValidator : AbstractValidator<GetAllUsersQuery>
{
    private const int MinPageSize = 1;
    private const int MaxPageSize = 100;
    private const int MaxSearchLength = 200;

    public GetAllUsersQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0)
            .WithError(Errors.Pagination.PageMustBeGreaterThanZero());

        RuleFor(x => x.PageSize)
            .InclusiveBetween(MinPageSize, MaxPageSize)
            .WithError(Errors.Pagination.PageSizeOutOfRange(MinPageSize, MaxPageSize));

        RuleFor(x => x.Search)
            .MaximumLength(MaxSearchLength)
            .WithError(Errors.User.SearchTooLong(MaxSearchLength))
            .When(x => !string.IsNullOrWhiteSpace(x.Search));

        RuleFor(x => x.Role)
            .IsInEnum()
            .WithError(Errors.User.RoleInvalid())
            .When(x => x.Role.HasValue);

        RuleFor(x => x.RegisteredAtUtc)
            .LessThanOrEqualTo(_ => DateTime.UtcNow)
            .WithError(Errors.User.RegisteredAtUtcInFuture())
            .When(x => x.RegisteredAtUtc.HasValue);

        RuleFor(x => x.LastLoginAtUtc)
            .LessThanOrEqualTo(_ => DateTime.UtcNow)
            .WithError(Errors.User.LastLoginAtUtcInFuture())
            .When(x => x.LastLoginAtUtc.HasValue);
    }
}