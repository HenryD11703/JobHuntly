"use client";

import Link from "next/link";
import { useMemo, useState, type DragEvent } from "react";
import { NewApplicationDialog } from "@/components/NewApplicationDialog";
import { RejectDialog } from "@/components/RejectDialog";
import { Empty, ErrorNotice, PageHead, useAction } from "@/components/ui";
import { api } from "@/lib/api";
import { useApp } from "@/lib/app-context";
import { useNow } from "@/lib/use-now";
import { daysAgo, daysBetween, formatDay, formatTime, roundLabel, sourceLabel, statusLabel, timeUntil } from "@/lib/format";
import { STATUSES, type ApplicationStatus, type JobApplication, type Nudge } from "@/lib/types";
import { useResource } from "@/lib/use-resource";

function nextInterview(application: JobApplication, now: number) {
  return application.interviews
    .filter((i) => i.status === "Scheduled" && new Date(i.interviewDate).getTime() > now)
    .sort((a, b) => a.interviewDate.localeCompare(b.interviewDate))[0];
}

function metaLine(application: JobApplication): string {
  const source = application.source ? ` · ${sourceLabel(application.source)}` : "";
  switch (application.status) {
    case "Backlog":
      return `SAVED ${daysAgo(application.appliedDate).toUpperCase()}${source}`.toUpperCase();
    case "Interviewing": {
      const n = application.interviews.length;
      return `${n} INTERVIEW${n === 1 ? "" : "S"}${source}`.toUpperCase();
    }
    case "TechnicalTest":
      return `TEST STAGE${source}`.toUpperCase();
    case "Offer":
      return `OFFER${source}`.toUpperCase();
    case "Rejected":
      return (application.rejectionReason ? `REJECTED · ${application.rejectionReason}` : "REJECTED · NO REASON GIVEN").toUpperCase();
    default:
      return `APPLIED ${daysAgo(application.appliedDate).toUpperCase()}${source}`.toUpperCase();
  }
}

function nudgeText(nudge: Nudge): { tag: string; text: string } {
  switch (nudge.type) {
    case "Prepare":
      return { tag: "Prepare", text: timeUntil(nudge.since) };
    case "WriteDebrief":
      return { tag: "Write debrief", text: `${formatDay(nudge.since)} round` };
    default:
      return { tag: "Follow up", text: `no reply in ${daysBetween(nudge.since)} days` };
  }
}

export default function BoardPage() {
  const { refreshToday } = useApp();
  const now = useNow();
  const apps = useResource("board:applications", api.applications.list);
  const today = useResource("board:today", api.today);
  const [creating, setCreating] = useState(false);
  const [rejecting, setRejecting] = useState<JobApplication | null>(null);
  const [dragOver, setDragOver] = useState<ApplicationStatus | null>(null);
  const [dragId, setDragId] = useState<string | null>(null);
  const move = useAction();

  const columns = useMemo(() => {
    const map = new Map<ApplicationStatus, JobApplication[]>(STATUSES.map((s) => [s, []]));
    for (const application of apps.data ?? []) map.get(application.status)?.push(application);
    return map;
  }, [apps.data]);

  const nudgesByApp = useMemo(() => {
    const map = new Map<string, Nudge[]>();
    for (const nudge of today.data ?? []) map.set(nudge.applicationId, [...(map.get(nudge.applicationId) ?? []), nudge]);
    return map;
  }, [today.data]);

  async function changeStatus(application: JobApplication, status: ApplicationStatus, reason?: string) {
    if (application.status === status) return;
    if (status === "Rejected" && reason === undefined) {
      setRejecting(application);
      return;
    }
    const updated = await move.run(() => api.applications.setStatus(application.id, status, reason));
    if (!updated) return;
    apps.setData((apps.data ?? []).map((a) => (a.id === updated.id ? updated : a)));
    today.reload();
    refreshToday();
  }

  function onDrop(event: DragEvent, status: ApplicationStatus) {
    event.preventDefault();
    setDragOver(null);
    const application = apps.data?.find((a) => a.id === (dragId ?? event.dataTransfer.getData("text/plain")));
    setDragId(null);
    if (application) void changeStatus(application, status);
  }

  const total = apps.data?.length ?? 0;

  return (
    <div className="wrap">
      <PageHead
        eyebrow={apps.data ? `${total} application${total === 1 ? "" : "s"}` : "Loading"}
        title="Pipeline"
      >
        <button type="button" className="btn primary" onClick={() => setCreating(true)}>+ New application</button>
      </PageHead>

      <ErrorNotice error={apps.error ?? move.error} />

      {apps.data && total === 0 && (
        <Empty title="Nothing here yet">
          Add your first application. You can paste a job post and let AI draft the form, or fill it in by hand.
        </Empty>
      )}

      <div className="board">
        {STATUSES.map((status) => {
          const items = columns.get(status) ?? [];
          return (
            <section
              key={status}
              className={`col${status === "Offer" ? " is-offer" : ""}${status === "Rejected" ? " is-reject" : ""}${dragOver === status ? " drop" : ""}`}
              aria-labelledby={`col-${status}`}
              onDragOver={(e) => { e.preventDefault(); setDragOver(status); }}
              onDragLeave={() => setDragOver((current) => (current === status ? null : current))}
              onDrop={(e) => onDrop(e, status)}
            >
              <h2 id={`col-${status}`}>
                {statusLabel(status)} <span className="n" aria-label={`${items.length} applications`}>{String(items.length).padStart(2, "0")}</span>
              </h2>
              <ul style={{ listStyle: "none" }}>
                {items.map((application) => {
                  const nudges = nudgesByApp.get(application.id) ?? [];
                  const upcoming = nextInterview(application, now);
                  return (
                    <li
                      key={application.id}
                      className={`card${nudges.length ? " needs" : ""}${dragId === application.id ? " dragging" : ""}`}
                      draggable
                      onDragStart={(e) => { setDragId(application.id); e.dataTransfer.setData("text/plain", application.id); e.dataTransfer.effectAllowed = "move"; }}
                      onDragEnd={() => { setDragId(null); setDragOver(null); }}
                    >
                      <Link href={`/applications/${application.id}`}>
                        <b>{application.companyName}</b>
                        <div className="role">{application.positionTitle}</div>
                        <div className="meta">{metaLine(application)}</div>
                      </Link>
                      {(upcoming || nudges.length > 0) && (
                        <div className="next">
                          {upcoming && (
                            <div>
                              NEXT: {roundLabel(upcoming.roundType).toUpperCase()}
                              <br />
                              {formatDay(upcoming.interviewDate).toUpperCase()} {formatTime(upcoming.interviewDate)}
                            </div>
                          )}
                          {nudges.map((nudge) => {
                            const { tag, text } = nudgeText(nudge);
                            return <div key={`${nudge.type}-${nudge.interviewId ?? ""}`}><span className="tag signal">{tag}</span> {text}</div>;
                          })}
                        </div>
                      )}
                      <div className="mover">
                        <label className="visually-hidden" htmlFor={`move-${application.id}`}>
                          Move {application.companyName} to another stage
                        </label>
                        <select
                          id={`move-${application.id}`}
                          value={application.status}
                          onChange={(e) => void changeStatus(application, e.target.value as ApplicationStatus)}
                          disabled={move.busy}
                        >
                          {STATUSES.map((s) => <option key={s} value={s}>{s === application.status ? `Stage: ${statusLabel(s)}` : `Move to ${statusLabel(s)}`}</option>)}
                        </select>
                      </div>
                    </li>
                  );
                })}
              </ul>
            </section>
          );
        })}
      </div>
      <p className="foot">Yellow cards need something from you. Everything else is waiting on the company.</p>

      <NewApplicationDialog
        open={creating}
        onClose={() => setCreating(false)}
        onCreated={(created) => { apps.setData([created, ...(apps.data ?? [])]); today.reload(); }}
      />
      <RejectDialog
        company={rejecting?.companyName ?? null}
        onCancel={() => setRejecting(null)}
        onConfirm={async (reason) => {
          const application = rejecting;
          setRejecting(null);
          if (application) await changeStatus(application, "Rejected", reason);
        }}
      />
    </div>
  );
}
