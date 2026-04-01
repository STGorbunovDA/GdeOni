using FluentValidation;
using GdeOni.Application.DeceasedRecords.ClearMetadata.Model;

namespace GdeOni.Application.DeceasedRecords.ClearMetadata.Validation;

public sealed class ClearMetadataRequestValidator : AbstractValidator<ClearMetadataRequest>
{
    public ClearMetadataRequestValidator()
    {
        RuleFor(x => x.DeceasedId).NotEmpty();
    }
}