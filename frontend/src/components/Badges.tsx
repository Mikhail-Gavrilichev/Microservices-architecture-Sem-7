import type {
  GameTaskStatus,
  ReviewSource,
  ReviewStatus,
  ReviewVerdict,
} from "../api/types";
import {
  reviewSourceLabels,
  reviewStatusLabels,
  reviewVerdictLabels,
  taskStatusLabels,
} from "../lib/format";

type BadgeTone =
  | "neutral"
  | "info"
  | "amber"
  | "violet"
  | "green"
  | "red"
  | "blue";

function Badge({
  tone,
  children,
  dot = true,
}: {
  tone: BadgeTone;
  children: string;
  dot?: boolean;
}) {
  return (
    <span className={`badge badge--${tone}`}>
      {dot ? <span className="badge__dot" /> : null}
      {children}
    </span>
  );
}

const reviewStatusTones: Record<ReviewStatus, BadgeTone> = {
  Created: "neutral",
  Queued: "amber",
  Assigned: "blue",
  Accepted: "violet",
  Refused: "red",
  TransferredToSenior: "info",
  Completed: "green",
};

export function TaskStatusBadge({ status }: { status: GameTaskStatus }) {
  return (
    <Badge tone={status === "Published" ? "green" : "neutral"}>
      {taskStatusLabels[status]}
    </Badge>
  );
}

export function ReviewStatusBadge({ status }: { status: ReviewStatus }) {
  return <Badge tone={reviewStatusTones[status]}>{reviewStatusLabels[status]}</Badge>;
}

export function SourceBadge({ source }: { source: ReviewSource }) {
  return (
    <span className={`source-tag source-tag--${source.toLowerCase()}`}>
      {reviewSourceLabels[source]}
    </span>
  );
}

export function VerdictBadge({ verdict }: { verdict: ReviewVerdict }) {
  return (
    <Badge tone={verdict === "Correct" ? "green" : "red"} dot={false}>
      {reviewVerdictLabels[verdict]}
    </Badge>
  );
}
