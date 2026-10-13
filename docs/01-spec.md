# Spec: JobHuntly, the interview notebook

Status: **DRAFT, awaiting owner approval (gate a).**

## 1. Problem and audience

People searching for a job (the owner is the first user, searching for remote work paid in USD from Latin America) lose track of three things: what they said in each interview, when to follow up, and how offers really compare. Existing trackers are Kanban boards that store rows; none helps with preparing for or learning from interviews.

**Primary user:** a developer running a job search with 10-60 active applications.
**Secondary user:** a recruiter or engineer reviewing this repo as a portfolio piece; they should understand and try the product in under a minute.

## 2. Scope

### In scope (v1)
- Applications on a Kanban board (kept from the current app) with source tracking.
- Timeline per application (automatic events).
- Interviews with round type, date, status.
- Interview preparation (AI-assisted) and debrief (AI-assisted structuring).
- STAR story bank, linked to preparation.
- "Today" view with deterministic nudges.
- Funnel analytics.
- Offer comparison in USD / GTQ / EUR with a user-supplied exchange rate.
- Paste-a-job-post extraction (AI-assisted).
- One-click demo sandbox.
- Auth hardening, error handling, tests, CI, deployment, English README.

### Out of scope (v1), deliberately
- Cross-debrief pattern detection and AI practice mode (later).
- Email/calendar integrations, browser extension, notifications by email.
- Salary tax/net calculations (no claims about taxes).
- Teams or sharing, multi-language UI.

## 3. User stories and acceptance criteria

Each criterion has an ID and the test type that proves it (U = unit, I = integration against real Postgres, E = Playwright end to end, M = manual check recorded in the task).

### US1. Capture an application quickly
- **AC-1.1** (E) Creating an application needs only company and position; status defaults to Backlog; it appears on the board without reload.
- **AC-1.2** (I) An application has an optional `Source` (LinkedIn, Referral, CompanySite, Recruiter, Other). Invalid values return 422.
- **AC-1.3** (I) Pasting a job post and pressing "Extract" returns a *draft* (company, position, salary range, key requirements, location/remote flag). Nothing is saved until the user confirms. (AI faked in tests; see AC-AI-*.)
- **AC-1.4** (U) Draft validation rejects model output that does not match the schema (missing fields, wrong types, over-long strings) and the UI falls back to the empty manual form.

### US2. See my pipeline
- **AC-2.1** (E) Dragging or using the keyboard menu moves a card between the six existing statuses; the change persists after reload.
- **AC-2.2** (E) The board is fully operable by keyboard (move a card without a mouse).
- **AC-2.3** (E) At 375 px width, no horizontal page scroll and all actions are reachable.

### US3. Prepare for an interview
- **AC-3.1** (I) An interview has `RoundType` in {HrScreen, Technical, Behavioral, TakeHome, Final, Other}, a date, a status (Scheduled/Done/Cancelled).
- **AC-3.2** (U) Each round type has a deterministic checklist (e.g. Technical: review the stack of the job post, prepare 2 questions to ask, test your setup). Checklist items can be ticked and ticks persist.
- **AC-3.3** (I) "Generate likely questions" takes the application's job post text, the round type and the chosen STAR stories, and returns up to 8 questions, each optionally linked to one of the user's stories. Output is validated; failure leaves the checklist usable.
- **AC-3.4** (U) Generated questions are stored with the interview so they are not regenerated (and not counted against quota) on every page view.

### US4. Debrief after an interview
- **AC-4.1** (I) After an interview is marked Done the user can write free-text notes and a 1-5 self-rating.
- **AC-4.2** (I) "Structure my notes" returns a draft with: questions asked, what went well, what went poorly, follow-up actions. The user edits and confirms before saving.
- **AC-4.3** (U) Saved debriefs are stored as structured data (not just text), so analytics can use them later.
- **AC-4.4** (I) A rejection reason is requested when moving to Rejected, but optional.

### US5. Never forget to follow up
- **AC-5.1** (U) The "Today" view lists, computed by code only: (a) Applied with no activity for 7 days ("follow up"), (b) interview within 48 h with no preparation started ("prepare"), (c) interview date passed with no debrief ("write debrief"). The 7-day threshold is a constant covered by tests, including boundary cases (6 d 23 h vs 7 d).
- **AC-5.2** (E) A nudge disappears once the action is done (e.g. logging a follow-up event or saving the debrief).
- **AC-5.3** (U) Applications in Rejected or Offer never produce follow-up nudges.

### US6. Understand my funnel
- **AC-6.1** (U) Funnel shows counts and percentage at each stage reached (Applied, Interviewing, TechnicalTest, Offer) computed from timeline events, not from current status only.
- **AC-6.2** (U) Response rate by source; average days spent in each stage; rejection reasons grouped. With fewer than 5 applications in a group, the UI shows "not enough data" instead of a percentage.
- **AC-6.3** (U) Empty state (0 applications) renders without errors or NaN.

### US7. Compare offers
- **AC-7.1** (I) An offer stores: amount, currency (USD, GTQ, EUR), period (monthly/annual), and free-text notes (benefits, equity).
- **AC-7.2** (U) The comparator normalises every offer to monthly GTQ and monthly USD using the exchange rates saved in the user's settings; changing a rate updates the table instantly. The comparison itself never calls an external service, so numbers never change under the user.
- **AC-7.3** (U) Rounding rules are fixed and tested (2 decimals, half away from zero); annual to monthly divides by 12.
- **AC-7.4** (E) The UI states "gross figures as entered; no tax calculation".
- **AC-7.5** (I) A "Fetch today's rate" button asks the backend for the current USD/GTQ and EUR/GTQ rates from a public rates API (open.er-api.com) and only fills the editable fields; nothing is saved until the user confirms. The backend caches the response for 12 hours, validates it (positive number, plausible range 1 to 20 for GTQ per USD), and shows the provider's attribution and update date. If the provider fails, the manual fields keep working and the UI says so. Tests use a fake provider; the real call is verified once manually (M).

### US8. Try it without signing up
- **AC-8.1** (E) The landing page has a "Try the demo" button; one click lands on the board with at least 8 sample applications, 3 interviews (one with debrief) and 2 stories.
- **AC-8.2** (I) Each click creates an **isolated** demo user; two demo sessions never see each other's changes.
- **AC-8.3** (I) Demo users are deleted with all their data after 24 h by a background job.
- **AC-8.4** (U) Sample data is fictional (fake companies, no real people or private data).
- **AC-8.5** (I) Demo users have a lower AI quota than registered users.
- **AC-8.6** (I) Demo creation is rate-limited per IP.

### US9. Keep my data safe
- **AC-9.1** (I) For every user-data endpoint, a test authenticates as user B and tries to read/update/delete user A's resource; expected 404 (not 403, to avoid leaking existence).
- **AC-9.2** (I) Auth token is delivered in an `HttpOnly`, `Secure`, `SameSite` cookie; the frontend never reads it from JavaScript.
- **AC-9.3** (I) CORS allows only the configured frontend origin(s).
- **AC-9.4** (I) Login, register and demo endpoints are rate-limited; exceeding returns 429.
- **AC-9.5** (I) Registration enforces email format and a minimum password length of 10; duplicate email returns 409 without revealing more.
- **AC-9.6** (I) Errors use RFC 7807 problem details; no stack traces or exception messages from internals reach the client; unknown errors are 500 with a correlation id.
- **AC-9.7** (M) A scan of the repository and its new commits finds no secret values; `.env.example` contains only fake values.

### AI behaviour (cross-cutting)
- **AC-AI-1** (I) Groq key and model come only from environment variables (`Groq__ApiKey`, `Groq__Model`) read at request time; the key is never sent to the browser or logged.
- **AC-AI-2** (I) Per-user daily AI quota (registered: 30 calls/day, demo: 8). Cache hits (AC-3.4) do not count. Exceeding returns 429 with the reset time; the manual flow still works.
- **AC-AI-3** (I) If the AI client throws, times out (10 s) or returns invalid output, the endpoint returns a problem-details response with a stable error code, and the frontend shows a non-blocking message.
- **AC-AI-4** (U) Prompts treat job-post text as data: tests include a post containing "ignore previous instructions" and verify the output is still validated against the schema and nothing is executed or saved without user confirmation.
- **AC-AI-5** (M) One recorded run against the real Groq API for each of the three AI features, with the model in use named in the task log.
- **AC-AI-6** (E) The UI discloses that pasted text is sent to a third-party AI provider before the first use.

### Non-functional
- **AC-NF-1** (E) axe reports 0 serious/critical violations on login, board, application detail, today, analytics, offers (desktop and 375 px).
- **AC-NF-2** (M) Lighthouse performance >= 90 on the landing page (desktop), recorded.
- **AC-NF-3** (E) All UI strings are English.
- **AC-NF-4** (M) Deployed cold start of the backend after auto-stop measured and written in the README (target: first request < 5 s; honest number if worse).
- **AC-NF-5** (I) `dotnet test` and `npm run check` pass in CI on every push to `main` and on pull requests.
- **AC-NF-6** (M) `npm audit --omit=dev` reports 0 high/critical; `dotnet list package --vulnerable` reports none.

## 4. Open questions (need the owner)
- None blocking. Proposed defaults that the owner may change: 7-day follow-up threshold; AI quotas 30/8; demo retention 24 h; password minimum 10.
