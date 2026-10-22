namespace TaskService.Domain.Common;

/// <summary>
/// Маркер доменного события. Доменные события фиксируют уже случившийся факт
/// и не содержат ссылок на инфраструктуру.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}

/// <summary>
/// Базовый класс корня агрегата: хранит доменные события до сохранения агрегата.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents;

    protected void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
