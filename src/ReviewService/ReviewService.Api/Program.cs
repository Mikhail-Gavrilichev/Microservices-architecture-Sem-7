using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ReviewService.Api.ExceptionHandling;
using ReviewService.Api.Extensions;
using ReviewService.Application;
using ReviewService.Infrastructure;
using ReviewService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Review Service API",
        Version = "v1",
        Description = "Сервис проверки работ. Агрегат «Запрос на проверку» из EventStorming."
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<ReviewDbContext>("postgres");

var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Review Service API v1");
    options.RoutePrefix = "swagger";
});

app.MapControllers();
app.MapHealthChecks("/health");

if (app.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    await app.ApplyMigrationsAsync();
}

await app.RunAsync();

public partial class Program;
