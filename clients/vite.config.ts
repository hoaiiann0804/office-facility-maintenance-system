import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "path";

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    host: true,
    port: 5173,
    // Cho phép truy cập qua tên miền Tunnel (Cloudflare Tunnel, Ngrok, Custom Domain) không bị Vite chặn
    allowedHosts: true,
    proxy: {
      "/api": "http://localhost:5253",
      "/swagger": "http://localhost:5253",
    },
  },
  preview: {
    host: true,
    port: 5173,
    allowedHosts: true,
    proxy: {
      "/api": "http://localhost:5253",
      "/swagger": "http://localhost:5253",
    },
  },
});
