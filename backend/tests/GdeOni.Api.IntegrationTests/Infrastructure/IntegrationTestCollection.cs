namespace GdeOni.Api.IntegrationTests.Infrastructure;

/// <summary>
/// xUnit-коллекция, объединяющая все integration-тесты под одним
/// инстансом <see cref="GdeOniWebAppFactory"/>. Без коллекции каждая
/// тестовая фикстура (тестовый класс с IClassFixture) поднимала бы
/// собственные контейнеры — это от 30 до 60 секунд оверхеда на класс.
///
/// С [Collection] фабрика создаётся один раз на всю коллекцию,
/// контейнеры тоже одни — тесты выполняются последовательно (xUnit
/// не запускает параллельно тесты внутри одной коллекции).
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<GdeOniWebAppFactory>
{
    public const string Name = "Integration";
}
