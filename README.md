# AskFix — ask. answer. fix.

An internal Q&A web app for your intranet: employees post problems with tools, setups and projects; colleagues answer; the answers that actually work get upvoted and can be marked **"This worked"** by the asker.

| | |
|---|---|
| **Backend** | ASP.NET Core 8 Web API + EF Core + SQLite (single file: `askfix.db`) |
| **Frontend** | React 18 + Vite + Tailwind CSS + TipTap rich editor (served by the API — one deployable) |
| **Auth** | Login page validated against **Active Directory** with the user's own credentials (no service account); 8-hour cookie session |
| **Search** | SQLite FTS5 full-text search (questions + answers) |
| **Deploy** | Self-contained publish → copy one folder → register as Windows service. **Zero runtimes to install on the server.** |

## Features

- **Feed** with Latest / Trending / Unanswered tabs, infinite scroll
- **Ask** with live *"similar questions — maybe already answered"* duplicate check, rich-text body, up to 5 tags with autocomplete
- **Answers** with rich editor: formatting, links, **code blocks with syntax highlighting + copy button**, inline images (paste/drop/upload)
- **Upvote / downvote** with optimistic UI; reputation (+10 per upvote, +25 when marked as worked, +2 per answer) and badges (Newcomer → Contributor → Helper → Problem Solver → Expert)
- **"This worked"** — asker marks the fix that worked; pinned and badged
- **Comments** on answers
- **Tags** (topics) with tag pages, colors, question counts
- **Follow** questions · **Bookmarks** · **Notifications** (answers, upvotes, comments, follows, accepted)
- **Profiles** synced from AD (name, e-mail, department) with stats and activity tabs
- **Search** across questions, answers and tags with result tabs
- Right rail: trending this month, popular tags, related questions, site stats
- **Dark mode** — follows your PC setting by default; switch anytime with the moon/sun button (navbar / login page) or Settings → Appearance (System / Light / Dark)
- Login rate-limiting, HTML sanitization (DOMPurify + server-side), image type/size validation

## Admin panel (`/admin`)

Visible only to admins (Shield icon in the left nav / user menu). Five tabs:

- **Overview** — site stats, top contributors, recent activity, questions still waiting for an answer
- **Users** — search, promote/revoke admins (a role change applies when the user signs in next — their session cookie keeps the old role until then)
- **Tags** — edit name/color/description, **merge duplicate tags** into one, delete unused tags
- **Content** — search and delete any question or answer (moderation)
- **Email** — SMTP settings for e-mail notifications (below)

Promoting the first admin on a fresh install (before anyone with admin can log in):
either run the app once in `Auth:Mode = "Dev"`, or set the flag directly in the database:

```powershell
sqlite3 .\askfix.db "UPDATE Users SET IsAdmin = 1 WHERE SamAccountName = 'corp\jdoe';"
```

## E-mail notifications

Configure SMTP in **Admin → Email** (host, port, SSL, credentials, from address, site URL for links) and flip the
Enabled toggle. The SMTP password is stored **DPAPI-encrypted** (decryptable only on that server) and never sent back
to the browser. "Send test email" delivers a branded test to your own address.

Users receive email (through a background queue with one retry) when:

- someone answers their question (or one they follow)
- someone comments on their answer
- their answer is marked **"This worked"**

Upvotes and follows stay in-app only (too noisy). Every user controls these three events on their **Settings** page —
toggles save immediately. Emails link back to the question and include a "Manage notifications" footer.

No external mail library is used (in-box `SmtpClient`), so the self-contained deployment is unchanged.

## Browser (desktop) notifications

Each user can enable desktop popups on the **Settings** page — they appear while an AskFix tab is open when new
notifications arrive (the bell's polling picks them up). Note: browsers only allow the Notification API on secure
origins (**HTTPS or localhost**); on plain-HTTP intranet origins the Settings page detects this and disables the toggle
with an explanation. True closed-browser push (Web Push/VAPID) is not included in this version.

## Repository layout

```
askfix/
├── src/AskFix.Api/         # ASP.NET Core 8 API (controllers, EF Core, auth, FTS5, seed)
├── src/AskFix.Api.Tests/   # xUnit integration tests (WebApplicationFactory + temp SQLite)
├── client/                 # React SPA (pages, components, editor, design system)
├── scripts/                # build-all.ps1 · install-service.ps1 · run-dev.ps1
└── shots/                  # screenshots from the E2E walkthrough
```

## Development

Prereqs on the dev machine: .NET 8 SDK, Node.js 18+.

```powershell
# 1. install client deps (once)
cd client; npm install; cd ..

# 2. run API (Dev auth mode + seeded demo data) and Vite dev server
.\scripts\run-dev.ps1
#   API:   http://localhost:8080   (Dev mode)
#   UI:    http://localhost:5173   (proxies /api to 8080)
```

**Dev mode login** (`Auth:Mode = Dev` in `appsettings.Development.json`): demo accounts are shown as chips on the login page, e.g. `corp\mahesh` with password `AskFix!123`.

Run the tests:

```powershell
dotnet test
```

## Production deployment (intranet Windows server)

### 1. Build the deployable folder (dev machine)

```powershell
.\scripts\build-all.ps1 -Output .\artifacts\askfix
```

This produces a **self-contained** folder (~120 MB): .NET runtime, app, and the built SPA. Nothing else is needed on the server.

### 2. Configure AD

Edit `appsettings.json` in the deployed folder:

```json
"Auth": {
  "Mode": "Ldap",
  "DefaultDomain": "CORP",
  "LdapServer": ""
}
```

- `DefaultDomain` — prepended when users sign in without a domain (`jdoe` → `CORP\jdoe`). Users can always type `CORP\jdoe` or `jdoe@corp.example.com`.
- `LdapServer` — optional specific domain controller; leave empty to auto-locate.
- Credentials are validated by binding to AD **with the user's own username/password** — no service account, passwords never stored or logged. Display name, e-mail and department are read from AD into the profile.

Port/URLs: set in `appsettings.json` (`"Urls": "http://0.0.0.0:8080"`) or via the service install script.

### 3. Install as a Windows service (server, as Administrator)

```powershell
.\install-service.ps1 -Port 8080
```

Creates an auto-start service `AskFix` with restart-on-failure. Users browse to `http://<server>:8080`.

### Data & backup

- All content lives in **one SQLite file**: `<deploy folder>\askfix.db` (+ `-wal`/`-shm`). Back it up by copying (stop the service or copy all three files).
- Uploaded images live in `wwwroot\uploads\` — include it in backups.
- The database is created and seeded with sample content on first start. To start empty: stop the service, delete `askfix.db*`, start again (then the first real login creates real users; sample demo users only exist in Dev mode).

### Making someone an admin

Admins can edit/delete anyone's content. Set it directly in the DB once:

```powershell
sqlite3 .\askfix.db "UPDATE Users SET IsAdmin = 1 WHERE SamAccountName = 'corp\jdoe';"
```

(or use any SQLite browser), then restart the service.

### HTTPS

Plain HTTP is fine on an isolated intranet (cookies use `SameAsRequest`). For HTTPS, front the service with IIS as a reverse proxy, or bind a certificate in `Urls: https://...` and set `Cookie.Secure` accordingly.

## Security notes

- Login attempts rate-limited to 5/minute per IP.
- All answer/question HTML is sanitized on the client (DOMPurify) **and** server-side (script/event-handler/javascript:-URL stripping).
- Image uploads: type sniffing + extension whitelist + 2 MB limit; stored with random names.
- Session cookies: HttpOnly, SameSite=Lax, 8h sliding expiry.
- The API is read-only without a session for feed/questions/search; all writes require an authenticated session.
