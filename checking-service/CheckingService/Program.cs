using System.Text.Json.Serialization;
using CheckingService.Data;
using CheckingService.Services;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
var connection = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Укажите ConnectionStrings__Default.");
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connection));
builder.Services.AddScoped<CheckingWorkflow>();
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.Use(async (context, next) =>
{
    try { await next(context); }
    catch (BusinessException ex)
    {
        context.Response.StatusCode = ex.Code;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message });
    }
    catch (DbUpdateException)
    {
        context.Response.StatusCode = 409;
        await context.Response.WriteAsJsonAsync(new { error = "Конфликт записи: проверка уже существует или жюри занято. Обновите данные." });
    }
    catch (PostgresException ex) when (ex.SqlState is "40001" or "23505")
    {
        context.Response.StatusCode = 409;
        await context.Response.WriteAsJsonAsync(new { error = "Данные изменились одновременно с запросом. Обновите страницу и повторите операцию." });
    }
});
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGet("/api/health", async (AppDbContext db) =>
    await db.Database.CanConnectAsync() ? Results.Ok(new { status = "ok" }) : Results.StatusCode(503));

// Один экземпляр API в учебном Compose: применяем миграции до запуска HTTP-сервера.
using (var scope = app.Services.CreateScope())
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();

app.Run();
