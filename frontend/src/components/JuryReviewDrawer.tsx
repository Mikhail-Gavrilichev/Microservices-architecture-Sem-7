import { useEffect, useState } from "react";
import {
  Check,
  CheckCircle2,
  ClipboardCheck,
  Clock3,
  RotateCcw,
  ShieldCheck,
  ThumbsDown,
  ThumbsUp,
  X,
} from "lucide-react";
import { getErrorMessage } from "../api/client";
import { reviewApi } from "../api/reviewApi";
import type { JuryReviewDto, ReviewVerdict } from "../api/types";
import { formatDateTime, reviewVerdictLabels } from "../lib/format";
import { ReviewStatusBadge, VerdictBadge } from "./Badges";
import { Drawer } from "./Drawer";
import { Modal } from "./Modal";
import { LoadingState } from "./States";
import { useToast } from "./ToastProvider";

export function JuryReviewDrawer({
  open,
  review,
  juryId,
  loading,
  loadError,
  onClose,
  onChanged,
}: {
  open: boolean;
  review: JuryReviewDto | null;
  juryId: string;
  loading: boolean;
  loadError: string | null;
  onClose: () => void;
  onChanged: () => void;
}) {
  const { notify } = useToast();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [verdict, setVerdict] = useState<ReviewVerdict>("Correct");
  const [comment, setComment] = useState("");
  const [refuseOpen, setRefuseOpen] = useState(false);
  const [reason, setReason] = useState("");
  const [refuseError, setRefuseError] = useState<string | null>(null);

  useEffect(() => {
    setVerdict("Correct");
    setComment(review?.comment ?? "");
    setError(null);
    setReason("");
    setRefuseError(null);
    setRefuseOpen(false);
  }, [review?.id, review?.comment]);

  const canAct =
    review !== null &&
    (review.status === "Assigned" ||
      review.status === "Accepted" ||
      review.status === "TransferredToSenior");
  const canAccept =
    review !== null &&
    (review.status === "Assigned" || review.status === "TransferredToSenior");

  const handleAccept = async () => {
    if (!review) {
      return;
    }

    setBusy(true);
    setError(null);

    try {
      await reviewApi.accept(review.id, juryId);
      notify({
        kind: "success",
        title: "Проверка принята",
        message: "Можно переходить к оценке ответа.",
      });
      onChanged();
    } catch (actionError) {
      setError(getErrorMessage(actionError));
    } finally {
      setBusy(false);
    }
  };

  const handleComplete = async () => {
    if (!review) {
      return;
    }

    if (comment.length > 2000) {
      setError("Комментарий не должен превышать 2000 символов.");
      return;
    }

    setBusy(true);
    setError(null);

    try {
      await reviewApi.complete(review.id, juryId, verdict, comment.trim() || undefined);
      notify({
        kind: "success",
        title: "Проверка завершена",
        message: `Вердикт: ${reviewVerdictLabels[verdict].toLowerCase()}.`,
      });
      onChanged();
    } catch (actionError) {
      setError(getErrorMessage(actionError));
    } finally {
      setBusy(false);
    }
  };

  const handleRefuse = async () => {
    if (!review) {
      return;
    }

    if (!reason.trim()) {
      setRefuseError("Укажите причину отказа.");
      return;
    }

    setBusy(true);
    setRefuseError(null);

    try {
      await reviewApi.refuse(review.id, juryId, reason.trim());
      setRefuseOpen(false);
      notify({
        kind: "info",
        title: "Отказ зафиксирован",
        message: "Запрос ожидает передачи старшему члену жюри.",
      });
      onChanged();
    } catch (actionError) {
      setRefuseError(getErrorMessage(actionError));
    } finally {
      setBusy(false);
    }
  };

  const timeline = review
    ? [
        { label: "Создан", at: review.createdAt },
        { label: "Назначен", at: review.assignedAt },
        { label: "Принят", at: review.acceptedAt },
        { label: "Завершён", at: review.completedAt },
      ].filter((item) => Boolean(item.at))
    : [];

  return (
    <>
      <Drawer
        open={open}
        title={review ? `Проверка ${review.id.slice(0, 8)}` : "Проверка решения"}
        subtitle="Представление для жюри · без команды и решения"
        onClose={onClose}
        footer={
          <div className="drawer__actions">
            <button
              className="btn btn--ghost"
              type="button"
              onClick={onClose}
              disabled={busy}
            >
              Закрыть
            </button>
          </div>
        }
      >
        {loading ? <LoadingState label="Загружаем проверку…" /> : null}

        {!loading && loadError && !review ? (
          <div className="form-error">{loadError}</div>
        ) : null}

        {!loading && review ? (
          <div className="detail-stack">
            <div className="detail-topline detail-topline--wrap">
              <ReviewStatusBadge status={review.status} />
              {review.verdict ? (
                <VerdictBadge verdict={review.verdict} />
              ) : null}
              <span className="detail-topline__id">ID {review.id}</span>
            </div>

            <div className="privacy-note privacy-note--compact">
              <ShieldCheck size={18} />
              <p>
                Жюри проверяет решение, не зная команду и её рейтинг. В этом
                ответе нет <code>teamId</code>, <code>solutionId</code> и
                рейтинга.
              </p>
            </div>

            {error ? <div className="form-error">{error}</div> : null}

            <div className="read-block">
              <span className="read-block__label">Условие задачи</span>
              <p>{review.taskStatement}</p>
            </div>
            <div className="read-block">
              <span className="read-block__label">Ответ участника</span>
              <p>{review.participantAnswer}</p>
            </div>
            <div className="read-block read-block--answer">
              <span className="read-block__label">Правильный ответ</span>
              <p>{review.correctAnswer}</p>
            </div>

            <div className="meta-grid meta-grid--two">
              <div>
                <span className="meta-grid__label">
                  <Clock3 size={14} /> Назначена
                </span>
                <strong>{formatDateTime(review.assignedAt)}</strong>
              </div>
              <div>
                <span className="meta-grid__label">
                  <CheckCircle2 size={14} /> Завершена
                </span>
                <strong>{formatDateTime(review.completedAt)}</strong>
              </div>
            </div>

            {review.status === "Completed" && review.verdict ? (
              <div className="verdict-panel">
                <span>
                  <ClipboardCheck size={18} /> Вердикт
                </span>
                <strong>{reviewVerdictLabels[review.verdict]}</strong>
                {review.comment ? <p>{review.comment}</p> : null}
              </div>
            ) : null}

            {review.status === "Refused" ? (
              <div className="action-panel action-panel--danger">
                <p>
                  Вы отказались от проверки. Запрос ожидает передачи старшему
                  члену жюри.
                </p>
              </div>
            ) : null}

            {canAct ? (
              <section className="section-block">
                <div className="section-block__head">
                  <h3>Действия жюри</h3>
                </div>

                <div className="jury-action-row">
                  {canAccept ? (
                    <button
                      className="btn btn--secondary"
                      type="button"
                      onClick={handleAccept}
                      disabled={busy}
                    >
                      {busy ? (
                        <span className="spinner spinner--sm" />
                      ) : (
                        <Check size={16} />
                      )}
                      Принять проверку
                    </button>
                  ) : (
                    <span className="accepted-indicator">
                      <CheckCircle2 size={16} />
                      Проверка принята
                    </span>
                  )}

                  <button
                    className="btn btn--danger-ghost"
                    type="button"
                    onClick={() => setRefuseOpen(true)}
                    disabled={busy}
                  >
                    <ThumbsDown size={16} />
                    Отказаться
                  </button>
                </div>

                <div className="complete-form">
                  <div className="complete-form__head">
                    <div>
                      <h4>Завершить проверку</h4>
                      <p>Вынесите вердикт по ответу участника.</p>
                    </div>
                    <ClipboardCheck size={20} />
                  </div>

                  <div className="verdict-choice">
                    <label
                      className={`verdict-option${
                        verdict === "Correct" ? " verdict-option--selected" : ""
                      }`}
                    >
                      <input
                        type="radio"
                        name="verdict"
                        value="Correct"
                        checked={verdict === "Correct"}
                        onChange={() => setVerdict("Correct")}
                      />
                      <ThumbsUp size={18} />
                      <span>
                        <strong>Верно</strong>
                        <small>ответ совпадает с эталоном</small>
                      </span>
                    </label>
                    <label
                      className={`verdict-option${
                        verdict === "Incorrect"
                          ? " verdict-option--selected"
                          : ""
                      }`}
                    >
                      <input
                        type="radio"
                        name="verdict"
                        value="Incorrect"
                        checked={verdict === "Incorrect"}
                        onChange={() => setVerdict("Incorrect")}
                      />
                      <ThumbsDown size={18} />
                      <span>
                        <strong>Неверно</strong>
                        <small>ответ не совпадает с эталоном</small>
                      </span>
                    </label>
                  </div>

                  <label className="field">
                    <span className="field__label">Комментарий</span>
                    <textarea
                      className="textarea textarea--compact"
                      value={comment}
                      onChange={(event) => setComment(event.target.value)}
                      maxLength={2000}
                      rows={3}
                      placeholder="Необязательный комментарий к вердикту"
                    />
                    <span className="field__hint">{comment.length} / 2000</span>
                  </label>

                  <button
                    className="btn btn--primary"
                    type="button"
                    onClick={handleComplete}
                    disabled={busy}
                  >
                    {busy ? (
                      <span className="spinner spinner--sm" />
                    ) : (
                      <ClipboardCheck size={16} />
                    )}
                    Завершить проверку
                  </button>
                </div>
              </section>
            ) : null}

            {timeline.length > 0 ? (
              <section className="section-block">
                <div className="section-block__head">
                  <h3>Хронология</h3>
                </div>
                <div className="timeline">
                  {timeline.map((item) => (
                    <div className="timeline__item" key={item.label}>
                      <span className="timeline__icon">
                        <RotateCcw size={15} />
                      </span>
                      <div>
                        <strong>{item.label}</strong>
                        <span>{formatDateTime(item.at)}</span>
                      </div>
                    </div>
                  ))}
                </div>
              </section>
            ) : null}
          </div>
        ) : null}
      </Drawer>

      <Modal
        open={refuseOpen}
        title="Отказаться от проверки?"
        description="Причина отказа будет сохранена. После отказа запрос должен быть передан старшему члену жюри."
        onClose={() => setRefuseOpen(false)}
        footer={
          <>
            <button
              className="btn btn--ghost"
              type="button"
              onClick={() => setRefuseOpen(false)}
              disabled={busy}
            >
              <X size={16} />
              Отмена
            </button>
            <button
              className="btn btn--danger"
              type="button"
              onClick={handleRefuse}
              disabled={busy || !reason.trim()}
            >
              {busy ? <span className="spinner spinner--sm" /> : null}
              Зафиксировать отказ
            </button>
          </>
        }
      >
        {refuseError ? <div className="form-error">{refuseError}</div> : null}

        <label className="field">
          <span className="field__label">
            Причина отказа <span className="field__required">*</span>
          </span>
          <textarea
            className="textarea"
            value={reason}
            onChange={(event) => setReason(event.target.value)}
            maxLength={1000}
            rows={4}
            placeholder="Например: недостаточно контекста для оценки"
            autoFocus
          />
          <span className="field__hint">{reason.length} / 1000</span>
        </label>
      </Modal>
    </>
  );
}
