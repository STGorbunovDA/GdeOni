using CSharpFunctionalExtensions;
using GdeOni.Application.Relatives.Queries.ListConversations.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Queries.ListConversations.UseCase;

public interface IListConversationsUseCase
{
    Task<Result<ListRelativeConversationsResponse, Error>> Execute(CancellationToken cancellationToken);
}
