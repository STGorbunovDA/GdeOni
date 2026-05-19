using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.Login.UseCase;
using GdeOni.Application.Auth.Logout.UseCase;
using GdeOni.Application.Auth.Refresh.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.AddAtGrave.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.AddMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMediaModeration.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.ApproveMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.ClearBurialLocation.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.ClearMetadata.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Create.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Delete.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.DeleteMedia.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.RejectMediaModeration.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.RejectMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.RemoveMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.SetBurialLocationFromGps.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.SetMainMediaPhoto.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Unverified.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Update.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMediaDescription.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UploadMedia.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Verify.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetAll.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetById.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetDistance.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaById.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaList.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetNearbyDeceased.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.UseCase;
using GdeOni.Application.Routing.Queries.GetRouteToGrave.UseCase;
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
using GdeOni.Application.Users.Queries.GetCurrent.UseCase;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedDetails.UseCase;
using GdeOni.Application.Users.Queries.GetMyTrackedDeceasedList.UseCase;
using GdeOni.Application.Users.Queries.IsTrackedByMe.UseCase;
using Microsoft.Extensions.DependencyInjection;

namespace GdeOni.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidatedUseCaseExecutor, ValidatedUseCaseExecutor>();

        services.AddScoped<ILoginUseCase, LoginUseCase>();
        services.AddScoped<IRefreshUseCase, RefreshUseCase>();
        services.AddScoped<ILogoutUseCase, LogoutUseCase>();

        services.AddScoped<IRegisterUserUseCase, RegisterUserUseCase>();
        services.AddScoped<IGetUserByIdUseCase, GetUserByIdUseCase>();
        services.AddScoped<IGetCurrentUserUseCase, GetCurrentUserUseCase>();
        services.AddScoped<IUpdateUserProfileUseCase, UpdateUserProfileUseCase>();
        services.AddScoped<IChangePasswordUseCase, ChangePasswordUseCase>();
        services.AddScoped<IChangeRoleUseCase, ChangeRoleUseCase>();
        services.AddScoped<IChangeEmailUseCase, ChangeEmailUseCase>();
        services.AddScoped<IGetAllUsersUseCase, GetAllUsersUseCase>();

        services.AddScoped<ITrackDeceasedUseCase, TrackDeceasedUseCase>();
        services.AddScoped<IUpdateTrackingUseCase, UpdateTrackingUseCase>();
        services.AddScoped<IGetMyTrackedDeceasedListUseCase, GetMyTrackedDeceasedListUseCase>();
        services.AddScoped<IGetMyTrackedDeceasedDetailsUseCase, GetMyTrackedDeceasedDetailsUseCase>();
        services.AddScoped<IIsTrackedByMeUseCase, IsTrackedByMeUseCase>();

        services.AddScoped<ICreateDeceasedUseCase, CreateDeceasedUseCase>();
        services.AddScoped<IAddDeceasedAtGraveUseCase, AddDeceasedAtGraveUseCase>();
        services.AddScoped<IGetAllDeceasedUseCase, GetAllDeceasedUseCase>();
        services.AddScoped<IGetNearbyDeceasedUseCase, GetNearbyDeceasedUseCase>();
        services.AddScoped<IGetDeceasedByIdUseCase, GetDeceasedByIdUseCase>();
        services.AddScoped<IUpdateDeceasedUseCase, UpdateDeceasedUseCase>();
        services.AddScoped<ISetBurialLocationFromGpsUseCase, SetBurialLocationFromGpsUseCase>();
        services.AddScoped<IDeleteDeceasedUseCase, DeleteDeceasedUseCase>();
        services.AddScoped<IDeleteUserUseCase, DeleteUserUseCase>();

        services.AddScoped<IAddMemoryUseCase, AddMemoryUseCase>();
        services.AddScoped<IRemoveMemoryUseCase, RemoveMemoryUseCase>();
        services.AddScoped<IUpdateMemoryUseCase, UpdateMemoryUseCase>();
        services.AddScoped<IApproveMemoryUseCase, ApproveMemoryUseCase>();
        services.AddScoped<IRejectMemoryUseCase, RejectMemoryUseCase>();

        services.AddScoped<IUpdateMetadataUseCase, UpdateMetadataUseCase>();
        services.AddScoped<IClearMetadataUseCase, ClearMetadataUseCase>();
        services.AddScoped<IClearBurialLocationUseCase, ClearBurialLocationUseCase>();
        services.AddScoped<IGetDistanceUseCase, GetDistanceUseCase>();

        services.AddScoped<IUploadMediaUseCase, UploadMediaUseCase>();
        services.AddScoped<IGetMediaListUseCase, GetMediaListUseCase>();
        services.AddScoped<IGetMediaByIdUseCase, GetMediaByIdUseCase>();
        services.AddScoped<IDeleteMediaUseCase, DeleteMediaUseCase>();
        services.AddScoped<ISetMainMediaPhotoUseCase, SetMainMediaPhotoUseCase>();
        services.AddScoped<IUpdateMediaDescriptionUseCase, UpdateMediaDescriptionUseCase>();
        services.AddScoped<IApproveMediaModerationUseCase, ApproveMediaModerationUseCase>();
        services.AddScoped<IRejectMediaModerationUseCase, RejectMediaModerationUseCase>();
        
        services.AddScoped<IGetAgeAtDeathUseCase, GetAgeAtDeathUseCase>();
        services.AddScoped<IHasMemoriesUseCase, HasMemoriesUseCase>();
        services.AddScoped<IVerifyDeceasedUseCase, VerifyDeceasedUseCase>();
        services.AddScoped<IUnverifiedDeceasedUseCase, UnverifiedDeceasedUseCase>();
        services.AddScoped<IRemoveTrackingUseCase, RemoveTrackingUseCase>();

        services.AddScoped<IGetRouteToGraveUseCase, GetRouteToGraveUseCase>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}