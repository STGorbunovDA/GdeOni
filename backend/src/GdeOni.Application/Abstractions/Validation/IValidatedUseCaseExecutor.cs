using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Abstractions.Validation;

public interface IValidatedUseCaseExecutor
{
    Task<Result<TResponse, Error>> Execute<TRequest, TResponse>(
        TRequest request,
        Func<TRequest, CancellationToken, Task<Result<TResponse, Error>>> handler,
        CancellationToken cancellationToken)
        where TRequest : class;
}