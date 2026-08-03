using CSharpFunctionalExtensions;
using GdeOni.Application.Relatives.Commands.ResolveRelativeReport.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Relatives.Commands.ResolveRelativeReport.UseCase;

public interface IResolveRelativeReportUseCase
{
    Task<UnitResult<Error>> Execute(
        ResolveRelativeReportCommand command, CancellationToken cancellationToken);
}
