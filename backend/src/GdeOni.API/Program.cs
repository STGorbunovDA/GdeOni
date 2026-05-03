using GdeOni.API;
using GdeOni.API.Extensions;
using GdeOni.API.Hosting;
using GdeOni.Application;
using GdeOni.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, config) =>
{
    config.ReadFrom.Configuration(context.Configuration);
});

builder.Services.AddApplication();
builder.Services.AddSecurity(builder.Configuration);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCustomCors(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddCustomSwagger();

var app = builder.Build();

await app.Services.SeedDatabaseAsync();
await app.Services.BootstrapStorageAsync();

// Должно идти раньше остального middleware: исправляет
// Connection.RemoteIpAddress / Scheme до того, как их прочитают
// логи запросов, CORS, аутентификация, RefreshToken.CreatedFromIp
// и т.п. Без конфигурации Hosting:KnownProxies / KnownNetworks —
// no-op (см. D7.38).
app.UseForwardedHeadersIfConfigured(builder.Configuration);

app.UseSerilogRequestLogging();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(GdeOni.API.DependencyInjection.CorsPolicyName);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();