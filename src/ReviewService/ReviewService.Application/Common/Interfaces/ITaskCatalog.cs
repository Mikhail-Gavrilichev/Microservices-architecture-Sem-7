using ReviewService.Application.Common.Models;

namespace ReviewService.Application.Common.Interfaces;

public interface ITaskCatalog
{
    /// <summary>
    /// Возвращает снимок опубликованной задачи.
    /// Бросает NotFoundException, если задачи нет, и ConflictException, если она не опубликована.
    /// </summary>
    Task<TaskSnapshot> GetPublishedTaskAsync(Guid taskId, CancellationToken cancellationToken);
}
