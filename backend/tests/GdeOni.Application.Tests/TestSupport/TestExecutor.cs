using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace GdeOni.Application.Tests.TestSupport;

/// <summary>
/// Helper для построения настоящего <see cref="ValidatedUseCaseExecutor"/>
/// с реальными FluentValidation-валидаторами (а не моками). Так тесты
/// покрывают настоящий путь request → validator → handler, включая
/// конвертацию ValidationResult в Error.Validation.
///
/// Use case'ы в проекте обращаются к executor'у через DI; в unit-тестах
/// мы собираем минимальный ServiceCollection с одним IValidator&lt;T&gt;
/// и заворачиваем его в новый ValidatedUseCaseExecutor.
/// </summary>
internal static class TestExecutor
{
    /// <summary>
    /// Создать executor, в котором зарегистрирован validator конкретного
    /// типа <typeparamref name="TValidator"/> для запроса <typeparamref name="TRequest"/>.
    /// </summary>
    public static IValidatedUseCaseExecutor With<TRequest, TValidator>()
        where TRequest : class
        where TValidator : class, IValidator<TRequest>, new()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<TRequest>>(new TValidator());
        return new ValidatedUseCaseExecutor(services.BuildServiceProvider());
    }

    /// <summary>
    /// Executor без валидаторов — тогда executor молча проходит и сразу
    /// зовёт handler. Полезно, когда тестируем поведение handler'а
    /// в обход валидации (например, проверяем 403 на ресурсе с правильным
    /// Guid'ом).
    /// </summary>
    public static IValidatedUseCaseExecutor Empty()
    {
        var services = new ServiceCollection();
        return new ValidatedUseCaseExecutor(services.BuildServiceProvider());
    }
}
