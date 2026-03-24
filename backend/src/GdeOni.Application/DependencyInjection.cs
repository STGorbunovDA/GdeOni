using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Deceased.Create.UseCase;
using GdeOni.Application.Users.Create.UseCase;
using Microsoft.Extensions.DependencyInjection;

namespace GdeOni.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidatedUseCaseExecutor, ValidatedUseCaseExecutor>();
        services.AddScoped<ICreateDeceasedUseCase, CreateDeceasedUseCase>();
        services.AddScoped<ICreateUserUseCase, CreateUserUseCase>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}