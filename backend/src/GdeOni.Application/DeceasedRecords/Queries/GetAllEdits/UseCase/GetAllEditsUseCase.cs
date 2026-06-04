using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.Queries.GetAllEdits.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.DeceasedRecords.Queries.GetAllEdits.UseCase;

/// <summary>
/// D24/F17.9. Лента всех правок по системе для админ-вкладки.
/// Авторизация на уровне контроллера (Roles=SuperAdmin/Admin), use case
/// полагается на это.
/// </summary>
public sealed class GetAllEditsUseCase(
    IDeceasedRepository deceasedRepository,
    IValidatedUseCaseExecutor validatedUseCaseExecutor) : IGetAllEditsUseCase
{
    public Task<Result<GetAllEditsResponse, Error>> Execute(
        GetAllEditsQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<GetAllEditsResponse, Error>> Handle(
        GetAllEditsQuery query,
        CancellationToken cancellationToken)
    {
        var (rows, totalCount) = await deceasedRepository.GetAllEditsPaged(
            query.Page, query.PageSize,
            query.DeceasedId,
            query.EditorUserId,
            query.EditedFromUtc,
            query.EditedToUtc,
            cancellationToken);

        var items = rows.Select(r => new DeceasedEditWithCardItem(
            r.Edit.Id,
            r.Edit.EditedAtUtc,
            r.Edit.DeceasedId,
            r.DeceasedFullName,
            r.Edit.EditedByUserId,
            r.EditorEmail,
            r.EditorDisplayName,
            r.Edit.Kind,
            r.Edit.ChangesJson)).ToList();

        return Result.Success<GetAllEditsResponse, Error>(
            new GetAllEditsResponse(items, totalCount, query.Page, query.PageSize));
    }
}
