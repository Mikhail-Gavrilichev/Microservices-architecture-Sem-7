import type { ReactNode } from "react";
import { AlertCircle, Inbox, RefreshCw } from "lucide-react";
import type { LucideIcon } from "lucide-react";

export function LoadingState({ label = "Загружаем данные…" }: { label?: string }) {
  return (
    <div className="state-panel state-panel--loading">
      <span className="spinner" aria-hidden="true" />
      <p>{label}</p>
    </div>
  );
}

export function EmptyState({
  icon: Icon = Inbox,
  title,
  description,
  action,
}: {
  icon?: LucideIcon;
  title: string;
  description: string;
  action?: ReactNode;
}) {
  return (
    <div className="state-panel state-panel--empty">
      <span className="state-panel__icon">
        <Icon size={24} />
      </span>
      <h3>{title}</h3>
      <p>{description}</p>
      {action ? <div className="state-panel__action">{action}</div> : null}
    </div>
  );
}

export function ErrorState({
  message,
  onRetry,
}: {
  message: string;
  onRetry?: () => void;
}) {
  return (
    <div className="state-panel state-panel--error">
      <span className="state-panel__icon">
        <AlertCircle size={24} />
      </span>
      <h3>Не удалось загрузить данные</h3>
      <p>{message}</p>
      {onRetry ? (
        <button className="btn btn--secondary" type="button" onClick={onRetry}>
          <RefreshCw size={16} />
          Повторить
        </button>
      ) : null}
    </div>
  );
}
