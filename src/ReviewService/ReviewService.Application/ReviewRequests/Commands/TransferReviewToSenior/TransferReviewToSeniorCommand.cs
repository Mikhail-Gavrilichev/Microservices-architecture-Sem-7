using MediatR;

namespace ReviewService.Application.ReviewRequests.Commands.TransferReviewToSenior;

/// <summary>Команда «Передать старшему» (политика после отказа).</summary>
public sealed record TransferReviewToSeniorCommand(
    Guid ReviewRequestId,
    Guid SeniorJuryMemberId) : IRequest<Unit>;
