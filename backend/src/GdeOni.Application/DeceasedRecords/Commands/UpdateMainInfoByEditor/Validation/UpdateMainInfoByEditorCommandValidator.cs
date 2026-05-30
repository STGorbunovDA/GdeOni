using FluentValidation;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMainInfoByEditor.Model;

namespace GdeOni.Application.DeceasedRecords.Commands.UpdateMainInfoByEditor.Validation;

/// <summary>
/// Валидатор формы DTO. Длины и формат проверяются в Domain (PersonName/
/// LifePeriod.Create) — тут только non-null/non-empty.
/// </summary>
public sealed class UpdateMainInfoByEditorCommandValidator : AbstractValidator<UpdateMainInfoByEditorCommand>
{
    public UpdateMainInfoByEditorCommandValidator()
    {
        RuleFor(x => x.DeceasedId).NotEmpty();
        RuleFor(x => x.FirstName).NotEmpty();
        RuleFor(x => x.LastName).NotEmpty();
    }
}
