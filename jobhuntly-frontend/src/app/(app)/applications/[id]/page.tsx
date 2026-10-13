"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useState } from "react";
import { AddInterviewDialog, AddOfferDialog, EditDetailsDialog, FollowUpDialog } from "@/components/ApplicationDialogs";
import { Dialog } from "@/components/Dialog";
import { InterviewPanel } from "@/components/InterviewPanel";
import { RejectDialog } from "@/components/RejectDialog";
import { Empty, ErrorNotice, Notice, useAction } from "@/components/ui";
import { api } from "@/lib/api";
import { useApp } from "@/lib/app-context";
import { useNow } from "@/lib/use-now";
import { formatDay, formatFullDay, formatTime, roundLabel, sourceLabel, statusLabel } from "@/lib/format";
import { STATUSES, type ApplicationEvent, type ApplicationStatus, type Interview, type JobApplication } from "@/lib/types";
import { useResource } from "@/lib/use-resource";

function describeEvent(event: ApplicationEvent): string {
  switch (event.type) {
    case "Created":
      return `Added as ${event.toStatus ? statusLabel(event.toStatus) : "a new application"}`;
    case "StatusChanged":
      return `Moved to ${event.toStatus ? statusLabel(event.toStatus) : "a new stage"}${event.note ? ` (${event.note})` : ""}`;
    case "InterviewAdded":
      return `${event.note ? roundLabel(event.note as never) : "An"} interview added`;
    case "FollowedUp":
      return `Follow-up sent${event.note ? `: ${event.note}` : ""}`;
    case "DebriefSaved":
      return `Debrief saved${event.note ? ` (${roundLabel(event.note as never)})` : ""}`;
    default:
      return `Offer added${event.note ? `: ${event.note}` : ""}`;
  }
}

/** The interview to show first: the next upcoming one, otherwise the most recent. */
function defaultInterviewId(interviews: Interview[], now: number): string | undefined {
  const upcoming = interviews
    .filter((i) => i.status === "Scheduled" && new Date(i.interviewDate).getTime() >= now)
    .sort((a, b) => a.interviewDate.localeCompare(b.interviewDate))[0];
  if (upcoming) return upcoming.id;
  return [...interviews].sort((a, b) => b.interviewDate.localeCompare(a.interviewDate))[0]?.id;
}

export default function ApplicationPage() {
  const { id } = useParams<{ id: string }>();
  const router = useRouter();
  const { refreshToday } = useApp();
  const now = useNow();
  const app = useResource(`app:${id}`, () => api.applications.get(id));
  const timeline = useResource(`timeline:${id}`, () => api.applications.timeline(id));
  const [selected, setSelected] = useState<string>();
  const [addingInterview, setAddingInterview] = useState(false);
  const [followingUp, setFollowingUp] = useState(false);
  const [editing, setEditing] = useState(false);
  const [offering, setOffering] = useState(false);
  const [offerAdded, setOfferAdded] = useState(false);
  const [rejecting, setRejecting] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const act = useAction();

  if (app.error) {
    return (
      <div className="wrap" style={{ paddingTop: 28 }}>
        <ErrorNotice error={app.error} />
        <p><Link href="/board">← Back to the pipeline</Link></p>
      </div>
    );
  }
  if (!app.data) return <p className="boot" role="status">Loading application…</p>;

  const application = app.data;
  const interviews = [...application.interviews].sort((a, b) => a.interviewDate.localeCompare(b.interviewDate));
  const activeId = selected && interviews.some((i) => i.id === selected) ? selected : defaultInterviewId(interviews, now);
  const active = interviews.find((i) => i.id === activeId);

  function replaceInterview(next: Interview) {
    app.setData({ ...application, interviews: application.interviews.map((i) => (i.id === next.id ? next : i)) });
    timeline.reload();
  }

  async function changeStatus(status: ApplicationStatus, reason?: string) {
    if (status === application.status) return;
    if (status === "Rejected" && reason === undefined) {
      setRejecting(true);
      return;
    }
    const updated = await act.run(() => api.applications.setStatus(application.id, status, reason));
    if (updated) {
      app.setData(updated);
      timeline.reload();
      refreshToday();
    }
  }

  async function remove() {
    const done = await act.run(async () => {
      await api.applications.remove(application.id);
      return true;
    });
    if (done) {
      refreshToday();
      router.push("/board");
    }
  }

  return (
    <>
      <div className="band">
        <div className="wrap">
          <div className="head">
            <div>
              <div className="eyebrow">
                <Link href="/board">← Pipeline</Link> · {statusLabel(application.status)}
                {application.source && ` · ${sourceLabel(application.source)}`} · applied {formatDay(application.appliedDate)}
              </div>
              <h1>{application.companyName}</h1>
              <p className="muted" style={{ fontSize: 18, marginTop: 6 }}>
                {application.positionTitle}
                {application.location && ` · ${application.location}`}
                {application.isRemote && !application.location?.toLowerCase().includes("remote") && " · remote"}
                {application.salaryRange && ` · ${application.salaryRange}`}
              </p>
            </div>
            <div className="actions">
              <label className="visually-hidden" htmlFor="status-select">Stage</label>
              <select id="status-select" value={application.status} onChange={(e) => void changeStatus(e.target.value as ApplicationStatus)} disabled={act.busy} style={{ width: "auto", padding: "8px 10px" }}>
                {STATUSES.map((s) => <option key={s} value={s}>{s === application.status ? `Stage: ${statusLabel(s)}` : `Move to ${statusLabel(s)}`}</option>)}
              </select>
              <button type="button" className="btn" onClick={() => setFollowingUp(true)}>Log a follow-up</button>
              <button type="button" className="btn" onClick={() => setEditing(true)}>Edit details</button>
            </div>
          </div>
        </div>
      </div>

      <div className="wrap" style={{ paddingTop: 20 }}>
        <ErrorNotice error={act.error} />
        <div className="grid2">
          <div>
            <section className="panel" aria-labelledby="interviews-h">
              <h2 id="interviews-h">
                Interviews
                <button type="button" className="btn small" onClick={() => setAddingInterview(true)}>+ Add interview</button>
              </h2>
              {interviews.length === 0 && (
                <Empty title="No interviews yet">When one is scheduled, add it here to get a checklist, likely questions and a debrief form.</Empty>
              )}
              {interviews.length > 1 && (
                <div className="interview-switch" role="group" aria-label="Choose an interview">
                  {interviews.map((i) => (
                    <button key={i.id} type="button" aria-pressed={i.id === activeId} onClick={() => setSelected(i.id)}>
                      {roundLabel(i.roundType)} · {formatDay(i.interviewDate)}
                    </button>
                  ))}
                </div>
              )}
              {active && <InterviewPanel key={active.id} applicationId={application.id} interview={active} onChange={replaceInterview} />}
            </section>
          </div>

          <aside>
            <section className="panel" aria-labelledby="timeline-h">
              <h2 id="timeline-h">Timeline</h2>
              {timeline.error && <ErrorNotice error={timeline.error} />}
              {timeline.loading && <p className="note">Loading the timeline…</p>}
              <ul className="timeline">
                {(timeline.data ?? []).map((event, index) => (
                  <li key={event.id} className={index === 0 ? "now" : undefined}>
                    <span className="t">{formatFullDay(event.occurredAt).toUpperCase()} · {formatTime(event.occurredAt)}</span>
                    {describeEvent(event)}
                  </li>
                ))}
              </ul>
            </section>

            <section className="panel" aria-labelledby="details-h">
              <h2 id="details-h">Details</h2>
              <div className="row"><span className="k">Source</span><div>{application.source ? sourceLabel(application.source) : <span className="muted">Not set</span>}</div></div>
              <div className="row"><span className="k">Location</span><div>{application.location || <span className="muted">Not set</span>}{application.isRemote && " · remote"}</div></div>
              <div className="row"><span className="k">Salary</span><div>{application.salaryRange || <span className="muted">Not set</span>}</div></div>
              <div className="row">
                <span className="k">Link</span>
                <div>{application.jobPostUrl ? <a href={application.jobPostUrl} target="_blank" rel="noopener noreferrer">{application.jobPostUrl}</a> : <span className="muted">Not set</span>}</div>
              </div>
              {application.status === "Rejected" && (
                <div className="row"><span className="k">Rejected</span><div>{application.rejectionReason || <span className="muted">No reason given</span>}</div></div>
              )}
              {application.notes && <div className="row"><span className="k">Notes</span><div style={{ whiteSpace: "pre-wrap" }}>{application.notes}</div></div>}
              {application.jobPostText && (
                <details style={{ marginTop: 10 }}>
                  <summary style={{ cursor: "pointer", fontWeight: 600 }}>Saved job post text</summary>
                  <p className="note" style={{ marginTop: 8, whiteSpace: "pre-wrap" }}>{application.jobPostText}</p>
                </details>
              )}
            </section>

            <section className="panel" aria-labelledby="offer-h">
              <h2 id="offer-h">Offer</h2>
              {offerAdded && <Notice>Offer saved. <Link href="/offers">Compare offers →</Link></Notice>}
              <p className="muted" style={{ marginBottom: 10 }}>Got an offer from {application.companyName}? Add it to compare it with the others.</p>
              <button type="button" className="btn small" onClick={() => setOffering(true)}>+ Add an offer</button>
            </section>

            <button type="button" className="btn danger small" onClick={() => setDeleting(true)}>Delete this application</button>
          </aside>
        </div>
      </div>

      <AddInterviewDialog
        open={addingInterview}
        applicationId={application.id}
        onClose={() => setAddingInterview(false)}
        onAdded={(created) => {
          app.setData({ ...application, interviews: [...application.interviews, created] });
          setSelected(created.id);
          timeline.reload();
          refreshToday();
        }}
      />
      <FollowUpDialog
        open={followingUp}
        applicationId={application.id}
        company={application.companyName}
        onClose={() => setFollowingUp(false)}
        onLogged={() => { timeline.reload(); refreshToday(); }}
      />
      <EditDetailsDialog open={editing} application={application} onClose={() => setEditing(false)} onSaved={(updated: JobApplication) => app.setData(updated)} />
      <AddOfferDialog
        open={offering}
        applications={[application]}
        preselectedId={application.id}
        onClose={() => setOffering(false)}
        onAdded={() => { setOfferAdded(true); timeline.reload(); }}
      />
      <RejectDialog
        company={rejecting ? application.companyName : null}
        onCancel={() => setRejecting(false)}
        onConfirm={async (reason) => { setRejecting(false); await changeStatus("Rejected", reason); }}
      />
      <Dialog open={deleting} title="Delete application?" onClose={() => setDeleting(false)}>
        <p>This permanently deletes <b>{application.companyName}</b>, its interviews, debriefs, timeline and offers. It cannot be undone.</p>
        <div className="dialog-actions">
          <button type="button" className="btn" onClick={() => setDeleting(false)}>Keep it</button>
          <button type="button" className="btn danger" onClick={() => { setDeleting(false); void remove(); }}>Delete permanently</button>
        </div>
      </Dialog>
    </>
  );
}
