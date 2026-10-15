# Deployment

Three pieces, each on a free or near-free plan. Estimated cost: **about USD 2 a month**, only for the Fly.io machine (see "Cost"). First deployed on 2026-10-14 (Fly app `job-huntly`, region `iad`; frontend on Vercel). Numbers below were measured on that deployment.

| Piece | Where | Needs |
|---|---|---|
| Database | [Neon](https://neon.com) PostgreSQL, **production** branch | A free account, no card |
| Backend (ASP.NET Core 8) | [Fly.io](https://fly.io), built from `JobHuntlyBackend/Dockerfile` | An account and a payment card |
| Frontend (Next.js) | [Vercel](https://vercel.com) | A free account, GitHub repo connected |

Never reuse your development database for production: tests create and delete users in it.

## 1. Database (Neon)

1. In your Neon project use the **production** branch (keep a separate `dev` branch for development).
2. Copy its connection string (the `postgresql://...` form is fine, the backend converts it). Use the **pooled** string (host contains `-pooler`): it worked for both the app and the migrations in development.
3. The backend creates the tables itself on its first start.

## 2. Backend (Fly.io)

From `JobHuntlyBackend/`:

```bash
fly auth login
fly launch --no-deploy --copy-config --name <your-unique-app-name>   # edit `app` in fly.toml if asked
fly secrets set \
  ConnectionStrings__DefaultConnection="<neon production connection string>" \
  JwtSettings__Secret="$(node -e "console.log(require('crypto').randomBytes(48).toString('base64url'))")" \
  Groq__ApiKey="<your Groq key, optional>"
fly deploy
```

- `fly deploy` builds the image on Fly's servers, so you do not need Docker installed.
- The JWT secret above is **new and random**. Never reuse the development one, and never commit it.
- Without `Groq__ApiKey` the AI buttons simply do not appear.
- Check it: `https://<app>.fly.dev/health` must answer `{"status":"ok"}`.

### Cost (honest numbers)

- A `shared-cpu-1x` machine with 256 MB costs about **USD 2.19 a month if it ran 24/7** (Fly's published price). With `auto_stop_machines` it stops when idle and is billed only while running, so it should cost less. New Fly accounts have no free tier (only a short trial).
- *Measured 2026-10-14:* .NET 8 fits in 256 MB. After a cold start, a demo sandbox creation and a few requests, the API process peaked at about 155 MB resident (`VmHWM` of the dotnet process, read over `fly ssh console`). It is a single short session, not a load test, so keep an eye on it if traffic grows.
- `fly launch` creates **two** machines by default (high availability). Run `fly scale count 1`: a second machine doubles the cost and a portfolio demo does not need it.
- *Measured 2026-10-14:* cold start, with the machine stopped, through the Vercel domain, `POST /api/auth/demo` (creates a sandbox, so it touches Neon): **6.6, 6.8 and 7.4 s** in three runs, against **0.7 s** warm. The Neon branch was probably awake in those runs, so a first visit after a long quiet period can be slower.

## 3. Frontend (Vercel)

1. Import the GitHub repository in Vercel and set the **root directory** to `jobhuntly-frontend`.
2. Add one environment variable, for **Production** (and Preview if you use it): `BACKEND_URL` = `https://<app>.fly.dev` (no trailing slash).
3. Deploy. The variable is read **at build time** (Next resolves rewrites during the build), so changing it needs a redeploy.

The browser only ever talks to the Vercel domain. `/api/*` is forwarded to Fly on the server side, which keeps the auth cookie first-party. Verified on the deployed Vercel site on 2026-10-14: the demo button signs you in and the board loads, so the `Set-Cookie` survives the rewrite. API writes need the `X-Requested-With: jobhuntly` header (CSRF guard), so a bare `curl -X POST` gets a 403.

## Environment variables, in one place

| Variable | Where | Required | Notes |
|---|---|---|---|
| `ConnectionStrings__DefaultConnection` | Fly secret | yes | Neon **production** string |
| `JwtSettings__Secret` | Fly secret | yes | 32+ random characters, unique to production |
| `Groq__ApiKey` | Fly secret | no | Enables the AI drafts |
| `Groq__Model` | `fly.toml` | no | Defaults to `openai/gpt-oss-120b`; models get retired, change it here if Groq says so |
| `Ai__DailyLimit`, `Ai__DemoDailyLimit` | Fly env | no | 30 and 8 by default |
| `BACKEND_URL` | Vercel | yes | The Fly URL, set at build time |

## After deploying

1. Open the Vercel URL and press **Try the demo**: you should land on a board with 14 sample applications.
2. Open **Today** and **Offers**; press **Fetch today's rate**.
3. If the AI is enabled, paste a job post in **New application** and check the draft.
4. Write the live URL in the README and record the cold-start time here.

## Rolling back

`fly releases` lists releases and `fly deploy --image <previous image>` redeploys one. The database only ever moves forward (migrations), so keep migrations backwards compatible for one release.
