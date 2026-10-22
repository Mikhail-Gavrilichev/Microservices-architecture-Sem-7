using MediatR;
using ReviewService.Application.Common.Interfaces;
using ReviewService.Application.ReviewRequests.Dtos;
using ReviewService.Domain.ReviewRequests;

namespace ReviewService.Application.ReviewRequests.Commands.CreateReviewRequest;

public sealed class CreateReviewRequestCommandHandler(
    IReviewRequestRepository repository,
    IUnitOfWork unitOfWork,
    ITaskCatalog taskCatalog,
    TimeProvider timeProvider)
    : IRequestHandler<CreateReviewRequestCommand, ReviewRequestDetailsDto>
{
    public async Task<ReviewRequestDetailsDto> Handle(
        CreateReviewRequestCommand request,
        CancellationToken cancellationToken)
    {
        // Жюри должно получить условие задачи и правильный ответ, но не данные команды.
        // Снимок берётся из TaskService в момент создания запроса, чтобы проверка
        // не зависела от последующих изменений задачи.
        var task = await taskCatalog.GetPublishedTaskAsync(request.TaskId, cancellationToken);

        var reviewRequest = ReviewRequest.Create(
            request.SolutionId,
            request.TaskId,
            task.Statement,
            task.CorrectAnswer,
            request.ParticipantAnswer,
            request.TeamId,
            request.Source,
            request.SourceReferenceId,
            timeProvider.GetUtcNow());

        await repository.AddAsync(reviewRequest, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ReviewRequestDetailsDto.FromDomain(reviewRequest);
    }
}
