import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  ArrowRight,
  ArrowUpRight,
  Dices,
  ListOrdered,
  RefreshCw,
  ShieldCheck,
  UserRoundCheck,
} from "lucide-react";
import { getErrorMessage } from "../api/client";
import { reviewApi } from "../api/reviewApi";
import type { JuryReviewDto, ReviewQueueItemDto } from "../api/types";
import { PageHeader } from "../components/PageHeader";
import { EmptyState, ErrorState, LoadingState } from "../components/States";
import { useToast } from "../components/ToastProvider";
import { useJuryId } from "../hooks/useJuryId";
import {
  formatDateTime,
  isGuid,
  newGuid,
  pluralize,
  truncate,
} from "../lib/format";

export function QueuePage() {
  const { notify } = useToast();
  const [juryId, setJuryId] = useJuryId();
  const [queue, setQueue] = useState<ReviewQueueItemDto[] | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [actionBusy, setActionBusy] = useState(false);
  const [lastAssigned, setLastAssigned] = useState<JuryReviewDto | null>(null);
  const [tick, setTick] = useState(0);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const items = await reviewApi.getQueue(50);
      setQueue(items);
    } catch (loadError) {
      setError(getErrorMessage(loadError));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load, tick]);

  const guardJuryId = () => {
    if (!isGuid(juryId)) {
      setError("Сначала укажите корректный GUID члена жюри.");
      return false;
    }

    return true;
  };

  const handleAssignNext = async () => {
    if (!guardJuryId()) {
      return;
    }

    setActionBusy(true);
    setError(null);

    try {
      const assigned = await reviewApi.assignNext(juryId.trim());

      if (!assigned) {
        notify({
          kind: "info",
          title: "Очередь пуста",
          message: "Сейчас нет запросов, ожидающих назначения.",
        });
        return;
      }

      setLastAssigned(assigned);
      setJuryId(juryId.trim());
      notify({
        kind: "success",
        title: "Следующая проверка назначена",
        message: "Она появилась в кабинете указанного члена жюри.",
      });
      setTick((value) => value + 1);
    } catch (actionError) {
      setError(getErrorMessage(actionError));
    } finally {
      setActionBusy(false);
    }
  };

  const handleAssign = async (reviewId: string) => {
    if (!guardJuryId()) {
      return;
    }

    setActionBusy(true);
    setError(null);

    try {
      await reviewApi.assign(reviewId, juryId.trim());
      setJuryId(juryId.trim());
      notify({
        kind: "success",
        title: "Проверка назначена",
        message: "Запрос убран из очереди.",
      });
      setLastAssigned(null);
      setTick((value) => value + 1);
    } catch (actionError) {
      setError(getErrorMessage(actionError));
    } finally {
      setActionBusy(false);
    }
  };

  const juryIdValid = isGuid(juryId);

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="ReviewService · политика назначения"
        title="Очередь проверок"
        description="Самые ранние запросы ожидают назначения. Ответы очереди не содержат команду, решение и рейтинг."
        actions={
          <button
            className="btn btn--secondary"
            type="button"
            onClick={() => setTick((value) => value + 1)}
            disabled={loading}
          >
            <RefreshCw size={16} className={loading ? "spin" : undefined} />
            Обновить
          </button>
        }
      />

      <section className="jury-console-card">
        <div className="jury-console-card__copy">
          <span className="eyebrow eyebrow--light">Доступный член жюри</span>
          <h2>Назначить следующую проверку</h2>
          <p>
            Будет выбран самый ранний элемент очереди. ID жюри сохраняется в
            браузере и используется в кабинете жюри.
          </p>
        </div>

        <div className="jury-console-card__form">
          <label className="field field--on-dark">
            <span className="field__label">ID члена жюри</span>
            <div className="input-with-action">
              <input
                className="input input--mono"
                value={juryId}
                onChange={(event) => setJuryId(event.target.value)}
                placeholder="GUID члена жюри"
              />
              <button
                className="icon-button icon-button--input"
                type="button"
                onClick={() => setJuryId(newGuid())}
                title="Сгенерировать GUID"
                aria-label="Сгенерировать ID жюри"
              >
                <Dices size={17} />
              </button>
            </div>
          </label>

          <button
            className="btn btn--accent"
            type="button"
            onClick={handleAssignNext}
            disabled={actionBusy || !juryIdValid || queue?.length === 0}
          >
            {actionBusy ? (
              <span className="spinner spinner--sm" />
            ) : (
              <UserRoundCheck size={17} />
            )}
            Назначить следующую
          </button>
        </div>
      </section>

      {lastAssigned ? (
        <div className="success-banner">
          <span className="success-banner__icon">
            <UserRoundCheck size={20} />
          </span>
          <div>
            <strong>Проверка назначена</strong>
            <p>
              {truncate(lastAssigned.taskStatement, 140)}
            </p>
          </div>
          <Link className="btn btn--secondary btn--sm" to="/jury">
            Открыть в кабинете жюри
            <ArrowRight size={15} />
          </Link>
        </div>
      ) : null}

      {error && queue ? <div className="inline-warning">{error}</div> : null}

      {loading && !queue ? <LoadingState label="Загружаем очередь…" /> : null}
      {!loading && !queue && error ? (
        <ErrorState
          message={error}
          onRetry={() => setTick((value) => value + 1)}
        />
      ) : null}

      {queue ? (
        <>
          <div className="list-summary">
            <span>
              {queue.length}{" "}
              {pluralize(queue.length, "запрос", "запроса", "запросов")} в
              очереди
            </span>
            <span className="list-summary__privacy">
              <ShieldCheck size={14} /> без данных команды
            </span>
          </div>

          {queue.length === 0 ? (
            <EmptyState
              icon={ListOrdered}
              title="Очередь пуста"
              description="Добавьте созданный запрос в очередь в разделе «Проверки», и он появится здесь."
              action={
                <Link className="btn btn--secondary" to="/reviews">
                  Перейти к проверкам
                  <ArrowUpRight size={15} />
                </Link>
              }
            />
          ) : (
            <div className="queue-list">
              {queue.map((item, index) => (
                <article className="queue-card" key={item.id}>
                  <span className="queue-card__index">
                    {String(index + 1).padStart(2, "0")}
                  </span>
                  <div className="queue-card__content">
                    <div className="queue-card__meta">
                      <span>Запрос {item.id.slice(0, 8)}</span>
                      <span>·</span>
                      <span>в очереди {formatDateTime(item.queuedAt)}</span>
                    </div>
                    <h3>{truncate(item.taskStatement, 160)}</h3>
                  </div>
                  <button
                    className="btn btn--secondary btn--sm"
                    type="button"
                    onClick={() => handleAssign(item.id)}
                    disabled={actionBusy || !juryIdValid}
                    title={
                      juryIdValid
                        ? "Назначить этому члену жюри"
                        : "Сначала укажите ID члена жюри"
                    }
                  >
                    <UserRoundCheck size={15} />
                    Назначить
                  </button>
                </article>
              ))}
            </div>
          )}

          <div className="privacy-note">
            <ShieldCheck size={18} />
            <p>
              Очередь намеренно не показывает команду, решение и рейтинг.
              Смотритель может увидеть эти данные только в административном
              разделе проверок.
            </p>
          </div>
        </>
      ) : null}
    </div>
  );
}
