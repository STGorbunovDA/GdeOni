using FluentValidation;
using GdeOni.Application.Geo.Queries.ForwardGeocode.Model;

namespace GdeOni.Application.Geo.Queries.ForwardGeocode.Validation;

public sealed class ForwardGeocodeQueryValidator : AbstractValidator<ForwardGeocodeQuery>
{
    public const int MaxQueryLength = 300;

    public ForwardGeocodeQueryValidator()
    {
        RuleFor(x => x.Query)
            .NotEmpty().WithMessage("Не указан адрес для поиска.")
            .MaximumLength(MaxQueryLength)
            .WithMessage($"Адрес не длиннее {MaxQueryLength} символов.");
    }
}
