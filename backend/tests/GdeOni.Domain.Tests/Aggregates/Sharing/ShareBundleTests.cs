using GdeOni.Domain.Aggregates.Sharing;

namespace GdeOni.Domain.Tests.SharingAggregate;

/// <summary>
/// D46. Инварианты подборки «поделиться»: непустой список, дедуп, лимит,
/// корректный срок жизни, проверка истечения.
/// </summary>
public sealed class ShareBundleTests
{
    private const string Code = "abc123XYZ789";
    private static readonly Guid Author = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);
    private static readonly TimeSpan Day = TimeSpan.FromHours(24);

    [Fact]
    public void Create_ValidInput_Succeeds()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var result = ShareBundle.Create(Code, Author, ids, Now, Day);

        result.IsSuccess.Should().BeTrue();
        result.Value.Code.Should().Be(Code);
        result.Value.CreatedByUserId.Should().Be(Author);
        result.Value.DeceasedIds.Should().BeEquivalentTo(ids);
        result.Value.CreatedAtUtc.Should().Be(Now);
        result.Value.ExpiresAtUtc.Should().Be(Now.Add(Day));
    }

    [Fact]
    public void Create_DuplicateIds_AreCollapsed()
    {
        var id = Guid.NewGuid();

        var result = ShareBundle.Create(Code, Author, new[] { id, id, id }, Now, Day);

        result.IsSuccess.Should().BeTrue();
        result.Value.DeceasedIds.Should().ContainSingle().Which.Should().Be(id);
    }

    [Fact]
    public void Create_EmptyIds_IsRejected()
    {
        var result = ShareBundle.Create(Code, Author, Array.Empty<Guid>(), Now, Day);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("share.deceased_ids.required");
    }

    [Fact]
    public void Create_OnlyEmptyGuids_IsRejected()
    {
        var result = ShareBundle.Create(Code, Author, new[] { Guid.Empty }, Now, Day);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("share.deceased_ids.required");
    }

    [Fact]
    public void Create_TooManyItems_IsRejected()
    {
        var ids = Enumerable.Range(0, ShareBundle.MaxItems + 1)
            .Select(_ => Guid.NewGuid())
            .ToArray();

        var result = ShareBundle.Create(Code, Author, ids, Now, Day);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("share.deceased_ids.too_many");
    }

    [Fact]
    public void Create_EmptyCode_IsRejected()
    {
        var result = ShareBundle.Create("  ", Author, new[] { Guid.NewGuid() }, Now, Day);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("share.code.required");
    }

    [Fact]
    public void Create_NonPositiveLifetime_IsRejected()
    {
        var result = ShareBundle.Create(Code, Author, new[] { Guid.NewGuid() }, Now, TimeSpan.Zero);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("share.lifetime.invalid");
    }

    [Fact]
    public void IsExpired_BeforeAndAfterExpiry()
    {
        var bundle = ShareBundle.Create(Code, Author, new[] { Guid.NewGuid() }, Now, Day).Value;

        bundle.IsExpired(Now.AddHours(1)).Should().BeFalse();
        bundle.IsExpired(Now.Add(Day)).Should().BeTrue();
        bundle.IsExpired(Now.Add(Day).AddSeconds(1)).Should().BeTrue();
    }
}
