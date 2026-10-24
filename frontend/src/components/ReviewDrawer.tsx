import { useEffect, useState } from "react";
import {
  ArrowRightLeft,
  CalendarClock,
  Check,
  ClipboardCheck,
  Dices,
  EyeOff,
  ListPlus,
  ShieldCheck,
  UserRoundCheck,
  X,
} from "lucide-react";
import { getErrorMessage } from "../api/client";
import { reviewApi } from "../api/reviewApi";
import type { ReviewRequestDetailsDto } from "../api/types";
import { useJuryId } from "../hooks/useJuryId";
import {
  formatDateTime,
  isGuid,
  newGuid,
  reviewSourceLabels,
  reviewStatusLabels,
  reviewVerdictLabels,
} from "../lib/format";
import {
  ReviewStatusBadge,
  SourceBadge,
  VerdictBadge,
} from "./Badges";
import { Drawer } from "./Drawer";
import { LoadingState } from "./States";
import { useToast } from "./ToastProvider";

export function ReviewDrawer({
  reviewId,
  onClose,
  onChanged,
}: {
  reviewId: string | null;
  onClose: () => void;
  onChanged: () => void;
}) {
  const { notify } = useToast();
  const [savedJuryId, setSavedJuryId] = useJuryId();
  const [review, setReview] = useState<ReviewRequestDetailsDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [actionBusy, setActionBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [assignJuryId, setAssignJuryId] = useState(savedJuryId);
  const [seniorJuryId, setSeniorJuryId] = useState("");
  const [reloadTick, setReloadTick] = useState(0);

  useEffect(() => {
    if (!reviewId) {
      setReview(null);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);
    setAssignJuryId(savedJuryId);

    reviewApi
      .get(reviewId)
      .then((data) => {
        if (!cancelled) {
          setReview(data);
        }
      })
      .catch((loadError: unknown) => {
        if (!cancelled) {
          setError(getErrorMessage(loadError));
        }
      })
      .finally(() => {
        if (!cancelled) {
          setLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [reviewId, reloadTick, savedJuryId]);

  const refresh = () => {
    setReloadTick((value) => value + 1);
    onChanged();
  };

  const handleEnqueue = async () => {
    if (!review) {
      return;
    }

    setActionBusy(true);
    setError(null);

    try {
      await reviewApi.enqueue(review.id);
      notify({
        kind: "success",
        title: "Запрос добавлен в очередь",
        message: "Теперь его можно назначить члену жюри.",
      });
      refresh();
    } catch (actionError) {
      setError(getErrorMessage(actionError));
    } finally {
      setActionBusy(false);
    }
  };

  const handleAssign = async () => {
    if (!review) {
      return;
    }

    if (!isGuid(assignJuryId)) {
      setError("Укажите корректный GUID члена жюри.");
      return;
    }

    setActionBusy(true);
    setError(null);

    try {
      await reviewApi.assign(review.id, assignJuryId.trim());
      setSavedJuryId(assignJuryId.trim());
      notify({
        kind: "success",
        title: "Проверка назначена",
        message: "Запрос появился в кабинете указанного члена жюри.",
      });
      refresh();
    } catch (actionError) {
      setError(getErrorMessage(actionError));
    } finally {
      setActionBusy(false);
    }
  };

  const handleTransfer = async () => {
    if (!review) {
      return;
    }

    if (!isGuid(seniorJuryId)) {
      setError("Укажите корректный GUID старшего члена жюри.");
      return;
    }

    if (seniorJuryId.trim() === review.assignedJuryMemberId) {
      setError("Нельзя передать проверку тому же члену жюри, который отказался.");
      return;
    }

    setActionBusy(true);
    setError(null);

    try {
      await reviewApi.transfer(review.id, seniorJuryId.trim());
      setSavedJuryId(seniorJuryId.trim());
      notify({
        kind: "success",
        title: "Проверка передана старшему",
        message: "Старший член жюри увидит её в своём кабинете.",
      });
      refresh();
    } catch (actionError) {
      setError(getErrorMessage(actionError));
    } finally {
      setActionBusy(false);
    }
  };

  const timeline = review
    ? [
        { label: "Создан", at: review.createdAt, icon: CalendarClock },
        { label: "В очереди", at: review.queuedAt, icon: ListPlus },
        {
          label: "Назначен",
          at: review.assignedAt,
          icon: UserRoundCheck,
        },
        { label: "Принят", at: review.acceptedAt, icon: Check },
        { label: "Отказ", at: review.refusedAt, icon: X },
        {
          label: "Передан старшему",
          at: review.transferredAt,
          icon: ArrowRightLeft,
        },
        {
          label: "Завершён",
          at: review.completedAt,
          icon: ClipboardCheck,
        },
      ].filter((item) => Boolean(item.at))
    : [];

  return (
    <Drawer
      open={reviewId !== null}
      title={review ? `Проверка ${review.id.slice(0, 8)}` : "Запрос на проверку"}
      subtitle="Административное представление для смотрителя"
      onClose={onClose}
      footer={
        <div className="drawer__actions">
          <button
            className="btn btn--ghost"
            type="button"
            onClick={onClose}
            disabled={actionBusy}
          >
            Закрыть
          </button>
        </div>
      }
    >
      {loading ? <LoadingState label="Открываем запрос…" /> : null}

      {!loading && error && !review ? (
        <div className="form-error">{error}</div>
      ) : null}

      {!loading && review ? (
        <div className="detail-stack">
          <div className="detail-topline detail-topline--wrap">
            <ReviewStatusBadge status={review.status} />
            <SourceBadge source={review.source} />
            {review.verdict ? <VerdictBadge verdict={review.verdict} /> : null}
          </div>

          <div className="admin-note">
            <ShieldCheck size={18} />
            <span>
              Это административное представление: команда и решение видны
              смотрителю, но не попадают в ответы для жюри.
            </span>
          </div>

          {error ? <div className="form-error">{error}</div> : null}

          <div className="meta-grid meta-grid--two">
            <div>
              <span className="meta-grid__label">Задача</span>
              <strong className="mono-value">{review.taskId}</strong>
            </div>
            <div>
              <span className="meta-grid__label">Команда</span>
              <strong className="mono-value">{review.teamId}</strong>
            </div>
            <div>
              <span className="meta-grid__label">Решение</span>
              <strong className="mono-value">{review.solutionId}</strong>
            </div>
            <div>
              <span className="meta-grid__label">Назначено жюри</span>
              <strong className="mono-value">
                {review.assignedJuryMemberId ?? "—"}
              </strong>
            </div>
          </div>

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

          {review.source === "Appeal" ? (
            <div className="read-block">
              <span className="read-block__label">Апелляция</span>
              <p className="mono-value">{review.sourceReferenceId ?? "—"}</p>
            </div>
          ) : null}

          {review.refusalReason ? (
            <div className="read-block read-block--danger">
              <span className="read-block__label">Причина отказа</span>
              <p>{review.refusalReason}</p>
            </div>
          ) : null}

          {review.status === "Completed" && review.verdict ? (
            <div className="verdict-panel">
              <span>
                <EyeOff size={18} /> Вердикт жюри
              </span>
              <strong>{reviewVerdictLabels[review.verdict]}</strong>
              {review.comment ? <p>{review.comment}</p> : null}
            </div>
          ) : null}

          <section className="section-block">
            <div className="section-block__head">
              <h3>Действия смотрителя</h3>
            </div>

            {review.status === "Created" ? (
              <div className="action-panel">
                <p>
                  Запрос создан. Добавьте его в очередь, чтобы назначить
                  проверяющего.
                </p>
                <button
                  className="btn btn--primary"
                  type="button"
                  onClick={handleEnqueue}
                  disabled={actionBusy}
                >
                  {actionBusy ? (
                    <span className="spinner spinner--sm" />
                  ) : (
                    <ListPlus size={16} />
                  )}
                  Добавить в очередь
                </button>
              </div>
            ) : null}

            {review.status === "Queued" ? (
              <div className="action-panel">
                <p>Назначьте проверку конкретному члену жюри.</p>
                <div className="input-with-action">
                  <input
                    className="input input--mono"
                    value={assignJuryId}
                    onChange={(event) => setAssignJuryId(event.target.value)}
                    placeholder="GUID члена жюри"
                  />
                  <button
                    className="icon-button icon-button--input"
                    type="button"
                    onClick={() => setAssignJuryId(newGuid())}
                    title="Сгенерировать GUID"
                    aria-label="Сгенерировать ID жюри"
                  >
                    <Dices size={17} />
                  </button>
                </div>
                <button
                  className="btn btn--primary"
                  type="button"
                  onClick={handleAssign}
                  disabled={actionBusy}
                >
                  {actionBusy ? (
                    <span className="spinner spinner--sm" />
                  ) : (
                    <UserRoundCheck size={16} />
                  )}
                  Назначить
                </button>
              </div>
            ) : null}

            {review.status === "Refused" ? (
              <div className="action-panel">
                <p>
                  Жюри отказалось от проверки. Передайте её старшему члену
                  жюри — тому же самому передать нельзя.
                </p>
                <div className="input-with-action">
                  <input
                    className="input input--mono"
                    value={seniorJuryId}
                    onChange={(event) => setSeniorJuryId(event.target.value)}
                    placeholder="GUID старшего члена жюри"
                  />
                  <button
                    className="icon-button icon-button--input"
                    type="button"
                    onClick={() => setSeniorJuryId(newGuid())}
                    title="Сгенерировать GUID"
                    aria-label="Сгенерировать ID старшего жюри"
                  >
                    <Dices size={17} />
                  </button>
                </div>
                <button
                  className="btn btn--primary"
                  type="button"
                  onClick={handleTransfer}
                  disabled={actionBusy}
                >
                  {actionBusy ? (
                    <span className="spinner spinner--sm" />
                  ) : (
                    <ArrowRightLeft size={16} />
                  )}
                  Передать старшему
                </button>
              </div>
            ) : null}

            {review.status === "Assigned" ||
            review.status === "Accepted" ||
            review.status === "TransferredToSenior" ? (
              <div className="action-panel action-panel--info">
                <p>
                  Проверка находится у жюри. Принять, отказаться или завершить
                  её можно в кабинете жюри.
                </p>
                <span className="muted-inline">
                  Статус: {reviewStatusLabels[review.status]}
                </span>
              </div>
            ) : null}

            {review.status === "Completed" ? (
              <div className="action-panel action-panel--success">
                <p>Проверка завершена, вердикт зафиксирован.</p>
                <span className="muted-inline">
                  Источник: {reviewSourceLabels[review.source]}
                </span>
              </div>
            ) : null}
          </section>

          <section className="section-block">
            <div className="section-block__head">
              <h3>Хронология</h3>
            </div>
            <div className="timeline">
              {timeline.map((item) => {
                const Icon = item.icon;
                return (
                  <div className="timeline__item" key={item.label}>
                    <span className="timeline__icon">
                      <Icon size={15} />
                    </span>
                    <div>
                      <strong>{item.label}</strong>
                      <span>{formatDateTime(item.at)}</span>
                    </div>
                  </div>
                );
              })}
            </div>
          </section>
        </div>
      ) : null}
    </Drawer>
  );
}
