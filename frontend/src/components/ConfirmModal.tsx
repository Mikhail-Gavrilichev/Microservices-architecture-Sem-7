import { AlertTriangle } from "lucide-react";
import { Modal } from "./Modal";

export function ConfirmModal({
  open,
  title,
  message,
  confirmLabel,
  busy = false,
  tone = "primary",
  onConfirm,
  onCancel,
}: {
  open: boolean;
  title: string;
  message: string;
  confirmLabel: string;
  busy?: boolean;
  tone?: "primary" | "danger";
  onConfirm: () => void;
  onCancel: () => void;
}) {
  return (
    <Modal
      open={open}
      title={title}
      onClose={onCancel}
      footer={
        <>
          <button
            className="btn btn--ghost"
            type="button"
            onClick={onCancel}
            disabled={busy}
          >
            Отмена
          </button>
          <button
            className={`btn ${tone === "danger" ? "btn--danger" : "btn--primary"}`}
            type="button"
            onClick={onConfirm}
            disabled={busy}
          >
            {busy ? <span className="spinner spinner--sm" /> : null}
            {confirmLabel}
          </button>
        </>
      }
    >
      <div className="confirm-message">
        <span className="confirm-message__icon">
          <AlertTriangle size={22} />
        </span>
        <p>{message}</p>
      </div>
    </Modal>
  );
}
