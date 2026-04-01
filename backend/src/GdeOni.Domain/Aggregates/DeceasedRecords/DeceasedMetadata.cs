using CSharpFunctionalExtensions;

namespace GdeOni.Domain.Aggregates.DeceasedRecords;

public sealed class DeceasedMetadata : ValueObject
{
    public string? Epitaph { get; private set; }
    public string? Religion { get; private set; }
    public string? Source { get; private set; }
    public bool IsMilitaryService { get; private set; }
    public string? AdditionalInfo { get; private set; }

    private DeceasedMetadata() { }

    private DeceasedMetadata(
        string? epitaph,
        string? religion,
        string? source,
        bool isMilitaryService,
        string? additionalInfo)
    {
        Epitaph = string.IsNullOrWhiteSpace(epitaph) ? null : epitaph.Trim();
        Religion = string.IsNullOrWhiteSpace(religion) ? null : religion.Trim();
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        IsMilitaryService = isMilitaryService;
        AdditionalInfo = string.IsNullOrWhiteSpace(additionalInfo) ? null : additionalInfo.Trim();
    }

    public static DeceasedMetadata Empty() => new(null, null, null, false, null);

    public static DeceasedMetadata Create(
        string? epitaph,
        string? religion,
        string? source,
        bool isMilitaryService,
        string? additionalInfo)
    {
        return new DeceasedMetadata(
            epitaph,
            religion,
            source,
            isMilitaryService,
            additionalInfo);
    }

    public bool IsEmpty() =>
        string.IsNullOrWhiteSpace(Epitaph) &&
        string.IsNullOrWhiteSpace(Religion) &&
        string.IsNullOrWhiteSpace(Source) &&
        !IsMilitaryService &&
        string.IsNullOrWhiteSpace(AdditionalInfo);

    public bool HasAnyData() => !IsEmpty();

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Epitaph ?? string.Empty;
        yield return Religion ?? string.Empty;
        yield return Source ?? string.Empty;
        yield return IsMilitaryService;
        yield return AdditionalInfo ?? string.Empty;
    }
}