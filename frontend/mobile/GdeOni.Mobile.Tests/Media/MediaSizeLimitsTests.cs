using FluentAssertions;
using GdeOni.Mobile.Shared.Media;
using Xunit;

namespace GdeOni.Mobile.Tests.Media;

public class MediaSizeLimitsTests
{
    [Fact]
    public void MaxPhoto_Is10Mb()
    {
        MediaSizeLimits.MaxPhotoSizeBytes.Should().Be(10L * 1024 * 1024);
    }

    [Fact]
    public void MaxDocument_Is25Mb()
    {
        MediaSizeLimits.MaxDocumentSizeBytes.Should().Be(25L * 1024 * 1024);
    }

    [Fact]
    public void CheckPhoto_AtLimit_ReturnsNull()
    {
        MediaSizeLimits.CheckPhoto(MediaSizeLimits.MaxPhotoSizeBytes).Should().BeNull();
    }

    [Fact]
    public void CheckPhoto_BelowLimit_ReturnsNull()
    {
        MediaSizeLimits.CheckPhoto(5 * 1024 * 1024).Should().BeNull();
        MediaSizeLimits.CheckPhoto(0).Should().BeNull();
    }

    [Fact]
    public void CheckPhoto_OneByteOver_ReturnsErrorMessage()
    {
        var msg = MediaSizeLimits.CheckPhoto(MediaSizeLimits.MaxPhotoSizeBytes + 1);
        msg.Should().NotBeNull();
        msg.Should().Contain("10 МБ");
    }

    [Fact]
    public void CheckPhoto_FifteenMb_ReturnsErrorMessage()
    {
        var msg = MediaSizeLimits.CheckPhoto(15L * 1024 * 1024);
        msg.Should().Be("Фото больше 10 МБ — выберите файл поменьше.");
    }

    [Fact]
    public void CheckDocument_AtLimit_ReturnsNull()
    {
        MediaSizeLimits.CheckDocument(MediaSizeLimits.MaxDocumentSizeBytes).Should().BeNull();
    }

    [Fact]
    public void CheckDocument_BelowLimit_ReturnsNull()
    {
        MediaSizeLimits.CheckDocument(20 * 1024 * 1024).Should().BeNull();
    }

    [Fact]
    public void CheckDocument_OneByteOver_ReturnsErrorMessage()
    {
        var msg = MediaSizeLimits.CheckDocument(MediaSizeLimits.MaxDocumentSizeBytes + 1);
        msg.Should().NotBeNull();
        msg.Should().Contain("25 МБ");
    }

    [Fact]
    public void CheckDocument_FiftyMb_ReturnsErrorMessage()
    {
        var msg = MediaSizeLimits.CheckDocument(50L * 1024 * 1024);
        msg.Should().Be("Документ больше 25 МБ — выберите файл поменьше.");
    }
}
