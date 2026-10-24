import type {
  GameTaskStatus,
  ReviewSource,
  ReviewStatus,
  ReviewVerdict,
} from "../api/types";

const dateTimeFormatter = new Intl.DateTimeFormat("ru-RU", {
  day: "2-digit",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

const dateFormatter = new Intl.DateTimeFormat("ru-RU", {
  day: "2-digit",
  month: "long",
  year: "numeric",
});

export function formatDateTime(value: string | null | undefined): string {
  if (!value) {
    return "—";
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "—" : dateTimeFormatter.format(date);
}

export function formatDate(value: string | null | undefined): string {
  if (!value) {
    return "—";
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "—" : dateFormatter.format(date);
}

export function truncate(value: string, length: number): string {
  if (value.length <= length) {
    return value;
  }

  return `${value.slice(0, length).trimEnd()}…`;
}

export function shortId(value: string | null | undefined): string {
  return value ? value.slice(0, 8) : "—";
}

export function pluralize(
  count: number,
  one: string,
  few: string,
  many: string,
): string {
  const mod10 = count % 10;
  const mod100 = count % 100;

  if (mod10 === 1 && mod100 !== 11) {
    return one;
  }

  if (mod10 >= 2 && mod10 <= 4 && (mod100 < 12 || mod100 > 14)) {
    return few;
  }

  return many;
}

export const taskStatusLabels: Record<GameTaskStatus, string> = {
  Draft: "Черновик",
  Published: "Опубликована",
};

export const reviewStatusLabels: Record<ReviewStatus, string> = {
  Created: "Создан",
  Queued: "В очереди",
  Assigned: "Назначен",
  Accepted: "Принят",
  Refused: "Отказ",
  TransferredToSenior: "У старшего",
  Completed: "Завершён",
};

export const reviewSourceLabels: Record<ReviewSource, string> = {
  Solution: "Решение",
  Appeal: "Апелляция",
};

export const reviewVerdictLabels: Record<ReviewVerdict, string> = {
  Correct: "Верно",
  Incorrect: "Неверно",
};

export const reviewStatuses: ReviewStatus[] = [
  "Created",
  "Queued",
  "Assigned",
  "Accepted",
  "Refused",
  "TransferredToSenior",
  "Completed",
];

export const juryActionStatuses: ReviewStatus[] = [
  "Assigned",
  "Accepted",
  "TransferredToSenior",
];

export function isGuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(
    value.trim(),
  );
}

export function newGuid(): string {
  return crypto.randomUUID();
}
