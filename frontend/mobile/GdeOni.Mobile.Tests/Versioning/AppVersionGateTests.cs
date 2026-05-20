using FluentAssertions;
using GdeOni.Mobile.Shared.Versioning;
using Xunit;

namespace GdeOni.Mobile.Tests.Versioning;

public sealed class AppVersionGateTests
{
    [Fact]
    public void Evaluate_CurrentAboveLatest_ReturnsOk()
    {
        var result = AppVersionGate.Evaluate(
            currentVersion: "1.2.0",
            minSupportedVersion: "1.0.0",
            latestVersion: "1.1.0",
            downloadUrl: null,
            forceUpdateMessage: null);

        result.Outcome.Should().Be(VersionCheckOutcome.Ok);
    }

    [Fact]
    public void Evaluate_CurrentEqualsLatest_ReturnsOk()
    {
        var result = AppVersionGate.Evaluate("1.0.0", "1.0.0", "1.0.0", null, null);

        result.Outcome.Should().Be(VersionCheckOutcome.Ok);
    }

    [Fact]
    public void Evaluate_CurrentBetweenMinAndLatest_ReturnsSoft()
    {
        var result = AppVersionGate.Evaluate(
            currentVersion: "1.1.0",
            minSupportedVersion: "1.0.0",
            latestVersion: "1.2.0",
            downloadUrl: "https://gdeoni.ru/download",
            forceUpdateMessage: null);

        result.Outcome.Should().Be(VersionCheckOutcome.SoftUpdateAvailable);
        result.DownloadUrl.Should().Be("https://gdeoni.ru/download");
    }

    [Fact]
    public void Evaluate_CurrentBelowMin_ReturnsForce()
    {
        var result = AppVersionGate.Evaluate(
            currentVersion: "0.9.0",
            minSupportedVersion: "1.0.0",
            latestVersion: "1.2.0",
            downloadUrl: "https://gdeoni.ru/download",
            forceUpdateMessage: "Обновите для безопасной работы");

        result.Outcome.Should().Be(VersionCheckOutcome.ForceUpdate);
        result.DownloadUrl.Should().Be("https://gdeoni.ru/download");
        result.ForceUpdateMessage.Should().Be("Обновите для безопасной работы");
    }

    [Fact]
    public void Evaluate_CurrentEqualsMin_ReturnsSoft()
    {
        // current == min → выше или равно min, ниже latest → SoftUpdate.
        var result = AppVersionGate.Evaluate("1.0.0", "1.0.0", "1.1.0", null, null);

        result.Outcome.Should().Be(VersionCheckOutcome.SoftUpdateAvailable);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("1.0")]
    public void Evaluate_CurrentVersionUnparseable_ReturnsOk_FailOpen(string? current)
    {
        // Если AppInfo вернул что-то странное — лучше пропустить
        // юзера, чем массово заблокировать.
        var result = AppVersionGate.Evaluate(current, "1.0.0", "1.0.0", null, null);

        result.Outcome.Should().Be(VersionCheckOutcome.Ok);
    }

    [Fact]
    public void Evaluate_MinUnparseable_TreatedAsAbsent_AndLatestStillChecked()
    {
        // Кривой min на бэке не должен блокировать; latest всё ещё
        // работает (юзеру предлагаем обновиться).
        var result = AppVersionGate.Evaluate("1.0.0", "garbage", "1.1.0", null, null);

        result.Outcome.Should().Be(VersionCheckOutcome.SoftUpdateAvailable);
    }

    [Fact]
    public void Evaluate_BothMinAndLatestUnparseable_ReturnsOk()
    {
        var result = AppVersionGate.Evaluate("1.0.0", "garbage", "trash", null, null);

        result.Outcome.Should().Be(VersionCheckOutcome.Ok);
    }
}
