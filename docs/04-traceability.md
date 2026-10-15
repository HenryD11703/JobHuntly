# Traceability: acceptance criterion to test

Every criterion in `01-spec.md` and where it is proven. **Unit** and **Integration** are xUnit tests in `JobHuntlyBackend/JobHuntly.Tests` (integration runs against a real PostgreSQL); **E2E** are Playwright specs in `jobhuntly-frontend/e2e` (desktop and a 375 px phone viewport, real backend, local mocks for Groq and exchange rates); **Manual** means a person ran a check and the result is written here.

Status is honest: **Verified** (a test or recorded check passed), **Partial** (part is proven, the gap is stated) or **Not verified** (cannot be done yet, and why).

| Criterion | Status | Proven by |
|---|---|---|
| **AC-1.1** create with company and position, defaults to Backlog, appears without reload | Verified | Integration `Creating_an_application_needs_only_company_and_position`; E2E board "a new application can be created by hand" |
| **AC-1.2** optional source, invalid value is 422 | Verified | Integration `Source_is_optional_and_invalid_values_return_422` |
| **AC-1.3** pasted post returns a draft, nothing saved | Verified | Integration `Extract_returns_a_draft_and_saves_nothing`; E2E "a pasted job post fills a draft…" |
| **AC-1.4** invalid model output rejected, UI falls back to the manual form | Partial | Unit `AiOutputParserTests` (types, JSON, limits); Integration `A_bad_first_answer_is_retried_once`, `Two_bad_answers_give_a_stable_error_code…`. The UI fallback (error shown, form untouched) has no E2E test |
| **AC-2.1** move a card between stages, persists | Verified | E2E "a card moves to another stage…", "dragged to another column" (desktop) |
| **AC-2.2** board operable by keyboard | Verified | E2E "a card can be moved with the keyboard alone" |
| **AC-2.3** no horizontal scroll at 375 px | Verified | E2E `expectNoHorizontalScroll` on every screen, mobile project |
| **AC-3.1** round type, date, status | Verified | Integration `Interviews_have_a_round_a_status_and_a_checklist_that_persists` |
| **AC-3.2** deterministic checklist per round, ticks persist | Verified | Unit `ChecklistCatalogTests`; Integration (checklist); E2E "ticking the checklist persists" |
| **AC-3.3** likely questions from post, round and stories, validated | Verified | Integration `Prep_sends_the_job_post_the_round_and_the_users_stories…`; E2E "AI can regenerate…" |
| **AC-3.4** generated questions are stored, not regenerated or counted | Verified | Integration `Prep_questions_are_saved_with_the_interview…`; E2E reload check |
| **AC-4.1** notes and 1 to 5 rating for a past or done interview | Verified | Integration `A_debrief_needs_a_past_interview_marks_it_done…`, `Debrief_validation_…`; E2E detail |
| **AC-4.2** AI structures notes into a draft the user confirms | Verified | Integration `Structuring_notes_returns_a_draft_and_saves_no_debrief`; E2E "a missing debrief is written with AI help…" |
| **AC-4.3** debriefs stored as structured data | Verified | Integration (structured lists round-trip) |
| **AC-4.4** rejection reason asked, optional | Verified | Integration `Status_changes_are_recorded_in_the_timeline…`; E2E "moving to Rejected asks for an optional reason" |
| **AC-5.1** nudges by code only, with boundaries (6 d 23 h vs 7 d) | Verified | Unit `NudgeCalculatorTests` (exact 7 days, one second before, 48 h window, ordering); Integration lifecycle tests |
| **AC-5.2** a nudge disappears when the action is done | Verified | Integration `Follow_up_nudge_appears_at_seven_days…`, `Prepare_then_debrief_nudges_…`; E2E Today |
| **AC-5.3** Rejected and Offer never get follow-up nudges | Verified | Unit `Only_applications_in_Applied_get_follow_up_nudges` |
| **AC-6.1** funnel from timeline events | Verified | Unit `InsightsCalculatorTests`; Integration `Insights_count_stages_from_the_timeline` |
| **AC-6.2** reply rate by source, days in stage, reasons, "not enough data" under 5 | Verified | Unit `Reply_rate_by_source_hides_percentages…`; E2E Insights |
| **AC-6.3** empty state without NaN | Verified | Unit `Empty_state_has_no_nan…`; Integration; E2E "an empty notebook says so" |
| **AC-7.1** offer amount, currency, period, notes | Verified | Integration `Offers_are_normalised_with_the_saved_rates…` |
| **AC-7.2** normalised with saved rates, no external call in the comparison | Verified | Unit `OfferComparatorTests`; Integration (rates change the result) |
| **AC-7.3** fixed rounding, annual to monthly /12 | Verified | Unit `OfferComparatorTests` (half away from zero, one rounding at the end) |
| **AC-7.4** "gross, no tax calculation" stated | Verified | E2E Offers |
| **AC-7.5** rate suggestion only fills the fields, cached, validated, attributed | Verified | Unit `OpenErApiRateProviderTests`; Integration `Suggested_rates_never_change_the_saved_rates`, `When_the_rate_provider_fails…`; E2E "today's rate only fills the fields…"; real call recorded (USD 7.6448 GTQ, EUR 8.5402 GTQ, 2026-10-13) |
| **AC-8.1** one-click sandbox with sample data | Verified | Integration `One_click_gives_a_signed_in_sandbox…`; Unit `DemoSeedTests`; E2E |
| **AC-8.2** sandboxes are isolated | Verified | Integration `Two_demo_sessions_never_see_each_others_changes` |
| **AC-8.3** deleted after 24 h with all data | Verified | Integration `Sandboxes_are_deleted_with_all_their_data_after_24_hours…` (the hourly background trigger itself is plain scheduling code, not timed in a test) |
| **AC-8.4** sample data is fictional | Verified | Unit `Sample_data_is_fictional` |
| **AC-8.5** demo has the lower AI allowance | Verified | Integration `Demo_users_get_the_lower_ai_allowance` |
| **AC-8.6** demo creation rate-limited per IP | Verified | Integration `Creating_sandboxes_is_rate_limited_per_ip` |
| **AC-9.1** another user gets 404 for every user-data endpoint | Verified | Integration `IsolationTests`, `User_B_cannot_touch_user_A_…`, `User_B_cannot_use_the_ai_…` |
| **AC-9.2** token in an HttpOnly, Secure, SameSite cookie, never in JS | Verified | Integration `AuthTests` (cookie flags, Secure outside Development, no token in the body); E2E (sign-in works through the proxy) |
| **AC-9.3** CORS only for configured origins | Verified | Integration `CorsTests` |
| **AC-9.4** auth and demo endpoints rate-limited | Verified | Integration `RateLimitTests`, `DemoTests` |
| **AC-9.5** email format, 10-character minimum, duplicate is 409 | Verified | Integration `AuthTests`; E2E (browser blocks a short password) |
| **AC-9.6** problem details, no internals, trace id | Verified | Integration `Errors_are_problem_details_without_internals` |
| **AC-9.7** no secrets in the repository | Verified (Manual) | Scan of tracked files on 2026-10-13: only placeholders (`.env.example`, a fake URI in a unit test). `.env` is git-ignored. The old development secrets exist only in git history and were rotated, never reused |
| **AC-AI-1** key and model from environment, read per call, never sent to the browser or logged | Partial | The client reads configuration on every call and the E2E mock rejects any call without the server-side key, so the key provably travels only backend to provider. "Never logged" is by code review (prompts and answers are not logged), not by a test |
| **AC-AI-2** daily allowance, cache hits free, atomic | Verified | Integration `The_daily_allowance_is_enforced…`, `Parallel_requests_cannot_slip_past_the_limit` |
| **AC-AI-3** failure, timeout or invalid output gives a stable code, manual flow works | Verified | Integration `When_the_provider_fails…`, `Without_a_key_ai_is_reported_as_disabled…`, `When_prep_fails_the_checklist_still_works` |
| **AC-AI-4** pasted text stays data | Verified | Unit `PromptSafetyTests`; Integration `Hostile_job_post_text_stays_data…`; real model run ignored an injection ("Acme Corp", not "HACKED") |
| **AC-AI-5** each AI feature run once against the real Groq | Verified (Manual) | 2026-10-13, `openai/gpt-oss-120b`: extract 1.4 s, injection attempt 0.9 s, prep 3.0 s, debrief 1.1 s (see `03-tasks.md`) |
| **AC-AI-6** third-party disclosure before first AI use | Verified | E2E "a pasted job post fills a draft after a one-time AI notice…", "the AI notice is only shown once"; landing footer |
| **AC-NF-1** axe: no serious or critical violations, desktop and 375 px | Verified | E2E on landing, sign-in, board, detail, Today, Insights, Offers, Stories |
| **AC-NF-2** Lighthouse performance 90+ on the landing page | Verified (Manual) | 2026-10-13, desktop preset, production build served locally: performance 100, accessibility 100, best practices 100, SEO 100 (FCP 0.3 s, LCP 0.8 s, CLS 0, TBT 0 ms). Not yet measured on the deployed URL (https://job-huntly-smoky.vercel.app/): a local run says nothing about network latency. The keyless PageSpeed Insights API had its daily quota exhausted on 2026-10-14 and this WSL lacks the system libraries for Chromium; run Lighthouse from Chrome DevTools or pagespeed.web.dev and record the numbers here |
| **AC-NF-3** all UI strings in English | Partial | By construction and review; no automated check |
| **AC-NF-4** cold start of the deployed backend measured | Verified (Manual) | 2026-10-14, Fly `shared-cpu-1x` 256 MB, region iad, through Vercel, `POST /api/auth/demo`: cold 6.6 / 6.8 / 7.4 s, warm 0.7 s. Memory peak about 155 MB. Details in `docs/DEPLOYMENT.md` |
| **AC-NF-5** tests pass in CI | Not verified | Workflows written for backend, frontend and end to end; they have never run on GitHub because nothing was pushed |
| **AC-NF-6** no high or critical vulnerable dependencies | Verified | `npm audit --omit=dev` 0 vulnerabilities; `dotnet list package --vulnerable` none (2026-10-13) |
