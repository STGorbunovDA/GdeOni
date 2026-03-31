using GdeOni.Domain.Shared;

namespace GdeOni.API.Extensions;

public static class EnumTypeHelper
{
    public static UserRole СonversionUserRole (string extension) => extension.ToLower() switch
    {
        "superadmin" => UserRole.SuperAdmin,
        "admin" => UserRole.Admin,
        "manager" => UserRole.Manager,
        "regularuser" => UserRole.RegularUser,
        _ => UserRole.Unknown
    };
}