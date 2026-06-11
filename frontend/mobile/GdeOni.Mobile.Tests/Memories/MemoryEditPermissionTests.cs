using FluentAssertions;
using GdeOni.Mobile.Shared.Memories;
using Xunit;

namespace GdeOni.Mobile.Tests.Memories;

public class MemoryEditPermissionTests
{
    private static readonly Guid Alice = Guid.NewGuid();
    private static readonly Guid Bob = Guid.NewGuid();
    private static readonly Guid Carol = Guid.NewGuid();

    [Fact]
    public void Author_of_memory_can_edit_their_own()
    {
        MemoryEditPermission.CanEdit(
            currentUserId: Alice,
            memoryAuthorUserId: Alice,
            cardCreatedByUserId: Bob).Should().BeTrue();
    }

    [Fact]
    public void Author_of_card_can_edit_memories_from_others()
    {
        // Bob завёл карточку — Alice оставила воспоминание. Bob может его
        // редактировать (модерация на своей карточке), Alice — тоже.
        MemoryEditPermission.CanEdit(
            currentUserId: Bob,
            memoryAuthorUserId: Alice,
            cardCreatedByUserId: Bob).Should().BeTrue();
    }

    [Fact]
    public void Stranger_cannot_edit_someone_elses_memory_on_someone_elses_card()
    {
        // Carol — посторонний: ни автор воспоминания, ни автор карточки.
        MemoryEditPermission.CanEdit(
            currentUserId: Carol,
            memoryAuthorUserId: Alice,
            cardCreatedByUserId: Bob).Should().BeFalse();
    }

    [Fact]
    public void Anonymous_visitor_cannot_edit_anything()
    {
        MemoryEditPermission.CanEdit(
            currentUserId: null,
            memoryAuthorUserId: Alice,
            cardCreatedByUserId: Bob).Should().BeFalse();
    }

    [Fact]
    public void System_memory_with_null_author_falls_back_to_card_creator_rule()
    {
        // Воспоминание без автора (теоретический случай — backend модель
        // позволяет AuthorUserId == null). Редактировать может только
        // автор карточки.
        MemoryEditPermission.CanEdit(
            currentUserId: Bob,
            memoryAuthorUserId: null,
            cardCreatedByUserId: Bob).Should().BeTrue();

        MemoryEditPermission.CanEdit(
            currentUserId: Alice,
            memoryAuthorUserId: null,
            cardCreatedByUserId: Bob).Should().BeFalse();
    }

    [Fact]
    public void Card_creator_null_does_not_grant_edit_to_anyone_except_memory_author()
    {
        // Карточка без CreatedByUserId (теоретически — не должно
        // случаться, но guard). Редактирует только автор воспоминания.
        MemoryEditPermission.CanEdit(
            currentUserId: Alice,
            memoryAuthorUserId: Alice,
            cardCreatedByUserId: null).Should().BeTrue();

        MemoryEditPermission.CanEdit(
            currentUserId: Bob,
            memoryAuthorUserId: Alice,
            cardCreatedByUserId: null).Should().BeFalse();
    }
}
