"use client";

import { Empty, ErrorNotice, PageHead } from "@/components/ui";
import { api } from "@/lib/api";
import { number, sourceLabel, stageLabel } from "@/lib/format";
import { useResource } from "@/lib/use-resource";

const pct = (value: number | null) => (value === null ? "–" : `${number(value, 0)}`);

export default function InsightsPage() {
  const insights = useResource("insights", api.insights);
  const data = insights.data;
  const offers = data?.funnel.find((s) => s.stage === "Offer")?.count ?? 0;

  return (
    <div className="wrap">
      <PageHead eyebrow={data ? `${data.totalApplications} applications` : "Loading"} title="Insights" />
      <ErrorNotice error={insights.error} />

      {data && data.appliedApplications === 0 && (
        <Empty title="Not enough yet">
          Insights start once an application reaches Applied. Until then there is nothing honest to say.
        </Empty>
      )}

      {data && data.appliedApplications > 0 && (
        <>
          <div className="stats">
            <div className="stat"><b>{data.appliedApplications}</b><span className="muted">applied (of {data.totalApplications})</span></div>
            <div className="stat"><b>{pct(data.interviewRatePercent)}%</b><span className="muted">reached an interview</span></div>
            <div className="stat"><b>{offers}</b><span className="muted">{offers === 1 ? "offer" : "offers"}</span></div>
            <div className="stat">
              <b>{data.averageDaysToFirstReply === null ? "–" : `${number(data.averageDaysToFirstReply, 1)} d`}</b>
              <span className="muted">average wait for a first reply</span>
            </div>
          </div>

          <div className="grid2">
            <div>
              <section className="panel" aria-labelledby="funnel-h">
                <h2 id="funnel-h">Funnel</h2>
                <div className="scroll">
                  <table>
                    <thead>
                      <tr><th scope="col">Stage reached</th><th scope="col" style={{ width: "45%" }}>Share of applications</th><th scope="col" className="num">Count</th><th scope="col" className="num">%</th></tr>
                    </thead>
                    <tbody>
                      {data.funnel.map((stage) => (
                        <tr key={stage.stage}>
                          <td>{stageLabel(stage.stage)}</td>
                          <td>
                            <div
                              className={`bar${stage.stage === "Offer" ? " alt" : ""}`}
                              style={{ width: `${Math.max(stage.percent ?? 0, stage.count > 0 ? 2 : 0)}%` }}
                              role="img"
                              aria-label={`${stage.percent ?? 0} percent`}
                            />
                          </td>
                          <td className="num">{stage.count}</td>
                          <td className="num">{pct(stage.percent)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                <p className="note" style={{ marginTop: 8 }}>
                  Computed from each timeline, so an application that ended in Rejected still counts for every stage it reached.
                  Applications still in Backlog are not counted.
                </p>
              </section>

              <section className="panel" aria-labelledby="sources-h">
                <h2 id="sources-h">Where replies come from</h2>
                <div className="scroll">
                  <table>
                    <thead>
                      <tr><th scope="col">Source</th><th scope="col" className="num">Applications</th><th scope="col" className="num">Replies</th><th scope="col" className="num">Reply rate</th></tr>
                    </thead>
                    <tbody>
                      {data.sources.map((source) => (
                        <tr key={source.source}>
                          <td>{sourceLabel(source.source)}</td>
                          <td className="num">{source.applications}</td>
                          <td className="num">{source.replies}</td>
                          <td className="num">{source.enoughData ? `${number(source.replyRatePercent ?? 0, 0)}%` : <span className="muted">not enough data</span>}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
                <p className="note" style={{ marginTop: 8 }}>
                  Percentages appear only with {data.minGroupSize} or more applications in a group. Smaller groups would mislead you.
                </p>
              </section>
            </div>

            <aside>
              <section className="panel" aria-labelledby="stages-h">
                <h2 id="stages-h">Days in each stage</h2>
                {data.stageDurations.map((d) => (
                  <div className="row" key={d.stage}>
                    <span className="k">{stageLabel(d.stage)}</span>
                    <div className="mono">{d.averageDays === null ? <span className="muted">no data yet</span> : `${number(d.averageDays, 1)} d avg (${d.samples})`}</div>
                  </div>
                ))}
                <p className="note" style={{ marginTop: 8 }}>Only stages you have already left are counted.</p>
              </section>

              <section className="panel" aria-labelledby="reasons-h">
                <h2 id="reasons-h">Rejection reasons</h2>
                {data.rejectionReasons.length === 0 && <p className="muted">No rejections recorded.</p>}
                {data.rejectionReasons.map((r) => (
                  <div className="row" key={r.reason}>
                    <span className="k">{r.count}×</span>
                    <div>{r.reason}</div>
                  </div>
                ))}
                {data.rejectionReasons.length > 0 && data.rejectionReasons.reduce((sum, r) => sum + r.count, 0) < 5 && (
                  <p className="note" style={{ marginTop: 8 }}>Too few rejections so far to call it a pattern.</p>
                )}
              </section>
            </aside>
          </div>
        </>
      )}
    </div>
  );
}
