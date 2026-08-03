using GdeOni.Domain.Aggregates.Relatives;

namespace GdeOni.Domain.Tests.RelativesAggregate;

/// <summary>
/// Жалоба на родственника (Фаза 5): нельзя на себя, причина обязательна и
/// ограничена по длине; разбор идемпотентен (не перезаписывает автора/время).
/// </summary>
public sealed class RelativeReportTests
{
    private static readonly Guid Reporter = Guid.NewGuid();
    private static readonly Guid Reported = Guid.NewGuid();
    private static readonly Guid Deceased = Guid.NewGuid();
    private static readonly Guid Conversation = Guid.NewGuid();
    private static readonly Guid Admin = Guid.NewGuid();
    private static readonly DateTime Now = new(2026, 8, 3, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_Valid_Succeeds()
    {
        var result = RelativeReport.Create(
            Reporter, Reported, Deceased, Conversation, "  Спам  ", Now);

        result.IsSuccess.Should().BeTrue();
        var report = result.Value;
        report.Reason.Should().Be("Спам"); // trim
        report.Status.Should().Be(RelativeReportStatus.Pending);
        report.ReporterUserId.Should().Be(Reporter);
        report.ReportedUserId.Should().Be(Reported);
        report.ConversationId.Should().Be(Conversation);
    }

    [Fact]
    public void Create_OnSelf_Rejected()
    {
        var result = RelativeReport.Create(
            Reporter, Reporter, Deceased, Conversation, "любой", Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("relatives.report.self");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyReason_Rejected(string reason)
    {
        var result = RelativeReport.Create(
            Reporter, Reported, Deceased, Conversation, reason, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("relatives.report.reason.required");
    }

    [Fact]
    public void Create_TooLongReason_Rejected()
    {
        var reason = new string('x', RelativeReport.MaxReasonLength + 1);

        var result = RelativeReport.Create(
            Reporter, Reported, Deceased, Conversation, reason, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("relatives.report.reason.too_long");
    }

    [Fact]
    public void Resolve_SetsStatusAndAudit()
    {
        var report = RelativeReport.Create(
            Reporter, Reported, Deceased, Conversation, "спам", Now).Value;

        var result = report.Resolve(Admin, "  заблокирован  ", Now);

        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(RelativeReportStatus.Resolved);
        report.ResolvedByUserId.Should().Be(Admin);
        report.ResolvedAtUtc.Should().Be(Now);
        report.ResolutionNote.Should().Be("заблокирован");
    }

    [Fact]
    public void Resolve_Twice_Idempotent_KeepsFirstAuthor()
    {
        var report = RelativeReport.Create(
            Reporter, Reported, Deceased, Conversation, "спам", Now).Value;
        report.Resolve(Admin, "первое", Now);

        var otherAdmin = Guid.NewGuid();
        var later = Now.AddHours(1);
        var result = report.Resolve(otherAdmin, "второе", later);

        result.IsSuccess.Should().BeTrue();
        report.ResolvedByUserId.Should().Be(Admin);
        report.ResolvedAtUtc.Should().Be(Now);
        report.ResolutionNote.Should().Be("первое");
    }
}
