using GdeOni.Application.Deceased.Create.Service;
using GdeOni.Application.Users.Create.Service;
using Microsoft.Extensions.DependencyInjection;

namespace GdeOni.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICreateDeceasedService, CreateDeceasedService>();
        services.AddScoped<ICreateUserService, CreateUserService>();
        return services;
    }
}