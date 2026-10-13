"use client";

import { useRouter } from "next/navigation";
import { api } from "@/lib/api";
import { ErrorNotice, useAction } from "./ui";

/** One click: creates a private sandbox with sample data and opens the board. */
export function DemoButton({ className = "btn primary", children = "Try the demo, no signup" }: { className?: string; children?: string }) {
  const router = useRouter();
  const { run, busy, error } = useAction();

  async function start() {
    const done = await run(async () => {
      await api.auth.demo();
      return true;
    });
    if (done) router.push("/board");
  }

  return (
    <>
      <button type="button" className={className} onClick={start} disabled={busy}>
        {busy ? "Preparing your sandbox…" : children}
      </button>
      {error && <div style={{ flexBasis: "100%" }}><ErrorNotice error={error} /></div>}
    </>
  );
}
