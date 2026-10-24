interface ProblemDetailsBody {
  title?: string;
  status?: number;
  detail?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  readonly status: number;
  readonly title?: string;
  readonly detail?: string;
  readonly validationErrors?: Record<string, string[]>;

  constructor(status: number, body: ProblemDetailsBody) {
    super(body.detail ?? body.title ?? `Ошибка сервиса (${status})`);
    this.name = "ApiError";
    this.status = status;
    this.title = body.title;
    this.detail = body.detail;
    this.validationErrors = body.errors;
  }
}

export const taskApiBaseUrl = normalizeBaseUrl(
  import.meta.env.VITE_TASK_API_URL ?? "/task-api",
);

export const reviewApiBaseUrl = normalizeBaseUrl(
  import.meta.env.VITE_REVIEW_API_URL ?? "/review-api",
);

function normalizeBaseUrl(value: string): string {
  return value.replace(/\/+$/, "");
}

export function toQueryString(
  params: Record<string, string | number | boolean | null | undefined>,
): string {
  const search = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined && value !== null && value !== "") {
      search.set(key, String(value));
    }
  }

  const query = search.toString();
  return query ? `?${query}` : "";
}

export async function requestJson<T>(
  baseUrl: string,
  path: string,
  init?: RequestInit,
): Promise<T> {
  let response: Response;

  try {
    response = await fetch(`${baseUrl}${path}`, {
      ...init,
      headers: {
        Accept: "application/json",
        ...(init?.body ? { "Content-Type": "application/json" } : {}),
        ...init?.headers,
      },
    });
  } catch {
    throw new Error(
      "Не удалось соединиться с сервисом. Проверьте, что API и базы данных запущены.",
    );
  }

  const text = await response.text();
  let body: unknown = null;

  if (text) {
    try {
      body = JSON.parse(text) as unknown;
    } catch {
      body = null;
    }
  }

  if (!response.ok) {
    const problem =
      body !== null && typeof body === "object"
        ? (body as ProblemDetailsBody)
        : {};

    throw new ApiError(response.status, problem);
  }

  return body as T;
}

export function getErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const validationMessage = error.validationErrors
      ? Object.values(error.validationErrors).flat().filter(Boolean)[0]
      : undefined;

    if (validationMessage) {
      return validationMessage;
    }

    if (error.status === 503) {
      return (
        error.detail ??
        "Сервис временно недоступен. Проверьте, что TaskService запущен."
      );
    }

    return error.detail ?? error.title ?? error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return "Произошла неизвестная ошибка.";
}
