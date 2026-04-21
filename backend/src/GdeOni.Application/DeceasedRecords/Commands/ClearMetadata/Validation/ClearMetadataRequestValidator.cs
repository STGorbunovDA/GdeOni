using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.ClearMetadata.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.ClearMetadata.Validation;

public sealed class ClearMetadataCommandValidator : AbstractValidator<ClearMetadataCommand>
{
    public ClearMetadataCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());
    }
}