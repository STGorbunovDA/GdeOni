using CSharpFunctionalExtensions;
using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Application.Abstractions.Validation;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Users.Queries.GetById.Model;
using GdeOni.Domain.Shared;

namespace GdeOni.Application.Users.Queries.GetById.UseCase;

public sealed class GetUserByIdUseCase(
    IUserRepository userRepository,
    ICurrentUserService currentUserService,
    IValidatedUseCaseExecutor validatedUseCaseExecutor)
    : IGetUserByIdUseCase
{
    public Task<Result<GetUserByIdResponse, Error>> Execute(
        GetUserByIdQuery query,
        CancellationToken cancellationToken)
    {
        return validatedUseCaseExecutor.Execute(query, Handle, cancellationToken);
    }

    private async Task<Result<GetUserByIdResponse, Error>> Handle(
        GetUserByIdQuery query,
        CancellationToken cancellationToken)
    {
        var currentUserIdResult = currentUserService.GetCurrentUserId();
        if (currentUserIdResult.IsFailure)
            return currentUserIdResult.Error;

        var currentUserId = currentUserIdResult.Value;
        var isAdmin = currentUserService.IsAdmin();

        // D7.37: вместо Include(TrackedDeceasedItems) — узкая проекция
        // (User, COUNT subquery). Раньше тянули всю коллекцию подписок
        // ради одного .Count в response.
        var row = await userRepository.GetByIdWithTrackingCount(query.UserId, cancellationToken);
        if (row is null)
            return Errors.General.NotFound("user", query.UserId);

        var (user, trackingCount) = row.Value;

        if (!isAdmin && user.Id != currentUserId)
            return Errors.User.UserForbidden();

        // SuperAdmin-аккаунты скрыты от админ-листинга ВООБЩЕ — даже
        // собственный SuperAdmin не должен видеть себя в списке /users.
        // Управление SuperAdmin-аккаунтами идёт через bootstrap-скрипт
        // или прямые запросы к БД. ВАЖНО: этот use case используется
        // также при просмотре своего профиля по userId (если фронт так
        // ходит) — но canonical путь это /me, поэтому 403 здесь
        // не сломает обычный UX.
        if (user.Role == Domain.Shared.UserRole.SuperAdmin)
            return Errors.User.UserForbidden();

        var nowUtc = DateTime.UtcNow;

        // Эффективный статус: если Status=Active/Trial/Cancelled, но
        // ExpiresAtUtc уже в прошлом — фактически Expired. Сырой Status
        // из БД может быть рассинхронен (например, ручные правки тестовых
        // данных). Админу нужен честный взгляд на текущее состояние.
        var effectiveStatus = user.Subscription.Status.ToString();
        if (user.Subscription.ExpiresAtUtc is { } expires && expires <= nowUtc &&
            user.Subscription.Status is Domain.Shared.SubscriptionStatus.Active
                or Domain.Shared.SubscriptionStatus.Trial
                or Domain.Shared.SubscriptionStatus.Cancelled
                or Domain.Shared.SubscriptionStatus.PendingPayment)
        {
            effectiveStatus = "Expired";
        }

        return Result.Success<GetUserByIdResponse, Error>(new GetUserByIdResponse
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            FullName = user.FullName,
            Role = user.Role.ToString(),
            RegisteredAtUtc = user.RegisteredAtUtc,
            LastLoginAtUtc = user.LastLoginAtUtc,
            TrackingCount = trackingCount,
            SubscriptionStatus = effectiveStatus,
            SubscriptionExpiresAtUtc = user.Subscription.ExpiresAtUtc,
            SubscriptionPlan = user.Subscription.Plan?.ToString(),
            HasComplimentaryAccess = user.HasComplimentaryAccess(nowUtc),
            ComplimentaryAccessUntilUtc = user.ComplimentaryAccessUntilUtc,
            ComplimentaryAccessNote = user.ComplimentaryAccessNote,
        });
    }
}