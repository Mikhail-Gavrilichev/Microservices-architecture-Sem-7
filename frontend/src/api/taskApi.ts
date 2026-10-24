import {
  requestJson,
  taskApiBaseUrl,
  toQueryString,
} from "./client";
import type {
  CreateTaskPayload,
  PagedResult,
  TaskDto,
  TaskListParams,
} from "./types";

export const taskApi = {
  list(params: TaskListParams = {}) {
    return requestJson<PagedResult<TaskDto>>(
      taskApiBaseUrl,
      `/tasks${toQueryString({
        status: params.status,
        search: params.search,
        page: params.page ?? 1,
        pageSize: params.pageSize ?? 20,
      })}`,
    );
  },

  get(id: string) {
    return requestJson<TaskDto>(taskApiBaseUrl, `/tasks/${id}`);
  },

  create(payload: CreateTaskPayload) {
    return requestJson<TaskDto>(taskApiBaseUrl, "/tasks", {
      method: "POST",
      body: JSON.stringify(payload),
    });
  },

  changeStatement(id: string, statement: string) {
    return requestJson<void>(taskApiBaseUrl, `/tasks/${id}/statement`, {
      method: "PUT",
      body: JSON.stringify({ statement }),
    });
  },

  changeCorrectAnswer(id: string, correctAnswer: string) {
    return requestJson<void>(taskApiBaseUrl, `/tasks/${id}/correct-answer`, {
      method: "PUT",
      body: JSON.stringify({ correctAnswer }),
    });
  },

  publish(id: string) {
    return requestJson<void>(taskApiBaseUrl, `/tasks/${id}/publish`, {
      method: "POST",
    });
  },
};
