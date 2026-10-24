import { useEffect, useState, type FormEvent } from "react";
import { Plus } from "lucide-react";
import { taskApi } from "../api/taskApi";
import { getErrorMessage } from "../api/client";
import type { TaskDto } from "../api/types";
import { Modal } from "./Modal";
import { useToast } from "./ToastProvider";

export function CreateTaskModal({
  open,
  onClose,
  onCreated,
}: {
  open: boolean;
  onClose: () => void;
  onCreated: (task: TaskDto) => void;
}) {
  const { notify } = useToast();
  const [title, setTitle] = useState("");
  const [statement, setStatement] = useState("");
  const [correctAnswer, setCorrectAnswer] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (open) {
      setTitle("");
      setStatement("");
      setCorrectAnswer("");
      setError(null);
    }
  }, [open]);

  const handleSubmit = async (event: FormEvent) => {
    event.preventDefault();
    setError(null);

    if (!title.trim()) {
      setError("Укажите название задачи.");
      return;
    }

    setBusy(true);

    try {
      const task = await taskApi.create({
        title: title.trim(),
        statement: statement.trim() || undefined,
        correctAnswer: correctAnswer.trim() || undefined,
      });

      notify({
        kind: "success",
        title: "Задача создана",
        message: "Черновик готов к заполнению и публикации.",
      });
      onCreated(task);
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
      title="Новая задача"
      description="Создайте черновик. Условие и ответ можно заполнить сразу или позже."
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
            form="create-task-form"
            disabled={busy}
          >
            {busy ? <span className="spinner spinner--sm" /> : <Plus size={16} />}
            Создать черновик
          </button>
        </>
      }
    >
      <form id="create-task-form" className="form-stack" onSubmit={handleSubmit}>
        {error ? <div className="form-error">{error}</div> : null}

        <label className="field">
          <span className="field__label">
            Название задачи <span className="field__required">*</span>
          </span>
          <input
            className="input"
            value={title}
            onChange={(event) => setTitle(event.target.value)}
            maxLength={200}
            placeholder="Например, «Сумма двух чисел»"
            autoFocus
            required
          />
          <span className="field__hint">{title.length} / 200</span>
        </label>

        <label className="field">
          <span className="field__label">Условие задачи</span>
          <textarea
            className="textarea"
            value={statement}
            onChange={(event) => setStatement(event.target.value)}
            maxLength={4000}
            rows={5}
            placeholder="Что видит участник и жюри"
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
            placeholder="Эталонный ответ для проверки"
          />
          <span className="field__hint">{correctAnswer.length} / 2000</span>
        </label>

        <p className="form-note">
          Для публикации условие и правильный ответ должны быть заполнены.
        </p>
      </form>
    </Modal>
  );
}
