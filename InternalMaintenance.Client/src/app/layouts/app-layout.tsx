import { useEffect } from "react";
import { Outlet, useLocation, useNavigate } from "react-router-dom";
import { Banner } from "../../shared/ui";
import { useBanner } from "../../shared/hooks/use-banner";

type BannerState = {
  banner?: {
    type: "success" | "error" | "info" | "warning";
    message: string;
  };
};

export function AppLayout() {
  const location = useLocation();
  const navigate = useNavigate();
  const { banner, showBanner, hideBanner } = useBanner();

  useEffect(() => {
    const state = location.state as BannerState | null;

    if (!state?.banner) {
      return;
    }

    showBanner(state.banner.type, state.banner.message);

    navigate(location.pathname, {
      replace: true,
      state: null,
    });
  }, [location.pathname, location.state, navigate, showBanner]);

  return (
    <div className="app-shell">
      {banner ? (
        <div style={{ width: "min(1680px, calc(100% - 32px))", margin: "0 auto", paddingTop: 16 }}>
          <Banner type={banner.type} message={banner.message} onClose={hideBanner} />
        </div>
      ) : null}

      <Outlet />
    </div>
  );
}
