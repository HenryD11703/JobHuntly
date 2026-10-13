import Link from "next/link";
import { DemoButton } from "@/components/DemoButton";
import { PublicBar } from "@/components/PublicBar";

export default function Landing() {
  return (
    <>
      <PublicBar />
      <main id="main" className="wrap">
        <section className="hero">
          <div>
            <div className="eyebrow">An interview notebook</div>
            <h1>
              Every
              <br />
              interview,
              <br />
              on the <mark>board.</mark>
            </h1>
            <p>
              Most trackers store rows. JobHuntly shows what is next, helps you prepare each round, captures what happened
              right after, and tells you what your search is really saying.
            </p>
            <div className="ctas">
              <DemoButton />
              <Link className="btn" href="/register">Create an account</Link>
            </div>
            <p className="note">The demo opens a private sandbox with fictional data. It is deleted after 24 hours.</p>
          </div>
          <aside className="ledger" aria-label="Preview of an interview entry">
            <div className="line">
              <span className="mono">THU 15 OCT</span>
              <div>
                <b>Technical round · Northwind Labs</b>
                <div className="muted" style={{ fontSize: 13 }}>Backend Engineer, remote (LATAM)</div>
              </div>
            </div>
            <div className="line">
              <span className="mono">PREP</span>
              <div>5 of 7 checklist items done · 6 likely questions <span className="tag ai">AI draft</span></div>
            </div>
            <div className="line">
              <span className="mono">NEXT</span>
              <div><span className="tag signal">Write debrief</span> for the HR screen with Tessel</div>
            </div>
            <div className="line">
              <span className="mono">FUNNEL</span>
              <div>14 applied → 5 replies → 3 interviews → <b>1 offer</b></div>
            </div>
          </aside>
        </section>

        <section className="stats" aria-label="What it does">
          <div className="stat"><b>01</b><h2 style={{ fontSize: 18, margin: "6px 0 4px" }}>Prepare</h2><p className="muted">Checklist per round type and likely questions matched to your own STAR stories.</p></div>
          <div className="stat"><b>02</b><h2 style={{ fontSize: 18, margin: "6px 0 4px" }}>Debrief</h2><p className="muted">Messy notes become structured: what was asked, what went well, what to fix.</p></div>
          <div className="stat"><b>03</b><h2 style={{ fontSize: 18, margin: "6px 0 4px" }}>Follow up</h2><p className="muted">A Today list tells you who to nudge. Plain rules, no guesswork.</p></div>
          <div className="stat"><b>04</b><h2 style={{ fontSize: 18, margin: "6px 0 4px" }}>Compare</h2><p className="muted">Offers in USD and quetzales side by side, with your own exchange rate.</p></div>
        </section>

        <footer className="foot">
          Open source. AI features are optional and use Groq; text you paste is sent to that provider.
        </footer>
      </main>
    </>
  );
}
