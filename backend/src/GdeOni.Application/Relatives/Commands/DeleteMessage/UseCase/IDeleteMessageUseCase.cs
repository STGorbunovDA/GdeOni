using CSharpFunctionalExtensions;
using GdeOni.Application.Relatives.Commands.DeleteMessage.Model;
using GdeOni.Application.Relatives.Common;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Commands.DeleteMessage.UseCase;

public interface IDeleteMessageUseCase
{
    Task<Result<RelativeConversationDetailResponse, Error>> Execute(
        DeleteMessageCommand command, CancellationToken cancellationToken);
}
