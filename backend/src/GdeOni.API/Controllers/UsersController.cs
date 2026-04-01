using GdeOni.API.Extensions;
using GdeOni.API.Response;
using GdeOni.Application.Common.Security;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.Users.ActivateTracking.UseCase;
using GdeOni.Application.Users.ChangeEmail.Model;
using GdeOni.Application.Users.ChangeEmail.UseCase;
using GdeOni.Application.Users.ChangePassword.Model;
using GdeOni.Application.Users.ChangePassword.UseCase;
using GdeOni.Application.Users.ChangeUserRole.Model;
using GdeOni.Application.Users.ChangeUserRole.UseCase;
using GdeOni.Application.Users.Create.Model;
using GdeOni.Application.Users.Create.UseCase;
using GdeOni.Application.Users.Delete.UseCase;
using GdeOni.Application.Users.GetAll.Model;
using GdeOni.Application.Users.GetAll.UseCase;
using GdeOni.Application.Users.GetById.Model;
using GdeOni.Application.Users.GetById.UseCase;
using GdeOni.Application.Users.GetTrackedDeceased.Model;
using GdeOni.Application.Users.GetTrackedDeceased.UseCase;
using GdeOni.Application.Users.GetTracking.Model;
using GdeOni.Application.Users.GetTracking.UseCase;
using GdeOni.Application.Users.HasNotificationsEnabled.Model;
using GdeOni.Application.Users.HasNotificationsEnabled.UseCase;
using GdeOni.Application.Users.IsTracking.Model;
using GdeOni.Application.Users.IsTracking.UseCase;
using GdeOni.Application.Users.MuteTracking.UseCase;
using GdeOni.Application.Users.RemoveTracking.Model;
using GdeOni.Application.Users.RemoveTracking.UseCase;
using GdeOni.Application.Users.StopTracking.UseCase;
using GdeOni.Application.Users.TrackDeceased.Model;
using GdeOni.Application.Users.TrackDeceased.UseCase;
using GdeOni.Application.Users.UpdateProfile.Model;
using GdeOni.Application.Users.UpdateProfile.UseCase;
using GdeOni.Application.Users.UpdateTracking.Model;
using GdeOni.Application.Users.UpdateTracking.UseCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GdeOni.API.Controllers;

/// <summary>
/// Пользователи
/// </summary>
[Route("api/users")]
public sealed class UsersController : ApiControllerBase
{
    /// <summary>
    /// Создать пользователя
    /// </summary>
    /// <param name="request"></param>
    /// <param name="createUserUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CreateUserResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        [FromServices] ICreateUserUseCase createUserUseCase,
        CancellationToken cancellationToken)
    {
        var result = await createUserUseCase.Execute(request, cancellationToken);

        return FromResult(
            result,
            value => Created($"/api/users/{value.Id}", ApiResponse<CreateUserResponse>.Success(value)));
    }

    /// <summary>
    /// Получить всех пользователей
    /// </summary>
    /// <param name="query"></param>
    /// <param name="getAllUsersUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<GetAllUsersResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<GetAllUsersResponse>>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAllUsersQuery query,
        [FromServices] IGetAllUsersUseCase getAllUsersUseCase,
        CancellationToken cancellationToken)
    {
        var result = await getAllUsersUseCase.Execute(query, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Получить пользователя по Id
    /// </summary>
    /// <param name="id"></param>
    /// <param name="currentUserService"></param>
    /// <param name="getUserByIdUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetUserByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IGetUserByIdUseCase getUserByIdUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(id, currentUserService.UserId, isAdmin))
            return Forbid();

        var result = await getUserByIdUseCase.Execute(id, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Обновить профиль пользователя
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <param name="currentUserService"></param>
    /// <param name="updateUserProfileUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateUserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateProfile(
        Guid id,
        [FromBody] UpdateUserProfileRequest request,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IUpdateUserProfileUseCase updateUserProfileUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(id, currentUserService.UserId, isAdmin))
            return Forbid();

        request.UserId = id;
        var result = await updateUserProfileUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Изменить пароль
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <param name="changePasswordUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/password")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword(
        Guid id,
        [FromBody] ChangePasswordRequest request,
        [FromServices] IChangePasswordUseCase changePasswordUseCase,
        CancellationToken cancellationToken)
    {
        request.UserId = id;
        var result = await changePasswordUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Изменить роль
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <param name="changeUserRoleUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/userRole")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ChangeUserRoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeUserRole(
        Guid id,
        [FromBody] ChangeUserRoleRequest request,
        [FromServices] IChangeUserRoleUseCase changeUserRoleUseCase,
        CancellationToken cancellationToken)
    {
        request.UserId = id;
        var result = await changeUserRoleUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Изменить почту
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <param name="changeEmailUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/email")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ChangeEmailResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeEmail(
        Guid id,
        [FromBody] ChangeEmailRequest request,
        [FromServices] IChangeEmailUseCase changeEmailUseCase,
        CancellationToken cancellationToken)
    {
        request.UserId = id;
        var result = await changeEmailUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Удалить пользователя
    /// </summary>
    /// <param name="id"></param>
    /// <param name="deleteUserUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Authorize(Roles = "SuperAdmin, Admin")]
    public async Task<IActionResult> Delete(
        Guid id,
        [FromServices] IDeleteUserUseCase deleteUserUseCase,
        CancellationToken cancellationToken)
    {
        var result = await deleteUserUseCase.Execute(id, cancellationToken);

        if (result.IsFailure)
            return result.Error.ToErrorResponse<object>();

        return NoContent();
    }

    /// <summary>
    /// Получить отслеживаемых умерших у пользователя
    /// </summary>
    /// <param name="id"></param>
    /// <param name="currentUserService"></param>
    /// <param name="getTrackedDeceasedUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpGet("{id:guid}/tracked-deceased")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetTrackedDeceasedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetTrackedDeceased(
        Guid id,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IGetTrackedDeceasedUseCase getTrackedDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(id, currentUserService.UserId, isAdmin))
            return Forbid();

        var result = await getTrackedDeceasedUseCase.Execute(id, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Добавить умершего для отслеживания пользователю
    /// </summary>
    /// <param name="id"></param>
    /// <param name="request"></param>
    /// <param name="currentUserService"></param>
    /// <param name="trackDeceasedUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("{id:guid}/tracked-deceased")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<TrackDeceasedResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> TrackDeceased(
        Guid id,
        [FromBody] TrackDeceasedRequest request,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] ITrackDeceasedUseCase trackDeceasedUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(id, currentUserService.UserId, isAdmin))
            return Forbid();

        request.UserId = id;
        var result = await trackDeceasedUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Обновить отслеживание умершего для пользователя
    /// </summary>
    /// <param name="id"></param>
    /// <param name="deceasedId"></param>
    /// <param name="request"></param>
    /// <param name="currentUserService"></param>
    /// <param name="updateTrackingUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/tracked-deceased/{deceasedId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateTracking(
        Guid id,
        Guid deceasedId,
        [FromBody] UpdateTrackingRequest request,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IUpdateTrackingUseCase updateTrackingUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(id, currentUserService.UserId, isAdmin))
            return Forbid();

        request.UserId = id;
        request.DeceasedId = deceasedId;

        var result = await updateTrackingUseCase.Execute(request, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Остановить отслеживание умершего для пользователя
    /// </summary>
    /// <param name="id"></param>
    /// <param name="deceasedId"></param>
    /// <param name="currentUserService"></param>
    /// <param name="stopTrackingUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpDelete("{id:guid}/tracked-deceased/{deceasedId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> StopTracking(
        Guid id,
        Guid deceasedId,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IStopTrackingUseCase stopTrackingUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(id, currentUserService.UserId, isAdmin))
            return Forbid();

        var result = await stopTrackingUseCase.Execute(id, deceasedId, cancellationToken);

        if (result.IsFailure)
            return result.Error.ToErrorResponse<object>();

        return NoContent();
    }

    /// <summary>
    /// Отключить уведомления отслеживания умершего у пользователя
    /// </summary>
    /// <param name="id"></param>
    /// <param name="deceasedId"></param>
    /// <param name="currentUserService"></param>
    /// <param name="muteTrackingUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/tracked-deceased/{deceasedId:guid}/mute")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> MuteTracking(
        Guid id,
        Guid deceasedId,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IMuteTrackingUseCase muteTrackingUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(id, currentUserService.UserId, isAdmin))
            return Forbid();

        var result = await muteTrackingUseCase.Execute(id, deceasedId, cancellationToken);

        if (result.IsFailure)
            return result.Error.ToErrorResponse<object>();

        return NoContent();
    }

    /// <summary>
    /// Активировать отслеживание умерщего у пользователя
    /// </summary>
    /// <param name="id"></param>
    /// <param name="deceasedId"></param>
    /// <param name="currentUserService"></param>
    /// <param name="activateTrackingUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/tracked-deceased/{deceasedId:guid}/activate")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ActivateTracking(
        Guid id,
        Guid deceasedId,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IActivateTrackingUseCase activateTrackingUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(id, currentUserService.UserId, isAdmin))
            return Forbid();

        var result = await activateTrackingUseCase.Execute(id, deceasedId, cancellationToken);

        if (result.IsFailure)
            return result.Error.ToErrorResponse<object>();

        return NoContent();
    }
    
    /// <summary>
    /// Получить конкретное отслеживание пользователя
    /// </summary>
    [HttpGet("{id:guid}/tracked-deceased/{deceasedId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTracking(
        Guid id,
        Guid deceasedId,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IGetTrackingUseCase getTrackingUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(id, currentUserService.UserId, isAdmin))
            return Forbid();

        var result = await getTrackingUseCase.Execute(id, deceasedId, cancellationToken);
        return FromResult(result);
    }
    
    /// <summary>
    /// Проверить, отслеживает ли пользователь указанного deceased
    /// </summary>
    [HttpGet("{id:guid}/tracked-deceased/{deceasedId:guid}/is-tracking")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<IsTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> IsTracking(
        Guid id,
        Guid deceasedId,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IIsTrackingUseCase isTrackingUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(id, currentUserService.UserId, isAdmin))
            return Forbid();

        var result = await isTrackingUseCase.Execute(id, deceasedId, cancellationToken);
        return FromResult(result);
    }
    
    /// <summary>
    /// Полностью удалить отслеживание пользователя
    /// </summary>
    [HttpDelete("{id:guid}/tracked-deceased/{deceasedId:guid}/hard")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<RemoveTrackingResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTracking(
        Guid id,
        Guid deceasedId,
        [FromServices] IRemoveTrackingUseCase removeTrackingUseCase,
        CancellationToken cancellationToken)
    {
        var result = await removeTrackingUseCase.Execute(id, deceasedId, cancellationToken);
        return FromResult(result);
    }
    
    /// <summary>
    /// Проверить, включены ли уведомления у отслеживания пользователя
    /// </summary>
    [HttpGet("{id:guid}/tracked-deceased/{deceasedId:guid}/has-notifications-enabled")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<HasNotificationsEnabledResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> HasNotificationsEnabled(
        Guid id,
        Guid deceasedId,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IHasNotificationsEnabledUseCase hasNotificationsEnabledUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(id, currentUserService.UserId, isAdmin))
            return Forbid();

        var result = await hasNotificationsEnabledUseCase.Execute(id, deceasedId, cancellationToken);
        return FromResult(result);
    }
}