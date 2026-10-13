"use client";

import Link from "next/link";
import { useState } from "react";
import { FollowUpDialog } from "@/components/ApplicationDialogs";
import { Empty, ErrorNotice, PageHead } from "@/components/ui";
import { api } from "@/lib/api";
import { useApp } from "@/lib/app-context";
import { formatFullDay } from "@/lib/format";
import type { Nudge, NudgeType } from "@/lib/types";
import { useResource } from "@/lib/use-resource";

const GROUPS: { type: NudgeType; title: string }[] = [
  { type: "Prepare", title: "Prepare" },
  { type: "WriteDebrief", title: "Write a debrief" },
  { type: "FollowUp", title: "Follow up" },
];

export default function TodayPage() {
  const { refreshToday } = useApp();
  const today = useResource("today", api.today);
  const [following, setFollowing] = useState<Nudge | null>(null);
  const nudges = today.data ?? [];

  return (
    <div className="wrap">
      <PageHead eyebrow={formatFullDay(new Date().toISOString()).toUpperCase()} title="Today">
        <p className="note" style={{ maxWidth: "34em" }}>
          Computed by plain rules, no AI: 7 days without news after you applied, interviews in the next 48 hours with an
          incomplete checklist, and finished interviews without a debrief.
        </p>
      </PageHead>

      <ErrorNotice error={today.error} />
      {today.data && nudges.length === 0 && (
        <Empty title="Nothing needs you today">
          Applications waiting on a company reply, rejected ones and offers never show up here.
        </Empty>
      )}

      {GROUPS.map(({ type, title }) => {
        const items = nudges.filter((n) => n.type === type);
        if (items.length === 0) return null;
        return (
          <section key={type} className="panel" aria-labelledby={`group-${type}`}>
            <h2 id={`group-${type}`}>{title} <span className="mono muted">{items.length}</span></h2>
            <div className="scroll">
              <table>
                <thead>
                  <tr><th scope="col">Company</th><th scope="col">Why</th><th scope="col" className="num">Action</th></tr>
                </thead>
                <tbody>
                  {items.map((nudge) => (
                    <tr key={`${nudge.type}-${nudge.applicationId}-${nudge.interviewId ?? ""}`}>
                      <td><b>{nudge.companyName}</b><br /><span className="muted">{nudge.positionTitle}</span></td>
                      <td>{nudge.reason}</td>
                      <td className="num">
                        {type === "FollowUp" ? (
                          <button type="button" className="btn small" onClick={() => setFollowing(nudge)}>Log follow-up</button>
                        ) : (
                          <Link className="btn small primary" href={`/applications/${nudge.applicationId}`}>
                            {type === "Prepare" ? "Open prep" : "Write debrief"}
                          </Link>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>
        );
      })}

      {following && (
        <FollowUpDialog
          open
          applicationId={following.applicationId}
          company={following.companyName}
          onClose={() => setFollowing(null)}
          onLogged={() => { today.reload(); refreshToday(); }}
        />
      )}
    </div>
  );
}
