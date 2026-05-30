using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetDeceasedEdits.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetDeceasedEdits.UseCase;

/// <summary>
/// D24. Админ-листинг истории правок карточки. Авторизация на уровне
/// контроллера (Roles = SuperAdmin/Admin), use case полагается на это.
/// </summary>
public sealed class GetDeceasedEditsUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetDeceasedEditsUseCase
{
    public Task<Result<GetDeceasedEditsResponse, Error>> Execute(
        GetDeceasedEditsQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<GetDeceasedEditsResponse, Error>> Handle(
        GetDeceasedEditsQuery query,
        CancellationToken cancellationToken)
    {
        var (rows, totalCount) = await deceasedRepository.GetEditsPaged(
            query.DeceasedId, query.Page, query.PageSize, cancellationToken);

        var items = rows
            .Select(r => new DeceasedEditItem(
                r.Edit.Id,
                r.Edit.EditedAtUtc,
                r.Edit.EditedByUserId,
                r.EditorEmail,
                r.EditorDisplayName,
                r.Edit.Kind,
                r.Edit.ChangesJson))
            .ToList();

        return Result.Success<GetDeceasedEditsResponse, Error>(
            new GetDeceasedEditsResponse(items, totalCount, query.Page, query.PageSize));
    }
}
