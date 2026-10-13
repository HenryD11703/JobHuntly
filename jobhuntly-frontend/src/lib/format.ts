import type { ApplicationSource, ApplicationStatus, Currency, InterviewRound } from "./types";

const DAY_MS = 86_400_000;

const STATUS_LABELS: Record<ApplicationStatus, string> = {
  Backlog: "Backlog",
  Applied: "Applied",
  Interviewing: "Interviewing",
  TechnicalTest: "Technical test",
  Offer: "Offer",
  Rejected: "Rejected",
};

const ROUND_LABELS: Record<InterviewRound, string> = {
  HrScreen: "HR screen",
  Technical: "Technical",
  Behavioral: "Behavioral",
  TakeHome: "Take-home",
  Final: "Final",
  Other: "Other",
};

const SOURCE_LABELS: Record<ApplicationSource, string> = {
  LinkedIn: "LinkedIn",
  Referral: "Referral",
  CompanySite: "Company site",
  Recruiter: "Recruiter",
  Other: "Other",
};

export const statusLabel = (status: ApplicationStatus) => STATUS_LABELS[status];
export const roundLabel = (round: InterviewRound) => ROUND_LABELS[round];
export const sourceLabel = (source: ApplicationSource | string) => SOURCE_LABELS[source as ApplicationSource] ?? source;
export const stageLabel = (stage: string) => STATUS_LABELS[stage as ApplicationStatus] ?? stage;

/** "Thu 15 Oct" in the viewer's time zone. */
export function formatDay(iso: string): string {
  return new Date(iso).toLocaleDateString("en-GB", { weekday: "short", day: "numeric", month: "short" });
}

/** "10:00", 24-hour, in the viewer's time zone. */
export function formatTime(iso: string): string {
  return new Date(iso).toLocaleTimeString("en-GB", { hour: "2-digit", minute: "2-digit", hour12: false });
}

/** "Thu 15 Oct 2026". */
export function formatFullDay(iso: string): string {
  return new Date(iso).toLocaleDateString("en-GB", { weekday: "short", day: "numeric", month: "short", year: "numeric" });
}

/** Whole days between two instants, rounded down (never negative). */
export function daysBetween(fromIso: string, to: Date = new Date()): number {
  return Math.max(0, Math.floor((to.getTime() - new Date(fromIso).getTime()) / DAY_MS));
}

/** "today", "1 day ago", "9 days ago". */
export function daysAgo(iso: string, now: Date = new Date()): string {
  const days = daysBetween(iso, now);
  if (days === 0) return "today";
  return days === 1 ? "1 day ago" : `${days} days ago`;
}

/** "in 5 hours", "in 2 days", or "now" when the moment has passed. */
export function timeUntil(iso: string, now: Date = new Date()): string {
  const ms = new Date(iso).getTime() - now.getTime();
  if (ms <= 0) return "now";
  const hours = Math.floor(ms / 3_600_000);
  if (hours < 1) return "in under an hour";
  if (hours < 48) return hours === 1 ? "in 1 hour" : `in ${hours} hours`;
  const days = Math.floor(hours / 24);
  return `in ${days} days`;
}

/** "USD 3,400" or "EUR 38,400.50": no symbol, so currencies are never confused. */
export function money(amount: number, currency: Currency): string {
  return `${currency} ${number(amount)}`;
}

/** 3400 -> "3,400", 3576.964 -> "3,576.96". */
export function number(value: number, decimals = 2): string {
  return value.toLocaleString("en-US", { minimumFractionDigits: 0, maximumFractionDigits: decimals });
}

/** Always two decimals, for the comparison table: 3400 -> "3,400.00". */
export function fixed2(value: number): string {
  return value.toLocaleString("en-US", { minimumFractionDigits: 2, maximumFractionDigits: 2 });
}

/** ISO instant to the value a <input type="datetime-local"> expects, in the viewer's time zone. */
export function toLocalInput(iso: string): string {
  const date = new Date(iso);
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}T${pad(date.getHours())}:${pad(date.getMinutes())}`;
}

/** The value of a datetime-local input (local time) to an ISO UTC instant. */
export function fromLocalInput(value: string): string {
  return new Date(value).toISOString();
}

/** One list item per non-empty line, trimmed. */
export function linesToList(text: string): string[] {
  return text.split("\n").map((line) => line.trim()).filter(Boolean);
}

export function listToLines(items: string[]): string {
  return items.join("\n");
}

/** "23 h", "45 min", or "expired". */
export function timeLeft(iso: string, now: Date = new Date()): string {
  const ms = new Date(iso).getTime() - now.getTime();
  if (ms <= 0) return "expired";
  const hours = Math.floor(ms / 3_600_000);
  if (hours >= 1) return `${hours} h`;
  return `${Math.max(1, Math.floor(ms / 60_000))} min`;
}
