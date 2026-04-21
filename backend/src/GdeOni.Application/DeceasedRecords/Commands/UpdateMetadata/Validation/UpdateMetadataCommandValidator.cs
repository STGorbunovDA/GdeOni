using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.Validation;

public sealed class UpdateMetadataCommandValidator
    : AbstractValidator<UpdateMetadataCommand>
{
    public UpdateMetadataCommandValidator()
    {
        RuleFor(x => x.DeceasedId)
            .NotEmpty()
            .WithError(Errors.Deceased.IdRequired());

        RuleFor(x => x.Epitaph)
            .MaximumLength(500)
            .WithError(Errors.Deceased.EpitaphTooLong(500))
            .When(x => !string.IsNullOrWhiteSpace(x.Epitaph));

        RuleFor(x => x.Religion)
            .MaximumLength(200)
            .WithError(Errors.Deceased.ReligionTooLong(200))
            .When(x => !string.IsNullOrWhiteSpace(x.Religion));

        RuleFor(x => x.Source)
            .MaximumLength(500)
            .WithError(Errors.Deceased.SourceTooLong(500))
            .When(x => !string.IsNullOrWhiteSpace(x.Source));

        RuleFor(x => x.AdditionalInfo)
            .MaximumLength(2000)
            .WithError(Errors.Deceased.AdditionalInfoTooLong(2000))
            .When(x => !string.IsNullOrWhiteSpace(x.AdditionalInfo));
    }
}