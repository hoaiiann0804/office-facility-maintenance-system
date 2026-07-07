import { useCallback, useRef, useState } from "react";

export type BannerType = "success" | "error" | "info" | "warning";

type BannerState = {
  type: BannerType;
  message: string;
} | null;

const getTimeoutDuration = (type: BannerType): number | null => {
  switch (type) {
    case "success":
      return 5000; // 5 giây
    case "info":
      return 6000; // 6 giây
    case "warning":
      return 8000; // 8 giây
    case "error":
      return null; // Lỗi quan trọng, không tự động ẩn
    default:
      return 6000;
  }
};

export function useBanner() {
  const [banner, setBanner] = useState<BannerState>(null);
  const timerRef = useRef<number | null>(null);

  const hideBanner = useCallback(() => {
    if (timerRef.current) {
      window.clearTimeout(timerRef.current);
      timerRef.current = null;
    }
    setBanner(null);
  }, []);

  const showBanner = useCallback(
    (type: BannerType, message: string) => {
      hideBanner(); // Xóa banner cũ trước khi hiển thị banner mới
      setBanner({ type, message });

      const duration = getTimeoutDuration(type);
      if (duration) {
        timerRef.current = window.setTimeout(hideBanner, duration);
      }
    },
    [hideBanner],
  );

  return { banner, showBanner, hideBanner };
}
