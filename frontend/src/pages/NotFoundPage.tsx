import { Link } from "react-router-dom";
import { Compass } from "lucide-react";

export function NotFoundPage() {
  return (
    <div className="not-found">
      <span className="not-found__icon">
        <Compass size={28} />
      </span>
      <span className="eyebrow">404</span>
      <h1>Страница не найдена</h1>
      <p>Такого раздела нет. Вернитесь на панель управления турниром.</p>
      <Link className="btn btn--primary" to="/">
        На главную
      </Link>
    </div>
  );
}
