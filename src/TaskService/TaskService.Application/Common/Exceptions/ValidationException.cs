using FluentValidation.Results;

namespace TaskService.Application.Common.Exceptions;

/// <summary>
/// Ошибки валидации входного запроса. Транслируются API в HTTP 400 с ProblemDetails.
/// </summary>
public sealed class ValidationException : Exception
{
    public ValidationException(IEnumerable<ValidationFailure> failures)
        : base("Один или несколько параметров запроса некорректны.")
    {
        Errors = failures
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray());
    }

    public IReadOnlyDictionary<string, string[]> Errors { get; }
}
