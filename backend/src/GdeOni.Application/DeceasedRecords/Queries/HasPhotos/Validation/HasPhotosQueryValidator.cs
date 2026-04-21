using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Queries.HasPhotos.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.HasPhotos.Validation;

public sealed class HasPhotosQueryValidator : AbstractValidator<HasPhotosQuery>
{
    public HasPhotosQueryValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());
    }
}