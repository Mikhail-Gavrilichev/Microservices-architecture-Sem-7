import { useEffect, useState } from "react";
import {
  Check,
  Clock3,
  FilePenLine,
  LockKeyhole,
  Rocket,
  Save,
} from "lucide-react";
import { getErrorMessage } from "../api/client";
import { taskApi } from "../api/taskApi";
import type { TaskDto } from "../api/types";
import { formatDateTime } from "../lib/format";
import { TaskStatusBadge } from "./Badges";
import { ConfirmModal } from "./ConfirmModal";
import { Drawer } from "./Drawer";
import { LoadingState } from "./States";
import { useToast } from "./ToastProvider";

export function TaskDrawer({
  taskId,
  onClose,
  onChanged,
}: {
  taskId: string | null;
  onClose: () => void;
  onChanged: () => void;
}) {
  const { notify } = useToast();
  const [task, setTask] = useState<TaskDto | null>(null);
  const [statement, setStatement] = useState("");
  const [correctAnswer, setCorrectAnswer] = useState("");
  const [loading, setLoading] = useState(false);
  const [saving, setSaving] = useState(false);
  const [publishing, setPublishing] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [confirmPublish, setConfirmPublish] = useState(false);
  const [reloadTick, setReloadTick] = useState(0);

  useEffect(() => {
    if (!taskId) {
      setTask(null);
      return;
    }

    let cancelled = false;
    setLoading(true);
    setError(null);

    taskApi
      .get(taskId)
      .then((data) => {
        if (cancelled) {
          return;
        }

        setTask(data);
        setStatement(data.statement);
        setCorrectAnswer(data.correctAnswer);
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
  }, [taskId, reloadTick]);

  const isDraft = task?.status === "Draft";
  const fieldsFilled =
    statement.trim().length > 0 && correctAnswer.trim().length > 0;
  const hasChanges =
    task !== null &&
    (statement.trim() !== task.statement ||
      correctAnswer.trim() !== task.correctAnswer);

  const persistChanges = async () => {
    if (!task || !isDraft) {
      return;
    }

    if (!fieldsFilled) {
      throw new Error(
        "Чтобы сохранить изменения, заполните условие задачи и правильный ответ.",
      );
    }

    // Выполняем последовательно: каждый PUT загружает и сохраняет агрегат.
    // Параллельные запросы могли бы перезаписать изменение друг друга.
    if (statement.trim() !== task.statement) {
      await taskApi.changeStatement(task.id, statement.trim());
    }

    if (correctAnswer.trim() !== task.correctAnswer) {
      await taskApi.changeCorrectAnswer(task.id, correctAnswer.trim());
    }
  };

  const handleSave = async () => {
    setError(null);
    setSaving(true);

    try {
      await persistChanges();
      setReloadTick((value) => value + 1);
      onChanged();
      notify({
        kind: "success",
        title: "Изменения сохранены",
        message: hasChanges
          ? "Условие и правильный ответ обновлены."
          : "Новых изменений не было.",
      });
    } catch (saveError) {
      setError(getErrorMessage(saveError));
    } finally {
      setSaving(false);
    }
  };

  const handlePublish = async () => {
    if (!task) {
      return;
    }

    setError(null);
    setPublishing(true);

    try {
      await persistChanges();
      await taskApi.publish(task.id);
      setConfirmPublish(false);
      setReloadTick((value) => value + 1);
      onChanged();
      notify({
        kind: "success",
        title: "Задача опубликована",
        message: "Теперь по ней можно создавать запросы на проверку.",
      });
    } catch (publishError) {
      setError(getErrorMessage(publishError));
      setConfirmPublish(false);
    } finally {
      setPublishing(false);
    }
  };

  const busy = saving || publishing;

  return (
    <>
      <Drawer
        open={taskId !== null}
        title={task?.title ?? "Задача"}
        subtitle={task ? `ID ${task.id}` : "Карточка задачи"}
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
            {isDraft ? (
              <>
                <button
                  className="btn btn--secondary"
                  type="button"
                  onClick={handleSave}
                  disabled={busy || !hasChanges}
                >
                  <Save size={16} />
                  Сохранить
                </button>
                <button
                  className="btn btn--primary"
                  type="button"
                  onClick={() => setConfirmPublish(true)}
                  disabled={busy || !fieldsFilled}
                  title={
                    fieldsFilled
                      ? "Опубликовать задачу"
                      : "Заполните условие и правильный ответ"
                  }
                >
                  <Rocket size={16} />
                  Опубликовать
                </button>
              </>
            ) : null}
          </div>
        }
      >
        {loading ? <LoadingState label="Открываем задачу…" /> : null}

        {!loading && error && !task ? (
          <div className="form-error">{error}</div>
        ) : null}

        {!loading && task ? (
          <div className="detail-stack">
            <div className="detail-topline">
              <TaskStatusBadge status={task.status} />
              <span className="detail-topline__id">ID {task.id}</span>
            </div>

            {error ? <div className="form-error">{error}</div> : null}

            <div className="meta-grid">
              <div>
                <span className="meta-grid__label">
                  <Clock3 size={14} /> Создана
                </span>
                <strong>{formatDateTime(task.createdAt)}</strong>
              </div>
              <div>
                <span className="meta-grid__label">
                  <FilePenLine size={14} /> Обновлена
                </span>
                <strong>{formatDateTime(task.updatedAt)}</strong>
              </div>
              <div>
                <span className="meta-grid__label">
                  <Check size={14} /> Опубликована
                </span>
                <strong>{formatDateTime(task.publishedAt)}</strong>
              </div>
            </div>

            {isDraft ? (
              <>
                <label className="field">
                  <span className="field__label">Условие задачи</span>
                  <textarea
                    className="textarea"
                    value={statement}
                    onChange={(event) => setStatement(event.target.value)}
                    maxLength={4000}
                    rows={7}
                  />
                  <span className="field__hint">{statement.length} / 4000</span>
                </label>

                <label className="field">
                  <span className="field__label">Правильный ответ</span>
                  <textarea
                    className="textarea textarea--compact"
                    value={correctAnswer}
                    onChange={(event) => setCorrectAnswer(event.target.value)}
                    maxLength={2000}
                    rows={3}
                  />
                  <span className="field__hint">
                    {correctAnswer.length} / 2000
                  </span>
                </label>

                <p className="form-note">
                  Условие и ответ можно менять только до публикации. После
                  публикации задача становится неизменяемой.
                </p>
              </>
            ) : (
              <>
                <div className="read-block">
                  <span className="read-block__label">Условие задачи</span>
                  <p>{task.statement || "—"}</p>
                </div>
                <div className="read-block read-block--answer">
                  <span className="read-block__label">
                    <LockKeyhole size={14} /> Правильный ответ
                  </span>
                  <p>{task.correctAnswer || "—"}</p>
                </div>
                <p className="form-note">
                  Задача опубликована и защищена от изменений. Её снимок уже
                  может использоваться в запросах на проверку.
                </p>
              </>
            )}
          </div>
        ) : null}
      </Drawer>

      <ConfirmModal
        open={confirmPublish}
        title="Опубликовать задачу?"
        message="После публикации условие и правильный ответ нельзя будет изменить. Убедитесь, что формулировки и эталонный ответ проверены."
        confirmLabel="Опубликовать"
        busy={publishing}
        onConfirm={handlePublish}
        onCancel={() => setConfirmPublish(false)}
      />
    </>
  );
}
