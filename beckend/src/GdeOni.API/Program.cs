using GdeOni.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Регистрируем infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Базовые сервисы API
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Swagger только в development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Маршруты контроллеров
app.MapControllers();

app.Run();