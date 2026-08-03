using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;

namespace GdeOni.Infrastructure.Persistence.Repositories;

/// <summary>
/// Функция «Родственники». Один запрос: свои активные отслеживания
/// self-join'ятся с чужими активными по deceased_id; чужой отслеживающий
/// должен иметь связывающую связь (не «Знакомый»/«Другое» — зеркало
/// <see cref="RelativeRelationships"/>), включённое согласие и не быть
/// заблокированным. Имя умершего склеиваем в памяти (PersonName.FullName —
/// вычисляемое, в SQL не транслируется). user_id у TrackedDeceased —
/// теневой FK, поэтому EF.Property.
/// </summary>
public sealed class RelativesRepository(AppDbContext dbContext) : IRelativesRepository
{
    public async Task<List<RelativeMatch>> GetRelativesForUser(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var tracked = dbContext.Set<TrackedDeceased>().AsNoTracking();

        var rows = await (
            from mine in tracked
            where EF.Property<Guid>(mine, "user_id") == userId
                  && mine.Status == TrackStatus.Active
            join theirs in tracked on mine.DeceasedId equals theirs.DeceasedId
            where EF.Property<Guid>(theirs, "user_id") != userId
                  && theirs.Status == TrackStatus.Active
                  && theirs.RelationshipType != RelationshipType.Acquaintance
                  && theirs.RelationshipType != RelationshipType.Other
            join u in dbContext.Users.AsNoTracking()
                on EF.Property<Guid>(theirs, "user_id") equals u.Id
            where u.AllowRelativeConnections && !u.IsBlocked
            join d in dbContext.DeceasedRecords.AsNoTracking()
                on mine.DeceasedId equals d.Id
            select new Row(
                d.Id,
                d.Name.FirstName,
                d.Name.LastName,
                d.Name.MiddleName,
                d.LifePeriod.BirthDate,
                d.LifePeriod.DeathDate,
                u.Id,
                u.UserName,
                theirs.RelationshipType))
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new RelativeMatch(
                r.DeceasedId,
                BuildFullName(r.LastName, r.FirstName, r.MiddleName),
                r.BirthDate,
                r.DeathDate,
                r.RelativeUserId,
                r.RelativeUserName,
                r.RelationshipType))
            .OrderBy(r => r.DeceasedFullName)
            .ThenBy(r => r.RelativeUserName)
            .ToList();
    }

    public async Task<bool> IsRelative(
        Guid viewerId, Guid targetUserId, Guid deceasedId, CancellationToken cancellationToken)
    {
        var tracked = dbContext.Set<TrackedDeceased>().AsNoTracking();

        var viewerTracks = await tracked.AnyAsync(
            t => EF.Property<Guid>(t, "user_id") == viewerId
                 && t.DeceasedId == deceasedId
                 && t.Status == TrackStatus.Active,
            cancellationToken);
        if (!viewerTracks)
            return false;

        var targetTracks = await tracked.AnyAsync(
            t => EF.Property<Guid>(t, "user_id") == targetUserId
                 && t.DeceasedId == deceasedId
                 && t.Status == TrackStatus.Active
                 && t.RelationshipType != RelationshipType.Acquaintance
                 && t.RelationshipType != RelationshipType.Other,
            cancellationToken);
        if (!targetTracks)
            return false;

        return await dbContext.Users.AsNoTracking().AnyAsync(
            u => u.Id == targetUserId && u.AllowRelativeConnections && !u.IsBlocked,
            cancellationToken);
    }

    // Зеркало PersonName.FullName: «Фамилия Имя Отчество» без пустых частей.
    private static string BuildFullName(string lastName, string firstName, string? middleName) =>
        string.Join(" ", new[] { lastName, firstName, middleName }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

    private sealed record Row(
        Guid DeceasedId,
        string FirstName,
        string LastName,
        string? MiddleName,
        DateOnly? BirthDate,
        DateOnly DeathDate,
        Guid RelativeUserId,
        string RelativeUserName,
        RelationshipType RelationshipType);
}
