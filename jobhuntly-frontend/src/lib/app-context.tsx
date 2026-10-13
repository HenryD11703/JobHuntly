"use client";

import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { usePathname, useRouter } from "next/navigation";
import { api, ApiError } from "./api";
import type { AiStatus, Profile } from "./types";

interface AppState {
  profile: Profile;
  /** How many things need the user today (shown as a badge on the Today tab). */
  todayCount: number;
  /** AI availability and allowance; null while loading or if the status call failed. */
  ai: AiStatus | null;
  refreshToday: () => void;
  refreshAi: () => void;
  logout: () => Promise<void>;
}

const AppContext = createContext<AppState | null>(null);

export function useApp(): AppState {
  const value = useContext(AppContext);
  if (!value) throw new Error("useApp must be used inside <AppProvider>");
  return value;
}

/** Signs the visitor in or sends them to /login. Wraps every screen that needs an account. */
export function AppProvider({ children }: { children: ReactNode }) {
  const router = useRouter();
  const pathname = usePathname();
  const [profile, setProfile] = useState<Profile>();
  const [todayCount, setTodayCount] = useState(0);
  const [ai, setAi] = useState<AiStatus | null>(null);

  useEffect(() => {
    let alive = true;
    api.auth.me().then(
      (me) => alive && setProfile(me),
      (error) => {
        if (!alive) return;
        if (error instanceof ApiError && error.status === 401) router.replace("/login");
        else router.replace("/login?error=unreachable");
      },
    );
    return () => {
      alive = false;
    };
  }, [router]);

  const refreshToday = useCallback(() => {
    api.today().then((nudges) => setTodayCount(nudges.length), () => undefined);
  }, []);

  const refreshAi = useCallback(() => {
    api.ai.status().then(setAi, () => setAi(null));
  }, []);

  // Refresh the badge and the AI allowance whenever the user moves to another screen.
  useEffect(() => {
    if (!profile) return;
    refreshToday();
    refreshAi();
  }, [profile, pathname, refreshToday, refreshAi]);

  const logout = useCallback(async () => {
    await api.auth.logout().catch(() => undefined);
    router.replace("/");
  }, [router]);

  const value = useMemo(
    () => (profile ? { profile, todayCount, ai, refreshToday, refreshAi, logout } : null),
    [profile, todayCount, ai, refreshToday, refreshAi, logout],
  );

  if (!value) return <p className="boot" role="status">Loading your notebook…</p>;
  return <AppContext.Provider value={value}>{children}</AppContext.Provider>;
}
