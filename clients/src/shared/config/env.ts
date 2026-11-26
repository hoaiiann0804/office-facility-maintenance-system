const rawDemoFlag = import.meta.env.VITE_ENABLE_DEMO_MODE?.trim().toLowerCase();
const rawApiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim();

function resolveApiBaseUrl(): string {
  if (!rawApiBaseUrl) return "/api";
  // Nếu người dùng đang truy cập qua tên miền Tunnel/Production (không phải localhost)
  // mà biến môi trường lại trỏ về localhost, tự động chuyển sang "/api" để đi qua proxy của Tunnel.
  if (
    typeof window !== "undefined" &&
    window.location.hostname !== "localhost" &&
    window.location.hostname !== "127.0.0.1" &&
    (rawApiBaseUrl.includes("localhost") || rawApiBaseUrl.includes("127.0.0.1"))
  ) {
    return "/api";
  }
  return rawApiBaseUrl;
}

export const env = {
  apiBaseUrl: resolveApiBaseUrl(),
  isProduction: Boolean(import.meta.env.PROD),
  // [QUICK LOGIN CHO CẢ DEVELOPMENT LẪN PRODUCTION]:
  // Mặc định BẬT (true) trên cả Development và Production để Nhà tuyển dụng / Người dùng
  // vào bản Production vẫn bấm Quick Login 1-click được ngay (dùng bộ tài khoản test riêng của Production).
  // Chỉ tắt khi chủ động cấu hình VITE_ENABLE_DEMO_MODE=false.
  enableDemoMode: rawDemoFlag === "false" ? false : true,
};
