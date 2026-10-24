import { useEffect, useMemo, useState, type FormEvent } from "react";
import { Link } from "react-router-dom";
import {
  ArrowUpRight,
  CheckCircle2,
  Dices,
  KeyRound,
  ListOrdered,
  RefreshCw,
  Scale,
  ShieldCheck,
  UserRoundCheck,
} from "lucide-react";
import { getErrorMessage } from "../api/client";
import { reviewApi } from "../api/reviewApi";
import type { JuryReviewDto } from "../api/types";
import { JuryReviewDrawer } from "../components/JuryReviewDrawer";
import { ReviewStatusBadge, VerdictBadge } from "../components/Badges";
import { PageHeader } from "../components/PageHeader";
import { EmptyState, ErrorState, LoadingState } from "../components/States";
import { useJuryId } from "../hooks/useJuryId";
import {
  formatDateTime,
  isGuid,
  newGuid,
  pluralize,
  truncate,
} from "../lib/format";

type JuryFilter = "active" | "completed" | "all";

const activeStatuses = new Set(["Assigned", "Accepted", "TransferredToSenior"]);

export function JuryPage() {
  const [savedJuryId, setJuryId] = useJuryId();
  const [loadedJuryId, setLoadedJuryId] = useState(() =>
    isGuid(savedJuryId) ? savedJuryId : "",
  );
  const [reviews, setReviews] = useState<JuryReviewDto[] | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [tick, setTick] = useState(0);
  const [filter, setFilter] = useState<JuryFilter>("active");
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [selectedReview, setSelectedReview] = useState<JuryReviewDto | null>(
    null,
  );
  const [detailLoading, setDetailLoading] = useState(false);
  const [detailError, setDetailError] = useState<string | null>(null);

  useEffect(() => {
    if (!loadedJuryId) {
      setReviews(null);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);

    reviewApi
      .getAssigned(loadedJuryId)
      .then((items) => {
        if (!cancelled) {
          setReviews(items);
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
  }, [loadedJuryId, tick]);

  useEffect(() => {
    if (!selectedId || !loadedJuryId) {
      setSelectedReview(null);
      return;
    }

    let cancelled = false;
    setDetailLoading(true);
    setDetailError(null);

    reviewApi
      .getForJury(selectedId, loadedJuryId)
      .then((review) => {
        if (!cancelled) {
          setSelectedReview(review);
        }
      })
      .catch((loadError: unknown) => {
        if (!cancelled) {
          setDetailError(getErrorMessage(loadError));
        }
      })
      .finally(() => {
        if (!cancelled) {
          setDetailLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [selectedId, loadedJuryId, tick]);

  const handleLoad = (event: FormEvent) => {
    event.preventDefault();
    setError(null);

    if (!isGuid(savedJuryId)) {
      setError("Укажите корректный GUID члена жюри.");
      return;
    }

    const normalized = savedJuryId.trim();
    setJuryId(normalized);
    setSelectedId(null);
    setSelectedReview(null);

    if (normalized === loadedJuryId) {
      setTick((value) => value + 1);
    } else {
      setReviews(null);
      setLoadedJuryId(normalized);
    }
  };

  const activeCount = reviews?.filter((review) => activeStatuses.has(review.status)).length ?? 0;
  const completedCount =
    reviews?.filter((review) => review.status === "Completed").length ?? 0;

  const filteredReviews = useMemo(() => {
    if (!reviews) {
      return [];
    }

    if (filter === "active") {
      return reviews.filter((review) => activeStatuses.has(review.status));
    }

    if (filter === "completed") {
      return reviews.filter((review) => review.status === "Completed");
    }

    return reviews;
  }, [reviews, filter]);

  const filters: Array<{ value: JuryFilter; label: string; count: number }> = [
    { value: "active", label: "Активные", count: activeCount },
    { value: "completed", label: "Завершённые", count: completedCount },
    { value: "all", label: "Все", count: reviews?.length ?? 0 },
  ];

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="ReviewService · рабочее место жюри"
        title="Кабинет жюри"
        description="Проверяйте условие, ответ участника и эталон. Команда, решение и рейтинг в этом интерфейсе недоступны."
        actions={
          loadedJuryId ? (
            <button
              className="btn btn--secondary"
              type="button"
              onClick={() => setTick((value) => value + 1)}
              disabled={loading}
            >
              <RefreshCw size={16} className={loading ? "spin" : undefined} />
              Обновить
            </button>
          ) : null
        }
      />

      <section className="jury-access-card">
        <div className="jury-access-card__copy">
          <span className="jury-access-card__icon">
            <Scale size={22} />
          </span>
          <div>
            <h2>Вход по идентификатору жюри</h2>
            <p>
              В этом срезе нет аутентификации: идентификатор передаётся в
              запросах и считается доверенным. Для демо можно сгенерировать
              GUID.
            </p>
          </div>
        </div>

        <form className="jury-access-card__form" onSubmit={handleLoad}>
          <label className="field">
            <span className="field__label">ID члена жюри</span>
            <div className="input-with-action">
              <input
                className="input input--mono"
                value={savedJuryId}
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
          <button className="btn btn--primary" type="submit">
            <KeyRound size={16} />
            Загрузить проверки
          </button>
        </form>
      </section>

      {error ? <div className="inline-warning">{error}</div> : null}

      {!loadedJuryId ? (
        <div className="onboarding-grid">
          <div className="onboarding-card">
            <span className="onboarding-card__number">01</span>
            <KeyRound size={20} />
            <h3>Укажите ID</h3>
            <p>
              Используйте GUID, на который смотритель назначит проверку. ID
              сохранится в браузере.
            </p>
          </div>
          <div className="onboarding-card">
            <span className="onboarding-card__number">02</span>
            <ListOrdered size={20} />
            <h3>Получите назначение</h3>
            <p>
              Смотритель берёт запрос из очереди и назначает его конкретному
              члену жюри.
            </p>
          </div>
          <div className="onboarding-card">
            <span className="onboarding-card__number">03</span>
            <ShieldCheck size={20} />
            <h3>Проверьте вслепую</h3>
            <p>
              В карточке видны только условие, ответ участника и эталон. Данных
              команды нет.
            </p>
          </div>
        </div>
      ) : null}

      {loading && !reviews ? <LoadingState label="Загружаем проверки…" /> : null}
      {!loading && !reviews && error ? (
        <ErrorState
          message={error}
          onRetry={() => setTick((value) => value + 1)}
        />
      ) : null}

      {reviews ? (
        <>
          <section className="jury-summary">
            <div>
              <span className="jury-summary__label">Текущий профиль</span>
              <strong className="mono-value">{loadedJuryId}</strong>
            </div>
            <div className="jury-summary__stats">
              <span>
                <strong>{activeCount}</strong>{" "}
                {pluralize(
                  activeCount,
                  "активная",
                  "активные",
                  "активных",
                )}
              </span>
              <span>
                <strong>{completedCount}</strong> завершено
              </span>
            </div>
          </section>

          <div className="toolbar">
            <div className="segmented" role="group" aria-label="Фильтр проверок">
              {filters.map((item) => (
                <button
                  key={item.value}
                  className={`segmented__item${
                    filter === item.value ? " segmented__item--active" : ""
                  }`}
                  type="button"
                  onClick={() => setFilter(item.value)}
                >
                  {item.label}
                  <span className="segmented__count">{item.count}</span>
                </button>
              ))}
            </div>
            <span className="toolbar-note">
              <ShieldCheck size={15} />
              приватный вид жюри
            </span>
          </div>

          {filteredReviews.length === 0 ? (
            <EmptyState
              icon={filter === "completed" ? CheckCircle2 : UserRoundCheck}
              title={
                filter === "completed"
                  ? "Завершённых проверок пока нет"
                  : filter === "active"
                    ? "Нет активных проверок"
                    : "Проверок пока нет"
              }
              description="Когда смотритель назначит проверку на этот ID, она появится здесь. Очередь находится в отдельном разделе."
              action={
                filter === "active" ? (
                  <Link className="btn btn--secondary" to="/queue">
                    Посмотреть очередь
                    <ArrowUpRight size={15} />
                  </Link>
                ) : undefined
              }
            />
          ) : (
            <div className="jury-review-list">
              {filteredReviews.map((review) => (
                <article
                  className="jury-review-card"
                  key={review.id}
                  onClick={() => setSelectedId(review.id)}
                  onKeyDown={(event) => {
                    if (event.key === "Enter" || event.key === " ") {
                      event.preventDefault();
                      setSelectedId(review.id);
                    }
                  }}
                  role="button"
                  tabIndex={0}
                >
                  <div className="jury-review-card__top">
                    <div className="jury-review-card__badges">
                      <ReviewStatusBadge status={review.status} />
                      {review.verdict ? (
                        <VerdictBadge verdict={review.verdict} />
                      ) : null}
                    </div>
                    <span className="jury-review-card__id">
                      #{review.id.slice(0, 8)}
                    </span>
                  </div>
                  <h3>{truncate(review.taskStatement, 170)}</h3>
                  <div className="jury-review-card__footer">
                    <span>
                      Назначена {formatDateTime(review.assignedAt)}
                    </span>
                    <span className="text-link">
                      {review.status === "Completed"
                        ? "Посмотреть вердикт"
                        : "Открыть проверку"}
                      <ArrowUpRight size={15} />
                    </span>
                  </div>
                </article>
              ))}
            </div>
          )}

          <div className="privacy-note">
            <ShieldCheck size={18} />
            <p>
              {reviews.length}{" "}
              {pluralize(reviews.length, "назначенная проверка", "назначенные проверки", "назначенных проверок")}
              . Сервер использует отдельный <code>JuryReviewDto</code> без
              <code>teamId</code>, <code>solutionId</code> и рейтинга, поэтому
              приватность не зависит от фильтрации на клиенте.
            </p>
          </div>
        </>
      ) : null}

      <JuryReviewDrawer
        open={selectedId !== null}
        review={selectedReview}
        juryId={loadedJuryId}
        loading={detailLoading}
        loadError={detailError}
        onClose={() => {
          setSelectedId(null);
          setSelectedReview(null);
        }}
        onChanged={() => setTick((value) => value + 1)}
      />
    </div>
  );
}
