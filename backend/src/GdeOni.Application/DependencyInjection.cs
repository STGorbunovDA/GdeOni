using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.Login.UseCase;
using GdeOni.Application.DeceasedRecords.AddMemory.UseCase;
using GdeOni.Application.DeceasedRecords.AddPhoto.UseCase;
using GdeOni.Application.DeceasedRecords.ApproveMemory.UseCase;
using GdeOni.Application.DeceasedRecords.ApprovePhoto.UseCase;
using GdeOni.Application.DeceasedRecords.ClearMetadata.UseCase;
using GdeOni.Application.DeceasedRecords.Create.UseCase;
using GdeOni.Application.DeceasedRecords.Delete.UseCase;
using GdeOni.Application.DeceasedRecords.GetAgeAtDeath.UseCase;
using GdeOni.Application.DeceasedRecords.GetAll.UseCase;
using GdeOni.Application.DeceasedRecords.GetById.UseCase;
using GdeOni.Application.DeceasedRecords.GetDistance.UseCase;
using GdeOni.Application.DeceasedRecords.HasMemories.UseCase;
using GdeOni.Application.DeceasedRecords.HasPhotos.UseCase;
using GdeOni.Application.DeceasedRecords.RejectMemory.UseCase;
using GdeOni.Application.DeceasedRecords.RejectPhoto.UseCase;
using GdeOni.Application.DeceasedRecords.RemoveMemory.UseCase;
using GdeOni.Application.DeceasedRecords.RemovePhoto.UseCase;
using GdeOni.Application.DeceasedRecords.SetPrimaryPhoto.UseCase;
using GdeOni.Application.DeceasedRecords.Unverify.UseCase;
using GdeOni.Application.DeceasedRecords.Update.UseCase;
using GdeOni.Application.DeceasedRecords.UpdateMemory.UseCase;
using GdeOni.Application.DeceasedRecords.UpdateMetadata.UseCase;
using GdeOni.Application.DeceasedRecords.UpdatePhoto.UseCase;
using GdeOni.Application.DeceasedRecords.Verify.UseCase;
using GdeOni.Application.Users.Commands.ChangeEmail.UseCase;
using GdeOni.Application.Users.Commands.ChangePassword.UseCase;
using GdeOni.Application.Users.Commands.ChangeRole.UseCase;
using GdeOni.Application.Users.Commands.Delete.UseCase;
using GdeOni.Application.Users.Commands.Register.UseCase;
using GdeOni.Application.Users.Commands.RemoveTracking.UseCase;
using GdeOni.Application.Users.Commands.TrackDeceased.UseCase;
using GdeOni.Application.Users.Commands.UpdateProfile.UseCase;
using GdeOni.Application.Users.Commands.UpdateTracking.UseCase;
using GdeOni.Application.Users.Queries.GetAll.UseCase;
using GdeOni.Application.Users.Queries.GetById.UseCase;
using GdeOni.Application.Users.Queries.GetTrackedDeceased.UseCase;
using GdeOni.Application.Users.Queries.GetTracking.UseCase;
using Microsoft.Extensions.DependencyInjection;

namespace GdeOni.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidatedUseCaseExecutor, ValidatedUseCaseExecutor>();

        services.AddScoped<ILoginUseCase, LoginUseCase>();

        services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
        services.AddScoped<IGetUserByIdUseCase, GetUserByIdUseCase>();
        services.AddScoped<IUpdateUserProfileUseCase, UpdateUserProfileUseCase>();
        services.AddScoped<IChangePasswordUseCase, ChangePasswordUseCase>();
        services.AddScoped<IChangeRoleUseCase, ChangeRoleUseCase>();
        services.AddScoped<IChangeEmailUseCase, ChangeEmailUseCase>();
        services.AddScoped<IGetAllUsersUseCase, GetAllUsersUseCase>();

        services.AddScoped<ITrackDeceasedUseCase, TrackDeceasedUseCase>();
        services.AddScoped<IGetTrackedDeceasedUseCase, GetTrackedDeceasedUseCase>();
        services.AddScoped<IUpdateTrackingUseCase, UpdateTrackingUseCase>();

        services.AddScoped<ICreateDeceasedUseCase, CreateDeceasedUseCase>();
        services.AddScoped<IGetAllDeceasedUseCase, GetAllDeceasedUseCase>();
        services.AddScoped<IGetDeceasedByIdUseCase, GetDeceasedByIdUseCase>();
        services.AddScoped<IUpdateDeceasedUseCase, UpdateDeceasedUseCase>();
        services.AddScoped<IDeleteDeceasedUseCase, DeleteDeceasedUseCase>();
        services.AddScoped<IDeleteUserUseCase, DeleteUserUseCase>();

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
        
        services.AddScoped<IGetAgeAtDeathUseCase, GetAgeAtDeathUseCase>();
        services.AddScoped<IHasPhotosUseCase, HasPhotosUseCase>();
        services.AddScoped<IHasMemoriesUseCase, HasMemoriesUseCase>();
        services.AddScoped<IVerifyDeceasedUseCase, VerifyDeceasedUseCase>();
        services.AddScoped<IUnverifyDeceasedUseCase, UnverifyDeceasedUseCase>();
        services.AddScoped<IGetTrackingUseCase, GetTrackingUseCase>();
        services.AddScoped<IRemoveTrackingUseCase, RemoveTrackingUseCase>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}