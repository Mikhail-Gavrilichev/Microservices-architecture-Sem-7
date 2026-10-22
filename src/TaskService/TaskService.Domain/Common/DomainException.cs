namespace TaskService.Domain.Common;

/// <summary>
/// Нарушение бизнес-правила агрегата. Транслируется API в HTTP 422.
/// </summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
