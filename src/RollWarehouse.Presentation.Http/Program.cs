using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RollWarehouse.Application;
using RollWarehouse.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services
    .AddApplication()
    .AddPersistence(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<PersistenceContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger("StartupMigrations");
        logger?.LogError(ex, "Ошибка при применении миграций на старте");
    }
}

var enableSwagger = builder.Configuration.GetValue<bool>("Swagger:Enabled", false);

if (enableSwagger || app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
