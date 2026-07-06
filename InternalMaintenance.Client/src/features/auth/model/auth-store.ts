import { create } from "zustand";
import type { AuthSession } from "../../../entities/auth/model/types";
import { clearLocalStorage, loadLocalStorage, saveLocalStorage } from "../../../shared/lib/storage";

type AuthState = {
  session: AuthSession | null;
  hydrated: boolean;
  hydrate: () => void;
  setSession: (session: AuthSession | null) => void;
  signOut: () => void;
};

export const useAuthStore = create<AuthState>((set) => ({
  session: null,
  hydrated: false,
  hydrate: () => {
    const session = loadLocalStorage<AuthSession | null>(null);
    set({ session, hydrated: true });
  },
  setSession: (session) => {
    if (session) {
      saveLocalStorage(session);
    } else {
      clearLocalStorage();
    }
    set({ session });
  },
  signOut: () => {
    clearLocalStorage();
    set({ session: null });
  },
}));
