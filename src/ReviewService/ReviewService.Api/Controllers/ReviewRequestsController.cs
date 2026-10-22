using MediatR;
using Microsoft.AspNetCore.Mvc;
using ReviewService.Api.Contracts;
using ReviewService.Application.Common.Models;
using ReviewService.Application.ReviewRequests.Commands.AddToQueue;
using ReviewService.Application.ReviewRequests.Commands.CreateReviewRequest;
using ReviewService.Application.ReviewRequests.Dtos;
using ReviewService.Application.ReviewRequests.Queries.GetReviewQueue;
using ReviewService.Application.ReviewRequests.Queries.GetReviewRequest;
using ReviewService.Application.ReviewRequests.Queries.ListReviewRequests;
using ReviewService.Domain.ReviewRequests;

namespace ReviewService.Api.Controllers;

[ApiController]
[Route("api/review-requests")]
public sealed class ReviewRequestsController(ISender sender) : ControllerBase
{
    /// <summary>Создать запрос на проверку (команда «Создать запрос на проверку»).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReviewRequestDetailsDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<ReviewRequestDetailsDto>> Create(
        [FromBody] CreateReviewRequestRequest request,
        CancellationToken cancellationToken)
    {
        var reviewRequest = await sender.Send(
            new CreateReviewRequestCommand(
                request.SolutionId,
                request.TaskId,
                request.ParticipantAnswer,
                request.TeamId,
                request.Source,
                request.SourceReferenceId),
            cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = reviewRequest.Id }, reviewRequest);
    }

    /// <summary>Полное представление запроса для смотрителя (содержит TeamId).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ReviewRequestDetailsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewRequestDetailsDto>> GetById(
        Guid id,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetReviewRequestQuery(id), cancellationToken));

    /// <summary>Список запросов для смотрителя.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReviewRequestDetailsDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReviewRequestDetailsDto>>> List(
        [FromQuery] ReviewStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new ListReviewRequestsQuery(status, page, pageSize), cancellationToken));

    /// <summary>Очередь проверок. Не содержит команду, решение и рейтинг.</summary>
    [HttpGet("queue")]
    [ProducesResponseType(typeof(IReadOnlyList<ReviewQueueItemDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ReviewQueueItemDto>>> GetQueue(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetReviewQueueQuery(limit), cancellationToken));

    /// <summary>Добавить запрос в очередь (команда «Добавить в очередь»).</summary>
    [HttpPost("{id:guid}/queue")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AddToQueue(Guid id, CancellationToken cancellationToken)
    {
        await sender.Send(new AddReviewRequestToQueueCommand(id), cancellationToken);
        return NoContent();
    }
}
