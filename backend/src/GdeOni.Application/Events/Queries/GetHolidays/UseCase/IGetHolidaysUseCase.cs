using CSharpFunctionalExtensions;
using GdeOni.Application.Events.Queries.GetHolidays.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Events.Queries.GetHolidays.UseCase;

public interface IGetHolidaysUseCase
{
    Task<Result<GetHolidaysResponse, Error>> Execute(
        GetHolidaysQuery query,
        CancellationToken cancellationToken);
}
