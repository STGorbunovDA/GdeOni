using GdeOni.API.Extensions;
using GdeOni.API.Models.Users;
using GdeOni.API.Response;
using GdeOni.Application.Common.Shared;
using GdeOni.Application.Users.Commands.ChangeEmail.Model;
using GdeOni.Application.Users.Commands.ChangeEmail.UseCase;
using GdeOni.Application.Users.Commands.ChangePassword.Model;
using GdeOni.Application.Users.Commands.ChangePassword.UseCase;
using GdeOni.Application.Users.Commands.ChangeRole.Model;
using GdeOni.Application.Users.Commands.ChangeRole.UseCase;
using GdeOni.Application.Users.Commands.Delete.Model;
using GdeOni.Application.Users.Commands.Delete.UseCase;
using GdeOni.Application.Users.Commands.Register.Model;
using GdeOni.Application.Users.Commands.Register.UseCase;
using GdeOni.Application.Users.Commands.UpdateProfile.Model;
using GdeOni.Application.Users.Commands.UpdateProfile.UseCase;
using GdeOni.Application.Users.Queries.GetAll.Model;
using GdeOni.Application.Users.Queries.GetAll.UseCase;
using GdeOni.Application.Users.Queries.GetById.Model;
using GdeOni.Application.Users.Queries.GetById.UseCase;
using GdeOni.Application.Users.Queries.GetCurrent.Model;
using GdeOni.Application.Users.Queries.GetCurrent.UseCase;
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
    /// Регистрация нового пользователя.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserRequest request,
        [FromServices] IRegisterUserUseCase registerUserUseCase,
        CancellationToken cancellationToken)
    {
        var command = new RegisterUserCommand(
            request.Email,
            request.UserName,
            request.FullName,
            request.Password);

        var result = await registerUserUseCase.Execute(command, cancellationToken);

        return FromResult(
            result,
            value => value.ToCreatedResponse($"/api/users/{value.Id}"));
    }

    /// <summary>
    /// Получение профиля текущего пользователя по access token.
    /// Идентификатор пользователя берётся только из JWT.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetCurrentUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrent(
        [FromServices] IGetCurrentUserUseCase getCurrentUserUseCase,
        CancellationToken cancellationToken)
    {
        var result = await getCurrentUserUseCase.Execute(cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Получение списка всех пользователей (с пагинацией и фильтрацией).
    /// Доступно только администраторам.
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<GetAllUsersResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAll(
        [FromQuery] GetAllUsersRequest request,
        [FromServices] IGetAllUsersUseCase getAllUsersUseCase,
        CancellationToken cancellationToken)
    {
        var query = new GetAllUsersQuery(
            request.Search,
            request.Role,
            request.RegisteredAtUtc,
            request.LastLoginAtUtc,
            request.Page,
            request.PageSize);

        var result = await getAllUsersUseCase.Execute(query, cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Получение информации о конкретном пользователе по идентификатору.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<GetUserByIdResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        [FromServices] IGetUserByIdUseCase getUserByIdUseCase,
        CancellationToken cancellationToken)
    {
        var query = new GetUserByIdQuery(id);
        var result = await getUserByIdUseCase.Execute(query, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Обновление профиля пользователя.
    /// Доступен текущему пользователю или администратору.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateUserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateProfile(
        [FromRoute] Guid id,
        [FromBody] UpdateUserProfileRequest request,
        [FromServices] IUpdateUserProfileUseCase updateUserProfileUseCase,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserProfileCommand(id, request.UserName, request.FullName);
        var result = await updateUserProfileUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Изменение пароля пользователя.
    /// Доступен текущему пользователю или администратору.
    /// </summary>
    [HttpPut("{id:guid}/password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangePassword(
        [FromRoute] Guid id,
        [FromBody] ChangePasswordRequest request,
        [FromServices] IChangePasswordUseCase changePasswordUseCase,
        CancellationToken cancellationToken)
    {
        var command = new ChangePasswordCommand(id, request.CurrentPassword, request.NewPassword);
        var result = await changePasswordUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Изменение email пользователя.
    /// Доступен текущему пользователю или администратору.
    /// </summary>
    [HttpPut("{id:guid}/email")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ChangeEmailResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ChangeEmail(
        [FromRoute] Guid id,
        [FromBody] ChangeEmailRequest request,
        [FromServices] IChangeEmailUseCase changeEmailUseCase,
        CancellationToken cancellationToken)
    {
        var command = new ChangeEmailCommand(id, request.Email);
        var result = await changeEmailUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Изменение роли пользователя.
    /// Доступно только администраторам.
    /// </summary>
    [HttpPut("{id:guid}/role")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ChangeRoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeRole(
        [FromRoute] Guid id,
        [FromBody] ChangeRoleRequest request,
        [FromServices] IChangeRoleUseCase changeRoleUseCase,
        CancellationToken cancellationToken)
    {
        var command = new ChangeRoleCommand(id, request.UserRole);
        var result = await changeRoleUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }

    /// <summary>
    /// Удаление пользователя.
    /// Доступно только администраторам.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        [FromServices] IDeleteUserUseCase deleteUserUseCase,
        CancellationToken cancellationToken)
    {
        var command = new DeleteUserCommand(id);
        var result = await deleteUserUseCase.Execute(command, cancellationToken);

        return FromResult(result);
    }
}