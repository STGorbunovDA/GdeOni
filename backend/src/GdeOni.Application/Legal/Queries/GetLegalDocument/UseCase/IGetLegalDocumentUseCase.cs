using CSharpFunctionalExtensions;
using GdeOni.Application.Legal.Queries.GetLegalDocument.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Legal.Queries.GetLegalDocument.UseCase;

public interface IGetLegalDocumentUseCase
{
    Task<Result<LegalDocumentResponse, Error>> Execute(
        GetLegalDocumentQuery query,
        CancellationToken cancellationToken);
}
