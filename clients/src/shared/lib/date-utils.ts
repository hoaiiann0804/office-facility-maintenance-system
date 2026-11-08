/**
 * Utility function to parse ISO string dates returned from ASP.NET Core API.
 * Ensures ISO strings lacking the 'Z' suffix (UTC) are correctly treated as UTC dates
 * instead of local browser dates to prevent timezone offsets (e.g., GMT+7 7-hour shift).
 */
export function parseApiDate(value: string | Date | null | undefined): Date {
  if (!value) return new Date();
  if (value instanceof Date) return value;
  let str = String(value).trim();
  if (str.includes(" ") && !str.includes("T")) {
    str = str.replace(" ", "T");
  }
  if (!str.endsWith("Z") && !/[+-]\d{2}:?\d{2}$/.test(str)) {
    str += "Z";
  }
  return new Date(str);
}

export function formatDateTime(value: string | Date | null | undefined, fallback = "N/A"): string {
  if (!value) return fallback;
  const date = parseApiDate(value);
  if (isNaN(date.getTime())) return fallback;

  return new Intl.DateTimeFormat("vi-VN", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

export function formatDateOnly(value: string | Date | null | undefined, fallback = "N/A"): string {
  if (!value) return fallback;
  const date = parseApiDate(value);
  if (isNaN(date.getTime())) return fallback;

  return new Intl.DateTimeFormat("vi-VN", {
    dateStyle: "medium",
  }).format(date);
}
