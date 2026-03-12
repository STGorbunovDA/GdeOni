using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.Deceased;

public sealed class BurialLocation : ValueObject
{
    public double Latitude { get; }
    public double Longitude { get; }
    public string Country { get; }
    public string? Region { get; }
    public string? City { get; }
    public string? CemeteryName { get; }
    public string? PlotNumber { get; }
    public string? GraveNumber { get; }
    public LocationAccuracy Accuracy { get; }
    
    private BurialLocation()
    {
        Country = null!;
    }
    
    private BurialLocation(
        double latitude,
        double longitude,
        string country,
        string? region,
        string? city,
        string? cemeteryName,
        string? plotNumber,
        string? graveNumber,
        LocationAccuracy accuracy)
    {
        Latitude = latitude;
        Longitude = longitude;
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


    public static Result<BurialLocation> Create(
        double latitude,
        double longitude,
        string country,
        string? region = null,
        string? city = null,
        string? cemeteryName = null,
        string? plotNumber = null,
        string? graveNumber = null,
        LocationAccuracy accuracy = LocationAccuracy.Exact)
    {
        if (latitude < -90 || latitude > 90)
            return Result.Failure<BurialLocation>("Некорректная широта");

        if (longitude < -180 || longitude > 180)
            return Result.Failure<BurialLocation>("Некорректная долгота");

        if (string.IsNullOrWhiteSpace(country))
            return Result.Failure<BurialLocation>("Страна обязательна");

        return Result.Success(new BurialLocation(
            latitude,
            longitude,
            country.Trim(),
            string.IsNullOrWhiteSpace(region) ? null : region.Trim(),
            string.IsNullOrWhiteSpace(city) ? null : city.Trim(),
            string.IsNullOrWhiteSpace(cemeteryName) ? null : cemeteryName.Trim(),
            string.IsNullOrWhiteSpace(plotNumber) ? null : plotNumber.Trim(),
            string.IsNullOrWhiteSpace(graveNumber) ? null : graveNumber.Trim(),
            accuracy));
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

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Latitude;
        yield return Longitude;
        yield return Country;
        yield return Region ?? string.Empty;
        yield return City ?? string.Empty;
        yield return CemeteryName ?? string.Empty;
        yield return PlotNumber ?? string.Empty;
        yield return GraveNumber ?? string.Empty;
        yield return Accuracy;
    }
}