namespace GdeOni.Domain.Shared;

// Partial-split от Errors.cs (см. D11.x): один god-файл на 800+ строк
// делал PR-ревью неудобным. Каждый bounded context в своём файле.
public static partial class Errors
{
    public static class User
    {
        public static Error IdRequired() =>
            Error.Validation("user.id.required", "User id is required");

        public static Error EmailRequired() =>
            Error.Validation("user.email.required", "Email is required");

        public static Error EmailInvalid() =>
            Error.Validation("user.email.invalid", "Email is invalid");

        public static Error EmailTooLong(int maxLength) =>
            Error.Validation("user.email.too_long", $"Email must be at most {maxLength} characters");

        public static Error UserNameRequired() =>
            Error.Validation("user.user_name.required", "User name is required");

        public static Error UserNameTooLong(int maxLength) =>
            Error.Validation("user.user_name.too_long", $"User name must be at most {maxLength} characters");

        public static Error FullNameTooLong(int maxLength) =>
            Error.Validation("user.full_name.too_long", $"Full name must be at most {maxLength} characters");

        public static Error PasswordHashRequired() =>
            Error.Validation("user.password_hash.required", "Password hash is required");

        public static Error PasswordRequired() =>
            Error.Validation("user.password.required", "Password is required");

        public static Error PasswordTooShort(int minLength) =>
            Error.Validation("user.password.too_short", $"Password must be at least {minLength} characters long");

        public static Error PasswordTooLong(int maxLength) =>
            Error.Validation("user.password.too_long", $"Password must be at most {maxLength} characters long");

        public static Error EmailAlreadyExists() =>
            Error.Conflict("user.email.already.exists", "User with this email already exists");

        public static Error UserNameAlreadyExists() =>
            Error.Conflict("user.user_name.already.exists", "User with this user name already exists");

        public static Error InvalidCredentials() =>
            Error.Unauthorized("user.invalid.credentials", "Invalid email or password");

        public static Error CurrentPasswordInvalid() =>
            Error.Unauthorized("user.current_password.invalid", "Current password is invalid");

        /// <summary>
        /// D43. Токен сброса не найден, не совпал или уже использован.
        /// Намеренно не отличается от «токен чужой» — по тексту ошибки
        /// не должно быть видно, существует ли такой токен вообще.
        /// </summary>
        public static Error PasswordResetTokenInvalid() =>
            Error.Unauthorized("user.password_reset_token.invalid", "Password reset link is invalid");

        /// <summary>D43. Срок действия ссылки из письма истёк.</summary>
        public static Error PasswordResetTokenExpired() =>
            Error.Unauthorized("user.password_reset_token.expired", "Password reset link has expired");

        public static Error RoleInvalid() =>
            Error.Validation("user.role.invalid", "User role is invalid");

        public static Error BirthDateRequired() =>
            Error.Validation("user.birth_date.required", "Birth date is required");

        public static Error BirthDateInvalid() =>
            Error.Validation("user.birth_date.invalid", "Birth date is invalid (cannot be in the future)");

        public static Error BirthDateMinAgeNotMet(int minAge) =>
            Error.Validation(
                "user.birth_date.min_age",
                $"You must be at least {minAge} years old to use the service");

        public static Error UserForbidden() =>
            Error.Forbidden("user.forbidden", "You do not have permission to access the current user.");

        public static Error ChangeSuperAdminRoleForbidden() =>
            Error.Forbidden(
                "user.role.change.super_admin.forbidden",
                "Only a SuperAdmin can change the role of another SuperAdmin.");

        public static Error ChangePeerAdminRoleForbidden() =>
            Error.Forbidden(
                "user.role.change.peer_admin.forbidden",
                "An Admin cannot change the role of another Admin. Only a SuperAdmin can.");

        public static Error AssignAdminRoleForbidden() =>
            Error.Forbidden(
                "user.role.assign.admin.forbidden",
                "An Admin cannot assign Admin or SuperAdmin roles. Only a SuperAdmin can.");

        public static Error DeleteSuperAdminForbidden() =>
            Error.Forbidden(
                "user.delete.super_admin.forbidden",
                "SuperAdmin cannot be deleted.");

        public static Error DeleteSelfForbidden() =>
            Error.Forbidden(
                "user.delete.self.forbidden",
                "You cannot delete your own account.");

        public static Error DeleteHasContent() =>
            Error.Conflict(
                "user.delete.has_content",
                "User created deceased records or uploaded media. " +
                "Reassign or delete that content before removing the user.");

        public static Error DeletePeerAdminForbidden() =>
            Error.Forbidden(
                "user.delete.peer_admin.forbidden",
                "An Admin cannot delete another Admin. Only a SuperAdmin can.");

        public static Error AdminIdRequired() =>
            Error.Validation("user.admin_id.required", "Admin id is required.");

        public static Error BlockSelfForbidden() =>
            Error.Forbidden("user.block.self.forbidden", "You cannot block your own account.");

        public static Error BlockSuperAdminForbidden() =>
            Error.Forbidden(
                "user.block.super_admin.forbidden",
                "Only a SuperAdmin can block another SuperAdmin (and currently it is disabled).");

        public static Error BlockPeerAdminForbidden() =>
            Error.Forbidden(
                "user.block.peer_admin.forbidden",
                "An Admin cannot block another Admin. Only a SuperAdmin can.");

        public static Error BlockReasonTooLong(int max) =>
            Error.Validation("user.block.reason.too_long",
                $"Block reason must not exceed {max} characters.");

        public static Error AccountBlocked(string? reason) =>
            Error.Forbidden(
                "user.account.blocked",
                reason is null
                    ? "Your account is blocked. Contact support."
                    : $"Your account is blocked. Reason: {reason}");

        public static Error RoleUnknownNotAllowed() =>
            Error.Validation("user.role.unknown.not_allowed", "The role cannot be Unknown");

        public static Error RoleSuperAdminNotAllowed() =>
            Error.Validation("user.role.super_admin.not_allowed", "The SuperAdmin role cannot be assigned");

        public static Error SearchTooLong(int maxLength) =>
            Error.Validation("user.search.too_long", $"Search must be at most {maxLength} characters");

        public static Error RegisteredAtUtcInFuture() =>
            Error.Validation("user.registered_at_utc.in_future", "RegisteredAtUtc cannot be in the future");

        public static Error LastLoginAtUtcInFuture() =>
            Error.Validation("user.last_login_at_utc.in_future", "LastLoginAtUtc cannot be in the future");

        public static Error InsufficientPermissionsToViewAllUsers() =>
            Error.Forbidden("user.insufficient_permissions.view_all",
                "You don't have permission to view all users. Admin or SuperAdmin rights are required.");
    }
}
