using CSharpFunctionalExtensions;
using GdeOni.Application.Relatives.Commands.EditMessage.Model;
using GdeOni.Application.Relatives.Common;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Commands.EditMessage.UseCase;

public interface IEditMessageUseCase
{
    Task<Result<RelativeConversationDetailResponse, Error>> Execute(
        EditMessageCommand command, CancellationToken cancellationToken);
}
