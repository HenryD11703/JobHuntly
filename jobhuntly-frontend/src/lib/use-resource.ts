"use client";

import { useCallback, useEffect, useState } from "react";
import { ApiError } from "./api";

export interface Resource<T> {
  data: T | undefined;
  error: ApiError | undefined;
  loading: boolean;
  /** Fetch again (keeps showing the old data until the new data arrives). */
  reload: () => void;
  /** Replace the data locally, for instant updates after a successful write. */
  setData: (next: T) => void;
}

interface Settled<T> {
  version: number;
  data?: T;
  error?: ApiError;
}

/**
 * Loads data once per `key` (and again on `reload`). State is only set after the request resolves,
 * never synchronously inside the effect.
 *
 * `key` must change whenever `load` would return something different (an id, for example).
 */
export function useResource<T>(key: string, load: () => Promise<T>): Resource<T> {
  const [version, setVersion] = useState(0);
  const [settled, setSettled] = useState<Settled<T>>();

  useEffect(() => {
    let alive = true;
    load().then(
      (data) => alive && setSettled({ version, data }),
      (error) => alive && setSettled({ version, error: error instanceof ApiError ? error : new ApiError(0, String(error)) }),
    );
    return () => {
      alive = false;
    };
    // `load` is intentionally not a dependency: callers pass an inline arrow and `key` says when it changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [key, version]);

  const reload = useCallback(() => setVersion((v) => v + 1), []);
  const setData = useCallback((next: T) => setSettled((s) => ({ version: s?.version ?? 0, data: next })), []);

  return {
    data: settled?.data,
    error: settled?.error,
    loading: settled === undefined || (settled.version !== version && settled.data === undefined && !settled.error),
    reload,
    setData,
  };
}
