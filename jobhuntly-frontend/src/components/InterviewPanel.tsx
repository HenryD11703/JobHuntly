"use client";

import { useState, type FormEvent } from "react";
import { api } from "@/lib/api";
import { useApp } from "@/lib/app-context";
import { useNow } from "@/lib/use-now";
import {
  formatDay, formatFullDay, formatTime, fromLocalInput, linesToList, listToLines, roundLabel, timeUntil, toLocalInput,
} from "@/lib/format";
import type { Debrief, DebriefDraft, Interview, InterviewStatus } from "@/lib/types";
import { AiAllowance, AiButton } from "./AiButton";
import { Dialog } from "./Dialog";
import { ErrorNotice, Notice, useAction } from "./ui";

interface Props {
  applicationId: string;
  interview: Interview;
  /** Called with the fresh interview after any change, so the page can update and refresh its timeline. */
  onChange: (interview: Interview) => void;
}

const STATUS_TAG: Record<InterviewStatus, string> = { Scheduled: "Scheduled", Done: "Done", Cancelled: "Cancelled" };

/** One interview: when, checklist, likely questions and debrief. */
export function InterviewPanel({ applicationId, interview, onChange }: Props) {
  const { refreshToday } = useApp();
  const [editing, setEditing] = useState(false);
  const now = useNow();
  const past = new Date(interview.interviewDate).getTime() < now;
  const upcoming = interview.status === "Scheduled" && !past;
  const checklistDone = interview.checklist.filter((c) => c.done).length;

  function changed(next: Interview) {
    onChange(next);
    refreshToday();
  }

  return (
    <div>
      <div className={`depart${past || interview.status !== "Scheduled" ? " past" : ""}`}>
        <span className="t">{formatTime(interview.interviewDate)}</span>
        <div>
          <span className="mono">{formatFullDay(interview.interviewDate).toUpperCase()}</span>
          <b>{roundLabel(interview.roundType)} round</b>
          <span className="mono">{upcoming ? timeUntil(interview.interviewDate) : STATUS_TAG[interview.status].toLowerCase()}</span>
        </div>
        {upcoming && !interview.checklistComplete && <span className="tag signal">Prepare · {timeUntil(interview.interviewDate)}</span>}
        {interview.status === "Cancelled" && <span className="tag bad">Cancelled</span>}
        {interview.status === "Done" && <span className="tag ok">Done</span>}
      </div>
      <div className="actions" style={{ marginBottom: 6 }}>
        <button type="button" className="btn small" onClick={() => setEditing(true)}>Edit interview</button>
      </div>
      {interview.notes && <p className="note" style={{ marginBottom: 8 }}>Notes: {interview.notes}</p>}

      <Checklist applicationId={applicationId} interview={interview} onChange={changed} done={checklistDone} />
      <PrepQuestions applicationId={applicationId} interview={interview} onChange={changed} />
      <DebriefSection applicationId={applicationId} interview={interview} onChange={changed} />

      <EditInterviewDialog
        open={editing}
        interview={interview}
        applicationId={applicationId}
        onClose={() => setEditing(false)}
        onSaved={changed}
      />
    </div>
  );
}

// ---------------------------------------------------------------- checklist

function Checklist({ applicationId, interview, onChange, done }: Props & { done: number }) {
  const tick = useAction();

  async function toggle(itemId: string, checked: boolean) {
    // Optimistic: show the tick at once, and put it back if the server says no.
    onChange({ ...interview, checklist: interview.checklist.map((c) => (c.id === itemId ? { ...c, done: checked } : c)) });
    const fresh = await tick.run(() => api.interviews.tick(applicationId, interview.id, itemId, checked));
    onChange(fresh ?? interview);
  }

  return (
    <section aria-labelledby={`check-${interview.id}`} style={{ marginTop: 14 }}>
      <h3 id={`check-${interview.id}`} style={{ fontSize: 20, marginBottom: 6 }}>
        Checklist <span className="mono muted">{done} of {interview.checklist.length}</span>
      </h3>
      <ErrorNotice error={tick.error} />
      {interview.checklist.map((item) => (
        <div className="check" key={item.id}>
          <label>
            <input type="checkbox" checked={item.done} onChange={(e) => void toggle(item.id, e.target.checked)} />
            <span>{item.text}</span>
          </label>
        </div>
      ))}
    </section>
  );
}

// ---------------------------------------------------------------- prep questions

function PrepQuestions({ applicationId, interview, onChange }: Props) {
  const { refreshAi } = useApp();
  const ask = useAction();
  const prep = interview.prepQuestions;

  async function generate(regenerate: boolean) {
    const result = await ask.run(() => api.ai.prep(applicationId, interview.id, regenerate));
    refreshAi();
    if (result) onChange({ ...interview, prepQuestions: result.prep });
  }

  return (
    <section aria-labelledby={`prep-${interview.id}`} style={{ marginTop: 18 }}>
      <h3 id={`prep-${interview.id}`} style={{ fontSize: 20, marginBottom: 6 }}>Likely questions</h3>
      <div className="actions" style={{ alignItems: "center", marginBottom: 8 }}>
        {prep && <span className="tag ai">AI draft · saved with this interview</span>}
        <AiButton onRun={() => generate(Boolean(prep))} busy={ask.busy}>
          {prep ? "Regenerate" : "Generate likely questions"}
        </AiButton>
        <AiAllowance />
      </div>
      <ErrorNotice error={ask.error} />
      {!prep && !ask.error && (
        <p className="muted">
          No questions yet. AI can suggest up to 8 from the job post, the round and your stories. The checklist above works without it.
        </p>
      )}
      {prep?.questions.map((q, index) => (
        <div className="quote" key={`${index}-${q.question}`}>
          <b>{q.question}</b>
          <div className="link">{q.storyTitle ? `Story: "${q.storyTitle}"` : "No story linked"}</div>
        </div>
      ))}
      {prep && <p className="note">Suggestions only: you decide what to practise.</p>}
    </section>
  );
}

// ---------------------------------------------------------------- debrief

function DebriefSection({ applicationId, interview, onChange }: Props) {
  const now = useNow();
  const past = new Date(interview.interviewDate).getTime() < now;
  const available = interview.status !== "Cancelled" && (past || interview.status === "Done");

  return (
    <section aria-labelledby={`debrief-${interview.id}`} style={{ marginTop: 18 }}>
      <h3 id={`debrief-${interview.id}`} style={{ fontSize: 20, marginBottom: 6 }}>
        Debrief {interview.debrief && <span className="tag ok">Saved</span>}
      </h3>
      {!available ? (
        <p className="muted">
          {interview.status === "Cancelled"
            ? "This interview was cancelled, so there is nothing to debrief."
            : `Available after the interview, ${formatDay(interview.interviewDate)} ${formatTime(interview.interviewDate)}.`}
        </p>
      ) : (
        <DebriefForm applicationId={applicationId} interview={interview} onChange={onChange} />
      )}
    </section>
  );
}

function DebriefForm({ applicationId, interview, onChange }: Props) {
  const { refreshAi } = useApp();
  const saved: Debrief | null = interview.debrief;
  const save = useAction();
  const structure = useAction();
  const [rating, setRating] = useState(saved?.rating ?? 3);
  const [raw, setRaw] = useState(saved?.rawNotes ?? "");
  const [asked, setAsked] = useState(listToLines(saved?.questionsAsked ?? []));
  const [well, setWell] = useState(listToLines(saved?.wentWell ?? []));
  const [poorly, setPoorly] = useState(listToLines(saved?.wentPoorly ?? []));
  const [followUps, setFollowUps] = useState(listToLines(saved?.followUps ?? []));
  const [drafted, setDrafted] = useState(false);
  const [justSaved, setJustSaved] = useState(false);

  function applyDraft(draft: DebriefDraft) {
    setAsked(listToLines(draft.questionsAsked));
    setWell(listToLines(draft.wentWell));
    setPoorly(listToLines(draft.wentPoorly));
    setFollowUps(listToLines(draft.followUps));
    setDrafted(true);
  }

  async function runStructure() {
    const draft = await structure.run(() => api.ai.structureDebrief(applicationId, interview.id, raw));
    refreshAi();
    if (draft) applyDraft(draft);
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    setJustSaved(false);
    const fresh = await save.run(() =>
      api.interviews.saveDebrief(applicationId, interview.id, {
        rating,
        rawNotes: raw,
        questionsAsked: linesToList(asked),
        wentWell: linesToList(well),
        wentPoorly: linesToList(poorly),
        followUps: linesToList(followUps),
      }),
    );
    if (fresh) {
      onChange(fresh);
      setDrafted(false);
      setJustSaved(true);
    }
  }

  return (
    <form className="form" onSubmit={submit} aria-label="Debrief">
      <label>
        <span>How did it feel? (1 to 5)</span>
        <select value={rating} onChange={(e) => setRating(Number(e.target.value))} style={{ maxWidth: 200 }}>
          {[1, 2, 3, 4, 5].map((n) => <option key={n} value={n}>{n}{n === 1 ? " · rough" : n === 5 ? " · great" : ""}</option>)}
        </select>
      </label>
      <label>
        <span>My notes</span>
        <textarea value={raw} onChange={(e) => setRaw(e.target.value)} rows={4} maxLength={10000} placeholder="Write it down while you remember: what they asked, how it went, what comes next." />
      </label>
      <div className="actions" style={{ alignItems: "center" }}>
        <AiButton onRun={runStructure} busy={structure.busy} disabled={raw.trim().length < 10}>Structure my notes</AiButton>
        <AiAllowance />
      </div>
      <ErrorNotice error={structure.error} />
      {drafted && <Notice kind="info"><b>AI draft.</b> Edit the lists below, then save the debrief. Nothing is saved until you do.</Notice>}

      <div className="pair">
        <label><span>Questions asked (one per line)</span><textarea value={asked} onChange={(e) => setAsked(e.target.value)} rows={4} /></label>
        <label><span>Went well</span><textarea value={well} onChange={(e) => setWell(e.target.value)} rows={4} /></label>
        <label><span>Went poorly</span><textarea value={poorly} onChange={(e) => setPoorly(e.target.value)} rows={4} /></label>
        <label><span>Follow-ups</span><textarea value={followUps} onChange={(e) => setFollowUps(e.target.value)} rows={4} /></label>
      </div>

      <ErrorNotice error={save.error} />
      {justSaved && <Notice>Debrief saved.</Notice>}
      <div className="actions">
        <button type="submit" className="btn primary" disabled={save.busy}>{save.busy ? "Saving…" : saved ? "Update debrief" : "Save debrief"}</button>
      </div>
    </form>
  );
}

// ---------------------------------------------------------------- edit dialog

function EditInterviewDialog({
  open, interview, applicationId, onClose, onSaved,
}: { open: boolean; interview: Interview; applicationId: string; onClose: () => void; onSaved: (next: Interview) => void }) {
  return (
    <Dialog open={open} title="Edit interview" onClose={onClose}>
      <EditInterviewForm interview={interview} applicationId={applicationId} onClose={onClose} onSaved={onSaved} />
    </Dialog>
  );
}

function EditInterviewForm({
  interview, applicationId, onClose, onSaved,
}: { interview: Interview; applicationId: string; onClose: () => void; onSaved: (next: Interview) => void }) {
  const save = useAction();
  const [when, setWhen] = useState(toLocalInput(interview.interviewDate));
  const [status, setStatus] = useState<InterviewStatus>(interview.status);
  const [notes, setNotes] = useState(interview.notes ?? "");

  async function submit(event: FormEvent) {
    event.preventDefault();
    const fresh = await save.run(() =>
      api.interviews.update(applicationId, interview.id, { interviewDate: fromLocalInput(when), status, notes }),
    );
    if (fresh) {
      onSaved(fresh);
      onClose();
    }
  }

  return (
    <form className="form" onSubmit={submit} aria-label="Edit interview">
      <label>
        <span>Date and time</span>
        <input type="datetime-local" value={when} onChange={(e) => setWhen(e.target.value)} required />
      </label>
      <label>
        <span>Status</span>
        <select value={status} onChange={(e) => setStatus(e.target.value as InterviewStatus)}>
          <option value="Scheduled">Scheduled</option>
          <option value="Done">Done</option>
          <option value="Cancelled">Cancelled</option>
        </select>
      </label>
      <label>
        <span>Notes</span>
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={3} maxLength={5000} />
      </label>
      <ErrorNotice error={save.error} />
      <div className="dialog-actions">
        <button type="button" className="btn" onClick={onClose}>Cancel</button>
        <button type="submit" className="btn primary" disabled={save.busy}>{save.busy ? "Saving…" : "Save"}</button>
      </div>
    </form>
  );
}
