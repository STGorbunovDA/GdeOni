using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GdeOni.Infrastructure.Tests.Persistence;

/// <summary>
/// Тесты EF-маппинга через Model API. Проверяем snake_case naming,
/// owned-типы (BurialLocation в плоские колонки burial_*; Metadata
/// в jsonb), и уникальный индекс на SearchKey.
///
/// Фикстура нужна, чтобы был сконфигурированный DbContext с
/// connection-string'ом — даже без создания таблиц мы лезем в Model.
/// </summary>
[Collection(PostgresCollection.Name)]
public sealed class EfConfigurationTests
{
    private readonly PostgresFixture _fixture;

    public EfConfigurationTests(PostgresFixture fixture) => _fixture = fixture;

    /// <summary>
    /// snake_case naming convention: User.Email → колонка email.
    /// </summary>
    [Fact]
    public void User_Email_MapsToSnakeCaseColumn()
    {
        using var dbContext = _fixture.CreateDbContext();
        var entity = dbContext.Model.FindEntityType(typeof(User));

        entity.Should().NotBeNull();
        var emailProperty = entity!.FindProperty(nameof(User.Email));
        emailProperty.Should().NotBeNull();
        emailProperty!.GetColumnName().Should().Be("email");

        // Заодно проверяем UserNameNormalized → user_name_normalized.
        entity.FindProperty(nameof(User.UserNameNormalized))!
            .GetColumnName().Should().Be("user_name_normalized");
    }

    /// <summary>
    /// DeceasedConfiguration: SearchKey unique index с известным именем.
    /// Это страхует Save() — он ожидает constraint name из DbConstraints
    /// при ловле SqlState "23505".
    /// </summary>
    [Fact]
    public void Deceased_SearchKey_HasUniqueIndexWithKnownName()
    {
        using var dbContext = _fixture.CreateDbContext();
        var entity = dbContext.Model.FindEntityType(typeof(Deceased))!;

        var searchKeyProp = entity.FindProperty(nameof(Deceased.SearchKey));
        searchKeyProp.Should().NotBeNull();

        var uniqueIndex = entity.GetIndexes()
            .FirstOrDefault(i =>
                i.IsUnique &&
                i.Properties.Any(p => p.Name == nameof(Deceased.SearchKey)));

        uniqueIndex.Should().NotBeNull();
        uniqueIndex!.GetDatabaseName().Should().Be(DbConstraints.DeceasedSearchKey);
    }

    /// <summary>
    /// BurialLocation owned-type — поля распыляются в колонки
    /// без префикса (latitude, longitude, country, ...). Проверяем
    /// два ключевых поля, чтобы поймать регрессии при перевешивании
    /// owned → component.
    /// </summary>
    [Fact]
    public void BurialLocation_Owned_MapsToFlatColumns()
    {
        using var dbContext = _fixture.CreateDbContext();
        var entity = dbContext.Model.FindEntityType(typeof(Deceased))!;

        var burialNav = entity.GetNavigations().Concat<INavigationBase>(entity.GetSkipNavigations())
            .Concat(entity.FindOwnership() is null ? Enumerable.Empty<INavigationBase>() : new[] { (INavigationBase)entity.FindOwnership()! });

        var burialOwned = entity.FindNavigation(nameof(Deceased.BurialLocation))?.TargetEntityType;
        burialOwned.Should().NotBeNull();
        burialOwned!.IsOwned().Should().BeTrue();

        var lat = burialOwned.FindProperty(nameof(BurialLocation.Latitude));
        lat.Should().NotBeNull();
        lat!.GetColumnName().Should().Be("latitude");

        var country = burialOwned.FindProperty(nameof(BurialLocation.Country));
        country.Should().NotBeNull();
        country!.GetColumnName().Should().Be("country");
    }

    /// <summary>
    /// DeceasedMetadata owned + ToJson — попадает в одну jsonb-колонку.
    /// Проверяем что владелец помечен IsOwned() и колонка одна (jsonb).
    /// </summary>
    [Fact]
    public void DeceasedMetadata_Owned_MapsToJsonColumn()
    {
        using var dbContext = _fixture.CreateDbContext();
        var entity = dbContext.Model.FindEntityType(typeof(Deceased))!;

        var metadataOwned = entity.FindNavigation(nameof(Deceased.Metadata))?.TargetEntityType;
        metadataOwned.Should().NotBeNull();
        metadataOwned!.IsOwned().Should().BeTrue();

        // ToJson хранится как single jsonb-колонка с именем "metadata"
        // (snake_case naming). Если конвертировано в JSON — у entity
        // есть IsMappedToJson == true (EF Core 8+).
        metadataOwned.IsMappedToJson().Should().BeTrue();
    }
}
