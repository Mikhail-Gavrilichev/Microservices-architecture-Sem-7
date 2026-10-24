import { ChevronLeft, ChevronRight } from "lucide-react";
import type { PagedResult } from "../api/types";
import { pluralize } from "../lib/format";

export function Pagination({
  result,
  onPageChange,
}: {
  result: PagedResult<unknown>;
  onPageChange: (page: number) => void;
}) {
  if (result.totalPages <= 1) {
    return null;
  }

  return (
    <div className="pagination">
      <span>
        Страница {result.page} из {result.totalPages} · {result.totalCount}{" "}
        {pluralize(result.totalCount, "запись", "записи", "записей")}
      </span>
      <div className="pagination__buttons">
        <button
          className="btn btn--ghost btn--sm"
          type="button"
          disabled={result.page <= 1}
          onClick={() => onPageChange(result.page - 1)}
        >
          <ChevronLeft size={16} />
          Назад
        </button>
        <button
          className="btn btn--ghost btn--sm"
          type="button"
          disabled={result.page >= result.totalPages}
          onClick={() => onPageChange(result.page + 1)}
        >
          Вперёд
          <ChevronRight size={16} />
        </button>
      </div>
    </div>
  );
}
