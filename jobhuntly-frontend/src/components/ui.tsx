"use client";

import { useCallback, useState, type ReactNode } from "react";
import { ApiError } from "@/lib/api";

export function Notice({ kind = "info", children }: { kind?: "info" | "error"; children: ReactNode }) {
  return (
    <div className={`notice ${kind}`} role={kind === "error" ? "alert" : "status"}>
      {children}
    </div>
  );
}

export function ErrorNotice({ error }: { error: unknown }) {
  if (!error) return null;
  return <Notice kind="error">{error instanceof Error ? error.message : "Something went wrong."}</Notice>;
}

export function Empty({ title, children }: { title: string; children?: ReactNode }) {
  return (
    <div className="empty">
      <b>{title}</b>
      {children && <p className="muted">{children}</p>}
    </div>
  );
}

/**
 * Wraps a button handler with a busy flag and an error message. Errors are shown, never swallowed;
 * the caller decides what to do on success by awaiting `run`.
 */
export function useAction() {
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<ApiError | Error | null>(null);

  const run = useCallback(async <T,>(action: () => Promise<T>): Promise<T | undefined> => {
    setBusy(true);
    setError(null);
    try {
      return await action();
    } catch (caught) {
      setError(caught instanceof Error ? caught : new Error("Something went wrong."));
      return undefined;
    } finally {
      setBusy(false);
    }
  }, []);

  const clear = useCallback(() => setError(null), []);
  return { run, busy, error, clear };
}

/** A page title that tells screen readers the page changed and keeps the visual h1 in one place. */
export function PageHead({ eyebrow, title, children }: { eyebrow?: ReactNode; title: string; children?: ReactNode }) {
  return (
    <div className="head">
      <div>
        {eyebrow && <div className="eyebrow">{eyebrow}</div>}
        <h1>{title}</h1>
      </div>
      {children && <div className="actions">{children}</div>}
    </div>
  );
}
