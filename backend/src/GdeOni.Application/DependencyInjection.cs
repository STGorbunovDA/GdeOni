using FluentValidation;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Auth.Login.UseCase;
using GdeOni.Application.Auth.Logout.UseCase;
using GdeOni.Application.Auth.Refresh.UseCase;
using GdeOni.Application.Complimentary.Commands.Grant.UseCase;
using GdeOni.Application.Complimentary.Commands.Revoke.UseCase;
using GdeOni.Application.Subscriptions.Commands.RestartTrialByAdmin.UseCase;
using GdeOni.Application.Subscriptions.Commands.RevokeByAdmin.UseCase;
using GdeOni.Application.Users.Commands.AdminRemoveUserTracking.UseCase;
using GdeOni.Application.Users.Queries.GetUserTrackedDeceasedForAdmin.UseCase;
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
using GdeOni.Application.Common.Security;
using GdeOni.Application.DeceasedRecords.Commands.Update.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateBurialLocationByEditor.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMainInfoByEditor.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMediaDescription.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadataByEditor.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMemory.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UpdateMetadata.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.UploadMedia.UseCase;
using GdeOni.Application.DeceasedRecords.Commands.Verify.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetAgeAtDeath.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetAll.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetById.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetDistance.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.DownloadMedia.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaById.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetMediaList.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetNearbyDeceased.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetAllEdits.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.GetDeceasedEdits.UseCase;
using GdeOni.Application.DeceasedRecords.Queries.HasMemories.UseCase;
using GdeOni.Application.Legal.Commands.AcceptLegal.UseCase;
using GdeOni.Application.Legal.Queries.GetLegalDocument.UseCase;
using GdeOni.Application.Admin.Queries.GetAdminStats.UseCase;
using GdeOni.Application.Events.Queries.GetHolidays.UseCase;
using GdeOni.Application.Geo.Queries.ReverseGeocode.UseCase;
using GdeOni.Application.Routing.Queries.GetRouteToGrave.UseCase;
using GdeOni.Application.Subscriptions.Commands.CancelSubscription.UseCase;
using GdeOni.Application.Subscriptions.Commands.CreatePayment.UseCase;
using GdeOni.Application.Subscriptions.Commands.ProcessPaymentWebhook.UseCase;
using GdeOni.Application.Subscriptions.Commands.SyncSubscription.UseCase;
using GdeOni.Application.Subscriptions.Commands.CancelPendingPayment.UseCase;
using GdeOni.Application.Subscriptions.Queries.GetAdminPayments.UseCase;
using GdeOni.Application.Subscriptions.Queries.GetMyPayments.UseCase;
using GdeOni.Application.Subscriptions.Queries.GetMySubscription.UseCase;
using GdeOni.Application.Support.Commands.AcceptResolution.UseCase;
using GdeOni.Application.Support.Commands.CopyAttachmentToDeceasedMedia.UseCase;
using GdeOni.Application.Support.Commands.Create.UseCase;
using GdeOni.Application.Support.Commands.CreateWithAttachments.UseCase;
using GdeOni.Application.Support.Commands.Reopen.UseCase;
using GdeOni.Application.Support.Queries.GetAttachmentById.UseCase;
using GdeOni.Application.Support.Commands.UpdateSeverity.UseCase;
using GdeOni.Application.Support.Commands.ForceClose.UseCase;
using GdeOni.Application.Support.Commands.UpdateStatus.UseCase;
using GdeOni.Application.Support.Queries.GetAll.UseCase;
using GdeOni.Application.Support.Queries.GetById.UseCase;
using GdeOni.Application.Support.Queries.GetMine.UseCase;
using GdeOni.Application.Users.Commands.ChangeEmail.UseCase;
using GdeOni.Application.Users.Commands.ChangePassword.UseCase;
using GdeOni.Application.Users.Commands.ChangeRole.UseCase;
using GdeOni.Application.Users.Commands.Block.UseCase;
using GdeOni.Application.Users.Commands.Delete.UseCase;
using GdeOni.Application.Users.Commands.Register.UseCase;
using GdeOni.Application.Users.Commands.Unblock.UseCase;
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
        // TimeProvider.System — единственный источник "сейчас" в use case'ах
        // и валидаторах. В тестах подменяется на FakeTimeProvider, в проде —
        // системные часы. Заменяет россыпь DateTime.UtcNow по слою.
        services.AddSingleton(TimeProvider.System);

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

        // D24. Collaborative editing
        services.AddScoped<ICanEditDeceasedPolicy, CanEditDeceasedPolicy>();
        services.AddScoped<IUpdateMainInfoByEditorUseCase, UpdateMainInfoByEditorUseCase>();
        services.AddScoped<IUpdateMetadataByEditorUseCase, UpdateMetadataByEditorUseCase>();
        services.AddScoped<IUpdateBurialLocationByEditorUseCase, UpdateBurialLocationByEditorUseCase>();
        services.AddScoped<IGetDeceasedEditsUseCase, GetDeceasedEditsUseCase>();
        services.AddScoped<IGetAllEditsUseCase, GetAllEditsUseCase>();
        services.AddScoped<IDeleteDeceasedUseCase, DeleteDeceasedUseCase>();
        services.AddScoped<IDeleteUserUseCase, DeleteUserUseCase>();

        // F17.10. Блокировка пользователя — мягкая альтернатива удалению:
        // данные сохраняются, доступ закрыт. SecurityStamp ротируется в
        // Block() — заблокированный юзер вылетает мгновенно.
        services.AddScoped<IBlockUserUseCase, BlockUserUseCase>();
        services.AddScoped<IUnblockUserUseCase, UnblockUserUseCase>();

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
        services.AddScoped<IDownloadMediaUseCase, DownloadMediaUseCase>();
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

        // События: справочник праздников (вычисляемый, без БД).
        services.AddScoped<IGetHolidaysUseCase, GetHolidaysUseCase>();

        // F38. Справка по системе для админа (счётчики).
        services.AddScoped<IGetAdminStatsUseCase, GetAdminStatsUseCase>();

        // D41. Обратное геокодирование: координаты → город.
        services.AddScoped<IReverseGeocodeUseCase, ReverseGeocodeUseCase>();

        // D16. Subscription use cases.
        services.AddScoped<IGetMySubscriptionUseCase, GetMySubscriptionUseCase>();
        services.AddScoped<ICreatePaymentUseCase, CreatePaymentUseCase>();
        services.AddScoped<ICancelSubscriptionUseCase, CancelSubscriptionUseCase>();
        services.AddScoped<IProcessPaymentWebhookUseCase, ProcessPaymentWebhookUseCase>();
        // Pull-fallback вместо webhook: работает и когда localhost
        // недоступен снаружи (dev), и как safety-net в проде.
        services.AddScoped<ISyncSubscriptionUseCase, SyncSubscriptionUseCase>();
        // Юзер тапнул «Отменить» на PendingPayment (например, нажал
        // «Назад» на странице YooKassa) — сбрасывает Pending платёж
        // и возвращает подписку в Trial/Expired.
        services.AddScoped<ICancelPendingPaymentUseCase, CancelPendingPaymentUseCase>();
        // D23. Payments history use cases.
        services.AddScoped<IGetMyPaymentsUseCase, GetMyPaymentsUseCase>();
        services.AddScoped<IGetAdminPaymentsUseCase, GetAdminPaymentsUseCase>();

        // D19. Legal use cases (Privacy / Terms / 152-ФЗ).
        services.AddScoped<IAcceptLegalUseCase, AcceptLegalUseCase>();
        services.AddScoped<IGetLegalDocumentUseCase, GetLegalDocumentUseCase>();

        // D22. Complimentary access (admin granted free access).
        services.AddScoped<IGrantComplimentaryAccessUseCase, GrantComplimentaryAccessUseCase>();
        services.AddScoped<IRevokeComplimentaryAccessUseCase, RevokeComplimentaryAccessUseCase>();
        services.AddScoped<IRevokeSubscriptionByAdminUseCase, RevokeSubscriptionByAdminUseCase>();
        services.AddScoped<IRestartTrialByAdminUseCase, RestartTrialByAdminUseCase>();
        services.AddScoped<IGetUserTrackedDeceasedForAdminUseCase, GetUserTrackedDeceasedForAdminUseCase>();
        services.AddScoped<IAdminRemoveUserTrackingUseCase, AdminRemoveUserTrackingUseCase>();
        services.AddScoped<IAdminRemoveAllUserTrackingUseCase, AdminRemoveAllUserTrackingUseCase>();

        // D25. Универсальные обращения в поддержку: юзер заполняет
        // форму или backend заводит auto-инцидент. Админка фильтрует
        // и меняет статус/severity.
        services.AddScoped<ICreateSupportTicketUseCase, CreateSupportTicketUseCase>();
        services.AddScoped<IGetMySupportTicketsUseCase, GetMySupportTicketsUseCase>();
        services.AddScoped<IGetSupportTicketByIdUseCase, GetSupportTicketByIdUseCase>();
        services.AddScoped<IGetAllSupportTicketsUseCase, GetAllSupportTicketsUseCase>();
        services.AddScoped<IUpdateSupportTicketStatusUseCase, UpdateSupportTicketStatusUseCase>();
        // D40. Принудительное закрытие обращения админом.
        services.AddScoped<IForceCloseSupportTicketUseCase, ForceCloseSupportTicketUseCase>();
        services.AddScoped<IUpdateSupportTicketSeverityUseCase, UpdateSupportTicketSeverityUseCase>();
        services.AddScoped<IAcceptSupportTicketResolutionUseCase, AcceptSupportTicketResolutionUseCase>();
        services.AddScoped<IReopenSupportTicketUseCase, ReopenSupportTicketUseCase>();

        // D33. Тикет с вложениями (multipart) + скачивание вложений.
        services.AddScoped<
            ICreateSupportTicketWithAttachmentsUseCase,
            CreateSupportTicketWithAttachmentsUseCase>();
        services.AddScoped<
            IGetSupportAttachmentByIdUseCase,
            GetSupportAttachmentByIdUseCase>();

        // D35. Универсальное копирование вложения тикета в media умершего:
        // главное фото / галерея / фото могилы / документ.
        services.AddScoped<
            ICopyAttachmentToDeceasedMediaUseCase,
            CopyAttachmentToDeceasedMediaUseCase>();

        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        return services;
    }
}