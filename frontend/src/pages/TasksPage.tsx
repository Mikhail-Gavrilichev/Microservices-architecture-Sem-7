import { useEffect, useState, type FormEvent } from "react";
import { useSearchParams } from "react-router-dom";
import {
  ArrowUpRight,
  ClipboardList,
  Plus,
  RefreshCw,
  Search,
} from "lucide-react";
import { getErrorMessage } from "../api/client";
import { taskApi } from "../api/taskApi";
import type {
  GameTaskStatus,
  PagedResult,
  TaskDto,
} from "../api/types";
import { TaskStatusBadge } from "../components/Badges";
import { CreateTaskModal } from "../components/CreateTaskModal";
import { PageHeader } from "../components/PageHeader";
import { Pagination } from "../components/Pagination";
import { EmptyState, ErrorState, LoadingState } from "../components/States";
import { TaskDrawer } from "../components/TaskDrawer";
import { formatDateTime, pluralize, truncate } from "../lib/format";

type StatusFilter = GameTaskStatus | "All";

const statusTabs: Array<{ value: StatusFilter; label: string }> = [
  { value: "All", label: "Все" },
  { value: "Draft", label: "Черновики" },
  { value: "Published", label: "Опубликованные" },
];

export function TasksPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const [result, setResult] = useState<PagedResult<TaskDto> | null>(null);
  const [status, setStatus] = useState<StatusFilter>("All");
  const [search, setSearch] = useState("");
  const [appliedSearch, setAppliedSearch] = useState("");
  const [page, setPage] = useState(1);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [tick, setTick] = useState(0);
  const [createOpen, setCreateOpen] = useState(false);
  const [selectedTaskId, setSelectedTaskId] = useState<string | null>(null);

  useEffect(() => {
    if (searchParams.get("new") === "1") {
      setCreateOpen(true);
      const next = new URLSearchParams(searchParams);
      next.delete("new");
      setSearchParams(next, { replace: true });
    }
  }, [searchParams, setSearchParams]);

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);

    taskApi
      .list({
        status: status === "All" ? undefined : status,
        search: appliedSearch || undefined,
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
  }, [status, appliedSearch, page, tick]);

  const handleSearch = (event: FormEvent) => {
    event.preventDefault();
    setPage(1);
    setAppliedSearch(search.trim());
  };

  const selectStatus = (value: StatusFilter) => {
    setStatus(value);
    setPage(1);
  };

  const refresh = () => setTick((value) => value + 1);

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="TaskService · агрегат «Задача»"
        title="Задачи"
        description="Создавайте черновики, заполняйте условие и правильный ответ, публикуйте задачи для проверки."
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
              Новая задача
            </button>
          </>
        }
      />

      <div className="toolbar">
        <div className="segmented" role="group" aria-label="Фильтр по статусу">
          {statusTabs.map((tab) => (
            <button
              key={tab.value}
              className={`segmented__item${
                status === tab.value ? " segmented__item--active" : ""
              }`}
              type="button"
              onClick={() => selectStatus(tab.value)}
            >
              {tab.label}
            </button>
          ))}
        </div>

        <form className="search-form" onSubmit={handleSearch}>
          <Search size={17} />
          <input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Поиск по названию или условию"
            aria-label="Поиск задач"
          />
          <button className="btn btn--secondary btn--sm" type="submit">
            Найти
          </button>
        </form>
      </div>

      {result ? (
        <div className="list-summary">
          <span>
            {result.totalCount}{" "}
            {pluralize(result.totalCount, "задача", "задачи", "задач")}
          </span>
          {appliedSearch ? (
            <span className="list-summary__filter">
              поиск: «{appliedSearch}»
              <button
                type="button"
                onClick={() => {
                  setSearch("");
                  setAppliedSearch("");
                  setPage(1);
                }}
              >
                сбросить
              </button>
            </span>
          ) : null}
        </div>
      ) : null}

      {loading && !result ? <LoadingState label="Загружаем задачи…" /> : null}

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
              icon={ClipboardList}
              title={appliedSearch ? "Ничего не найдено" : "Задач пока нет"}
              description={
                appliedSearch
                  ? "Попробуйте изменить поисковый запрос или сбросить фильтр."
                  : "Создайте первый черновик, заполните условие и опубликуйте его."
              }
              action={
                !appliedSearch ? (
                  <button
                    className="btn btn--primary"
                    type="button"
                    onClick={() => setCreateOpen(true)}
                  >
                    <Plus size={16} />
                    Создать задачу
                  </button>
                ) : (
                  <button
                    className="btn btn--secondary"
                    type="button"
                    onClick={() => {
                      setSearch("");
                      setAppliedSearch("");
                    }}
                  >
                    Сбросить поиск
                  </button>
                )
              }
            />
          ) : (
            <div className="table-scroll">
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Задача</th>
                    <th>Статус</th>
                    <th>Обновлена</th>
                    <th aria-label="Действия" />
                  </tr>
                </thead>
                <tbody>
                  {result.items.map((task) => (
                    <tr
                      key={task.id}
                      className="clickable-row"
                      onClick={() => setSelectedTaskId(task.id)}
                    >
                      <td>
                        <div className="cell-title">{task.title}</div>
                        <div className="cell-muted">
                          {task.statement
                            ? truncate(task.statement, 92)
                            : "Условие не заполнено"}
                        </div>
                      </td>
                      <td>
                        <TaskStatusBadge status={task.status} />
                      </td>
                      <td className="cell-nowrap">
                        {formatDateTime(task.updatedAt)}
                      </td>
                      <td className="cell-action">
                        <button
                          className="btn btn--ghost btn--sm"
                          type="button"
                          onClick={(event) => {
                            event.stopPropagation();
                            setSelectedTaskId(task.id);
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

      <CreateTaskModal
        open={createOpen}
        onClose={() => setCreateOpen(false)}
        onCreated={(task) => {
          setSelectedTaskId(task.id);
          refresh();
        }}
      />

      <TaskDrawer
        taskId={selectedTaskId}
        onClose={() => setSelectedTaskId(null)}
        onChanged={refresh}
      />
    </div>
  );
}
