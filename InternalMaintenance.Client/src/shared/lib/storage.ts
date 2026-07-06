const authStorageKey = "internal-maintenance.auth";

export function loadLocalStorage<T>(fallback: T, parser: (value: string) => T = JSON.parse): T {
  if (typeof window === "undefined") return fallback;
  const raw = window.localStorage.getItem(authStorageKey);
  if (!raw) return fallback;
  try {
    return parser(raw);
  } catch {
    return fallback;
  }
}

export function saveLocalStorage(value: unknown) {
  if (typeof window === "undefined") return;
  window.localStorage.setItem(authStorageKey, JSON.stringify(value));
}

export function clearLocalStorage() {
  if (typeof window === "undefined") return;
  window.localStorage.removeItem(authStorageKey);
}
