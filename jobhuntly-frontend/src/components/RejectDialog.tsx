"use client";

import { useState, type FormEvent } from "react";
import { Dialog } from "./Dialog";

interface Props {
  /** Company name of the application being rejected, or null when the dialog is closed. */
  company: string | null;
  onCancel: () => void;
  onConfirm: (reason: string) => void | Promise<void>;
}

/** Asks (optionally) why an application was rejected. The reason feeds the Insights screen. */
export function RejectDialog({ company, onCancel, onConfirm }: Props) {
  return (
    <Dialog open={company !== null} title="Why was it rejected?" onClose={onCancel}>
      {company !== null && <RejectForm company={company} onCancel={onCancel} onConfirm={onConfirm} />}
    </Dialog>
  );
}

function RejectForm({ company, onCancel, onConfirm }: { company: string; onCancel: () => void; onConfirm: (reason: string) => void | Promise<void> }) {
  const [reason, setReason] = useState("");

  function submit(event: FormEvent) {
    event.preventDefault();
    void onConfirm(reason.trim());
  }

  return (
    <form className="form" onSubmit={submit} aria-label="Rejection reason">
      <p>
        Moving <b>{company}</b> to Rejected. A reason helps Insights spot patterns, but it is optional.
      </p>
      <label>
        <span>Reason (optional)</span>
        <input type="text" value={reason} onChange={(e) => setReason(e.target.value)} maxLength={1000} placeholder="Skills gap, no reason given, role closed…" />
      </label>
      <div className="dialog-actions">
        <button type="button" className="btn" onClick={onCancel}>Cancel</button>
        <button type="submit" className="btn primary">Move to Rejected</button>
      </div>
    </form>
  );
}
