import { useEffect, useState } from "react";
import {
  ArrowUpRight,
  FileSearch,
  Filter,
  ListOrdered,
  Plus,
  RefreshCw,
} from "lucide-react";
import { getErrorMessage } from "../api/client";
import { reviewApi } from "../api/reviewApi";
import type {
  PagedResult,
  ReviewRequestDetailsDto,
  ReviewStatus,
} from "../api/types";
import {
  ReviewStatusBadge,
  SourceBadge,
  VerdictBadge,
} from "../components/Badges";
import { CreateReviewModal } from "../components/CreateReviewModal";
import { PageHeader } from "../components/PageHeader";
import { Pagination } from "../components/Pagination";
import { ReviewDrawer } from "../components/ReviewDrawer";
import { EmptyState, ErrorState, LoadingState } from "../components/States";
import {
  formatDateTime,
  pluralize,
  reviewStatusLabels,
  reviewStatuses,
  shortId,
  truncate,
} from "../lib/format";

type StatusFilter = ReviewStatus | "All";

export function ReviewsPage() {
  const [result, setResult] =
    useState<PagedResult<ReviewRequestDetailsDto> | null>(null);
  const [status, setStatus] = useState<StatusFilter>("All");
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tick, setTick] = useState(0);
  const [createOpen, setCreateOpen] = useState(false);
  const [selectedReviewId, setSelectedReviewId] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    reviewApi
      .list({
        status: status === "All" ? undefined : status,
        page,
        pageSize: 12,
      })
      .then((data) => {
        if (!cancelled) {
          setResult(data);
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
  }, [status, page, tick]);

  const refresh = () => setTick((value) => value + 1);

  const changeStatus = (value: StatusFilter) => {
    setStatus(value);
    setPage(1);
  };

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="ReviewService · административный контур"
        title="Запросы на проверку"
        description="Здесь видны команда, решение и полная история. Данные команды не передаются в кабинет жюри."
        actions={
          <>
            <button
              className="btn btn--secondary"
              type="button"
              onClick={refresh}
              disabled={loading}
            >
              <RefreshCw size={16} className={loading ? "spin" : undefined} />
              Обновить
            </button>
            <button
              className="btn btn--primary"
              type="button"
              onClick={() => setCreateOpen(true)}
            >
              <Plus size={16} />
              Новый запрос
            </button>
          </>
        }
      />

      <div className="toolbar">
        <label className="filter-select">
          <Filter size={16} />
          <span>Статус</span>
          <select
            className="select select--compact"
            value={status}
            onChange={(event) =>
              changeStatus(event.target.value as StatusFilter)
            }
          >
            <option value="All">Все статусы</option>
            {reviewStatuses.map((value) => (
              <option key={value} value={value}>
                {reviewStatusLabels[value]}
              </option>
            ))}
          </select>
        </label>

        <div className="toolbar__right">
          <span className="toolbar-note">
            <ListOrdered size={15} />
            Очередь назначений — в отдельном разделе
          </span>
        </div>
      </div>

      {result ? (
        <div className="list-summary">
          <span>
            {result.totalCount}{" "}
            {pluralize(result.totalCount, "запрос", "запроса", "запросов")}
            {status !== "All" ? ` · ${reviewStatusLabels[status]}` : ""}
          </span>
          <span className="list-summary__privacy">административный вид</span>
        </div>
      ) : null}

      {loading && !result ? <LoadingState label="Загружаем запросы…" /> : null}
      {error && !result ? (
        <ErrorState message={error} onRetry={refresh} />
      ) : null}

      {result ? (
        <div className="table-card">
          {loading ? (
            <div className="table-loading">
              <span className="spinner spinner--sm" />
              Обновляем список…
            </div>
          ) : null}

          {result.items.length === 0 ? (
            <EmptyState
              icon={FileSearch}
              title="Запросов пока нет"
              description="Создайте запрос из опубликованной задачи. Условие и ответ будут сохранены снимком из TaskService."
              action={
                <button
                  className="btn btn--primary"
                  type="button"
                  onClick={() => setCreateOpen(true)}
                >
                  <Plus size={16} />
                  Создать запрос
                </button>
              }
            />
          ) : (
            <div className="table-scroll">
              <table className="data-table data-table--reviews">
                <thead>
                  <tr>
                    <th>Запрос</th>
                    <th>Источник</th>
                    <th>Команда</th>
                    <th>Статус</th>
                    <th>Создан</th>
                    <th aria-label="Действия" />
                  </tr>
                </thead>
                <tbody>
                  {result.items.map((review) => (
                    <tr
                      key={review.id}
                      className="clickable-row"
                      onClick={() => setSelectedReviewId(review.id)}
                    >
                      <td>
                        <div className="cell-title">
                          {truncate(review.taskStatement, 82)}
                        </div>
                        <div className="cell-muted">
                          ID {shortId(review.id)} · решение{" "}
                          {shortId(review.solutionId)}
                        </div>
                      </td>
                      <td>
                        <div className="stacked-badges">
                          <SourceBadge source={review.source} />
                          {review.verdict ? (
                            <VerdictBadge verdict={review.verdict} />
                          ) : null}
                        </div>
                      </td>
                      <td>
                        <span className="mono-chip">{shortId(review.teamId)}</span>
                      </td>
                      <td>
                        <ReviewStatusBadge status={review.status} />
                      </td>
                      <td className="cell-nowrap">
                        {formatDateTime(review.createdAt)}
                      </td>
                      <td className="cell-action">
                        <button
                          className="btn btn--ghost btn--sm"
                          type="button"
                          onClick={(event) => {
                            event.stopPropagation();
                            setSelectedReviewId(review.id);
                          }}
                        >
                          Открыть
                          <ArrowUpRight size={15} />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {error && result ? (
            <div className="inline-warning">{error}</div>
          ) : null}

          {result.items.length > 0 ? (
            <Pagination result={result} onPageChange={setPage} />
          ) : null}
        </div>
      ) : null}

      <CreateReviewModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onCreated={(review) => {
          setSelectedReviewId(review.id);
          refresh();
        }}
      />

      <ReviewDrawer
        reviewId={selectedReviewId}
        onClose={() => setSelectedReviewId(null)}
        onChanged={refresh}
      />
    </div>
  );
}
