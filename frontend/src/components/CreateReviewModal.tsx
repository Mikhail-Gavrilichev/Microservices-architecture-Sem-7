import { useEffect, useState, type FormEvent } from "react";
import { Dices, FilePlus2, Link2 } from "lucide-react";
import { getErrorMessage } from "../api/client";
import { reviewApi } from "../api/reviewApi";
import { taskApi } from "../api/taskApi";
import type {
  ReviewRequestDetailsDto,
  ReviewSource,
  TaskDto,
} from "../api/types";
import { isGuid, newGuid, truncate } from "../lib/format";
import { EmptyState } from "./States";
import { Modal } from "./Modal";
import { useToast } from "./ToastProvider";

export function CreateReviewModal({
  open,
  onClose,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  onCreated: (review: ReviewRequestDetailsDto) => void;
}) {
  const { notify } = useToast();
  const [tasks, setTasks] = useState<TaskDto[]>([]);
  const [tasksLoading, setTasksLoading] = useState(false);
  const [tasksError, setTasksError] = useState<string | null>(null);
  const [taskId, setTaskId] = useState("");
  const [solutionId, setSolutionId] = useState("");
  const [teamId, setTeamId] = useState("");
  const [participantAnswer, setParticipantAnswer] = useState("");
  const [source, setSource] = useState<ReviewSource>("Solution");
  const [sourceReferenceId, setSourceReferenceId] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!open) {
      return;
    }

    setTaskId("");
    setSolutionId(newGuid());
    setTeamId(newGuid());
    setParticipantAnswer("");
    setSource("Solution");
    setSourceReferenceId("");
    setError(null);
    setTasksError(null);
    setTasksLoading(true);

    let cancelled = false;

    taskApi
      .list({ status: "Published", page: 1, pageSize: 100 })
      .then((result) => {
        if (cancelled) {
          return;
        }

        setTasks(result.items);
        if (result.items.length > 0) {
          setTaskId(result.items[0].id);
        }
      })
      .catch((loadError: unknown) => {
        if (!cancelled) {
          setTasksError(getErrorMessage(loadError));
        }
      })
      .finally(() => {
        if (!cancelled) {
          setTasksLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [open]);

  const selectedTask = tasks.find((task) => task.id === taskId) ?? null;

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);

    if (!taskId) {
      setError("Выберите опубликованную задачу.");
      return;
    }

    if (!isGuid(solutionId)) {
      setError("ID решения должен быть корректным GUID.");
      return;
    }

    if (!isGuid(teamId)) {
      setError("ID команды должен быть корректным GUID.");
      return;
    }

    if (!participantAnswer.trim()) {
      setError("Укажите ответ участника.");
      return;
    }

    if (source === "Appeal" && !isGuid(sourceReferenceId)) {
      setError("Для апелляции укажите корректный ID апелляции.");
      return;
    }

    setBusy(true);

    try {
      const review = await reviewApi.create({
        taskId,
        solutionId: solutionId.trim(),
        teamId: teamId.trim(),
        participantAnswer: participantAnswer.trim(),
        source,
        sourceReferenceId:
          source === "Appeal" ? sourceReferenceId.trim() : undefined,
      });

      notify({
        kind: "success",
        title: "Запрос на проверку создан",
        message: "Снимок задачи сохранён. Запрос можно добавить в очередь.",
      });
      onCreated(review);
      onClose();
    } catch (submitError) {
      setError(getErrorMessage(submitError));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Modal
      open={open}
      title="Новый запрос на проверку"
      description="Условие и правильный ответ будут получены из TaskService и сохранены снимком."
      onClose={onClose}
      size="lg"
      footer={
        <>
          <button
            className="btn btn--ghost"
            type="button"
            onClick={onClose}
            disabled={busy}
          >
            Отмена
          </button>
          <button
            className="btn btn--primary"
            type="submit"
            form="create-review-form"
            disabled={busy || tasksLoading || tasks.length === 0}
          >
            {busy ? (
              <span className="spinner spinner--sm" />
            ) : (
              <FilePlus2 size={16} />
            )}
            Создать запрос
          </button>
        </>
      }
    >
      {tasksLoading ? (
        <div className="inline-loader">
          <span className="spinner spinner--sm" />
          Загружаем опубликованные задачи…
        </div>
      ) : null}

      {tasksError ? <div className="form-error">{tasksError}</div> : null}

      {!tasksLoading && tasks.length === 0 ? (
        <EmptyState
          icon={Link2}
          title="Нет опубликованных задач"
          description="Сначала создайте и опубликуйте задачу — только после этого можно сформировать запрос на проверку."
        />
      ) : null}

      {tasks.length > 0 ? (
        <form id="create-review-form" className="form-stack" onSubmit={handleSubmit}>
          {error ? <div className="form-error">{error}</div> : null}

          <label className="field">
            <span className="field__label">
              Опубликованная задача <span className="field__required">*</span>
            </span>
            <select
              className="select"
              value={taskId}
              onChange={(event) => setTaskId(event.target.value)}
            >
              {tasks.map((task) => (
                <option key={task.id} value={task.id}>
                  {task.title}
                </option>
              ))}
            </select>
            {selectedTask ? (
              <span className="field__hint">
                {truncate(selectedTask.statement, 150)}
              </span>
            ) : null}
          </label>

          {selectedTask ? (
            <div className="snapshot-preview">
              <div className="snapshot-preview__head">
                <span>Снимок из TaskService</span>
                <Link2 size={15} />
              </div>
              <div>
                <strong>Условие</strong>
                <p>{selectedTask.statement || "—"}</p>
              </div>
              <div>
                <strong>Правильный ответ</strong>
                <p>{selectedTask.correctAnswer || "—"}</p>
              </div>
            </div>
          ) : null}

          <div className="form-grid">
            <label className="field">
              <span className="field__label">
                ID решения <span className="field__required">*</span>
              </span>
              <div className="input-with-action">
                <input
                  className="input input--mono"
                  value={solutionId}
                  onChange={(event) => setSolutionId(event.target.value)}
                  placeholder="GUID решения"
                />
                <button
                  className="icon-button icon-button--input"
                  type="button"
                  onClick={() => setSolutionId(newGuid())}
                  title="Сгенерировать GUID"
                  aria-label="Сгенерировать ID решения"
                >
                  <Dices size={17} />
                </button>
              </div>
              <span className="field__hint">
                Внешняя ссылка: агрегат «Решение» не входит в этот сервис.
              </span>
            </label>

            <label className="field">
              <span className="field__label">
                ID команды <span className="field__required">*</span>
              </span>
              <div className="input-with-action">
                <input
                  className="input input--mono"
                  value={teamId}
                  onChange={(event) => setTeamId(event.target.value)}
                  placeholder="GUID команды"
                />
                <button
                  className="icon-button icon-button--input"
                  type="button"
                  onClick={() => setTeamId(newGuid())}
                  title="Сгенерировать GUID"
                  aria-label="Сгенерировать ID команды"
                >
                  <Dices size={17} />
                </button>
              </div>
              <span className="field__hint">
                Виден только смотрителю, не попадает в кабинет жюри.
              </span>
            </label>
          </div>

          <div className="form-grid form-grid--narrow">
            <label className="field">
              <span className="field__label">Источник</span>
              <select
                className="select"
                value={source}
                onChange={(event) =>
                  setSource(event.target.value as ReviewSource)
                }
              >
                <option value="Solution">Решение</option>
                <option value="Appeal">Апелляция</option>
              </select>
            </label>

            {source === "Appeal" ? (
              <label className="field">
                <span className="field__label">
                  ID апелляции <span className="field__required">*</span>
                </span>
                <div className="input-with-action">
                  <input
                    className="input input--mono"
                    value={sourceReferenceId}
                    onChange={(event) =>
                      setSourceReferenceId(event.target.value)
                    }
                    placeholder="GUID апелляции"
                  />
                  <button
                    className="icon-button icon-button--input"
                    type="button"
                    onClick={() => setSourceReferenceId(newGuid())}
                    title="Сгенерировать GUID"
                    aria-label="Сгенерировать ID апелляции"
                  >
                    <Dices size={17} />
                  </button>
                </div>
              </label>
            ) : (
              <div className="form-note form-note--boxed">
                Для повторной проверки выберите источник «Апелляция» и укажите
                её идентификатор.
              </div>
            )}
          </div>

          <label className="field">
            <span className="field__label">
              Ответ участника <span className="field__required">*</span>
            </span>
            <textarea
              className="textarea"
              value={participantAnswer}
              onChange={(event) => setParticipantAnswer(event.target.value)}
              maxLength={4000}
              rows={4}
              placeholder="Ответ, который нужно проверить"
            />
            <span className="field__hint">
              {participantAnswer.length} / 4000
            </span>
          </label>
        </form>
      ) : null}
    </Modal>
  );
}
