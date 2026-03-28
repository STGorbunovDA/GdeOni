using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.DeceasedRecords.AddMemory.UseCase;
using GdeOni.Application.DeceasedRecords.AddPhoto.UseCase;
using GdeOni.Application.DeceasedRecords.ApproveMemory.UseCase;
using GdeOni.Application.DeceasedRecords.ApprovePhoto.UseCase;
using GdeOni.Application.DeceasedRecords.ClearMetadata.UseCase;
using GdeOni.Application.DeceasedRecords.Create.UseCase;
using GdeOni.Application.DeceasedRecords.Delete.UseCase;
using GdeOni.Application.DeceasedRecords.GetAll.UseCase;
using GdeOni.Application.DeceasedRecords.GetById.UseCase;
using GdeOni.Application.DeceasedRecords.GetDistance.UseCase;
using GdeOni.Application.DeceasedRecords.RejectMemory.UseCase;
using GdeOni.Application.DeceasedRecords.RejectPhoto.UseCase;
using GdeOni.Application.DeceasedRecords.RemoveMemory.UseCase;
using GdeOni.Application.DeceasedRecords.RemovePhoto.UseCase;
using GdeOni.Application.DeceasedRecords.SetPrimaryPhoto.UseCase;
using GdeOni.Application.DeceasedRecords.Update.UseCase;
using GdeOni.Application.DeceasedRecords.UpdateMemory.UseCase;
using GdeOni.Application.DeceasedRecords.UpdateMetadata.UseCase;
using GdeOni.Application.DeceasedRecords.UpdatePhoto.UseCase;
using GdeOni.Application.Users.Create.UseCase;
using Microsoft.Extensions.DependencyInjection;

namespace GdeOni.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidatedUseCaseExecutor, ValidatedUseCaseExecutor>();

        services.AddScoped<ICreateDeceasedUseCase, CreateDeceasedUseCase>();
        services.AddScoped<IGetAllDeceasedUseCase, GetAllDeceasedUseCase>();
        services.AddScoped<IGetDeceasedByIdUseCase, GetDeceasedByIdUseCase>();
        services.AddScoped<IUpdateDeceasedUseCase, UpdateDeceasedUseCase>();
        services.AddScoped<IDeleteDeceasedUseCase, DeleteDeceasedUseCase>();

        services.AddScoped<IAddPhotoUseCase, AddPhotoUseCase>();
        services.AddScoped<IRemovePhotoUseCase, RemovePhotoUseCase>();
        services.AddScoped<IUpdatePhotoUseCase, UpdatePhotoUseCase>();
        services.AddScoped<ISetPrimaryPhotoUseCase, SetPrimaryPhotoUseCase>();
        services.AddScoped<IApprovePhotoUseCase, ApprovePhotoUseCase>();
        services.AddScoped<IRejectPhotoUseCase, RejectPhotoUseCase>();

        services.AddScoped<IAddMemoryUseCase, AddMemoryUseCase>();
        services.AddScoped<IRemoveMemoryUseCase, RemoveMemoryUseCase>();
        services.AddScoped<IUpdateMemoryUseCase, UpdateMemoryUseCase>();
        services.AddScoped<IApproveMemoryUseCase, ApproveMemoryUseCase>();
        services.AddScoped<IRejectMemoryUseCase, RejectMemoryUseCase>();

        services.AddScoped<IUpdateMetadataUseCase, UpdateMetadataUseCase>();
        services.AddScoped<IClearMetadataUseCase, ClearMetadataUseCase>();
        services.AddScoped<IGetDistanceUseCase, GetDistanceUseCase>();

        services.AddScoped<ICreateUserUseCase, CreateUserUseCase>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}