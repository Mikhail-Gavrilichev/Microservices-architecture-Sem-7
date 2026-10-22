using MediatR;
using Microsoft.AspNetCore.Mvc;
using ReviewService.Api.Contracts;
using ReviewService.Application.ReviewRequests.Commands.AcceptReview;
using ReviewService.Application.ReviewRequests.Commands.AssignNextReview;
using ReviewService.Application.ReviewRequests.Commands.AssignReview;
using ReviewService.Application.ReviewRequests.Commands.CompleteReview;
using ReviewService.Application.ReviewRequests.Commands.RefuseReview;
using ReviewService.Application.ReviewRequests.Commands.TransferReviewToSenior;
using ReviewService.Application.ReviewRequests.Dtos;
using ReviewService.Application.ReviewRequests.Queries.GetAssignedReviews;
using ReviewService.Application.ReviewRequests.Queries.GetJuryReview;

namespace ReviewService.Api.Controllers;

[ApiController]
[Route("api/reviews")]
public sealed class ReviewsController(ISender sender) : ControllerBase
{
    /// <summary>Назначить проверку конкретному члену жюри.</summary>
    [HttpPost("{id:guid}/assign")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Assign(
        Guid id,
        [FromBody] AssignReviewRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new AssignReviewCommand(id, request.JuryMemberId), cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Политика «назначить следующую работу доступному члену жюри».
    /// Возвращает 204, если очередь пуста.
    /// </summary>
    [HttpPost("assign-next")]
    [ProducesResponseType(typeof(JuryReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult<JuryReviewDto>> AssignNext(
        [FromBody] AssignReviewRequest request,
        CancellationToken cancellationToken)
    {
        var review = await sender.Send(
            new AssignNextReviewCommand(request.JuryMemberId),
            cancellationToken);

        return review is null ? NoContent() : Ok(review);
    }

    /// <summary>Принять проверку.</summary>
    [HttpPost("{id:guid}/accept")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Accept(
        Guid id,
        [FromBody] AssignReviewRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(new AcceptReviewCommand(id, request.JuryMemberId), cancellationToken);
        return NoContent();
    }

    /// <summary>Отказаться от проверки. После отказа запрос передаётся старшему члену жюри.</summary>
    [HttpPost("{id:guid}/refuse")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Refuse(
        Guid id,
        [FromBody] RefuseReviewRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new RefuseReviewCommand(id, request.JuryMemberId, request.Reason),
            cancellationToken);

        return NoContent();
    }

    /// <summary>Передать проверку старшему члену жюри (команда «Передать старшему»).</summary>
    [HttpPost("{id:guid}/transfer-to-senior")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> TransferToSenior(
        Guid id,
        [FromBody] TransferReviewRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new TransferReviewToSeniorCommand(id, request.SeniorJuryMemberId),
            cancellationToken);

        return NoContent();
    }

    /// <summary>Завершить проверку и зафиксировать вердикт.</summary>
    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Complete(
        Guid id,
        [FromBody] CompleteReviewRequest request,
        CancellationToken cancellationToken)
    {
        await sender.Send(
            new CompleteReviewCommand(id, request.JuryMemberId, request.Verdict, request.Comment),
            cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Проверки, назначенные члену жюри. Ответ не содержит команду и рейтинг.
    /// </summary>
    [HttpGet("assigned/{juryMemberId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<JuryReviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<JuryReviewDto>>> GetAssigned(
        Guid juryMemberId,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetAssignedReviewsQuery(juryMemberId), cancellationToken));

    /// <summary>Проверка для жюри. Не раскрывает команду и рейтинг.</summary>
    [HttpGet("{id:guid}/jury/{juryMemberId:guid}")]
    [ProducesResponseType(typeof(JuryReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<JuryReviewDto>> GetForJury(
        Guid id,
        Guid juryMemberId,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetJuryReviewQuery(id, juryMemberId), cancellationToken));
}
