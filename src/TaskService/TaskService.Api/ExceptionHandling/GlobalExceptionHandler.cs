using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskService.Application.Common.Exceptions;
using TaskService.Domain.Common;

namespace TaskService.Api.ExceptionHandling;

/// <summary>
/// Единый формат ошибок API (RFC 7807 ProblemDetails).
/// </summary>
public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var (statusCode, title, detail) = exception switch
        {
            ValidationException validationException => (
                StatusCodes.Status400BadRequest,
                "Ошибка валидации",
                validationException.Message),

            DomainException domainException => (
                StatusCodes.Status422UnprocessableEntity,
                "Нарушено бизнес-правило",
                domainException.Message),

            NotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                "Объект не найден",
                notFoundException.Message),

            ConflictException conflictException => (
                StatusCodes.Status409Conflict,
                "Конфликт состояния",
                conflictException.Message),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Внутренняя ошибка сервера",
                "Произошла непредвиденная ошибка. Обратитесь к администратору.")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Необработанная ошибка при обработке {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogInformation("Запрос {Method} {Path} завершился ошибкой {StatusCode}: {Message}",
                httpContext.Request.Method, httpContext.Request.Path, statusCode, exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        if (exception is ValidationException validation && validation.Errors.Count > 0)
        {
            problem.Extensions["errors"] = validation.Errors;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
