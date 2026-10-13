"use client";

import { useState, type FormEvent } from "react";
import { api } from "@/lib/api";
import { useApp } from "@/lib/app-context";
import { sourceLabel, statusLabel } from "@/lib/format";
import { SOURCES, STATUSES, type ApplicationSource, type ApplicationStatus, type JobApplication } from "@/lib/types";
import { AiAllowance, AiButton } from "./AiButton";
import { Dialog } from "./Dialog";
import { ErrorNotice, Notice, useAction } from "./ui";

interface Props {
  open: boolean;
  onClose: () => void;
  onCreated: (application: JobApplication) => void;
}

/** Creates an application by hand, or from a pasted job post (AI fills a draft the user reviews). */
export function NewApplicationDialog({ open, onClose, onCreated }: Props) {
  return (
    <Dialog open={open} title="New application" onClose={onClose}>
      <NewApplicationForm onClose={onClose} onCreated={onCreated} />
    </Dialog>
  );
}

function NewApplicationForm({ onClose, onCreated }: Omit<Props, "open">) {
  const { refreshAi, refreshToday } = useApp();
  const save = useAction();
  const extract = useAction();
  const [company, setCompany] = useState("");
  const [position, setPosition] = useState("");
  const [status, setStatus] = useState<ApplicationStatus>("Backlog");
  const [source, setSource] = useState<ApplicationSource | "">("");
  const [location, setLocation] = useState("");
  const [isRemote, setIsRemote] = useState(false);
  const [salary, setSalary] = useState("");
  const [url, setUrl] = useState("");
  const [postText, setPostText] = useState("");
  const [notes, setNotes] = useState("");
  const [drafted, setDrafted] = useState(false);
  const [nothingFound, setNothingFound] = useState(false);

  async function fillFromPost() {
    setNothingFound(false);
    const draft = await extract.run(() => api.ai.extractJobPost(postText));
    refreshAi();
    if (!draft) return; // the error is shown; the form stays exactly as it was
    const found = draft.companyName || draft.positionTitle || draft.salaryRange || draft.location || draft.requirements.length > 0;
    if (!found) {
      setNothingFound(true);
      return;
    }
    if (draft.companyName) setCompany(draft.companyName);
    if (draft.positionTitle) setPosition(draft.positionTitle);
    if (draft.salaryRange) setSalary(draft.salaryRange);
    if (draft.location) setLocation(draft.location);
    setIsRemote(draft.isRemote);
    if (draft.requirements.length > 0 && !notes) setNotes(`Requirements:\n${draft.requirements.map((r) => `- ${r}`).join("\n")}`);
    setDrafted(true);
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    const created = await save.run(() =>
      api.applications.create({
        companyName: company,
        positionTitle: position,
        status,
        source: source || null,
        location: location || null,
        isRemote,
        salaryRange: salary || null,
        jobPostUrl: url || null,
        jobPostText: postText || null,
        notes: notes || null,
      }),
    );
    if (!created) return;
    refreshToday();
    onCreated(created);
    onClose();
  }

  return (
    <form className="form" onSubmit={submit} aria-label="New application">
      <label>
        <span>Job post text (optional)</span>
        <textarea
          value={postText}
          onChange={(e) => setPostText(e.target.value)}
          rows={4}
          maxLength={20000}
          placeholder="Paste the job post here to keep it with the application, or let AI fill the form from it."
        />
      </label>
      <div className="actions" style={{ alignItems: "center" }}>
        <AiButton onRun={fillFromPost} busy={extract.busy} disabled={postText.trim().length < 30}>
          Fill the form from this text
        </AiButton>
        <AiAllowance />
      </div>
      <ErrorNotice error={extract.error} />
      {extract.error && <p className="note">You can still fill the form by hand below.</p>}
      {nothingFound && <Notice>The AI could not find a job post in that text. Fill the form by hand below.</Notice>}
      {drafted && <Notice kind="info"><b>AI draft.</b> Check every field before saving; nothing is saved until you press Create.</Notice>}

      <div className="pair">
        <label>
          <span>Company</span>
          <input type="text" value={company} onChange={(e) => setCompany(e.target.value)} required maxLength={150} />
        </label>
        <label>
          <span>Position</span>
          <input type="text" value={position} onChange={(e) => setPosition(e.target.value)} required maxLength={150} />
        </label>
      </div>
      <div className="pair">
        <label>
          <span>Stage</span>
          <select value={status} onChange={(e) => setStatus(e.target.value as ApplicationStatus)}>
            {STATUSES.map((s) => <option key={s} value={s}>{statusLabel(s)}</option>)}
          </select>
        </label>
        <label>
          <span>Source</span>
          <select value={source} onChange={(e) => setSource(e.target.value as ApplicationSource | "")}>
            <option value="">Not set</option>
            {SOURCES.map((s) => <option key={s} value={s}>{sourceLabel(s)}</option>)}
          </select>
        </label>
      </div>
      <div className="pair">
        <label>
          <span>Location</span>
          <input type="text" value={location} onChange={(e) => setLocation(e.target.value)} maxLength={150} />
        </label>
        <label>
          <span>Salary range</span>
          <input type="text" value={salary} onChange={(e) => setSalary(e.target.value)} maxLength={100} placeholder="USD 3,000 to 3,600 / month" />
        </label>
      </div>
      <label className="check-inline">
        <input type="checkbox" checked={isRemote} onChange={(e) => setIsRemote(e.target.checked)} />
        Remote
      </label>
      <label>
        <span>Job post link</span>
        <input type="url" value={url} onChange={(e) => setUrl(e.target.value)} maxLength={2000} placeholder="https://" />
      </label>
      <label>
        <span>Notes</span>
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={3} maxLength={5000} />
      </label>

      <ErrorNotice error={save.error} />
      <div className="dialog-actions">
        <button type="button" className="btn" onClick={onClose}>Cancel</button>
        <button type="submit" className="btn primary" disabled={save.busy}>{save.busy ? "Saving…" : "Create application"}</button>
      </div>
    </form>
  );
}
