import type { BannerType } from "../hooks/use-banner";

type BannerProps = {
  type: BannerType;
  message: string;
  onClose?: () => void;
};

export function Banner({ type, message, onClose }: BannerProps) {
  return (
    <div className={`banner ${type}`} role="alert" aria-live="polite">
      <span>{message}</span>

      {onClose ? (
        <button type="button" className="banner-close" onClick={onClose} aria-label="Close banner">
          x
        </button>
      ) : null}
    </div>
  );
}
