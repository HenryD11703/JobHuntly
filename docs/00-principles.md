# Principles

Rules that do not get broken. If a task conflicts with one of these, the task changes, not the rule.

1. **The interview is the unit, not the application.** JobHuntly is an interview notebook. A Kanban board alone is a commodity; preparing, debriefing and learning across interviews is the product.
2. **Deterministic core, AI as assistant.** Anything that decides, counts or warns (nudges, funnel numbers, offer comparison, quotas) is plain code with tests. AI only drafts: it extracts, suggests and structures, and the user confirms before anything is saved.
3. **The app works with AI switched off.** If Groq is down, rate-limited or the key is missing, every feature still works manually. AI features show a clear, non-blocking message.
4. **Data isolation is non-negotiable.** Every query is scoped by the authenticated user. Every endpoint that returns user data has a test proving another user cannot read or change it.
5. **Secrets never live in the repo.** Not in `appsettings`, not in `docker-compose.yml`, not in history going forward. Config comes from environment variables; the repo ships only `.env.example` with fake values.
6. **Free-tier friendly, honestly priced.** Target cost: about USD 2/month (backend on Fly.io); everything else on free plans. The README states the real cost and the limits.
7. **A visitor must be able to try it in under 10 seconds.** No signup wall: the demo sandbox is one click.
8. **Every acceptance criterion becomes a test.** If a criterion cannot be tested, it is rewritten until it can.
9. **Real services, not only mocks.** Postgres is real in integration tests. The Groq integration is verified once against the real API before it is called done.
10. **English everywhere in the repo** (UI, code, docs, commits). Spanish stays in the chat with the owner.
11. **Distinct design, not generic.** Swiss/brutalist identity kept and sharpened: strong grid, real typography, no gradients, no purple, no rounded-card template look. Mockups are approved before UI work.
12. **Small and understandable beats clever.** No dependency is added without a sentence in the plan explaining why.
