import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";

const taskTarget = process.env.TASK_API_PROXY_TARGET ?? "http://localhost:5101";
const reviewTarget =
  process.env.REVIEW_API_PROXY_TARGET ?? "http://localhost:5102";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      "/task-api": {
        target: taskTarget,
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/task-api/, "/api"),
      },
      "/review-api": {
        target: reviewTarget,
        changeOrigin: true,
        rewrite: (path) => path.replace(/^\/review-api/, "/api"),
      },
    },
  },
});
