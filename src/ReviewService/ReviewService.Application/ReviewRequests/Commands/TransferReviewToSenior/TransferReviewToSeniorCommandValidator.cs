using FluentValidation;

namespace ReviewService.Application.ReviewRequests.Commands.TransferReviewToSenior;

public sealed class TransferReviewToSeniorCommandValidator
    : AbstractValidator<TransferReviewToSeniorCommand>
{
    public TransferReviewToSeniorCommandValidator()
    {
        RuleFor(command => command.ReviewRequestId).NotEmpty();
        RuleFor(command => command.SeniorJuryMemberId).NotEmpty();
    }
}
