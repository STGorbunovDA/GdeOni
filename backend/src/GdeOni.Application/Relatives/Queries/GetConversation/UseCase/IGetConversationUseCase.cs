using CSharpFunctionalExtensions;
using GdeOni.Application.Relatives.Common;
using GdeOni.Application.Relatives.Queries.GetConversation.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Queries.GetConversation.UseCase;

public interface IGetConversationUseCase
{
    Task<Result<RelativeConversationDetailResponse, Error>> Execute(
        GetConversationQuery query, CancellationToken cancellationToken);
}
