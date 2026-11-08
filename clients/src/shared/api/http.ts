import axios from "axios";
import { env } from "../config/env";
import { loadLocalStorage } from "../lib/storage";
import type { AuthSession, RefreshTokenResponse } from "../../entities/auth/model/types";
import { useAuthStore } from "../../features/auth/model/auth-store";

// Lấy session khởi tạo ban đầu từ LocalStorage
const session = loadLocalStorage<AuthSession | null>(null);

// Tạo instance axios dùng chung cho toàn bộ dự án
export const http = axios.create({
  baseURL: env.apiBaseUrl,
  headers: {
    "Content-Type": "application/json",
  },
});

/* ==========================================================================
   1. REQUEST INTERCEPTOR: Tự động đính kèm Access Token vào mọi HTTP Request
   ========================================================================== */
http.interceptors.request.use((config) => {
  // Lấy session mới nhất từ Zustand store (hoặc fallback từ LocalStorage)
  const currentSession =
    useAuthStore.getState().session ?? loadLocalStorage<AuthSession | null>(null) ?? session;
  const token = currentSession?.accessToken;

  // Nếu đã đăng nhập và có token, tự động gắn vào Header: Authorization: Bearer <token>
  if (token) {
    config.headers = config.headers ?? {};
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

/* ==========================================================================
   2. CÁC BIẾN & HÀM BỔ TRỢ CHO CƠ CHẾ REFRESH TOKEN (QUEUE & LOCK)
   ========================================================================== */

// Mở rộng kiểu dữ liệu của Axios Request Config để bổ sung cờ `_retry`
interface CustomAxiosRequestConfig {
  _retry?: boolean; // Cờ đánh dấu request này đã từng thử lại (retry) hay chưa
}

// Cờ Lock: true khi đang có 1 request thực hiện gia hạn token (ngăn gửi trùng lặp nhiều request refresh)
let isRefreshing = false;

// Hàng chờ (Queue): Chứa danh sách các request bị lỗi 401 đến sau trong lúc token đang được refresh
let failedQueue: Array<{
  resolve: (token: string) => void;
  reject: (error: unknown) => void;
}> = [];

/**
 * Hàm giải phóng hàng chờ (Queue):
 * - Nếu refresh thành công: cho phép các request trong hàng chờ tiếp tục chạy với token mới.
 * - Nếu refresh thất bại: hủy tất cả các request trong hàng chờ.
 */
const processQueue = (error: unknown, token: string | null = null) => {
  failedQueue.forEach((promise) => {
    if (error) {
      promise.reject(error);
    } else if (token) {
      promise.resolve(token);
    }
  });
  failedQueue = [];
};

/* ==========================================================================
   3. RESPONSE INTERCEPTOR: Xử lý tự động gia hạn Token khi nhận lỗi HTTP 401
   ========================================================================== */
http.interceptors.response.use(
  // Trường hợp Request thành công (HTTP status 2xx) -> Trả về kết quả bình thường
  (response) => response,

  // Trường hợp Request gặp lỗi -> Bắt lỗi và kiểm tra xem có cần Refresh Token không
  async (error) => {
    const originalRequest = error.config as typeof error.config & CustomAxiosRequestConfig;

    // BƯỚC 1: Nếu không phải lỗi 401 Unauthorized (ví dụ 400, 403, 500) -> Ném lỗi ra cho UI xử lý
    if (!error.response || error.response.status !== 401) {
      return Promise.reject(error);
    }

    const url = originalRequest?.url ?? "";
    const isAuthEndpoint = url.includes("/auth/login") || url.includes("/auth/refresh-token");

    // BƯỚC 2: Tránh lặp vô tận (Infinite Loop)
    // Nếu lỗi 401 xảy ra ở API Login / RefreshToken, hoặc request này ĐÃ RETRY 1 LẦN DỒI -> Đăng xuất ngay
    if (isAuthEndpoint || originalRequest?._retry) {
      if (!url.includes("/auth/login")) {
        useAuthStore.getState().signOut();
      }
      return Promise.reject(error);
    }

    // BƯỚC 3: Nếu ĐANG CÓ request khác thực hiện refresh token (isRefreshing === true)
    // -> Đưa request hiện tại vào hàng chờ (failedQueue) và nằm đợi token mới
    if (isRefreshing) {
      return new Promise((resolve, reject) => {
        failedQueue.push({
          // Khi lấy được token mới thành công -> Gắn token mới và gửi lại request này
          resolve: (token: string) => {
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${token}`;
            }
            resolve(http(originalRequest));
          },
          // Nếu refresh thất bại -> Từ chối request
          reject: (err) => {
            reject(err);
          },
        });
      });
    }

    // BƯỚC 4: Request 401 ĐẦU TIÊN vươn tới đây -> Khóa Lock và chuẩn bị gọi API gia hạn token
    originalRequest._retry = true; // Đánh dấu request này bắt đầu retry
    isRefreshing = true; // Khóa cờ để các request 401 khác phải vào hàng chờ (BƯỚC 3)

    // BƯỚC 5: Kiểm tra RefreshToken lưu trong Client
    const currentSession =
      useAuthStore.getState().session ?? loadLocalStorage<AuthSession | null>(null);
    const refreshTokenStr = currentSession?.refreshToken;

    // Nếu không tìm thấy RefreshToken -> Đăng xuất và báo lỗi
    if (!refreshTokenStr) {
      isRefreshing = false;
      useAuthStore.getState().signOut();
      return Promise.reject(error);
    }

    // BƯỚC 6: Gọi API Refresh Token lên Backend
    try {
      // Dùng axios thuần để gọi API refresh (tránh lặp interceptor)
      const { data } = await axios.post<RefreshTokenResponse>(
        `${env.apiBaseUrl}/auth/refresh-token`,
        { refreshToken: refreshTokenStr },
      );

      // BƯỚC 7: Cập nhật Cặp Token mới vào Store & LocalStorage
      const updatedSession: AuthSession = {
        user: currentSession.user,
        accessToken: data.accessToken,
        refreshToken: data.refreshToken,
      };
      useAuthStore.getState().setSession(updatedSession);

      // BƯỚC 8: Giải phóng hàng chờ (gửi token mới cho tất cả request đang đợi ở BƯỚC 3)
      processQueue(null, data.accessToken);

      // BƯỚC 9: Gắn token mới vào request ban đầu và thực hiện lại (Retry)
      if (originalRequest.headers) {
        originalRequest.headers.Authorization = `Bearer ${data.accessToken}`;
      }
      return http(originalRequest);
    } catch (refreshError) {
      // BƯỚC 10: Nếu API Refresh Token bị thất bại (ví dụ: Refresh Token hết hạn 7 ngày)
      processQueue(refreshError, null); // Hủy tất cả request đang chờ
      useAuthStore.getState().signOut(); // Xóa session, đẩy về trang Login
      return Promise.reject(refreshError);
    } finally {
      // BƯỚC 11: Mở khóa Lock để chuẩn bị cho các đợt hết hạn token tiếp theo
      isRefreshing = false;
    }
  },
);
