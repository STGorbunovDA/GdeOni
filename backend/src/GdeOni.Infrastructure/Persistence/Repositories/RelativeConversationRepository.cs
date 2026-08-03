using GdeOni.Application.Abstractions.Persistence;
using GdeOni.Domain.Aggregates.Relatives;
using GdeOni.Domain.Aggregates.User;
using GdeOni.Domain.Shared;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace GdeOni.Infrastructure.Persistence.Repositories;

/// <summary>
/// Функция «Родственники»: диалоги внутренней переписки. Шапку/список
/// собираем батч-запросами (имя собеседника, ФИО умершего, тип связи,
/// непрочитанные, чей ход) — turn-логика считается на доменной модели.
/// </summary>
public sealed class RelativeConversationRepository(AppDbContext dbContext)
    : IRelativeConversationRepository
{
    public async Task Add(RelativeConversation conversation, CancellationToken cancellationToken)
    {
        await dbContext.RelativeConversations.AddAsync(conversation, cancellationToken);
    }

    public Task<RelativeConversation?> GetByIdWithMessages(Guid id, CancellationToken cancellationToken)
    {
        return dbContext.RelativeConversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public Task<RelativeConversation?> GetByParticipants(
        Guid deceasedId, Guid userX, Guid userY, CancellationToken cancellationToken)
    {
        var (a, b) = userX.CompareTo(userY) <= 0 ? (userX, userY) : (userY, userX);
        return dbContext.RelativeConversations
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(
                c => c.DeceasedId == deceasedId && c.ParticipantAId == a && c.ParticipantBId == b,
                cancellationToken);
    }

    public async Task<RelativeConversationHeader?> GetHeader(
        Guid deceasedId, Guid otherUserId, CancellationToken cancellationToken)
    {
        var name = await dbContext.DeceasedRecords.AsNoTracking()
            .Where(d => d.Id == deceasedId)
            .Select(d => new { d.Name.FirstName, d.Name.LastName, d.Name.MiddleName })
            .FirstOrDefaultAsync(cancellationToken);
        if (name is null)
            return null;

        var otherName = await dbContext.Users.AsNoTracking()
            .Where(u => u.Id == otherUserId)
            .Select(u => u.UserName)
            .FirstOrDefaultAsync(cancellationToken);
        if (otherName is null)
            return null;

        var relationship = await dbContext.Set<TrackedDeceased>().AsNoTracking()
            .Where(t => EF.Property<Guid>(t, "user_id") == otherUserId && t.DeceasedId == deceasedId)
            .Select(t => (RelationshipType?)t.RelationshipType)
            .FirstOrDefaultAsync(cancellationToken);

        return new RelativeConversationHeader(
            BuildFullName(name.LastName, name.FirstName, name.MiddleName),
            otherName,
            relationship);
    }

    public async Task<List<RelativeConversationSummary>> GetConversationsForUser(
        Guid userId, CancellationToken cancellationToken)
    {
        var convos = await dbContext.RelativeConversations.AsNoTracking()
            .Include(c => c.Messages)
            .Where(c => c.ParticipantAId == userId || c.ParticipantBId == userId)
            .OrderByDescending(c => c.LastMessageAtUtc)
            .ToListAsync(cancellationToken);

        if (convos.Count == 0)
            return new List<RelativeConversationSummary>();

        var otherIds = convos.Select(c => c.OtherParticipant(userId)).Distinct().ToList();
        var deceasedIds = convos.Select(c => c.DeceasedId).Distinct().ToList();

        var userNames = await dbContext.Users.AsNoTracking()
            .Where(u => otherIds.Contains(u.Id))
            .Select(u => new { u.Id, u.UserName })
            .ToDictionaryAsync(u => u.Id, u => u.UserName, cancellationToken);

        var deceasedNames = (await dbContext.DeceasedRecords.AsNoTracking()
            .Where(d => deceasedIds.Contains(d.Id))
            .Select(d => new { d.Id, d.Name.FirstName, d.Name.LastName, d.Name.MiddleName })
            .ToListAsync(cancellationToken))
            .ToDictionary(d => d.Id, d => BuildFullName(d.LastName, d.FirstName, d.MiddleName));

        var relationships = (await dbContext.Set<TrackedDeceased>().AsNoTracking()
            .Where(t => deceasedIds.Contains(t.DeceasedId)
                        && otherIds.Contains(EF.Property<Guid>(t, "user_id")))
            .Select(t => new
            {
                UserId = EF.Property<Guid>(t, "user_id"),
                t.DeceasedId,
                t.RelationshipType,
            })
            .ToListAsync(cancellationToken))
            .ToDictionary(x => (x.UserId, x.DeceasedId), x => x.RelationshipType);

        return convos.Select(c =>
        {
            var otherId = c.OtherParticipant(userId);
            var last = c.LastVisibleMessage();
            var unread = c.Messages.Count(m => !m.IsDeleted && m.SenderId == otherId && !m.IsRead);
            relationships.TryGetValue((otherId, c.DeceasedId), out var rel);

            return new RelativeConversationSummary(
                c.Id,
                c.DeceasedId,
                deceasedNames.GetValueOrDefault(c.DeceasedId, "—"),
                otherId,
                userNames.GetValueOrDefault(otherId, "—"),
                relationships.ContainsKey((otherId, c.DeceasedId)) ? rel : null,
                c.LastMessageAtUtc,
                last?.Text,
                last is not null && last.SenderId == userId,
                unread,
                c.CanSend(userId));
        }).ToList();
    }

    public async Task Save(CancellationToken cancellationToken)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is PostgresException postgresException &&
            postgresException.SqlState == "23505")
        {
            throw new UniqueConstraintException(postgresException.ConstraintName);
        }
    }

    // Зеркало PersonName.FullName: «Фамилия Имя Отчество» без пустых частей.
    private static string BuildFullName(string lastName, string firstName, string? middleName) =>
        string.Join(" ", new[] { lastName, firstName, middleName }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
}
