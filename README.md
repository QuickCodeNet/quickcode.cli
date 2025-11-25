# QuickCode.Cli

QuickCode.Cli is a .NET 10 console application that mirrors the QuickCode web UI: create projects, manage modules, trigger generation and stream SignalR updates directly from the terminal.

- Default README language: **English**  
- Turkish version: [`README.tr.md`](README.tr.md)

---

## Prerequisites
- Internet access to `https://api.quickcode.net/`
- Each project's email + secret must be known or retrievable via `project forgot-secret`

---

## Installation

### macOS (Homebrew) - Recommended
```bash
brew tap uzeyirapaydin/quickcode-cli
brew install quickcode-cli
```

### Windows (Scoop) - Recommended
```powershell
scoop bucket add quickcode-cli https://github.com/uzeyirapaydin/scoop-bucket
scoop install quickcode-cli
```

### Manual Installation
1. Download the latest release from [GitHub Releases](https://github.com/QuickCodeNet/quickcode.cli/releases/latest)
2. Extract the archive for your platform:
   - **macOS (Apple Silicon)**: `quickcode-cli-osx-arm64-v*.tar.gz`
   - **macOS (Intel)**: `quickcode-cli-osx-x64-v*.tar.gz`
   - **Windows (x64)**: `quickcode-cli-win-x64-v*.zip`
   - **Windows (ARM64)**: `quickcode-cli-win-arm64-v*.zip`
3. Extract and add the executable to your PATH
4. Verify installation: `quickcode --version`

### Development Build
If you want to build from source, you need .NET SDK 10.0.100 or newer:
```bash
git clone https://github.com/QuickCodeNet/quickcode.cli.git
cd quickcode.cli
dotnet build
dotnet run --project src/QuickCode.Cli -- --help
```

---

## Quick Start
You can run the CLI either from the repository root (recommended) or from inside the project directory. The command examples below cover the full workflow including config, project creation, secret management, verification, generation and watching.

### Option A – Using installed binary (recommended)
```bash
# 1. CLI help
quickcode --help

# 2. Store project credentials
quickcode config --project demo --set email=demo@quickcode.net
quickcode config --project demo --set secret_code=SECRET123

# 3. Project operations
quickcode project create --name demo --email demo@quickcode.net
quickcode project check --name demo
quickcode project forgot-secret --name demo --email demo@quickcode.net
quickcode project verify-secret --name demo --email demo@quickcode.net --secret-code SECRET123

# 4. Module listing / editing (examples)
quickcode module list --project demo
quickcode module available

# 5. Generate and watch
quickcode generate demo --watch
```

### Option B – Development mode (from source)
> Useful for development or if you haven't installed the binary yet.

```bash
cd /path/to/quickcode.cli

# 1. CLI help
dotnet run --project src/QuickCode.Cli -- --help

# 2. Store project credentials
dotnet run --project src/QuickCode.Cli -- config --project demo --set email=demo@quickcode.net
dotnet run --project src/QuickCode.Cli -- config --project demo --set secret_code=SECRET123

# 3. Project operations
dotnet run --project src/QuickCode.Cli -- project create --name demo --email demo@quickcode.net
dotnet run --project src/QuickCode.Cli -- project check --name demo
dotnet run --project src/QuickCode.Cli -- project forgot-secret --name demo --email demo@quickcode.net
dotnet run --project src/QuickCode.Cli -- project verify-secret --name demo --email demo@quickcode.net --secret-code SECRET123

# 4. Module & generation
dotnet run --project src/QuickCode.Cli -- module list --project demo
dotnet run --project src/QuickCode.Cli -- generate demo --watch
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
| `config --set api_url=...` | Set API endpoint (global) | `quickcode config --set api_url=https://api.quickcode.net/` |
| `config --project demo --set email=... secret_code=...` | Store project credentials | `quickcode config --project demo --set email=demo@quickcode.net secret_code=SECRET123` |
| `project create` | Create project / trigger secret e-mail | `quickcode project create --name demo --email demo@quickcode.net` |
| `project check` | Check if project exists | `quickcode project check --name demo` |
| `project forgot-secret` | Send secret reminder mail | `quickcode project forgot-secret --name demo --email demo@quickcode.net` |
| `project verify-secret` | Validate email + secret combination | `quickcode project verify-secret --name demo --email demo@quickcode.net --secret-code SECRET123` |
| `module available` | List available templates | `quickcode module available` |
| `module list/add/remove/get-dbml/save-dbml` | Manage project modules | `quickcode module list --project demo` |
| `generate [--watch]` | Trigger generation and optionally stream progress | `quickcode generate demo --watch` |
| `status --session-id` | Query generation status once | `quickcode status --session-id <id>` |

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
