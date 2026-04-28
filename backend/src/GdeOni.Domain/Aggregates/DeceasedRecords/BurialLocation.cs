using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.DeceasedRecords;

public sealed class BurialLocation : ValueObject
{
    public const int MaxCountryLength = 200;
    public const int MaxRegionLength = 200;
    public const int MaxCityLength = 200;
    public const int MaxCemeteryNameLength = 300;
    public const int MaxPlotNumberLength = 100;
    public const int MaxGraveNumberLength = 100;

    public double Latitude { get; }
    public double Longitude { get; }
    public double? AccuracyMeters { get; }
    public string? Country { get; }
    public string? Region { get; }
    public string? City { get; }
    public string? CemeteryName { get; }
    public string? PlotNumber { get; }
    public string? GraveNumber { get; }
    public LocationAccuracy Accuracy { get; }

    private BurialLocation()
    {
    }

    private BurialLocation(
        double latitude,
        double longitude,
        double? accuracyMeters,
        string? country,
        string? region,
        string? city,
        string? cemeteryName,
        string? plotNumber,
        string? graveNumber,
        LocationAccuracy accuracy)
    {
        Latitude = latitude;
        Longitude = longitude;
        AccuracyMeters = accuracyMeters;
        Country = country;
        Region = region;
        City = city;
        CemeteryName = cemeteryName;
        PlotNumber = plotNumber;
        GraveNumber = graveNumber;
        Accuracy = accuracy;
    }

    public string FullAddress =>
        string.Join(", ", new[]
        {
            Country,
            Region,
            City,
            CemeteryName,
            PlotNumber,
            GraveNumber
        }.Where(x => !string.IsNullOrWhiteSpace(x)));

    public static Result<BurialLocation, Error> Create(
        double latitude,
        double longitude,
        string? country = null,
        string? region = null,
        string? city = null,
        string? cemeteryName = null,
        string? plotNumber = null,
        string? graveNumber = null,
        LocationAccuracy accuracy = LocationAccuracy.Exact,
        double? accuracyMeters = null)
    {
        if (latitude < -90 || latitude > 90)
            return Errors.BurialLocation.LatitudeInvalid();

        if (longitude < -180 || longitude > 180)
            return Errors.BurialLocation.LongitudeInvalid();

        if (accuracyMeters is < 0)
            return Errors.BurialLocation.AccuracyMetersInvalid();

        if (!Enum.IsDefined(typeof(LocationAccuracy), accuracy))
            return Errors.BurialLocation.AccuracyInvalid();

        var normalizedCountry = NormalizeOptional(country);
        if (normalizedCountry is not null && normalizedCountry.Length > MaxCountryLength)
            return Errors.BurialLocation.CountryTooLong(MaxCountryLength);

        var normalizedRegion = NormalizeOptional(region);
        if (normalizedRegion is not null && normalizedRegion.Length > MaxRegionLength)
            return Errors.BurialLocation.RegionTooLong(MaxRegionLength);

        var normalizedCity = NormalizeOptional(city);
        if (normalizedCity is not null && normalizedCity.Length > MaxCityLength)
            return Errors.BurialLocation.CityTooLong(MaxCityLength);

        var normalizedCemeteryName = NormalizeOptional(cemeteryName);
        if (normalizedCemeteryName is not null && normalizedCemeteryName.Length > MaxCemeteryNameLength)
            return Errors.BurialLocation.CemeteryNameTooLong(MaxCemeteryNameLength);

        var normalizedPlotNumber = NormalizeOptional(plotNumber);
        if (normalizedPlotNumber is not null && normalizedPlotNumber.Length > MaxPlotNumberLength)
            return Errors.BurialLocation.PlotNumberTooLong(MaxPlotNumberLength);

        var normalizedGraveNumber = NormalizeOptional(graveNumber);
        if (normalizedGraveNumber is not null && normalizedGraveNumber.Length > MaxGraveNumberLength)
            return Errors.BurialLocation.GraveNumberTooLong(MaxGraveNumberLength);

        return Result.Success<BurialLocation, Error>(
            new BurialLocation(
                latitude,
                longitude,
                accuracyMeters,
                normalizedCountry,
                normalizedRegion,
                normalizedCity,
                normalizedCemeteryName,
                normalizedPlotNumber,
                normalizedGraveNumber,
                accuracy));
    }

    public static Result<BurialLocation, Error> CreateFromGps(
        double latitude,
        double longitude,
        double? accuracyMeters)
    {
        return Create(
            latitude,
            longitude,
            country: null,
            region: null,
            city: null,
            cemeteryName: null,
            plotNumber: null,
            graveNumber: null,
            accuracy: LocationAccuracy.Exact,
            accuracyMeters: accuracyMeters);
    }

    public double DistanceTo(double latitude, double longitude)
    {
        var dLat = ToRadians(latitude - Latitude);
        var dLon = ToRadians(longitude - Longitude);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(Latitude)) * Math.Cos(ToRadians(latitude)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return 6371 * c;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
        yield return AccuracyMeters ?? double.NaN;
        yield return Country ?? string.Empty;
        yield return Region ?? string.Empty;
        yield return City ?? string.Empty;
        yield return CemeteryName ?? string.Empty;
        yield return PlotNumber ?? string.Empty;
        yield return GraveNumber ?? string.Empty;
        yield return Accuracy;
    }
}
