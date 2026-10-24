import { NavLink, Outlet, useLocation } from "react-router-dom";
import {
  ClipboardList,
  FileSearch,
  LayoutDashboard,
  ListOrdered,
  Scale,
  ShieldCheck,
  Trophy,
} from "lucide-react";

const navigation = [
  { to: "/", label: "Обзор", icon: LayoutDashboard, end: true },
  { to: "/tasks", label: "Задачи", icon: ClipboardList },
  { to: "/reviews", label: "Проверки", icon: FileSearch },
  { to: "/queue", label: "Очередь", icon: ListOrdered },
  { to: "/jury", label: "Кабинет жюри", icon: Scale },
];

const pageNames: Record<string, string> = {
  "/": "Обзор",
  "/tasks": "Задачи",
  "/reviews": "Запросы на проверку",
  "/queue": "Очередь проверок",
  "/jury": "Кабинет жюри",
};

export function AppLayout() {
  const location = useLocation();
  const pageName = pageNames[location.pathname] ?? "Tournament Ops";

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <span className="brand__mark">
            <Trophy size={22} />
          </span>
          <span>
            <strong>Tournament</strong>
            <small>operations console</small>
          </span>
        </div>

        <nav className="sidebar__nav" aria-label="Основная навигация">
          {navigation.map(({ to, label, icon: Icon, end }) => (
            <NavLink
              key={to}
              to={to}
              end={end}
              className={({ isActive }) =>
                `nav-link${isActive ? " nav-link--active" : ""}`
              }
            >
              <Icon size={18} />
              <span>{label}</span>
            </NavLink>
          ))}
        </nav>

        <div className="sidebar__note">
          <span className="sidebar__note-icon">
            <ShieldCheck size={17} />
          </span>
          <div>
            <strong>Приватность жюри</strong>
            <p>В кабинете жюри нет команды, решения и рейтинга.</p>
          </div>
        </div>

        <div className="sidebar__footer">
          <span className="service-indicator" />
          TaskService · ReviewService
          <small>2 сервиса · 2 базы данных</small>
        </div>
      </aside>

      <div className="workspace">
        <header className="topbar">
          <div className="topbar__crumbs">
            <span>Tournament Ops</span>
            <span className="topbar__slash">/</span>
            <strong>{pageName}</strong>
          </div>
          <div className="topbar__actions">
            <span className="mode-pill">демо · без авторизации</span>
            <a
              className="topbar__link"
              href="http://localhost:5101/swagger"
              target="_blank"
              rel="noreferrer"
            >
              Task API
            </a>
            <a
              className="topbar__link"
              href="http://localhost:5102/swagger"
              target="_blank"
              rel="noreferrer"
            >
              Review API
            </a>
          </div>
        </header>

        <main className="main-content">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
