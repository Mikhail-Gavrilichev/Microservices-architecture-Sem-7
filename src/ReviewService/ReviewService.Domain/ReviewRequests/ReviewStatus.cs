namespace ReviewService.Domain.ReviewRequests;

public enum ReviewStatus
{
    Created = 1,
    Queued = 2,
    Assigned = 3,
    Accepted = 4,
    Refused = 5,
    TransferredToSenior = 6,
    Completed = 7
}
