import axios from "axios";
import { env } from "../config/env";
import { loadLocalStorage } from "../lib/storage";
import type { AuthSession } from "../../entities/auth/model/types";

const session = loadLocalStorage<AuthSession | null>(null);

export const http = axios.create({
  baseURL: env.apiBaseUrl,
  headers: {
    "Content-Type": "application/json",
  },
});

http.interceptors.request.use((config) => {
  const token = loadLocalStorage<AuthSession | null>(null)?.accessToken ?? session?.accessToken;
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});
