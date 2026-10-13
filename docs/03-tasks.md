# Tasks

Status: **DRAFT, awaiting owner approval (gate a).** Order matters. Each task has a "done when" and the criteria it covers. Tick when finished; keep a one-line note for anything surprising. Gates for the owner: (a) this document set, (b) mockups at T14, (c) any push, PR, tag or deployment.

Legend: [ ] pending, [x] done.

## Phase 0. Hygiene

> Progress notes: T01-T26, T28 and T29 done (backend and frontend feature-complete). Remaining: T27 measurements (memory in 256 MB and cold start need a real Fly deployment), T30 final verification, T31 publish. The CI workflow has been written for backend, frontend and end-to-end but has never run on GitHub (no push yet). Not run: Lighthouse (AC-NF-2). on branch `feat/revamp`; T14 approved (Wayfinding direction). Verified: 164 tests pass (159 run, 5 skipped real-service tests) (unit + integration against a Neon dev branch; the real rates API was also called once by hand through `RUN_REAL_API_TESTS=1`: USD 7.6448 GTQ, EUR 8.5402 GTQ on 2026-10-13); `dotnet list package --vulnerable` clean. Not yet verified: the CI workflow has never run on GitHub (no push yet); cookie survival through a Vercel rewrite (T05 spike, needs the deployed frontend). T02 (repo CLAUDE.md) is still to be written.
- [x] **T01 Repo hygiene.** Add root `.gitignore`; remove tracked `bin/`, `obj/` (230 files); add `.env.example`; move secrets out of `appsettings.json` and `docker-compose.yml`. *Done when:* `git ls-files` has no build output and a secret scan of tracked files finds nothing. Covers AC-9.7.
- [x] **T02 Repo guide.** `CLAUDE.md` in the repo (commands, structure, rules) so a new chat can continue. *Done when:* a fresh reader can run the checks from it.

## Phase 1. Backend foundation
- [x] **T03 PostgreSQL.** Swap provider to Npgsql (accepting Neon's `postgresql://` URI form), delete old migrations, create one baseline, remove the SQL Server `docker-compose.yml`. Remove `Class1.cs`. Owner creates a free Neon account and a **development** database; the connection string goes in a git-ignored `.env`. *Done when:* app boots against the dev database and migrates.
- [x] **T04 Test harness.** xUnit project, `WebApplicationFactory`, real Postgres via env var (Neon dev DB locally, service container in CI), test helpers (register user A/B). *Done when:* one smoke test passes locally.
- [x] **T05 Auth hardening.** Cookie auth, spike through Vercel-style proxy, restricted CORS, rate limiting, password and email rules, `/health`. *Done when:* AC-9.2 to AC-9.5 pass.
- [x] **T06 Errors.** Typed exceptions plus problem-details middleware; controllers stop catching `Exception`. *Done when:* AC-9.6 passes; not-found is 404.
- [x] **T07 CI (backend).** GitHub Actions with Postgres service. *Done when:* green on a PR. Covers AC-NF-5 (backend half).

## Phase 2. Domain
- [x] **T08 Model v2 + migration** (D4). *Done when:* migration applies; mapping tests pass.
- [x] **T09 Applications API v2.** Source, job post text, timeline events written automatically. *Done when:* AC-1.1, AC-1.2 and isolation tests (AC-9.1) pass for applications.
- [x] **T10 Interviews, checklists, debriefs API.** `ChecklistCatalog`. *Done when:* AC-3.1, 3.2, 4.1, 4.3, 4.4 and isolation tests pass.
- [x] **T11 Nudges.** `NudgeCalculator` + endpoint. *Done when:* AC-5.1, 5.3 unit tests incl. boundaries pass.
- [x] **T12 Funnel analytics v2.** *Done when:* AC-6.1 to 6.3 pass.
- [x] **T13 Offers + comparator + rate suggestion + stories API.** *Done when:* AC-7.1 to 7.3 and AC-7.5 pass (fake provider); isolation tests for offers and stories; one manual run against the real rates API recorded.

## Phase 3. Design (can run in parallel with Phase 2)
- [x] **T14 Mockups, gate (b), approved by the owner (Wayfinding).** Static HTML mockups for landing, board, application detail, Today, analytics, offers at desktop and 375 px; shown to the owner. *Done when:* owner approves a direction.

## Phase 4. AI

> T18 log (2026-10-13): model `openai/gpt-oss-120b`, all four real calls validated by the production parsers. Latency: extract 1.4 s, injection attempt 0.9 s (ignored: company came back as "Acme Corp", not "HACKED"), prep 3.0 s (6 questions, 1 linked to a story), debrief 1.1 s. Default timeout stays at 10 s. Run again with `RUN_REAL_API_TESTS=1` whenever the model or a prompt changes.
- [x] **T15 AI plumbing.** `IAiClient`, `GroqAiClient`, `FakeAiClient`, quota table, timeout, error codes. *Done when:* AC-AI-1 to AC-AI-3 pass with the fake.
- [x] **T16 Extract job post.** Endpoint + validator + injection test. *Done when:* AC-1.3, 1.4, AC-AI-4 pass.
- [x] **T17 Interview prep + debrief structuring.** Endpoints, caching of generated questions. *Done when:* AC-3.3, 3.4, 4.2, 4.3 pass.
- [x] **T18 Real Groq verification.** Run each feature against the real API with the owner's key from env (never printed); confirm the model exists; record model name and a sample outcome. *Done when:* AC-AI-5 recorded. If the model is retired, change the env default.

## Phase 5. Demo
- [x] **T19 Demo sandbox.** Endpoint, fictional seed, cleanup service, lower quota, rate limit. *Done when:* AC-8.2 to 8.6 pass.

## Phase 6. Frontend (after T14 approval)
- [x] **T20 Frontend foundation.** Upgrade Next to a patched version, fix the 3 lint errors, strict TypeScript, tokens, typed API client, auth through cookie. *Done when:* lint, tsc and audit are clean (AC-NF-6 frontend half).
- [x] **T21 Landing, auth, demo button.** *Done when:* AC-8.1 and AC-NF-3 for these pages pass in E2E.
- [x] **T22 Board + quick capture + paste-a-job-post.** Includes AI disclosure. *Done when:* AC-1.1, 1.3 UI, 1.4 fallback, 2.1 to 2.3, AC-AI-6 pass.
- [x] **T23 Application detail.** Timeline, interviews, checklist, prep questions, debrief with AI structuring, rejection reason. *Done when:* AC-3.2 to 3.4, 4.1 to 4.4 pass in E2E.
- [x] **T24 Today view.** *Done when:* AC-5.2 passes in E2E.
- [x] **T25 Analytics, offers, stories screens.** *Done when:* AC-6.x, 7.x, 7.4 pass in E2E.
- [x] **T26 Mobile + accessibility pass.** *Done when:* AC-NF-1 passes everywhere; screenshots reviewed at 375 px.

## Phase 7. Ship
- [ ] **T27 Deployment files.** `fly.toml`, Dockerfile check, measure memory and cold start. *Done when:* numbers recorded (AC-NF-4); owner asked before any size above 256 MB.
- [x] **T28 CI complete.** Add frontend and E2E jobs. *Done when:* green; AC-NF-5.
- [x] **T29 Docs.** English `README.md` (what, why, screenshots, real cost, setup, deployment, limits), `CONTRIBUTING.md`, `docs/ARCHITECTURE.md` (decisions and rejected alternatives). *Done when:* a stranger can run it from the README.
- [ ] **T30 Traceability + final verification.** Table "criterion → test"; run everything; Lighthouse (AC-NF-2). *Done when:* every AC is green or explicitly listed as unverified.
- [ ] **T31 Publish, gate (c).** With the owner's OK: push branch, PR, merge, deploy (owner creates Fly/Neon/Vercel accounts and enters payment), set variables, smoke test the live URL. *Done when:* the public URL works end to end, the demo button works, and the README links to it.

## Later (not v1)
Cross-debrief pattern detection; AI practice mode; light theme; custom domain.
