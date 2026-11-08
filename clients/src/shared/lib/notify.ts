import { toast as sonnerToast } from "sonner";

/**
 * Custom notify helper wrapping sonner.
 * Tự động deduplicate các thông báo trùng lặp bằng `id` để tránh việc
 * bấm submit nhiều lần làm chồng đống các ô đỏ rác UI.
 */
export const notify = {
  success: (message: string, duration = 3000) => {
    return sonnerToast.success(message, { id: message, duration });
  },
  error: (message: string, duration = 4000) => {
    return sonnerToast.error(message, { id: message, duration });
  },
  info: (message: string, duration = 3000) => {
    return sonnerToast.info(message, { id: message, duration });
  },
  warning: (message: string, duration = 3500) => {
    return sonnerToast.warning(message, { id: message, duration });
  },
  dismiss: (id?: string) => {
    sonnerToast.dismiss(id);
  },
};
