using CSharpFunctionalExtensions;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Aggregates.Sharing;

/// <summary>
/// D46. Подборка карточек умерших, которой пользователь делится с другим
/// человеком по короткой ссылке/QR. За кодом лежит список id карточек;
/// получатель открывает ссылку, входит и добавляет карточки себе в
/// отслеживание.
///
/// Модель простая: пишется один раз при создании, читается по коду. Список
/// id хранится массивом <c>uuid[]</c> прямо в строке (не отдельной
/// таблицей) — запросов «по id внутри подборки» нет, только «по коду →
/// весь список».
/// </summary>
public sealed class ShareBundle : Entity<Guid>
{
    /// <summary>Максимум карточек в одной подборке — защита от гигантских
    /// ссылок/QR и злоупотреблений.</summary>
    public const int MaxItems = 100;

    public const int MaxCodeLength = 32;

    /// <summary>Короткий url-safe код из ссылки <c>/s/{code}</c>. Уникален.</summary>
    public string Code { get; private set; }

    /// <summary>Кто создал подборку. Для аудита; при удалении юзера — SetNull.</summary>
    public Guid? CreatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    /// <summary>Момент, после которого ссылка недействительна (по умолчанию
    /// now + 24 часа, срок задаётся из конфига в use case).</summary>
    public DateTime ExpiresAtUtc { get; private set; }

    /// <summary>
    /// Id карточек в подборке. Массив <c>uuid[]</c>. Приватный setter,
    /// наружу неизменяемый список. Дубликаты убираются в <see cref="Create"/>.
    /// </summary>
    public Guid[] DeceasedIds { get; private set; }

    private ShareBundle() : base(Guid.Empty)
    {
        Code = null!;
        DeceasedIds = Array.Empty<Guid>();
    }

    private ShareBundle(
        Guid id,
        string code,
        Guid createdByUserId,
        Guid[] deceasedIds,
        DateTime createdAtUtc,
        DateTime expiresAtUtc) : base(id)
    {
        Code = code;
        CreatedByUserId = createdByUserId;
        DeceasedIds = deceasedIds;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>
    /// Создаёт подборку. Код генерируется в use case (домен не занимается
    /// криптографией), срок жизни приходит оттуда же. Дубликаты id
    /// схлопываются, пустой список и превышение лимита — ошибки.
    /// </summary>
    public static Result<ShareBundle, Error> Create(
        string code,
        Guid createdByUserId,
        IReadOnlyCollection<Guid> deceasedIds,
        DateTime nowUtc,
        TimeSpan lifetime)
    {
        if (string.IsNullOrWhiteSpace(code))
            return Errors.Share.CodeRequired();

        if (code.Length > MaxCodeLength)
            return Errors.Share.CodeRequired();

        if (createdByUserId == Guid.Empty)
            return Errors.Share.CreatedByRequired();

        if (deceasedIds is null || deceasedIds.Count == 0)
            return Errors.Share.DeceasedIdsRequired();

        var distinct = deceasedIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (distinct.Length == 0)
            return Errors.Share.DeceasedIdsRequired();

        if (distinct.Length > MaxItems)
            return Errors.Share.TooManyItems(MaxItems);

        if (lifetime <= TimeSpan.Zero)
            return Errors.Share.LifetimeInvalid();

        return Result.Success<ShareBundle, Error>(
            new ShareBundle(
                Guid.NewGuid(),
                code.Trim(),
                createdByUserId,
                distinct,
                nowUtc,
                nowUtc.Add(lifetime)));
    }

    /// <summary>Истёк ли срок действия ссылки.</summary>
    public bool IsExpired(DateTime nowUtc) => nowUtc >= ExpiresAtUtc;
}
