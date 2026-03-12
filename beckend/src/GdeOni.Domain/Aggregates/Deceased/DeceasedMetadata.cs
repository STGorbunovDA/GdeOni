namespace GdeOni.Domain.Aggregates.Deceased;

// {
//     "Epitaph": "Любим, помним, скорбим",
//     "Religion": "Православный",
//     "Source": "Архив семьи",
//     "IsMilitaryService": true,
//     "AdditionalInfo": "Участник войны"
// }

public sealed class DeceasedMetadata
{
    public string? Epitaph { get; private set; }
    public string? Religion { get; private set; }
    public string? Source { get; private set; }
    public bool IsMilitaryService { get; private set; }
    public string? AdditionalInfo { get; private set; }

    private DeceasedMetadata()
    {
    }

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

    public static DeceasedMetadata Empty() =>
        new(null, null, null, false, null);

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
}