using FluentValidation;
using GdeOni.Application.DeceasedRecords.Queries.GetDeceasedEdits.Model;

namespace GdeOni.Application.DeceasedRecords.Queries.GetDeceasedEdits.Validation;

public sealed class GetDeceasedEditsQueryValidator : AbstractValidator<GetDeceasedEditsQuery>
{
    public GetDeceasedEditsQueryValidator()
    {
        RuleFor(x => x.DeceasedId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
    }
}
