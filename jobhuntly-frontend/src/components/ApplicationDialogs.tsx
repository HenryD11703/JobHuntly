"use client";

import { useState, type FormEvent } from "react";
import { api } from "@/lib/api";
import { fromLocalInput, roundLabel, sourceLabel, toLocalInput } from "@/lib/format";
import {
  CURRENCIES, ROUNDS, SOURCES, type ApplicationSource, type Currency, type Interview, type InterviewRound,
  type JobApplication, type Offer, type PayPeriod,
} from "@/lib/types";
import { Dialog } from "./Dialog";
import { ErrorNotice, useAction } from "./ui";

// ---------------------------------------------------------------- add interview

export function AddInterviewDialog({
  open, applicationId, onClose, onAdded,
}: { open: boolean; applicationId: string; onClose: () => void; onAdded: (interview: Interview) => void }) {
  return (
    <Dialog open={open} title="Add interview" onClose={onClose}>
      <AddInterviewForm applicationId={applicationId} onClose={onClose} onAdded={onAdded} />
    </Dialog>
  );
}

function defaultInterviewTime(): string {
  const tomorrow = new Date(Date.now() + 86_400_000);
  tomorrow.setHours(10, 0, 0, 0);
  return toLocalInput(tomorrow.toISOString());
}

function AddInterviewForm({ applicationId, onClose, onAdded }: { applicationId: string; onClose: () => void; onAdded: (i: Interview) => void }) {
  const save = useAction();
  const [round, setRound] = useState<InterviewRound>("HrScreen");
  const [when, setWhen] = useState(defaultInterviewTime);
  const [notes, setNotes] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    const created = await save.run(() => api.interviews.add(applicationId, fromLocalInput(when), round, notes));
    if (created) {
      onAdded(created);
      onClose();
    }
  }

  return (
    <form className="form" onSubmit={submit} aria-label="Add interview">
      <div className="pair">
        <label>
          <span>Round</span>
          <select value={round} onChange={(e) => setRound(e.target.value as InterviewRound)}>
            {ROUNDS.map((r) => <option key={r} value={r}>{roundLabel(r)}</option>)}
          </select>
        </label>
        <label>
          <span>Date and time</span>
          <input type="datetime-local" value={when} onChange={(e) => setWhen(e.target.value)} required />
        </label>
      </div>
      <label>
        <span>Notes (optional)</span>
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={3} maxLength={5000} placeholder="Who you will talk to, the video link, anything to remember." />
      </label>
      <ErrorNotice error={save.error} />
      <div className="dialog-actions">
        <button type="button" className="btn" onClick={onClose}>Cancel</button>
        <button type="submit" className="btn primary" disabled={save.busy}>{save.busy ? "Adding…" : "Add interview"}</button>
      </div>
    </form>
  );
}

// ---------------------------------------------------------------- follow-up

export function FollowUpDialog({
  open, applicationId, company, onClose, onLogged,
}: { open: boolean; applicationId: string; company: string; onClose: () => void; onLogged: () => void }) {
  return (
    <Dialog open={open} title="Log a follow-up" onClose={onClose}>
      <FollowUpForm applicationId={applicationId} company={company} onClose={onClose} onLogged={onLogged} />
    </Dialog>
  );
}

function FollowUpForm({ applicationId, company, onClose, onLogged }: { applicationId: string; company: string; onClose: () => void; onLogged: () => void }) {
  const save = useAction();
  const [note, setNote] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    const evt = await save.run(() => api.applications.followUp(applicationId, note));
    if (evt) {
      onLogged();
      onClose();
    }
  }

  return (
    <form className="form" onSubmit={submit} aria-label="Log a follow-up">
      <p>Record that you followed up with <b>{company}</b>. This restarts the 7-day clock for the Today reminder.</p>
      <label>
        <span>What did you send? (optional)</span>
        <input type="text" value={note} onChange={(e) => setNote(e.target.value)} maxLength={1000} placeholder="Emailed the recruiter" />
      </label>
      <ErrorNotice error={save.error} />
      <div className="dialog-actions">
        <button type="button" className="btn" onClick={onClose}>Cancel</button>
        <button type="submit" className="btn primary" disabled={save.busy}>{save.busy ? "Saving…" : "Log follow-up"}</button>
      </div>
    </form>
  );
}

// ---------------------------------------------------------------- edit details

export function EditDetailsDialog({
  open, application, onClose, onSaved,
}: { open: boolean; application: JobApplication; onClose: () => void; onSaved: (a: JobApplication) => void }) {
  return (
    <Dialog open={open} title="Edit details" onClose={onClose}>
      <EditDetailsForm application={application} onClose={onClose} onSaved={onSaved} />
    </Dialog>
  );
}

function EditDetailsForm({ application, onClose, onSaved }: { application: JobApplication; onClose: () => void; onSaved: (a: JobApplication) => void }) {
  const save = useAction();
  const [source, setSource] = useState<ApplicationSource | "">(application.source ?? "");
  const [location, setLocation] = useState(application.location ?? "");
  const [isRemote, setIsRemote] = useState(application.isRemote);
  const [salary, setSalary] = useState(application.salaryRange ?? "");
  const [url, setUrl] = useState(application.jobPostUrl ?? "");
  const [postText, setPostText] = useState(application.jobPostText ?? "");
  const [notes, setNotes] = useState(application.notes ?? "");
  const [reason, setReason] = useState(application.rejectionReason ?? "");

  async function submit(event: FormEvent) {
    event.preventDefault();
    const updated = await save.run(() =>
      api.applications.update(application.id, {
        source: source || null,
        location: location || null,
        isRemote,
        salaryRange: salary || null,
        jobPostUrl: url || null,
        jobPostText: postText || null,
        notes: notes || null,
        rejectionReason: application.status === "Rejected" ? reason || null : application.rejectionReason,
      }),
    );
    if (updated) {
      onSaved(updated);
      onClose();
    }
  }

  return (
    <form className="form" onSubmit={submit} aria-label="Edit details">
      <div className="pair">
        <label>
          <span>Source</span>
          <select value={source} onChange={(e) => setSource(e.target.value as ApplicationSource | "")}>
            <option value="">Not set</option>
            {SOURCES.map((s) => <option key={s} value={s}>{sourceLabel(s)}</option>)}
          </select>
        </label>
        <label>
          <span>Location</span>
          <input type="text" value={location} onChange={(e) => setLocation(e.target.value)} maxLength={150} />
        </label>
      </div>
      <label className="check-inline">
        <input type="checkbox" checked={isRemote} onChange={(e) => setIsRemote(e.target.checked)} />
        Remote
      </label>
      <div className="pair">
        <label>
          <span>Salary range</span>
          <input type="text" value={salary} onChange={(e) => setSalary(e.target.value)} maxLength={100} />
        </label>
        <label>
          <span>Job post link</span>
          <input type="url" value={url} onChange={(e) => setUrl(e.target.value)} maxLength={2000} placeholder="https://" />
        </label>
      </div>
      {application.status === "Rejected" && (
        <label>
          <span>Rejection reason</span>
          <input type="text" value={reason} onChange={(e) => setReason(e.target.value)} maxLength={1000} />
        </label>
      )}
      <label>
        <span>Job post text</span>
        <textarea value={postText} onChange={(e) => setPostText(e.target.value)} rows={5} maxLength={20000} />
        <div className="hint">Used to suggest likely interview questions.</div>
      </label>
      <label>
        <span>Notes</span>
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={3} maxLength={5000} />
      </label>
      <ErrorNotice error={save.error} />
      <div className="dialog-actions">
        <button type="button" className="btn" onClick={onClose}>Cancel</button>
        <button type="submit" className="btn primary" disabled={save.busy}>{save.busy ? "Saving…" : "Save details"}</button>
      </div>
    </form>
  );
}

// ---------------------------------------------------------------- add offer

export function AddOfferDialog({
  open, applications, preselectedId, onClose, onAdded,
}: {
  open: boolean;
  /** Applications the offer can be attached to. */
  applications: Pick<JobApplication, "id" | "companyName" | "positionTitle">[];
  preselectedId?: string;
  onClose: () => void;
  onAdded: (offer: Offer) => void;
}) {
  return (
    <Dialog open={open} title="Add an offer" onClose={onClose}>
      <AddOfferForm applications={applications} preselectedId={preselectedId} onClose={onClose} onAdded={onAdded} />
    </Dialog>
  );
}

function AddOfferForm({
  applications, preselectedId, onClose, onAdded,
}: { applications: Pick<JobApplication, "id" | "companyName" | "positionTitle">[]; preselectedId?: string; onClose: () => void; onAdded: (o: Offer) => void }) {
  const save = useAction();
  const [applicationId, setApplicationId] = useState(preselectedId ?? applications[0]?.id ?? "");
  const [amount, setAmount] = useState("");
  const [currency, setCurrency] = useState<Currency>("USD");
  const [period, setPeriod] = useState<PayPeriod>("Monthly");
  const [notes, setNotes] = useState("");

  async function submit(event: FormEvent) {
    event.preventDefault();
    const offer = await save.run(() =>
      api.offers.add(applicationId, { amount: Number(amount), currency, period, notes: notes || null }),
    );
    if (offer) {
      onAdded(offer);
      onClose();
    }
  }

  return (
    <form className="form" onSubmit={submit} aria-label="Add an offer">
      {!preselectedId && (
        <label>
          <span>Application</span>
          <select value={applicationId} onChange={(e) => setApplicationId(e.target.value)} required>
            {applications.map((a) => <option key={a.id} value={a.id}>{a.companyName} · {a.positionTitle}</option>)}
          </select>
        </label>
      )}
      <div className="pair">
        <label>
          <span>Amount (gross)</span>
          <input type="number" value={amount} onChange={(e) => setAmount(e.target.value)} min="0.01" step="0.01" required inputMode="decimal" />
        </label>
        <label>
          <span>Currency</span>
          <select value={currency} onChange={(e) => setCurrency(e.target.value as Currency)}>
            {CURRENCIES.map((c) => <option key={c} value={c}>{c}</option>)}
          </select>
        </label>
      </div>
      <label>
        <span>Per</span>
        <select value={period} onChange={(e) => setPeriod(e.target.value as PayPeriod)} style={{ maxWidth: 220 }}>
          <option value="Monthly">Month</option>
          <option value="Annual">Year</option>
        </select>
      </label>
      <label>
        <span>Notes (benefits, equity, contract type)</span>
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={3} maxLength={2000} />
      </label>
      <p className="note">Gross figures as entered; no tax calculation.</p>
      <ErrorNotice error={save.error} />
      <div className="dialog-actions">
        <button type="button" className="btn" onClick={onClose}>Cancel</button>
        <button type="submit" className="btn primary" disabled={save.busy || !applicationId}>{save.busy ? "Saving…" : "Add offer"}</button>
      </div>
    </form>
  );
}
