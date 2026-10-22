using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TaskService.Api.ExceptionHandling;
using TaskService.Api.Extensions;
using TaskService.Application;
using TaskService.Infrastructure;
using TaskService.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Task Service API",
        Version = "v1",
        Description = "Сервис подготовки задач на игру. Агрегат «Задача» из EventStorming."
    });
});

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks()
    .AddDbContextCheck<TaskDbContext>("postgres");

var app = builder.Build();

app.UseExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Task Service API v1");
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
