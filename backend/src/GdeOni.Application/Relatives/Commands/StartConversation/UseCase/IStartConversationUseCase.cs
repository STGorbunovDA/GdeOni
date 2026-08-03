using CSharpFunctionalExtensions;
using GdeOni.Application.Relatives.Commands.StartConversation.Model;
using GdeOni.Application.Relatives.Common;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Commands.StartConversation.UseCase;

public interface IStartConversationUseCase
{
    Task<Result<RelativeConversationDetailResponse, Error>> Execute(
        StartConversationCommand command, CancellationToken cancellationToken);
}
