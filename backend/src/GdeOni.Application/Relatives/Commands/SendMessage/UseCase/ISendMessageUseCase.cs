using CSharpFunctionalExtensions;
using GdeOni.Application.Relatives.Commands.SendMessage.Model;
using GdeOni.Application.Relatives.Common;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Commands.SendMessage.UseCase;

public interface ISendMessageUseCase
{
    Task<Result<RelativeConversationDetailResponse, Error>> Execute(
        SendMessageCommand command, CancellationToken cancellationToken);
}
