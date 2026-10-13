# Technical plan

Status: **DRAFT, awaiting owner approval (gate a).** Covers the spec in `01-spec.md`. Each decision lists the alternatives that were rejected and why.

## 1. Topology

```
Browser ──► Vercel (Next.js 16, frontend only) ──rewrite /api/*──► Fly.io (ASP.NET Core 8, Docker) ──► Neon (PostgreSQL)
                                                                         └──► Groq API (server-side only)
```

Estimated cost: Fly.io shared-cpu-1x about USD 2.19/month if it ran 24/7 (published price; less with auto-stop). Vercel and Neon on free plans. **Unverified:** whether .NET 8 runs comfortably in 256 MB; task T27 measures it. If 512 MB is needed the owner is asked before spending more.

## 2. Decisions

### D1. Keep ASP.NET Core / Clean Architecture; switch SQL Server to PostgreSQL
- Why: SQL Server has no usable free host; Neon Postgres is permanent and free. Infrastructure is isolated, so the swap is `UseNpgsql` plus regenerated migrations.
- Existing migrations are discarded and a single new baseline is created. There is no production data to preserve.
- Rejected: rewriting the backend in Next route handlers (deploys as one piece and costs nothing, but removes the C# backend that differentiates the project; owner chose to keep it). Azure free tiers (conflicting information on SQL duration, free App Service stops at its CPU quota). Render free (about 1 minute cold start; its free Postgres expires in 30 days).

### D2. Auth: HttpOnly cookie through a same-origin proxy
- The browser only talks to the Vercel origin. `next.config.ts` rewrites `/api/*` to the Fly backend, so the cookie set by the backend is first-party. No `localStorage` token, no third-party-cookie problem in Safari.
- JWT stays (already implemented) but is carried in an `HttpOnly; Secure; SameSite=Lax` cookie. CSRF: state-changing requests also require a custom header (`X-Requested-With`) and the cookie is `SameSite=Lax`; documented in ARCHITECTURE.md.
- Rejected: token in `localStorage` (XSS-readable); `SameSite=None` cross-site cookie (blocked by some browsers); full server sessions (more state for no gain here).
- **Risk to verify in T05:** that `Set-Cookie` survives the Vercel rewrite. If not, fall back to a Next route handler acting as a thin proxy.

### D3. Errors and validation
- Domain/application throw typed exceptions (`NotFoundException`, `ConflictException`, `ValidationException`) mapped by one middleware to RFC 7807 problem details (404, 409, 422, 429, 500). No more `catch (Exception) → 400`.
- Input validation with DataAnnotations plus `[ApiController]` automatic 422/400 responses. Rejected: FluentValidation (extra dependency not needed at this size).

### D4. Data model additions (EF Core, Postgres)
- `JobApplication`: add `Source` (enum), `JobPostText` (text, up to 20,000 chars), `Location`/`Remote`.
- `ApplicationEvent` (id, application, type, timestamp, payload json): `Created`, `StatusChanged`, `InterviewAdded`, `FollowedUp`, `DebriefSaved`, `OfferAdded`. Source of truth for funnel and time-in-stage.
- `Interview`: `RoundType` becomes an enum; add `Status`, `PrepQuestionsJson`, `ChecklistStateJson`.
- `Debrief` (one per interview): `Rating`, `RawNotes`, structured `QuestionsAsked`, `WentWell`, `WentPoorly`, `FollowUps` (jsonb).
- `Story` (STAR): title, situation, task, action, result, tags.
- `Offer`: application, amount, currency, period, notes.
- `AiUsage` (user, date, count) for quotas.
- User gets `IsDemo` and `CreatedAt`.
- Rejected: storing everything as free-text notes (blocks analytics, which is the point of principle 1).

### D5. Deterministic services (Application layer, pure and unit-tested)
`NudgeCalculator`, `FunnelCalculator`, `OfferComparator`, `ChecklistCatalog`. They take plain data and a clock (`TimeProvider`) so tests control time. No EF inside them.

### D6. AI integration
- `IAiClient` in Application; `GroqAiClient` in Infrastructure using `HttpClient` against Groq's OpenAI-compatible chat completions endpoint. Config: `Groq__ApiKey`, `Groq__Model` (default `openai/gpt-oss-120b`, the model verified in the sibling project Rung; models get retired, so T18 re-verifies it).
- Three use cases (`ExtractJobPost`, `GenerateInterviewPrep`, `StructureDebrief`), each: build prompt, call with JSON-only instruction and 10 s timeout, parse, validate against a C# schema with explicit limits, return a *draft*. Nothing persists without a separate confirm call.
- Output limits are trimmed rather than rejected when slightly exceeded (lists to their maximum, strings to their limit); wrong types, non-JSON and strings more than 4x the limit are rejected. One retry only when the output fails validation. Quota check happens before the call; cache hits skip it.
- Job-post text is delimited and labelled as untrusted data in the prompt. This reduces, but does not eliminate, prompt injection; the real defence is that outputs are validated and never executed or saved automatically.
- Tests use `FakeAiClient`. No test calls Groq. Real verification is the manual task T18.
- Rejected: calling Groq from the browser (exposes the key); a vector DB or agent framework (over-engineering); AI deciding nudges or numbers (violates principle 2).

### D7. Demo sandbox
- `POST /api/auth/demo`: creates a unique demo user, seeds fictional data from a fixed seed file, sets the cookie. Rate-limited per IP.
- `DemoCleanupService` (hosted background service) deletes demo users older than 24 h with cascading deletes.
- Rejected: one shared demo account (visitors would overwrite each other's data and leave junk).

### D8. Rate limiting and CORS
- ASP.NET built-in rate limiter (fixed window) per IP on auth/demo; per-user quota table for AI. CORS limited to configured origins. No Redis.

### D9. Frontend
- Stay on Next.js 16 / React 19, upgrade to a patched version (current one has a critical advisory). TypeScript strict, no `any`.
- Vanilla CSS with design tokens (CSS variables). **Deviation from the first draft:** one global stylesheet with semantic class names instead of CSS modules per component. The approved mockups were written that way, the design is a small closed system, and modules would have meant dozens of files for no benefit. The 446-line board page was split into components; typed API client in `src/lib/api.ts`; data loading in `src/lib/use-resource.ts`; session and AI allowance in `src/lib/app-context.tsx`.
- Design direction: "Wayfinding" (see `docs/mockups/wayfinding.css`): Barlow Condensed / Barlow / IBM Plex Mono, black bars, concrete background, signal yellow `#FFD400` always with black text, green only for offers and red only for rejections, and every state also has a word (never colour alone). The first direction tried ("Ledger": warm paper plus orange) was dropped because it looked too much like current generic editorial templates.
- Rejected: Tailwind or a component library (would pull toward the generic look the owner dislikes); a state library (server data plus local state is enough).
- **Design gate (b):** mockups of landing, board, application detail (timeline, prep, debrief), Today, analytics, offers are shown to the owner before UI implementation (task T14).

### D13. Exchange rates
- `IExchangeRateProvider` (Application) with an `OpenErApiRateProvider` (Infrastructure) calling `https://open.er-api.com/v6/latest/USD`. Checked on 2026-10-13: it includes GTQ and EUR, needs no key and updates daily; its terms require attribution, which the UI shows. Cached 12 h in memory; response validated before use.
- It only *suggests* values for the editable rate fields; saved rates drive every calculation (deterministic, principle 2).
- Rejected: Frankfurter (ECB data, no GTQ); a community CDN dataset (works, but no terms or SLA); fetching on every page load (numbers would drift under the user).

### D10. Testing and CI
- Backend: xUnit. Unit tests for the calculators and services; integration tests with `WebApplicationFactory` against a real Postgres. The owner's machine has no Docker, so locally the tests and dev server use a **separate development database on Neon** (never the production one; connection string in a git-ignored `.env`); in CI a Postgres service container is used. Tests create their own unique users and clean up after themselves.
- Frontend: Vitest for pure logic; Playwright E2E (desktop and 375 px) with axe, against the real backend and Postgres.
- GitHub Actions: backend job (build, test, vulnerable-package check), frontend job (lint, tsc, unit, build, audit), E2E job (compose up, Playwright).
- A traceability table "acceptance criterion → test" is generated at the end (task T30).

### D11. Config and secrets
- All config from env. `.env.example` (fake values) at root and in the frontend. `docker-compose.yml` reads from `.env`. `.gitignore` covers `bin/`, `obj/`, `.env`, `.next/`, `node_modules/`.
- The old JWT secret and SQL password in history are treated as burned: never reused. History is not rewritten (the repo is the owner's and the values were development-only); a note in the README states they were rotated.

### D12. Deployment
- Backend: existing Dockerfile (multi-stage, .NET 8), `fly.toml` with auto-stop/auto-start, health endpoint `/health`, secrets via `fly secrets`. Migrations applied at startup (acceptable for a single instance; documented trade-off).
- Frontend: Vercel with `NEXT_PUBLIC_*` free; the backend URL lives in a server-side env var used by the rewrite.
- The owner performs account creation, card entry and deployment; the repo provides the exact list of variables and commands.

## 3. Risks

| Risk | Mitigation |
|---|---|
| Cookie lost through Vercel rewrite | Spike in T05; fallback route-handler proxy |
| .NET does not fit 256 MB | Measure in T27; ask owner before 512 MB |
| Fly cold start too slow for first impression | Measure; README states it; landing shows a "waking the server" state |
| Groq model retired or limits change | Model in env; T18 verifies; AI optional by design |
| Prompt injection via job post | Validate output, never auto-save, never execute, test AC-AI-4 |
| Scope creep | Out-of-scope list in the spec; new ideas go to a "later" list |
