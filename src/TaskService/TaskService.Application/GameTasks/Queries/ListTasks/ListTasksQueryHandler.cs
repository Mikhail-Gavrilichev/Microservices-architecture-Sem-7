using MediatR;
using TaskService.Application.Common.Interfaces;
using TaskService.Application.Common.Models;
using TaskService.Application.GameTasks.Dtos;

namespace TaskService.Application.GameTasks.Queries.ListTasks;

public sealed class ListTasksQueryHandler(IGameTaskRepository repository)
    : IRequestHandler<ListTasksQuery, PagedResult<TaskDto>>
{
    public async Task<PagedResult<TaskDto>> Handle(
        ListTasksQuery request,
        CancellationToken cancellationToken)
    {
        var (items, totalCount) = await repository.ListAsync(
            request.Status,
            request.Search,
            request.Page,
            request.PageSize,
            cancellationToken);

        return new PagedResult<TaskDto>(
            items.Select(TaskDto.FromDomain).ToArray(),
            request.Page,
            request.PageSize,
            totalCount);
    }
}
