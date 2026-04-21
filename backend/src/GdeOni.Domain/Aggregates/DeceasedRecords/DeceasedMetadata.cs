using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.DeceasedRecords;

public sealed class DeceasedMetadata : ValueObject
{
    public const int MaxEpitaphLength = 500;
    public const int MaxReligionLength = 200;
    public const int MaxSourceLength = 500;
    public const int MaxAdditionalInfoLength = 2000;

    public string? Epitaph { get; }
    public string? Religion { get; }
    public string? Source { get; }
    public bool IsMilitaryService { get; }
    public string? AdditionalInfo { get; }

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
        Epitaph = epitaph;
        Religion = religion;
        Source = source;
        IsMilitaryService = isMilitaryService;
        AdditionalInfo = additionalInfo;
    }

    public static DeceasedMetadata Empty() => new(null, null, null, false, null);

    public static Result<DeceasedMetadata, Error> Create(
        string? epitaph,
        string? religion,
        string? source,
        bool isMilitaryService,
        string? additionalInfo)
    {
        var normalizedEpitaph = NormalizeOptional(epitaph);
        if (normalizedEpitaph is not null && normalizedEpitaph.Length > MaxEpitaphLength)
            return Errors.Deceased.EpitaphTooLong(MaxEpitaphLength);

        var normalizedReligion = NormalizeOptional(religion);
        if (normalizedReligion is not null && normalizedReligion.Length > MaxReligionLength)
            return Errors.Deceased.ReligionTooLong(MaxReligionLength);

        var normalizedSource = NormalizeOptional(source);
        if (normalizedSource is not null && normalizedSource.Length > MaxSourceLength)
            return Errors.Deceased.SourceTooLong(MaxSourceLength);

        var normalizedAdditionalInfo = NormalizeOptional(additionalInfo);
        if (normalizedAdditionalInfo is not null && normalizedAdditionalInfo.Length > MaxAdditionalInfoLength)
            return Errors.Deceased.AdditionalInfoTooLong(MaxAdditionalInfoLength);

        return Result.Success<DeceasedMetadata, Error>(
            new DeceasedMetadata(
                normalizedEpitaph,
                normalizedReligion,
                normalizedSource,
                isMilitaryService,
                normalizedAdditionalInfo));
    }

    public bool IsEmpty() =>
        string.IsNullOrWhiteSpace(Epitaph) &&
        string.IsNullOrWhiteSpace(Religion) &&
        string.IsNullOrWhiteSpace(Source) &&
        !IsMilitaryService &&
        string.IsNullOrWhiteSpace(AdditionalInfo);

    public bool HasAnyData() => !IsEmpty();

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Epitaph ?? string.Empty;
        yield return Religion ?? string.Empty;
        yield return Source ?? string.Empty;
        yield return IsMilitaryService;
        yield return AdditionalInfo ?? string.Empty;
    }
}