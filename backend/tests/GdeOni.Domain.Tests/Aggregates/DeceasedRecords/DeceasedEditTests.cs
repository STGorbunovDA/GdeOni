using System.Text.Json;
using GdeOni.Domain.Aggregates.DeceasedRecords;

namespace GdeOni.Domain.Tests.Aggregates.DeceasedRecords;

/// <summary>
/// D24. Тесты audit log правок карточки умершего. Проверяем что:
///  - реальное изменение → запись DeceasedEdit правильного Kind'а;
///  - "то же значение" → no-op, edit не создан;
///  - editorUserId == null (старый flow) → edit не создан даже при изменении;
///  - diff содержит только реально изменённые поля.
/// </summary>
public sealed class DeceasedEditTests
{
    private static readonly Guid EditorId = Guid.NewGuid();
    private static readonly Guid CreatorId = Guid.NewGuid();

    private static Deceased CreateSample() =>
        Deceased.Create(
            firstName: "Иван",
            lastName: "Иванов",
            middleName: "Сергеевич",
            birthDate: new DateOnly(1950, 1, 1),
            deathDate: new DateOnly(2020, 5, 10),
            burialLocation: null,
            createdByUserId: CreatorId,
            shortDescription: "Дед",
            biography: null).Value;

    [Fact]
    public void UpdateMainInfo_WithEditor_RecordsEdit_WithMainInfoKind()
    {
        var deceased = CreateSample();

        var result = deceased.UpdateMainInfo(
            firstName: "Иоанн",
            lastName: "Иванов",
            middleName: "Сергеевич",
            birthDate: new DateOnly(1950, 1, 1),
            deathDate: new DateOnly(2020, 5, 10),
            shortDescription: "Дед",
            biography: null,
            editorUserId: EditorId);

        Assert.True(result.IsSuccess);
        Assert.Single(deceased.Edits);
        var edit = deceased.Edits.Single();
        Assert.Equal(DeceasedEditKind.MainInfo, edit.Kind);
        Assert.Equal(EditorId, edit.EditedByUserId);

        var changes = JsonSerializer.Deserialize<Dictionary<string, ChangePair>>(edit.ChangesJson)!;
        Assert.True(changes.ContainsKey("FirstName"));
        Assert.Equal("Иван", changes["FirstName"].Old);
        Assert.Equal("Иоанн", changes["FirstName"].New);
        Assert.False(changes.ContainsKey("LastName"));
    }

    [Fact]
    public void UpdateMainInfo_WithoutEditor_DoesNotRecordEdit()
    {
        var deceased = CreateSample();

        deceased.UpdateMainInfo(
            firstName: "Иоанн", lastName: "Иванов", middleName: "Сергеевич",
            birthDate: new DateOnly(1950, 1, 1),
            deathDate: new DateOnly(2020, 5, 10),
            shortDescription: "Дед", biography: null,
            editorUserId: null);

        Assert.Empty(deceased.Edits);
    }

    [Fact]
    public void UpdateMainInfo_SameValues_DoesNotRecordEdit()
    {
        var deceased = CreateSample();

        deceased.UpdateMainInfo(
            firstName: "Иван", lastName: "Иванов", middleName: "Сергеевич",
            birthDate: new DateOnly(1950, 1, 1),
            deathDate: new DateOnly(2020, 5, 10),
            shortDescription: "Дед", biography: null,
            editorUserId: EditorId);

        Assert.Empty(deceased.Edits);
    }

    [Fact]
    public void ChangeBurialLocation_FromNullToValue_RecordsEdit()
    {
        var deceased = CreateSample();
        var location = BurialLocation.Create(
            latitude: 55.755826, longitude: 37.617300,
            country: "Россия", city: "Москва").Value;

        deceased.ChangeBurialLocation(location, EditorId);

        Assert.Single(deceased.Edits);
        var edit = deceased.Edits.Single();
        Assert.Equal(DeceasedEditKind.BurialLocation, edit.Kind);
        var changes = JsonSerializer.Deserialize<Dictionary<string, ChangePair>>(edit.ChangesJson)!;
        Assert.True(changes.ContainsKey("Latitude"));
        Assert.True(changes.ContainsKey("Country"));
    }

    [Fact]
    public void UpdateMetadata_WithEditor_RecordsMetadataKind()
    {
        var deceased = CreateSample();
        var metadata = DeceasedMetadata.Create(
            epitaph: "Светлая память",
            religion: "Православие",
            source: null,
            isMilitaryService: true,
            additionalInfo: null).Value;

        deceased.UpdateMetadata(metadata, EditorId);

        Assert.Single(deceased.Edits);
        var edit = deceased.Edits.Single();
        Assert.Equal(DeceasedEditKind.Metadata, edit.Kind);
        var changes = JsonSerializer.Deserialize<Dictionary<string, ChangePair>>(edit.ChangesJson)!;
        Assert.True(changes.ContainsKey("Epitaph"));
        Assert.True(changes.ContainsKey("IsMilitaryService"));
    }

    [Fact]
    public void MultipleEdits_AccumulateInOrder()
    {
        var deceased = CreateSample();

        deceased.UpdateMainInfo(
            firstName: "Иоанн", lastName: "Иванов", middleName: "Сергеевич",
            birthDate: new DateOnly(1950, 1, 1),
            deathDate: new DateOnly(2020, 5, 10),
            shortDescription: "Дед", biography: null,
            editorUserId: EditorId);

        var anotherEditor = Guid.NewGuid();
        deceased.UpdateMainInfo(
            firstName: "Иоанн", lastName: "Иванов", middleName: "Сергеевич",
            birthDate: new DateOnly(1950, 1, 1),
            deathDate: new DateOnly(2020, 5, 10),
            shortDescription: "Любимый дед",
            biography: null,
            editorUserId: anotherEditor);

        Assert.Equal(2, deceased.Edits.Count);
        Assert.Equal(EditorId, deceased.Edits.First().EditedByUserId);
        Assert.Equal(anotherEditor, deceased.Edits.Last().EditedByUserId);
    }
}
