# Deployment

Three pieces, each on a free or near-free plan. Estimated cost: **about USD 2 a month**, only for the Fly.io machine (see "Cost"). Everything below was written before the first deploy: items marked *not yet verified* are confirmed on the first real deployment and then edited here.

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
- *Not yet verified:* that .NET 8 runs comfortably in 256 MB. If the first deploy shows memory errors, change `memory` in `fly.toml` to `512mb` (it costs more; check Fly's price list first).
- *Not yet verified:* the cold start after the machine has stopped. The first request after a quiet period is slower while the machine wakes and Neon resumes. Measure it and write the number here.

## 3. Frontend (Vercel)

1. Import the GitHub repository in Vercel and set the **root directory** to `jobhuntly-frontend`.
2. Add one environment variable, for **Production** (and Preview if you use it): `BACKEND_URL` = `https://<app>.fly.dev` (no trailing slash).
3. Deploy. The variable is read **at build time** (Next resolves rewrites during the build), so changing it needs a redeploy.

The browser only ever talks to the Vercel domain. `/api/*` is forwarded to Fly on the server side, which keeps the auth cookie first-party. *Not yet verified on Vercel:* that `Set-Cookie` survives the rewrite there (it does with `next start` locally and in the end-to-end tests).

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
