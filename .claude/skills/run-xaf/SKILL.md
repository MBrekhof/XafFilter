---
name: run-xaf
description: Start or stop the XafFilter.Blazor.Server app cleanly. Checks the port is free before launching, runs dotnet from the host project, and verifies with an HTTP health probe to /LoginPage. Use when the user asks to run, launch, start, restart, or stop the app, or when you need a live app to verify a change.
---

# Run the XafFilter Blazor Server app

You are responsible for the full lifecycle: port check → start → health probe → stop when done. Do not leave orphaned processes.

## Default port

The XAF Blazor template uses **port 44318** for HTTPS by default (check `XafFilter/XafFilter.Blazor.Server/Properties/launchSettings.json` if it exists — otherwise default applies).

## Starting

1. **Port check first.** Run `netstat -ano | grep :44318`. If something is listening:
   - If it's a stale `dotnet` from a previous Claude session, kill it with `taskkill //PID <pid> //F //T` and continue.
   - If you can't identify it, stop and ask the user before killing.

2. **Start the host** from the repo root in the background:
   ```
   dotnet run --project XafFilter/XafFilter.Blazor.Server
   ```
   Use Bash's `run_in_background: true`. Capture the BashOutput shell id so you can stop it later.

3. **Health probe** — poll until ready (give it up to ~30s, XAF cold start is slow):
   ```
   curl -s -o /dev/null -w "%{http_code}" --max-time 5 -k https://localhost:44318/LoginPage
   ```
   A 200 means it's up. `netstat` showing the port bound is NOT sufficient — XAF can bind the port before it's serving requests.

4. If startup fails (process exits, port never opens, or 500 from `/LoginPage`), read the dotnet output yourself — do not ask the user to paste it. Common causes: LocalDB not running, database upgrade needed, missing AdditionalExportedTypes registration.

## Stopping

1. Kill the dotnet process you started: `taskkill //PID <pid> //F //T`.
2. Verify with `netstat -ano | grep :44318` — port must be free.
3. Confirm to the user with one line: "Stopped XafFilter.Blazor.Server (PID was X)."

## Common pitfalls

- LocalDB instance asleep — `sqllocaldb start mssqllocaldb` wakes it.
- Database needs upgrade — XAF will throw `DatabaseVersionMismatch`. The template's default behavior runs the Updater; if disabled, run with the Updater enabled or update via the XAF dialog.
- HTTPS dev cert missing — `dotnet dev-certs https --trust` once per machine.
