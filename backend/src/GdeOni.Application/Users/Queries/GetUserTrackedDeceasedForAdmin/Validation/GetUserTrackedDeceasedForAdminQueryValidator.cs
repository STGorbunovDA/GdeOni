using FluentValidation;
using GdeOni.Application.Users.Queries.GetUserTrackedDeceasedForAdmin.Model;

namespace GdeOni.Application.Users.Queries.GetUserTrackedDeceasedForAdmin.Validation;

public sealed class GetUserTrackedDeceasedForAdminQueryValidator
    : AbstractValidator<GetUserTrackedDeceasedForAdminQuery>
{
    public GetUserTrackedDeceasedForAdminQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
