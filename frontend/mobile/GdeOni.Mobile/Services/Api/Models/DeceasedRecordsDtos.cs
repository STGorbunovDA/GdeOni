namespace GdeOni.Mobile.Services.Api.Models;

public sealed record AddDeceasedAtGraveRequest(
    string FirstName,
    string LastName,
    string? MiddleName,
    DateOnly? BirthDate,
    DateOnly DeathDate,
    string? ShortDescription,
    string? Biography,
    AddDeceasedAtGraveLocationRequest GraveLocation,
    AddDeceasedAtGraveTrackingRequest Tracking);

public sealed record AddDeceasedAtGraveLocationRequest(
    double Latitude,
    double Longitude,
    double? AccuracyMeters,
    string? Country,
    string? City,
    string? CemeteryName,
    string? PlotNumber,
    string? GraveNumber);

/// <summary>
/// Backend ожидает RelationshipType строкой (Friend, Parent, ...) благодаря
/// JsonStringEnumConverter (см. D11.15 в backend/docs/PlanFull.txt).
/// </summary>
public sealed record AddDeceasedAtGraveTrackingRequest(
    string RelationshipType,
    string? PersonalNotes,
    bool NotifyOnDeathAnniversary,
    bool NotifyOnBirthAnniversary);

public sealed record AddDeceasedAtGraveResponse(Guid DeceasedId);

/// <summary>
/// Перезаписывает координаты места захоронения существующей карточки.
/// Backend сохраняет уже введённые адресные поля (Country/City/Cemetery/
/// Plot/Grave) — этот DTO не их касается, только lat/lon/accuracy.
/// </summary>
public sealed record SetBurialLocationRequest(
    double Latitude,
    double Longitude,
    double? AccuracyMeters);

public sealed record SetBurialLocationResponse(Guid DeceasedId);

// Mobile-фасад: константы и публичные API делегируются в
// GdeOni.Mobile.Shared.Relationships.RelationshipCatalog (юнит-
// тестируется). Здесь оставлены для совместимости с XAML-биндингами
// и существующими VM, которые ссылаются на эти имена.
public static class RelationshipTypes
{
    public const string Parent = Shared.Relationships.RelationshipTypeValues.Parent;
    public const string Grandparent = Shared.Relationships.RelationshipTypeValues.Grandparent;
    public const string Child = Shared.Relationships.RelationshipTypeValues.Child;
    public const string Spouse = Shared.Relationships.RelationshipTypeValues.Spouse;
    public const string Sibling = Shared.Relationships.RelationshipTypeValues.Sibling;
    public const string Relative = Shared.Relationships.RelationshipTypeValues.Relative;
    public const string Friend = Shared.Relationships.RelationshipTypeValues.Friend;
    public const string Acquaintance = Shared.Relationships.RelationshipTypeValues.Acquaintance;
    public const string Other = Shared.Relationships.RelationshipTypeValues.Other;

    public static IReadOnlyList<RelationshipOption> All { get; } =
        Shared.Relationships.RelationshipCatalog.All
            .Select(o => new RelationshipOption(o.Value, o.Display))
            .ToArray();

    public static string Display(string? value) =>
        Shared.Relationships.RelationshipCatalog.Display(value);
}

// XAML-биндинги в формах ссылаются на этот record через
// x:DataType="models:RelationshipOption" — оставляем тип в mobile-
// namespace. Логика Display/All — в Shared (см. выше).
public sealed record RelationshipOption(string Value, string Display);

/// <summary>D24. Тело PATCH /api/deceased-records/{id}/main-info.</summary>
public sealed record UpdateMainInfoRequest(
    string FirstName,
    string LastName,
    string? MiddleName,
    DateOnly? BirthDate,
    DateOnly DeathDate,
    string? ShortDescription,
    string? Biography);

/// <summary>D24. Тело PATCH /api/deceased-records/{id}/metadata.</summary>
public sealed record UpdateMetadataRequest(
    string? Epitaph,
    string? Religion,
    string? Source,
    bool IsMilitaryService,
    string? AdditionalInfo);

/// <summary>
/// D24. Тело PATCH /api/deceased-records/{id}/burial-location.
/// Если Latitude/Longitude null — координаты удаляются.
/// Accuracy = 0 (Exact) по умолчанию.
/// </summary>
public sealed record UpdateBurialLocationRequest(
    double? Latitude,
    double? Longitude,
    double? AccuracyMeters,
    string? Country,
    string? Region,
    string? City,
    string? CemeteryName,
    string? PlotNumber,
    string? GraveNumber,
    int Accuracy = 0);

/// <summary>
/// D24. Ответ GET /api/deceased-records/{id}/edits. Kind: 1=MainInfo,
/// 2=Metadata, 3=BurialLocation. ChangesJson — словарь
/// { "FieldName": { "Old": "...", "New": "..." } } как строка.
/// </summary>
public sealed record DeceasedEditsResponse(
    IReadOnlyList<DeceasedEditDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed record DeceasedEditDto(
    Guid Id,
    DateTime EditedAtUtc,
    Guid? EditedByUserId,
    string? EditedByEmail,
    string? EditedByDisplayName,
    int Kind,
    string ChangesJson);
