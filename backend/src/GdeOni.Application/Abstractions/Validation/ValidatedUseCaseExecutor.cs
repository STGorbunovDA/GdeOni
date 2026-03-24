using CSharpFunctionalExtensions;
using FluentValidation;
using GdeOni.Domain.Shared;
using Microsoft.Extensions.DependencyInjection;

namespace GdeOni.Application.Abstractions.Validation;

public sealed class ValidatedUseCaseExecutor(IServiceProvider serviceProvider)
    : IValidatedUseCaseExecutor
{
    public async Task<Result<TResponse, Error>> Execute<TRequest, TResponse>(
        TRequest request,
        Func<TRequest, CancellationToken, Task<Result<TResponse, Error>>> handler,
        CancellationToken cancellationToken)
        where TRequest : class
    {
        if (request is null)
            return Errors.General.ValueIsRequired(typeof(TRequest).Name);

        var validator = serviceProvider.GetService<IValidator<TRequest>>();

        if (validator is not null)
        {
            var validationResult = await validator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
                return validationResult.ToValidationError();
        }

        return await handler(request, cancellationToken);
    }
}