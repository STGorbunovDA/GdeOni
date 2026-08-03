using CSharpFunctionalExtensions;
using GdeOni.Application.Relatives.Commands.ReportRelative.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Commands.ReportRelative.UseCase;

public interface IReportRelativeUseCase
{
    Task<Result<ReportRelativeResponse, Error>> Execute(
        ReportRelativeCommand command, CancellationToken cancellationToken);
}
