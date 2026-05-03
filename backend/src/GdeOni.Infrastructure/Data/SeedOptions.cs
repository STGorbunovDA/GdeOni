namespace GdeOni.Infrastructure.Data;

public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    public SuperAdminOptions? SuperAdmin { get; set; }
}

public sealed class SuperAdminOptions
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? FullName { get; set; }
    public string? UserName { get; set; }
}
