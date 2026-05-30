using GdeOni.Domain.Shared;

namespace GdeOni.API.Models.DeceasedRecords;

public sealed record UpdateBurialLocationByEditorRequest(
    double? Latitude,
    double? Longitude,
    double? AccuracyMeters,
    string? Country,
    string? Region,
    string? City,
    string? CemeteryName,
    string? PlotNumber,
    string? GraveNumber,
    LocationAccuracy Accuracy = LocationAccuracy.Exact);
