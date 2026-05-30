using FluentValidation;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadataByEditor.Model;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMetadataByEditor.Validation;

public sealed class UpdateMetadataByEditorCommandValidator : AbstractValidator<UpdateMetadataByEditorCommand>
{
    public UpdateMetadataByEditorCommandValidator()
    {
        RuleFor(x => x.DeceasedId).NotEmpty();
    }
}
