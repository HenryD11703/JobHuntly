# Contributing

Thanks for looking. This is a portfolio project, but it is built like a real one, so changes follow a few rules.

## How work is organised

Spec-driven: `docs/00-principles.md` (rules that are not broken), `docs/01-spec.md` (acceptance criteria), `docs/02-plan.md` (decisions and rejected alternatives) and `docs/03-tasks.md` (ordered tasks). A change should say which acceptance criterion it serves, and every criterion has a test. If a criterion cannot be tested, rewrite it until it can.

## Setup

You need .NET 8 SDK (or newer), Node 22+, and a PostgreSQL database (a free [Neon](https://neon.com) project works; use a **development** branch, never production).

```bash
cp .env.example .env                                   # fill in the connection string and a JWT secret
cp jobhuntly-frontend/.env.example jobhuntly-frontend/.env.local
cd JobHuntlyBackend && dotnet run --project JobHuntly.Api        # http://localhost:5070/swagger
cd jobhuntly-frontend && npm ci && npm run dev                   # http://localhost:3000
```

The Groq key is optional: without it the AI buttons simply do not appear.

## Checks (run these before opening a pull request)

```bash
cd JobHuntlyBackend  && dotnet test                                  # unit + integration (needs the database)
cd jobhuntly-frontend && npm run check                               # lint, types, unit tests
cd jobhuntly-frontend && npm run test:e2e                            # Playwright, desktop and mobile, with axe
```

Calling real services (Groq, exchange rates) is opt-in: `RUN_REAL_API_TESTS=1 dotnet test --filter Real`.

## Rules that matter

- **Never commit secrets.** Config comes from environment variables; the repository only ships `.env.example` with empty values.
- **Every user-data endpoint needs an isolation test** (user B gets 404 for user A's data).
- **Deterministic logic stays deterministic.** Reminders, funnel numbers and offer maths are plain code with boundary tests; AI may only draft, and the user confirms.
- **No test calls the internet.** Use the fakes and the local mock.
- **UI strings, code, docs and commits are in English.**
- **Keep the design distinct.** Signal yellow means "this needs you" and nothing else; every state has a word, not only a colour; no gradients, no purple, no rounded-card template look.
- Add a dependency only with a sentence in `docs/02-plan.md` saying why.

## Commits

Small, with a message that explains the *why*. Do not rewrite shared history.
