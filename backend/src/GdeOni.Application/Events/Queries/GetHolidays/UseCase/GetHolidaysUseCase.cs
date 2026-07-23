using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Events.Queries.GetHolidays.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Events.Queries.GetHolidays.UseCase;

/// <summary>
/// Считает праздники в диапазоне через <see cref="HolidayCalculator"/>.
/// Репозитория нет — данные вычисляются формулами; валидацию диапазона
/// делает <see cref="Validation.GetHolidaysQueryValidator"/>.
/// </summary>
public sealed class GetHolidaysUseCase(IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetHolidaysUseCase
{
    public Task<Result<GetHolidaysResponse, Error>> Execute(
        GetHolidaysQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private Task<Result<GetHolidaysResponse, Error>> Handle(
        GetHolidaysQuery query,
        CancellationToken cancellationToken)
    {
        var holidays = HolidayCalculator.GetHolidays(query.From, query.To)
            .Select(h => new HolidayDto(h.Date, h.Name, h.Category.ToString()))
            .ToList();

        return Task.FromResult(
            Result.Success<GetHolidaysResponse, Error>(new GetHolidaysResponse(holidays)));
    }
}
