using GdeOni.Domain.Aggregates.DeceasedRecords;

namespace GdeOni.Domain.Tests.Aggregates.DeceasedRecords;

/// <summary>
/// Тесты value object'а <see cref="BurialLocation"/>.
/// BurialLocation — координата места захоронения с опциональными
/// текстовыми полями (страна, город, кладбище и т.п.). Инварианты
/// проверяются прямо в Create/CreateFromGps и должны падать как
/// доменная ошибка, а не выбрасывать исключение.
/// </summary>
public sealed class BurialLocationTests
{
    /// <summary>
    /// Latitude валиден в диапазоне [-90; 90]. Любое значение за
    /// этим диапазоном — это либо опечатка клиента, либо повреждённый
    /// GPS-сигнал; домен обязан отвергнуть с конкретным error-кодом
    /// `burial_location.latitude.invalid`, а не молча создать объект,
    /// который потом сломает routing/distance-расчёты.
    /// </summary>
    [Theory]
    [InlineData(91.0)]   // чуть выше северного полюса — недопустимо
    [InlineData(-91.0)]  // чуть ниже южного полюса — тоже
    [InlineData(180.0)]  // явная путаница latitude ↔ longitude
    public void Create_LatitudeOutsideRange_ReturnsLatitudeInvalid(double latitude)
    {
        // Act: пытаемся создать BurialLocation с заведомо невалидной широтой.
        var result = BurialLocation.Create(latitude, longitude: 30.0);

        // Assert: операция падает, в Error лежит именно код широты
        // (а не общий "validation failed") — так клиент сможет
        // показать корректное сообщение в UI.
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("burial_location.latitude.invalid");
    }

    /// <summary>
    /// Longitude валиден в диапазоне [-180; 180]. Те же соображения,
    /// что и для latitude, плюс на практике 181..360 — частая ошибка
    /// конвертации из градусов 0..360 в -180..180.
    /// </summary>
    [Theory]
    [InlineData(181.0)]
    [InlineData(-181.0)]
    [InlineData(360.0)]
    public void Create_LongitudeOutsideRange_ReturnsLongitudeInvalid(double longitude)
    {
        // Act
        var result = BurialLocation.Create(latitude: 55.0, longitude: longitude);

        // Assert: код ошибки — про longitude, не про latitude.
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("burial_location.longitude.invalid");
    }

    /// <summary>
    /// CreateFromGps — отдельная фабрика для главного user-сценария:
    /// пользователь у могилы передал только координаты и accuracyMeters,
    /// а текстовые поля (Country/City/CemeteryName и т.д.) ещё не
    /// введены. Объект должен создаться, все текстовые поля — null.
    /// Ровно этот случай покрывает D7.59 / at-grave-сценарий.
    /// </summary>
    [Fact]
    public void CreateFromGps_WithoutCountry_BuildsLocationWithNulls()
    {
        // Act: координаты Москвы + 5 метров accuracy от GPS.
        var result = BurialLocation.CreateFromGps(
            latitude: 55.7558,
            longitude: 37.6173,
            accuracyMeters: 5);

        // Assert: успех + ровно те координаты + accuracy + все
        // опциональные текстовые поля — null (а не "" или "null"-строка).
        result.IsSuccess.Should().BeTrue();
        result.Value.Latitude.Should().Be(55.7558);
        result.Value.Longitude.Should().Be(37.6173);
        result.Value.AccuracyMeters.Should().Be(5);
        result.Value.Country.Should().BeNull();
        result.Value.Region.Should().BeNull();
        result.Value.City.Should().BeNull();
        result.Value.CemeteryName.Should().BeNull();
    }

    /// <summary>
    /// AccuracyMeters не может быть отрицательным: GPS-устройство
    /// технически не может вернуть -5 метров погрешности. Если такое
    /// прилетает — это либо подделанный запрос, либо баг клиента.
    /// </summary>
    [Fact]
    public void Create_NegativeAccuracyMeters_ReturnsAccuracyMetersInvalid()
    {
        var result = BurialLocation.Create(
            latitude: 55.0,
            longitude: 37.0,
            accuracyMeters: -1);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("burial_location.accuracy_meters.invalid");
    }

    /// <summary>
    /// Equality должен корректно работать для двух BurialLocation
    /// без AccuracyMeters: пара (HasValue=false, Value=0.0) даёт
    /// детерминированный результат. Если бы equality использовал
    /// `AccuracyMeters ?? double.NaN`, оба объекта были бы не равны
    /// сами себе (NaN != NaN по IEEE-754). См. комментарий в
    /// GetEqualityComponents.
    /// </summary>
    [Fact]
    public void Equality_TwoLocationsWithNullAccuracy_AreEqual()
    {
        // Arrange: две одинаковых location'а без AccuracyMeters.
        var a = BurialLocation.Create(55.0, 37.0).Value;
        var b = BurialLocation.Create(55.0, 37.0).Value;

        // Assert: рефлексивность + симметричность equality.
        a.Should().Be(b);
        b.Should().Be(a);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }
}
