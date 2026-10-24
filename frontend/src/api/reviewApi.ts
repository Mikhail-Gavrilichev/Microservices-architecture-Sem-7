import {
  requestJson,
  reviewApiBaseUrl,
  toQueryString,
} from "./client";
import type {
  CreateReviewRequestPayload,
  JuryReviewDto,
  PagedResult,
  ReviewListParams,
  ReviewQueueItemDto,
  ReviewRequestDetailsDto,
  ReviewVerdict,
} from "./types";

export const reviewApi = {
  list(params: ReviewListParams = {}) {
    return requestJson<PagedResult<ReviewRequestDetailsDto>>(
      reviewApiBaseUrl,
      `/review-requests${toQueryString({
        status: params.status,
        page: params.page ?? 1,
        pageSize: params.pageSize ?? 20,
      })}`,
    );
  },

  get(id: string) {
    return requestJson<ReviewRequestDetailsDto>(
      reviewApiBaseUrl,
      `/review-requests/${id}`,
    );
  },

  create(payload: CreateReviewRequestPayload) {
    return requestJson<ReviewRequestDetailsDto>(
      reviewApiBaseUrl,
      "/review-requests",
      {
        method: "POST",
        body: JSON.stringify(payload),
      },
    );
  },

  enqueue(id: string) {
    return requestJson<void>(
      reviewApiBaseUrl,
      `/review-requests/${id}/queue`,
      { method: "POST" },
    );
  },

  getQueue(limit = 50) {
    return requestJson<ReviewQueueItemDto[]>(
      reviewApiBaseUrl,
      `/review-requests/queue${toQueryString({ limit })}`,
    );
  },

  assign(id: string, juryMemberId: string) {
    return requestJson<void>(reviewApiBaseUrl, `/reviews/${id}/assign`, {
      method: "POST",
      body: JSON.stringify({ juryMemberId }),
    });
  },

  assignNext(juryMemberId: string) {
    return requestJson<JuryReviewDto | null>(
      reviewApiBaseUrl,
      "/reviews/assign-next",
      {
        method: "POST",
        body: JSON.stringify({ juryMemberId }),
      },
    );
  },

  accept(id: string, juryMemberId: string) {
    return requestJson<void>(reviewApiBaseUrl, `/reviews/${id}/accept`, {
      method: "POST",
      body: JSON.stringify({ juryMemberId }),
    });
  },

  refuse(id: string, juryMemberId: string, reason: string) {
    return requestJson<void>(reviewApiBaseUrl, `/reviews/${id}/refuse`, {
      method: "POST",
      body: JSON.stringify({ juryMemberId, reason }),
    });
  },

  transfer(id: string, seniorJuryMemberId: string) {
    return requestJson<void>(
      reviewApiBaseUrl,
      `/reviews/${id}/transfer-to-senior`,
      {
        method: "POST",
        body: JSON.stringify({ seniorJuryMemberId }),
      },
    );
  },

  complete(
    id: string,
    juryMemberId: string,
    verdict: ReviewVerdict,
    comment?: string,
  ) {
    return requestJson<void>(reviewApiBaseUrl, `/reviews/${id}/complete`, {
      method: "POST",
      body: JSON.stringify({ juryMemberId, verdict, comment }),
    });
  },

  getAssigned(juryMemberId: string) {
    return requestJson<JuryReviewDto[]>(
      reviewApiBaseUrl,
      `/reviews/assigned/${juryMemberId}`,
    );
  },

  getForJury(id: string, juryMemberId: string) {
    return requestJson<JuryReviewDto>(
      reviewApiBaseUrl,
      `/reviews/${id}/jury/${juryMemberId}`,
    );
  },
};
