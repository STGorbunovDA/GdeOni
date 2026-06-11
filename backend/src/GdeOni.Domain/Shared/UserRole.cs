namespace GdeOni.Domain.Shared;

public enum UserRole
{
    // Sentinel: default(UserRole) намеренно резолвится в Unknown,
    // чтобы пропуск поля в JSON или повреждённый JWT-claim не давали
    // неожиданно SuperAdmin (см. D11.1.4).
    Unknown = 0,
    SuperAdmin = 1,
    Admin = 2,
    Manager = 3,
    RegularUser = 4
}