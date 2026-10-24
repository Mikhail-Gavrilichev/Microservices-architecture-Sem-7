export type GameTaskStatus = "Draft" | "Published";

export interface TaskDto {
  id: string;
  title: string;
  statement: string;
  correctAnswer: string;
  status: GameTaskStatus;
  createdAt: string;
  updatedAt: string;
  publishedAt: string | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CreateTaskPayload {
  title: string;
  statement?: string;
  correctAnswer?: string;
}

export type ReviewStatus =
  | "Created"
  | "Queued"
  | "Assigned"
  | "Accepted"
  | "Refused"
  | "TransferredToSenior"
  | "Completed";

export type ReviewSource = "Solution" | "Appeal";

export type ReviewVerdict = "Correct" | "Incorrect";

export interface ReviewRequestDetailsDto {
  id: string;
  solutionId: string;
  taskId: string;
  taskStatement: string;
  correctAnswer: string;
  participantAnswer: string;
  teamId: string;
  source: ReviewSource;
  sourceReferenceId: string | null;
  status: ReviewStatus;
  assignedJuryMemberId: string | null;
  refusalReason: string | null;
  verdict: ReviewVerdict | null;
  comment: string | null;
  createdAt: string;
  updatedAt: string;
  queuedAt: string | null;
  assignedAt: string | null;
  acceptedAt: string | null;
  refusedAt: string | null;
  transferredAt: string | null;
  completedAt: string | null;
}

export interface JuryReviewDto {
  id: string;
  taskId: string;
  taskStatement: string;
  participantAnswer: string;
  correctAnswer: string;
  status: ReviewStatus;
  verdict: ReviewVerdict | null;
  comment: string | null;
  createdAt: string;
  assignedAt: string | null;
  acceptedAt: string | null;
  completedAt: string | null;
}

export interface ReviewQueueItemDto {
  id: string;
  taskId: string;
  taskStatement: string;
  createdAt: string;
  queuedAt: string | null;
}

export interface CreateReviewRequestPayload {
  solutionId: string;
  taskId: string;
  participantAnswer: string;
  teamId: string;
  source: ReviewSource;
  sourceReferenceId?: string | null;
}

export interface TaskListParams {
  status?: GameTaskStatus;
  search?: string;
  page?: number;
  pageSize?: number;
}

export interface ReviewListParams {
  status?: ReviewStatus;
  page?: number;
  pageSize?: number;
}
