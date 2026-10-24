import { BrowserRouter, Route, Routes } from "react-router-dom";
import { AppLayout } from "./components/AppLayout";
import { ToastProvider } from "./components/ToastProvider";
import { DashboardPage } from "./pages/DashboardPage";
import { JuryPage } from "./pages/JuryPage";
import { NotFoundPage } from "./pages/NotFoundPage";
import { QueuePage } from "./pages/QueuePage";
import { ReviewsPage } from "./pages/ReviewsPage";
import { TasksPage } from "./pages/TasksPage";

export default function App() {
  return (
    <ToastProvider>
      <BrowserRouter>
        <Routes>
          <Route element={<AppLayout />}>
            <Route index element={<DashboardPage />} />
            <Route path="tasks" element={<TasksPage />} />
            <Route path="reviews" element={<ReviewsPage />} />
            <Route path="queue" element={<QueuePage />} />
            <Route path="jury" element={<JuryPage />} />
            <Route path="*" element={<NotFoundPage />} />
          </Route>
        </Routes>
      </BrowserRouter>
    </ToastProvider>
  );
}
