using FluentValidation.Results;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Abstractions.Validation;

public static class ValidationExtensions
{
    public static Error ToValidationError(this ValidationResult validationResult)
    {
        var details = validationResult.Errors
            .Select(x => new ValidationErrorDetail(
                x.PropertyName,
                x.ErrorCode,
                x.ErrorMessage))
            .ToArray();

        return Error.Validation(
            "validation.failed",
            "One or more validation errors occurred.",
            details);
    }
}