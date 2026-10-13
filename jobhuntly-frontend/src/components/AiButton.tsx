"use client";

import { useState, type ReactNode } from "react";
import { useApp } from "@/lib/app-context";
import { Dialog } from "./Dialog";

const CONSENT_KEY = "jobhuntly:ai-consent";

function hasConsent(): boolean {
  try {
    return window.localStorage.getItem(CONSENT_KEY) === "yes";
  } catch {
    return false; // storage can be blocked: ask every time rather than assume
  }
}

function rememberConsent() {
  try {
    window.localStorage.setItem(CONSENT_KEY, "yes");
  } catch {
    /* storage blocked: the dialog will simply ask again next time */
  }
}

interface AiButtonProps {
  children: ReactNode;
  onRun: () => void;
  busy?: boolean;
  disabled?: boolean;
  /** Visual weight: small by default so AI never looks like the main action. */
  className?: string;
}

/**
 * Every AI action goes through this button. It hides itself when the server has no AI key, disables
 * itself when today's allowance is used up, and asks once for consent because the text is sent to a
 * third-party provider (Groq).
 */
export function AiButton({ children, onRun, busy, disabled, className = "btn small" }: AiButtonProps) {
  const { ai } = useApp();
  const [asking, setAsking] = useState(false);

  if (!ai || !ai.enabled) return null;
  const exhausted = ai.usedToday >= ai.dailyLimit;

  function click() {
    if (hasConsent()) onRun();
    else setAsking(true);
  }

  return (
    <>
      <button
        type="button"
        className={className}
        onClick={click}
        disabled={disabled || busy || exhausted}
        title={exhausted ? "Today's AI allowance is used up. It resets at midnight UTC." : undefined}
      >
        {busy ? "Working…" : children} <span className="tag ai" aria-hidden="true">AI</span>
      </button>
      <Dialog open={asking} title="Before you use AI" onClose={() => setAsking(false)}>
        <p>
          This sends the text you wrote or pasted to <b>Groq</b>, a third-party AI provider, to produce a draft. Nothing is
          saved until you review it. Leave out anything you would not want a third party to read.
        </p>
        <p className="note" style={{ marginTop: 10 }}>
          You can always do this step by hand; the rest of the app never needs AI.
        </p>
        <div className="dialog-actions">
          <button type="button" className="btn" onClick={() => setAsking(false)}>Cancel</button>
          <button
            type="button"
            className="btn primary"
            onClick={() => {
              rememberConsent();
              setAsking(false);
              onRun();
            }}
          >
            I understand, continue
          </button>
        </div>
      </Dialog>
    </>
  );
}

/** "AI: 22 of 30 left today", or nothing when AI is unavailable. */
export function AiAllowance() {
  const { ai } = useApp();
  if (!ai || !ai.enabled) return null;
  const left = Math.max(0, ai.dailyLimit - ai.usedToday);
  return <p className="note">AI drafts left today: {left} of {ai.dailyLimit}</p>;
}
