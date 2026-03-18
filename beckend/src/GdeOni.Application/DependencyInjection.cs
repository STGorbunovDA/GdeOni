using GdeOni.Application.Deceased.Create.UseCase;
using GdeOni.Application.Users.Create.UseCase;
using Microsoft.Extensions.DependencyInjection;

namespace GdeOni.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICreateDeceasedUseCase, CreateDeceasedUseCase>();
        services.AddScoped<ICreateUserUseCase, CreateUserUseCase>();
        return services;
    }
}