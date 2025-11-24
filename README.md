# QuickCode.Cli

QuickCode.Cli is a .NET 10 console application that mirrors the QuickCode web UI: create projects, manage modules, trigger generation and stream SignalR updates directly from the terminal.

- Default README language: **English**  
- Turkish version: [`README.tr.md`](README.tr.md)

---

## Prerequisites
- .NET SDK 10.0.100 or newer (`dotnet --version`)
- Internet access to `https://api.quickcode.net/`
- Each project’s email + secret must be known or retrievable via `project forgot-secret`

---

## Quick Start
You can run the CLI either from the repository root (recommended) or from inside the project directory. The command examples below cover the full workflow including config, project creation, secret management, verification, generation and watching.

### Option A – Run from repository root (recommended)
```bash
cd /Users/uzeyirapaydin/Documents/Projects/quickcode-generator

# 1. CLI help
dotnet run --project QuickCode.Cli -- --help

# 2. Store project credentials
dotnet run --project QuickCode.Cli -- config --project demo --set email=demo@quickcode.net
dotnet run --project QuickCode.Cli -- config --project demo --set secret_code=SECRET123

# 3. Project operations
dotnet run --project QuickCode.Cli -- project create --name demo --email demo@quickcode.net
dotnet run --project QuickCode.Cli -- project check --name demo
dotnet run --project QuickCode.Cli -- project forgot-secret --name demo --email demo@quickcode.net
dotnet run --project QuickCode.Cli -- project verify-secret --name demo --email demo@quickcode.net --secret-code SECRET123

# 4. Module listing / editing (examples)
dotnet run --project QuickCode.Cli -- module list --project demo
dotnet run --project QuickCode.Cli -- module available

# 5. Generate and watch
dotnet run --project QuickCode.Cli -- generate demo --watch
```

### Option B – Run from inside `QuickCode.Cli` folder
> Useful if you prefer shorter `dotnet run -- ...` commands.

```bash
cd /Users/uzeyirapaydin/Documents/Projects/quickcode-generator/QuickCode.Cli

# 1. CLI help
dotnet run -- --help

# 2. Store project credentials
dotnet run -- config --project demo --set email=demo@quickcode.net
dotnet run -- config --project demo --set secret_code=SECRET123

# 3. Project operations
dotnet run -- project create --name demo --email demo@quickcode.net
dotnet run -- project check --name demo
dotnet run -- project forgot-secret --name demo --email demo@quickcode.net
dotnet run -- project verify-secret --name demo --email demo@quickcode.net --secret-code SECRET123

# 4. Module & generation
dotnet run -- module list --project demo
dotnet run -- generate demo --watch
```

---

## Configuration Rules
- API endpoint defaults to `https://api.quickcode.net/`. Change it via `config --set api_url=...` only if you target a different backend.
- Every project must store its own `email` and `secret_code` via `config --project <name>`.
- Commands require explicit project name; there are no default_* fallbacks.
- Config file is stored at `~/.quickcode/config.json`.

---

## Command Reference

| Command | Description | Example |
|---------|-------------|---------|
| `config --set api_url=...` | Set API endpoint (global) | `dotnet run --project QuickCode.Cli -- config --set api_url=https://api.quickcode.net/` |
| `config --project demo --set email=... secret_code=...` | Store project credentials | `dotnet run --project QuickCode.Cli -- config --project demo --set email=demo@quickcode.net secret_code=SECRET123` |
| `project create` | Create project / trigger secret e-mail | `dotnet run --project QuickCode.Cli -- project create --name demo --email demo@quickcode.net` |
| `project check` | Check if project exists | `dotnet run --project QuickCode.Cli -- project check --name demo` |
| `project forgot-secret` | Send secret reminder mail | `dotnet run --project QuickCode.Cli -- project forgot-secret --name demo --email demo@quickcode.net` |
| `project verify-secret` | Validate email + secret combination | `dotnet run --project QuickCode.Cli -- project verify-secret --name demo --email demo@quickcode.net --secret-code SECRET123` |
| `module available` | List available templates | `dotnet run --project QuickCode.Cli -- module available` |
| `module list/add/remove/get-dbml/save-dbml` | Manage project modules | `dotnet run --project QuickCode.Cli -- module list --project demo` |
| `generate [--watch]` | Trigger generation and optionally stream progress | `dotnet run --project QuickCode.Cli -- generate demo --watch` |
| `status --session-id` | Query generation status once | `dotnet run --project QuickCode.Cli -- status --session-id <id>` |

---

## Watcher Behavior
- SignalR hub: `/quickcodeHub?sessionId=...`.
- If no SignalR update arrives for 5 seconds, CLI automatically fetches status via HTTP to keep progress accurate.
- Once all actions finish, the CLI prints a success message and exits the watcher automatically.
- Duration labels mimic the web UI (e.g. `12.3s`, `5m 8s`, `3h 2m 1s`, or `...` when timing is unknown).

---

## Additional Notes
- Any change in credentials should be applied with `config --project`.
- For Turkish instructions read [`README.tr.md`](README.tr.md).
