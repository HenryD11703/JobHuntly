"use client";

import Link from "next/link";
import { useState, type FormEvent } from "react";
import { AddOfferDialog } from "@/components/ApplicationDialogs";
import { Empty, ErrorNotice, Notice, PageHead, useAction } from "@/components/ui";
import { api } from "@/lib/api";
import { fixed2, formatDay, money } from "@/lib/format";
import type { OfferComparison, SuggestedRates } from "@/lib/types";
import { useResource } from "@/lib/use-resource";

export default function OffersPage() {
  const comparison = useResource("offers", api.offers.compare);
  const apps = useResource("offers:apps", api.applications.list);
  const [adding, setAdding] = useState(false);
  const remove = useAction();

  async function removeOffer(id: string) {
    const done = await remove.run(async () => {
      await api.offers.remove(id);
      return true;
    });
    if (done) comparison.reload();
  }

  const offers = comparison.data?.offers ?? [];

  return (
    <div className="wrap">
      <PageHead eyebrow={`${offers.length} offer${offers.length === 1 ? "" : "s"}`} title="Compare offers">
        <button type="button" className="btn primary" onClick={() => setAdding(true)} disabled={!apps.data?.length}>+ Add offer</button>
      </PageHead>
      <ErrorNotice error={comparison.error ?? remove.error} />

      {comparison.data && <RatesPanel rates={comparison.data.rates} onSaved={comparison.reload} />}

      <section className="panel" aria-labelledby="compare-h">
        <h2 id="compare-h">Side by side <span className="mono muted">monthly, gross</span></h2>
        {comparison.data && offers.length === 0 && (
          <Empty title="No offers yet">
            When you get one, add it from the application or with the button above. Then it lines up here against the others.
          </Empty>
        )}
        {offers.length > 0 && (
          <div className="scroll">
            <table>
              <thead>
                <tr>
                  <th scope="col">Company</th>
                  <th scope="col">As offered</th>
                  <th scope="col" className="num">Per month, USD</th>
                  <th scope="col" className="num">Per month, GTQ</th>
                  <th scope="col">Notes</th>
                  <th scope="col"><span className="visually-hidden">Actions</span></th>
                </tr>
              </thead>
              <tbody>
                {offers.map((offer) => (
                  <tr key={offer.id}>
                    <td>
                      <b><Link href={`/applications/${offer.jobApplicationId}`}>{offer.companyName}</Link></b>
                      <br /><span className="muted">{offer.positionTitle}</span>
                    </td>
                    <td className="mono">{money(offer.amount, offer.currency)} / {offer.period === "Annual" ? "year" : "month"}</td>
                    <td className="num"><b>{fixed2(offer.monthlyUsd)}</b></td>
                    <td className="num"><b>{fixed2(offer.monthlyGtq)}</b></td>
                    <td>{offer.notes}</td>
                    <td>
                      <button type="button" className="link-button" onClick={() => void removeOffer(offer.id)} aria-label={`Delete the ${offer.companyName} offer`}>Delete</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        <p className="note" style={{ marginTop: 10 }}>
          Gross figures as entered; no tax calculation. Annual amounts are divided by 12 and all results are rounded to 2 decimals.
        </p>
      </section>

      <AddOfferDialog
        open={adding}
        applications={apps.data ?? []}
        onClose={() => setAdding(false)}
        onAdded={comparison.reload}
      />
    </div>
  );
}

function RatesPanel({ rates, onSaved }: { rates: OfferComparison["rates"]; onSaved: () => void }) {
  const save = useAction();
  const fetchRates = useAction();
  const [usd, setUsd] = useState(String(rates.usdToGtq));
  const [eur, setEur] = useState(String(rates.eurToGtq));
  const [suggestion, setSuggestion] = useState<SuggestedRates>();
  const [savedNow, setSavedNow] = useState(false);
  const dirty = Number(usd) !== rates.usdToGtq || Number(eur) !== rates.eurToGtq;

  async function fetchToday() {
    const suggested = await fetchRates.run(() => api.rates.suggested());
    if (!suggested) return;
    setSuggestion(suggested);
    setUsd(String(suggested.usdToGtq));
    setEur(String(suggested.eurToGtq));
    setSavedNow(false);
  }

  async function submit(event: FormEvent) {
    event.preventDefault();
    const result = await save.run(() => api.rates.save({ usdToGtq: Number(usd), eurToGtq: Number(eur) }));
    if (result) {
      setSavedNow(true);
      onSaved();
    }
  }

  return (
    <section className="panel" aria-labelledby="rates-h">
      <h2 id="rates-h">Exchange rates you trust</h2>
      <form className="form" onSubmit={submit} aria-label="Exchange rates">
        <div className="pair" style={{ maxWidth: 520 }}>
          <label>
            <span>1 USD in GTQ</span>
            <input type="number" className="mono" value={usd} onChange={(e) => { setUsd(e.target.value); setSavedNow(false); }} min="1" max="20" step="0.0001" required inputMode="decimal" />
          </label>
          <label>
            <span>1 EUR in GTQ</span>
            <input type="number" className="mono" value={eur} onChange={(e) => { setEur(e.target.value); setSavedNow(false); }} min="1" max="30" step="0.0001" required inputMode="decimal" />
          </label>
        </div>
        <div className="actions">
          <button type="button" className="btn small" onClick={fetchToday} disabled={fetchRates.busy}>{fetchRates.busy ? "Fetching…" : "Fetch today's rate"}</button>
          <button type="submit" className="btn small primary" disabled={save.busy || !dirty}>{save.busy ? "Saving…" : "Save rates"}</button>
        </div>
        <ErrorNotice error={fetchRates.error} />
        {fetchRates.error && <p className="note">You can still type the rates yourself.</p>}
        <ErrorNotice error={save.error} />
        {suggestion && (
          <p className="note">
            Suggested from <a href={suggestion.attributionUrl} target="_blank" rel="noopener noreferrer">{suggestion.provider}</a> (updated {formatDay(suggestion.providerUpdatedAt)}).
            It only fills these fields: press Save to use them.
          </p>
        )}
        {dirty && !savedNow && <Notice kind="info">The table below still uses your <b>saved</b> rates. Save to apply the new values.</Notice>}
        {savedNow && <Notice>Rates saved. The comparison now uses them.</Notice>}
        <p className="note">Comparisons always use the saved values, so the numbers never change under you.</p>
      </form>
    </section>
  );
}
