using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using GdeOni.Infrastructure.Relatives;
using Microsoft.EntityFrameworkCore;

namespace GdeOni.Infrastructure.Persistence.Repositories;

/// <summary>
/// Функция «Родственники». Один запрос: свои активные отслеживания
/// self-join'ятся с чужими активными по deceased_id; показываем ВСЕХ
/// со-отслеживающих (любая связь, включая «Знакомый»/«Другое»), у кого
/// включено согласие и кто не заблокирован — фильтр по связи вынесен на
/// клиент (комбобокс на странице «Родственники»). Имя умершего склеиваем в
/// памяти (PersonName.FullName — вычисляемое, в SQL не транслируется).
/// user_id у TrackedDeceased — теневой FK, поэтому EF.Property.
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
                 && t.Status == TrackStatus.Active,
            cancellationToken);
        if (!targetTracks)
            return false;

        return await dbContext.Users.AsNoTracking().AnyAsync(
            u => u.Id == targetUserId && u.AllowRelativeConnections && !u.IsBlocked,
            cancellationToken);
    }

    public async Task<List<NewRelativeItem>> GetNewRelatives(
        Guid ownerId,
        CancellationToken cancellationToken)
    {
        var tracked = dbContext.Set<TrackedDeceased>().AsNoTracking();

        // Перепроверяем каждый discovery по текущему состоянию: владелец и
        // родственник всё ещё активно отслеживают карточку, связь связывающая,
        // согласие включено, не заблокирован. Устаревшие discovery молча
        // отпадают (не попадают в результат), хотя строка в БД остаётся.
        var rows = await (
            from disc in dbContext.Set<RelativeDiscovery>().AsNoTracking()
            where disc.OwnerUserId == ownerId && disc.IsNew
            join mine in tracked on disc.DeceasedId equals mine.DeceasedId
            where EF.Property<Guid>(mine, "user_id") == ownerId
                  && mine.Status == TrackStatus.Active
            join theirs in tracked on disc.DeceasedId equals theirs.DeceasedId
            where EF.Property<Guid>(theirs, "user_id") == disc.RelativeUserId
                  && theirs.Status == TrackStatus.Active
            join u in dbContext.Users.AsNoTracking() on disc.RelativeUserId equals u.Id
            where u.AllowRelativeConnections && !u.IsBlocked
            join d in dbContext.DeceasedRecords.AsNoTracking() on disc.DeceasedId equals d.Id
            select new NewRow(
                disc.DeceasedId,
                d.Name.FirstName,
                d.Name.LastName,
                d.Name.MiddleName,
                disc.RelativeUserId,
                u.UserName,
                theirs.RelationshipType,
                disc.DiscoveredAtUtc))
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new NewRelativeItem(
                r.DeceasedId,
                BuildFullName(r.LastName, r.FirstName, r.MiddleName),
                r.RelativeUserId,
                r.RelativeUserName,
                r.RelationshipType,
                r.DiscoveredAtUtc))
            .OrderByDescending(r => r.DiscoveredAtUtc)
            .ThenBy(r => r.DeceasedFullName)
            .ToList();
    }

    public async Task MarkRelativesSeen(Guid ownerId, CancellationToken cancellationToken)
    {
        await dbContext.Set<RelativeDiscovery>()
            .Where(d => d.OwnerUserId == ownerId && d.IsNew)
            .ExecuteUpdateAsync(
                s => s.SetProperty(d => d.IsNew, false),
                cancellationToken);
    }

    // Зеркало PersonName.FullName: «Фамилия Имя Отчество» без пустых частей.
    private static string BuildFullName(string lastName, string firstName, string? middleName) =>
        string.Join(" ", new[] { lastName, firstName, middleName }
            .Where(x => !string.IsNullOrWhiteSpace(x)));

    private sealed record NewRow(
        Guid DeceasedId,
        string FirstName,
        string LastName,
        string? MiddleName,
        Guid RelativeUserId,
        string RelativeUserName,
        RelationshipType RelationshipType,
        DateTime DiscoveredAtUtc);

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
