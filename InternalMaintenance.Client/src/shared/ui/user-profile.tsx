import { useState, useRef, useEffect } from "react";
import { ThemeToggle } from "./theme-toggle";

type UserProfileProps = {
  fullName: string | undefined;
  roleName: string | undefined;
  onLogout: () => void;
  onChangePassword: () => void;
};

function getInitials(name: string | undefined) {
  if (!name) return "?";
  const parts = name.trim().split(" ");
  if (parts.length >= 2) {
    return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
  }
  return name.charAt(0).toUpperCase();
}

export function UserProfile({ fullName, roleName, onLogout, onChangePassword }: UserProfileProps) {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  return (
    <div className="badge-row" style={{ display: "flex", alignItems: "center" }}>
      <ThemeToggle />
      <div className="user-info-text">
        <span className="user-info-name">{fullName ?? "Guest"}</span>
        <span className="user-info-role">{roleName ?? "Guest"}</span>
      </div>
      <div className="user-dropdown-container" ref={dropdownRef}>
        <button
          type="button"
          className="user-dropdown-trigger"
          onClick={() => setIsOpen(!isOpen)}
          aria-expanded={isOpen}
          aria-haspopup="true"
        >
          {getInitials(fullName)}
        </button>

        {isOpen && (
          <div className="user-dropdown-menu">
            <button
              type="button"
              className="user-dropdown-item"
              onClick={() => {
                setIsOpen(false);
                onChangePassword();
              }}
            >
              Cài đặt Profile (Đổi mật khẩu)
            </button>
            <button
              type="button"
              className="user-dropdown-item danger"
              onClick={() => {
                setIsOpen(false);
                onLogout();
              }}
            >
              Đăng xuất
            </button>
          </div>
        )}
      </div>
    </div>
  );
}
