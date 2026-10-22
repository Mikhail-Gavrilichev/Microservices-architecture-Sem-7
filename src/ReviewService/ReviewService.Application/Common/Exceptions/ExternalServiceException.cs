namespace ReviewService.Application.Common.Exceptions;

/// <summary>
/// Внешний сервис недоступен или вернул неожиданный ответ. Транслируется в HTTP 503.
/// </summary>
public sealed class ExternalServiceException : Exception
{
    public ExternalServiceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
