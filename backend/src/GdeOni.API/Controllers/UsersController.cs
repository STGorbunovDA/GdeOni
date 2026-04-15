using GdeOni.API.Extensions;
using GdeOni.API.Models;
using GdeOni.API.Response;
using GdeOni.Application.Common.Security;
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
    /// <param name="command"></param>
    /// <param name="registerUserUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<RegisterUserResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserCommand command,
        [FromServices] IRegisterUserUseCase registerUserUseCase,
        CancellationToken cancellationToken)
    {
        var result = await registerUserUseCase.Execute(command, cancellationToken);

        return FromResult(
            result,
            value => Created($"/api/users/{value.Id}",
                ApiResponse<RegisterUserResponse>.Success(value)));
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
    [ProducesResponseType(typeof(ApiResponse<GetUserByIdResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IGetUserByIdUseCase getUserByIdUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(id, currentUserService.UserId, isAdmin))
            return Forbid();
        
        var result = await getUserByIdUseCase.Execute(new GetUserByIdQuery(id), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Обновить профиль пользователя
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto"></param>
    /// <param name="currentUserService"></param>
    /// <param name="updateUserProfileUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPatch("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UpdateUserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UpdateUserProfileResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateProfile(
        [FromRoute] Guid id,
        [FromBody] UpdateUserProfileDto dto,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IUpdateUserProfileUseCase updateUserProfileUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(id, currentUserService.UserId, isAdmin))
            return Forbid();

        var result = await updateUserProfileUseCase.Execute(
            new UpdateUserProfileCommand(id, dto.UserName, dto.FullName), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Изменить пароль
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto"></param>
    /// <param name="currentUserService"></param>
    /// <param name="changePasswordUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ChangePasswordResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangePassword(
        [FromRoute] Guid id,
        [FromBody] ChangePasswordDto dto,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IChangePasswordUseCase changePasswordUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(id, currentUserService.UserId, isAdmin))
            return Forbid();
        
        var result = await changePasswordUseCase.Execute(
            new ChangePasswordCommand(id, dto.CurrentPassword, dto.NewPassword), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Изменить роль
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto"></param>
    /// <param name="changeRoleUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/role")]
    [Authorize(Roles = "SuperAdmin,Admin")]
    [ProducesResponseType(typeof(ApiResponse<ChangeRoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeRole(
        [FromRoute] Guid id,
        [FromBody] ChangeRoleDto dto,
        [FromServices] IChangeRoleUseCase changeRoleUseCase,
        CancellationToken cancellationToken)
    {
        var result = await changeRoleUseCase.Execute(new ChangeRoleCommand(id, dto.UserRole), cancellationToken);
        return FromResult(result);
    }

    /// <summary>
    /// Изменить почту
    /// </summary>
    /// <param name="id"></param>
    /// <param name="dto"></param>
    /// <param name="currentUserService"></param>
    /// <param name="changeEmailUseCase"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPut("{id:guid}/email")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<ChangeEmailResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ChangeEmail(
        [FromRoute] Guid id,
        [FromBody] ChangeEmailDto dto,
        [FromServices] ICurrentUserService currentUserService,
        [FromServices] IChangeEmailUseCase changeEmailUseCase,
        CancellationToken cancellationToken)
    {
        var isAdmin = currentUserService.IsInRole("SuperAdmin", "Admin");

        if (!CanAccessUserResource(id, currentUserService.UserId, isAdmin))
            return Forbid();
        
        var result = await changeEmailUseCase.Execute(new ChangeEmailCommand(id, dto.Email), cancellationToken);
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
    [ProducesResponseType(typeof(ApiResponse<DeleteUserResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<DeleteUserResponse>), StatusCodes.Status404NotFound)]
    [Authorize(Roles = "SuperAdmin, Admin")]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        [FromServices] IDeleteUserUseCase deleteUserUseCase,
        CancellationToken cancellationToken)
    {
        var result = await deleteUserUseCase.Execute(new DeleteUserCommand(id), cancellationToken);

        if (result.IsFailure)
            return result.Error.ToErrorResponse<object>();

        return FromResult(result);
    }
}