using System.Text.Json;
using GdeOni.API.Extensions;
using GdeOni.Domain.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace GdeOni.Api.IntegrationTests.Misc;

/// <summary>
/// D9.5.4 ExceptionMiddleware: unit-тест без factory/Docker. Проверяем,
/// что unhandled-исключение превращается в 500 + обезличенное сообщение
/// (general.internal_server_error), а UniqueConstraintException — в 409.
/// </summary>
public sealed class ExceptionMiddlewareTests
{
    /// <summary>
    /// Unhandled Exception от next-delegate → 500 + JSON с
    /// Errors.General.InternalServerError(). Stacktrace в response не
    /// попадает — наружу уходит только обезличенный errorMessage.
    /// </summary>
    [Fact]
    public async Task UnhandledException_Returns500WithGenericMessage()
    {
        var context = BuildHttpContext();
        var middleware = new ExceptionMiddleware(
            next: _ => throw new InvalidOperationException("secret-internal-detail"),
            logger: NullLogger<ExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().StartWith("application/json");

        var body = await ReadBodyAsync(context);
        body.Should().NotContain("secret-internal-detail");
        body.Should().Contain(Errors.General.InternalServerError().Code);
    }

    /// <summary>
    /// UniqueConstraintException → 409 + errorCode из Errors.UniqueConstraint.FromName.
    /// </summary>
    [Fact]
    public async Task UniqueConstraintException_Returns409()
    {
        var context = BuildHttpContext();
        var middleware = new ExceptionMiddleware(
            next: _ => throw new UniqueConstraintException(DbConstraints.UxUsersEmail),
            logger: NullLogger<ExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        var body = await ReadBodyAsync(context);
        body.Should().Contain("user.email.already.exists");
    }

    private static HttpContext BuildHttpContext()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }
}
