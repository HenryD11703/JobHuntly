"use client";

import { useEffect, useState } from "react";

/**
 * The current time as a stable value for rendering ("is this interview in the past?"). Reading the
 * clock directly during render is impure, so it is captured once and refreshed every minute.
 */
export function useNow(refreshMs = 60_000): number {
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), refreshMs);
    return () => window.clearInterval(timer);
  }, [refreshMs]);
  return now;
}
