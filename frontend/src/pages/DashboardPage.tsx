import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import {
  ArrowRight,
  ArrowUpRight,
  CheckCircle2,
  ClipboardList,
  FileSearch,
  Layers3,
  ListOrdered,
  Plus,
  RefreshCw,
  Sparkles,
} from "lucide-react";
import { getErrorMessage } from "../api/client";
import { reviewApi } from "../api/reviewApi";
import { taskApi } from "../api/taskApi";
import type {
  PagedResult,
  ReviewQueueItemDto,
  ReviewRequestDetailsDto,
} from "../api/types";
import { ReviewStatusBadge } from "../components/Badges";
import { PageHeader } from "../components/PageHeader";
import { ErrorState, LoadingState } from "../components/States";
import { formatDateTime, shortId, truncate } from "../lib/format";

interface DashboardData {
  taskTotal: number;
  publishedTotal: number;
  queuedTotal: number;
  completedTotal: number;
  reviews: PagedResult<ReviewRequestDetailsDto>;
  queue: ReviewQueueItemDto[];
}

export function DashboardPage() {
  const [data, setData] = useState<DashboardData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tick, setTick] = useState(0);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const [
        tasks,
        publishedTasks,
        queued,
        completed,
        reviews,
        queue,
      ] = await Promise.all([
        taskApi.list({ page: 1, pageSize: 1 }),
        taskApi.list({ status: "Published", page: 1, pageSize: 1 }),
        reviewApi.list({ status: "Queued", page: 1, pageSize: 1 }),
        reviewApi.list({ status: "Completed", page: 1, pageSize: 1 }),
        reviewApi.list({ page: 1, pageSize: 5 }),
        reviewApi.getQueue(4),
      ]);

      setData({
        taskTotal: tasks.totalCount,
        publishedTotal: publishedTasks.totalCount,
        queuedTotal: queued.totalCount,
        completedTotal: completed.totalCount,
        reviews,
        queue,
      });
    } catch (loadError) {
      setError(getErrorMessage(loadError));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load, tick]);

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="Панель смотрителя"
        title="Турнирные задачи и проверки"
        description="Один экран для публикации задач, управления очередью и контроля работы жюри."
        actions={
          <>
            <button
              className="btn btn--secondary"
              type="button"
              onClick={() => setTick((value) => value + 1)}
              disabled={loading}
            >
              <RefreshCw size={16} className={loading ? "spin" : undefined} />
              Обновить
            </button>
            <Link className="btn btn--primary" to="/tasks?new=1">
              <Plus size={16} />
              Новая задача
            </Link>
          </>
        }
      />

      {loading && !data ? <LoadingState /> : null}
      {error && !data ? (
        <ErrorState
          message={error}
          onRetry={() => setTick((value) => value + 1)}
        />
      ) : null}

      {data ? (
        <>
          <section className="hero-card">
            <div className="hero-card__content">
              <span className="hero-card__eyebrow">
                <Sparkles size={15} />
                Сквозной сценарий
              </span>
              <h2>От черновика задачи до вердикта жюри</h2>
              <p>
                Опубликуйте задачу в TaskService, создайте запрос на проверку,
                поставьте его в очередь и назначьте жюри. Снимок задачи
                сохранится в ReviewService.
              </p>
              <div className="hero-card__actions">
                <Link className="btn btn--primary" to="/tasks">
                  Перейти к задачам
                  <ArrowRight size={16} />
                </Link>
                <Link className="btn btn--hero-ghost" to="/jury">
                  Открыть кабинет жюри
                </Link>
              </div>
            </div>
            <div className="hero-card__flow" aria-label="Поток обработки">
              <FlowStep number="01" title="Задача" text="Draft → Published" />
              <span className="flow-line" />
              <FlowStep number="02" title="Очередь" text="Created → Queued" />
              <span className="flow-line" />
              <FlowStep number="03" title="Жюри" text="Assign → Complete" />
            </div>
          </section>

          <section className="stats-grid">
            <StatCard
              icon={ClipboardList}
              label="Всего задач"
              value={data.taskTotal}
              hint={`${data.publishedTotal} опубликовано`}
              tone="indigo"
            />
            <StatCard
              icon={Layers3}
              label="Опубликовано"
              value={data.publishedTotal}
              hint="доступно для проверок"
              tone="green"
            />
            <StatCard
              icon={ListOrdered}
              label="В очереди"
              value={data.queuedTotal}
              hint="ожидают назначения"
              tone="amber"
            />
            <StatCard
              icon={CheckCircle2}
              label="Завершено"
              value={data.completedTotal}
              hint="вердикт зафиксирован"
              tone="violet"
            />
          </section>

          <div className="dashboard-grid">
            <section className="card card--table">
              <div className="card__head">
                <div>
                  <h2>Последние запросы</h2>
                  <p>Административный список с данными команды и решения.</p>
                </div>
                <Link className="text-link" to="/reviews">
                  Все проверки <ArrowUpRight size={15} />
                </Link>
              </div>

              {data.reviews.items.length === 0 ? (
                <div className="inline-empty">
                  <FileSearch size={22} />
                  <span>Запросов пока нет. Создайте первый из опубликованной задачи.</span>
                  <Link className="text-link" to="/reviews">
                    Создать запрос
                  </Link>
                </div>
              ) : (
                <div className="mini-list">
                  {data.reviews.items.map((review) => (
                    <Link
                      className="mini-row"
                      to="/reviews"
                      key={review.id}
                    >
                      <span className="mini-row__main">
                        <strong>{truncate(review.taskStatement, 62)}</strong>
                        <small>
                          {formatDateTime(review.createdAt)} · команда{" "}
                          {shortId(review.teamId)}
                        </small>
                      </span>
                      <ReviewStatusBadge status={review.status} />
                    </Link>
                  ))}
                </div>
              )}
            </section>

            <section className="card">
              <div className="card__head">
                <div>
                  <h2>Очередь сейчас</h2>
                  <p>Самые ранние запросы без данных команды.</p>
                </div>
                <Link className="text-link" to="/queue">
                  В очередь <ArrowUpRight size={15} />
                </Link>
              </div>

              {data.queue.length === 0 ? (
                <div className="inline-empty inline-empty--compact">
                  <ListOrdered size={22} />
                  <span>Очередь пуста.</span>
                </div>
              ) : (
                <div className="queue-preview">
                  {data.queue.map((item, index) => (
                    <div className="queue-preview__item" key={item.id}>
                      <span className="queue-preview__number">
                        {String(index + 1).padStart(2, "0")}
                      </span>
                      <span className="queue-preview__copy">
                        <strong>{truncate(item.taskStatement, 58)}</strong>
                        <small>
                          в очереди {formatDateTime(item.queuedAt)}
                        </small>
                      </span>
                    </div>
                  ))}
                </div>
              )}
            </section>
          </div>

          {error ? (
            <div className="inline-warning">
              Не удалось обновить часть данных: {error}
            </div>
          ) : null}
        </>
      ) : null}
    </div>
  );
}

function StatCard({
  icon: Icon,
  label,
  value,
  hint,
  tone,
}: {
  icon: typeof ClipboardList;
  label: string;
  value: number;
  hint: string;
  tone: "indigo" | "green" | "amber" | "violet";
}) {
  return (
    <div className={`stat-card stat-card--${tone}`}>
      <span className="stat-card__icon">
        <Icon size={20} />
      </span>
      <div>
        <span className="stat-card__label">{label}</span>
        <strong>{value}</strong>
        <small>{hint}</small>
      </div>
    </div>
  );
}

function FlowStep({
  number,
  title,
  text,
}: {
  number: string;
  title: string;
  text: string;
}) {
  return (
    <div className="flow-step">
      <span>{number}</span>
      <strong>{title}</strong>
      <small>{text}</small>
    </div>
  );
}
