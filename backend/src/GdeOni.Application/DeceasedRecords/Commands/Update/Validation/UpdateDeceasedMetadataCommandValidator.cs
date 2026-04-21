using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Commands.Update.Model;
using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Commands.Update.Validation;

public sealed class UpdateDeceasedMetadataCommandValidator
    : AbstractValidator<UpdateDeceasedMetadataCommand>
{
    public UpdateDeceasedMetadataCommandValidator()
    {
        RuleFor(x => x.Epitaph)
            .MaximumLength(DeceasedMetadata.MaxEpitaphLength)
            .WithError(Errors.Deceased.EpitaphTooLong(DeceasedMetadata.MaxEpitaphLength))
            .When(x => !string.IsNullOrWhiteSpace(x.Epitaph));

        RuleFor(x => x.Religion)
            .MaximumLength(DeceasedMetadata.MaxReligionLength)
            .WithError(Errors.Deceased.ReligionTooLong(DeceasedMetadata.MaxReligionLength))
            .When(x => !string.IsNullOrWhiteSpace(x.Religion));

        RuleFor(x => x.Source)
            .MaximumLength(DeceasedMetadata.MaxSourceLength)
            .WithError(Errors.Deceased.SourceTooLong(DeceasedMetadata.MaxSourceLength))
            .When(x => !string.IsNullOrWhiteSpace(x.Source));

        RuleFor(x => x.AdditionalInfo)
            .MaximumLength(DeceasedMetadata.MaxAdditionalInfoLength)
            .WithError(Errors.Deceased.AdditionalInfoTooLong(DeceasedMetadata.MaxAdditionalInfoLength))
            .When(x => !string.IsNullOrWhiteSpace(x.AdditionalInfo));
    }
}