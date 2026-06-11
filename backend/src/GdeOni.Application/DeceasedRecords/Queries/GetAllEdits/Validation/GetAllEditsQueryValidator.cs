using FluentValidation;
using GdeOni.Application.DeceasedRecords.Queries.GetAllEdits.Model;

namespace GdeOni.Application.DeceasedRecords.Queries.GetAllEdits.Validation;

public sealed class GetAllEditsQueryValidator : AbstractValidator<GetAllEditsQuery>
{
    public GetAllEditsQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
