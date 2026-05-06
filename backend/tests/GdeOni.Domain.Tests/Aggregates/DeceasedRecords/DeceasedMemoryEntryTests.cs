using GdeOni.Domain.Aggregates.DeceasedRecords;
using GdeOni.Domain.Shared;

namespace GdeOni.Domain.Tests.Aggregates.DeceasedRecords;

/// <summary>
/// Тесты <see cref="DeceasedMemoryEntry"/> — отдельная запись
/// в коллекции воспоминаний карточки. Имеет свой статус модерации
/// (Pending/Approved/Rejected) и инвариант "после правки текст
/// возвращается в Pending" (anti-bypass модерации).
/// </summary>
public sealed class DeceasedMemoryEntryTests
{
    /// <summary>
    /// Текст обязателен. Все три классические варианта пустоты —
    /// одинаково отвергаются.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Create_EmptyText_ReturnsTextRequired(string? text)
    {
        var result = DeceasedMemoryEntry.Create(text!);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_memory.text.required");
    }

    /// <summary>
    /// Текст длиннее MaxTextLength (5000) — TooLong. На границе
    /// "ровно 5001 символ" — обязательно отвергается.
    /// </summary>
    [Fact]
    public void Create_TextTooLong_ReturnsTextTooLong()
    {
        var text = new string('а', DeceasedMemoryEntry.MaxTextLength + 1);

        var result = DeceasedMemoryEntry.Create(text);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_memory.text.too_long");
    }

    /// <summary>
    /// AuthorUserId == Guid.Empty — это "автор передан, но пустой".
    /// В отличие от null (анонимная memory), Guid.Empty — это явная
    /// ошибка. Domain отличает: null OK, Empty Forbidden.
    /// </summary>
    [Fact]
    public void Create_EmptyAuthorUserId_ReturnsUserIdRequired()
    {
        var result = DeceasedMemoryEntry.Create("Хорошее воспоминание", authorUserId: Guid.Empty);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("user.id.required");
    }

    /// <summary>
    /// Happy path Create: статус по умолчанию Pending, текст
    /// trim'ится, AuthorUserId сохраняется, CreatedAtUtc недавно.
    /// </summary>
    [Fact]
    public void Create_ValidParameters_StartsAsPending()
    {
        var authorId = Guid.NewGuid();

        var result = DeceasedMemoryEntry.Create("  Хорошее воспоминание  ", authorUserId: authorId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Text.Should().Be("Хорошее воспоминание"); // trim'нуто
        result.Value.AuthorUserId.Should().Be(authorId);
        result.Value.ModerationStatus.Should().Be(ModerationStatus.Pending);
        result.Value.UpdatedAtUtc.Should().BeNull();
    }

    /// <summary>
    /// Anti-bypass модерации: после Approve, если автор редактирует
    /// текст — статус сбрасывается обратно в Pending. Без этого
    /// можно было бы написать "хорошее" → дождаться Approve →
    /// заменить на "плохое", и оно осталось бы Approved публично.
    /// Покрываем именно это поведение.
    /// </summary>
    [Fact]
    public void EditText_AfterApprove_ResetsToPending()
    {
        // Arrange: создали + апрувнули memory.
        var memory = DeceasedMemoryEntry.Create("Первый текст", Guid.NewGuid()).Value;
        memory.Approve();
        memory.ModerationStatus.Should().Be(ModerationStatus.Approved);

        // Act: автор редактирует текст.
        var result = memory.EditText("Изменённый текст");

        // Assert: текст обновлён + статус возвращён в Pending +
        // UpdatedAtUtc проставлен.
        result.IsSuccess.Should().BeTrue();
        memory.Text.Should().Be("Изменённый текст");
        memory.ModerationStatus.Should().Be(ModerationStatus.Pending);
        memory.UpdatedAtUtc.Should().NotBeNull();
    }

    /// <summary>
    /// Approve повторно (на уже Approved memory) → AlreadyApproved.
    /// Conflict-ошибка, ровно 409 на уровне API.
    /// </summary>
    [Fact]
    public void Approve_AlreadyApproved_ReturnsAlreadyApproved()
    {
        var memory = DeceasedMemoryEntry.Create("Текст").Value;
        memory.Approve();

        var result = memory.Approve();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_memory.already.approved");
    }

    /// <summary>
    /// Reject повторно — AlreadyRejected. Симметрично Approve выше.
    /// </summary>
    [Fact]
    public void Reject_AlreadyRejected_ReturnsAlreadyRejected()
    {
        var memory = DeceasedMemoryEntry.Create("Текст").Value;
        memory.Reject();

        var result = memory.Reject();

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("deceased_memory.already.rejected");
    }
}
