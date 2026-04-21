using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetById.Validation;

public sealed class GetDeceasedByIdQueryValidator : AbstractValidator<GetDeceasedByIdQuery>
{
    public GetDeceasedByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());
    }
}