using FluentValidation;
using GdeOni.Application.Users.Queries.GetById.Model;

namespace GdeOni.Application.Users.Queries.GetById.Validation;

public sealed class GetUserByIdQueryValidator : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty();
    }
}