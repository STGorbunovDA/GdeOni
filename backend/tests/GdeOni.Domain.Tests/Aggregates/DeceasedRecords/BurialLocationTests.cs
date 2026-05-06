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

    /// <summary>
    /// DistanceTo использует Haversine-формулу (геодезическое
    /// расстояние по сфере). Проверяем на эталонных координатах:
    /// Москва (55.7558, 37.6173) ↔ Санкт-Петербург (59.9343, 30.3351)
    /// ≈ 635 км (по разным источникам — 632-639 км). Ставим допуск 5 км
    /// — больше эпсилона на радиус Земли (6371 vs 6378), но меньше
    /// расхождения "что-то сломали".
    /// </summary>
    [Fact]
    public void DistanceTo_MoscowToSaintPetersburg_ReturnsApprox635Km()
    {
        var moscow = BurialLocation.Create(55.7558, 37.6173).Value;

        // Coordinate СПб → передаём в DistanceTo как (lat, lon).
        var distance = moscow.DistanceTo(59.9343, 30.3351);

        distance.Should().BeApproximately(635.0, precision: 5.0);
    }

    /// <summary>
    /// DistanceTo от точки до самой себя = 0. Защита от ошибок в
    /// формуле, которая могла бы вернуть NaN на нулевом дельта-vector'е.
    /// </summary>
    [Fact]
    public void DistanceTo_SamePoint_ReturnsZero()
    {
        var location = BurialLocation.Create(55.0, 37.0).Value;
        location.DistanceTo(55.0, 37.0).Should().BeApproximately(0.0, precision: 0.001);
    }

    /// <summary>
    /// MaxLength-проверки на текстовых полях. Каждый отдельный код
    /// ошибки покрываем — клиент будет показывать конкретное
    /// сообщение про конкретное поле в UI.
    /// </summary>
    [Theory]
    [InlineData(nameof(BurialLocation.Country), "burial_location.country.too_long")]
    [InlineData(nameof(BurialLocation.Region), "burial_location.region.too_long")]
    [InlineData(nameof(BurialLocation.City), "burial_location.city.too_long")]
    [InlineData(nameof(BurialLocation.CemeteryName), "burial_location.cemetery_name.too_long")]
    [InlineData(nameof(BurialLocation.PlotNumber), "burial_location.plot_number.too_long")]
    [InlineData(nameof(BurialLocation.GraveNumber), "burial_location.grave_number.too_long")]
    public void Create_TextFieldExceedsMaxLength_ReturnsCorrectTooLongError(string field, string expectedCode)
    {
        // Arrange: создаём строку чуть длиннее максимума для проверяемого поля.
        var maxLengths = new Dictionary<string, int>
        {
            [nameof(BurialLocation.Country)] = BurialLocation.MaxCountryLength,
            [nameof(BurialLocation.Region)] = BurialLocation.MaxRegionLength,
            [nameof(BurialLocation.City)] = BurialLocation.MaxCityLength,
            [nameof(BurialLocation.CemeteryName)] = BurialLocation.MaxCemeteryNameLength,
            [nameof(BurialLocation.PlotNumber)] = BurialLocation.MaxPlotNumberLength,
            [nameof(BurialLocation.GraveNumber)] = BurialLocation.MaxGraveNumberLength
        };
        var tooLong = new string('a', maxLengths[field] + 1);

        // Act: подставляем длинное значение в нужное поле.
        var result = BurialLocation.Create(
            latitude: 55.0,
            longitude: 37.0,
            country: field == nameof(BurialLocation.Country) ? tooLong : null,
            region: field == nameof(BurialLocation.Region) ? tooLong : null,
            city: field == nameof(BurialLocation.City) ? tooLong : null,
            cemeteryName: field == nameof(BurialLocation.CemeteryName) ? tooLong : null,
            plotNumber: field == nameof(BurialLocation.PlotNumber) ? tooLong : null,
            graveNumber: field == nameof(BurialLocation.GraveNumber) ? tooLong : null);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedCode);
    }

    /// <summary>
    /// FullAddress собирает строку через ", " только из заполненных
    /// полей. Если только Country и City — между ними нет пустого
    /// "" из Region. Защита от UI-багов "Россия, , Москва".
    /// </summary>
    [Fact]
    public void FullAddress_OnlyCountryAndCity_BuildsTwoPartString()
    {
        var location = BurialLocation.Create(
            latitude: 55.0,
            longitude: 37.0,
            country: "Россия",
            city: "Москва").Value;

        location.FullAddress.Should().Be("Россия, Москва");
    }

    /// <summary>
    /// FullAddress без всех текстовых полей — пустая строка
    /// (Trim'ed Join over empty enumerable).
    /// </summary>
    [Fact]
    public void FullAddress_NoTextFields_ReturnsEmptyString()
    {
        var location = BurialLocation.Create(latitude: 55.0, longitude: 37.0).Value;
        location.FullAddress.Should().Be(string.Empty);
    }
}
