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
brew tap QuickCodeNet/quickcode-cli
brew install quickcode-cli
```

> Update to latest version when a new release is available:
```bash
brew update
brew upgrade quickcode-cli
```

If `brew` shows a conflict/syntax error, reset the tap and try again:
```bash
rm -rf /opt/homebrew/Library/Taps/quickcodenet/homebrew-quickcode-cli
brew tap quickcodenet/quickcode-cli
brew upgrade quickcode-cli
```

### Windows (Scoop) - Recommended
```powershell
scoop bucket add quickcode-cli https://github.com/QuickCodeNet/scoop-bucket
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

# 2. Check if project exists
quickcode demo check

# 3. Create project (if it doesn't exist)
quickcode demo create --email demo@quickcode.net
# Note: The email is automatically saved to local config after creation.

# 4. Store project secret_code (check your email for the secret code)
quickcode demo config --set secret_code=SECRET123

# 5. Validate configuration
quickcode config validate
quickcode demo validate

# 6. List modules in the project
quickcode demo modules

# 7. Add a new module (all parameters except --module-name are optional with defaults)
quickcode demo modules add --module-name UserManager
# Or with all parameters explicitly:
quickcode demo modules add --module-name ProductModule --template-key UserManager --db-type mssql --pattern Service
# Module name must be in PascalCase: start with letter, after digits use uppercase (e.g., SmsModule2Test)
# Default values: --template-key=Empty, --db-type=mssql, --pattern=Service
# Valid db-type values: mssql, mysql, postgresql
# Valid pattern values: Service, CqrsAndMediator
# Note: Adding a module automatically downloads and saves the template DBML locally

# 8. Remove a module
quickcode demo modules remove --module-name UserManager
# Module name must be in PascalCase: start with letter, after digits use uppercase (e.g., SmsModule2Test)

# 9. Download project & template DBML files
quickcode demo get-dbmls
# Running `quickcode demo get-dbmls` also places the latest `README.md` in the same folder so the command reference is always available offline.

# 10. Upload DBML files back to the API
quickcode demo update-dbmls

# 11. Generate and watch
# Trigger code generation for the project. By default, watches the progress in real-time.
quickcode demo generate
# Or disable watching:
quickcode demo generate --watch false

# 12. Other useful commands
quickcode templates                    # List available module templates
quickcode demo forgot-secret           # Request secret code reminder
quickcode demo verify-secret           # Verify email + secret combination
quickcode demo remove                  # Remove stored credentials and local DBML folder

# 13. Clone or pull project from GitHub
quickcode demo pull

# 14. Push changes to project on GitHub
quickcode demo push
```

### Option B – Development mode (from source)
> Useful for development or if you haven't installed the binary yet.

```bash
cd /path/to/quickcode.cli

# 1. CLI help
dotnet run --project src/QuickCode.Cli -- --help

# 2. Check if project exists
dotnet run --project src/QuickCode.Cli -- demo check

# 3. Create project (if it doesn't exist)
dotnet run --project src/QuickCode.Cli -- demo create --email demo@quickcode.net
# Note: The email is automatically saved to local config after creation.

# 4. Store project secret_code (check your email for the secret code)
dotnet run --project src/QuickCode.Cli -- demo config --set secret_code=SECRET123

# 5. Validate configuration
dotnet run --project src/QuickCode.Cli -- config validate
dotnet run --project src/QuickCode.Cli -- demo validate

# 6. List modules in the project
dotnet run --project src/QuickCode.Cli -- demo modules

# 7. Add a new module (all parameters except --module-name are optional with defaults)
dotnet run --project src/QuickCode.Cli -- demo modules add --module-name UserManager
# Or with all parameters explicitly:
dotnet run --project src/QuickCode.Cli -- demo modules add --module-name ProductModule --template-key UserManager --db-type mssql --pattern Service
# Module name must be in PascalCase: start with letter, after digits use uppercase (e.g., SmsModule2Test)
# Default values: --template-key=Empty, --db-type=mssql, --pattern=Service
# Valid db-type values: mssql, mysql, postgresql
# Valid pattern values: Service, CqrsAndMediator
# Note: Adding a module automatically downloads and saves the template DBML locally

# 8. Remove a module
dotnet run --project src/QuickCode.Cli -- demo modules remove --module-name UserManager
# Module name must be in PascalCase: start with letter, after digits use uppercase (e.g., SmsModule2Test)

# 9. Download project & template DBML files
dotnet run --project src/QuickCode.Cli -- demo get-dbmls

# 10. Upload DBML files back to the API
dotnet run --project src/QuickCode.Cli -- demo update-dbmls

# 11. Generate and watch
# Trigger code generation for the project. By default, watches the progress in real-time.
dotnet run --project src/QuickCode.Cli -- demo generate
# Or disable watching:
dotnet run --project src/QuickCode.Cli -- demo generate --watch false

# 6. Clone or pull demo project from GitHub
dotnet run --project src/QuickCode.Cli -- demo pull

# 7. Push changes to demo project on GitHub
dotnet run --project src/QuickCode.Cli -- demo push
```

---

## Configuration Rules
- API endpoint defaults to `https://api.quickcode.net/`. Change it via `config --set api_url=...` only if you target a different backend.
- Every project must store its own `email` and `secret_code` via `config --project <name>`.
- Commands require explicit project name; there are no default_* fallbacks.
- Config file is stored at `~/.quickcode/config.json`.
- **Security**: Secret codes are automatically encrypted using AES-256 encryption. The encryption key is stored at `~/.quickcode/.key` with restricted permissions (600).
- Use `config validate` to check all projects or `project validate --name <project>` to validate a specific project.

---

## Command Reference

| Command | Description | Example |
|---------|-------------|---------|
| `config --set api_url=...` | Set API endpoint (global) | `quickcode config --set api_url=https://api.quickcode.net/` |
| `config --project demo --set email=... secret_code=...` | Store project credentials (secret_code is encrypted) | `quickcode config --project demo --set email=demo@quickcode.net secret_code=SECRET123` |
| `config validate` | Validate all project configurations | `quickcode config validate` |
| `project create` | Create project / trigger secret e-mail | `quickcode project create --name demo --email demo@quickcode.net` |
| `project check` | Check if project exists | `quickcode project check --name demo` |
| `project forgot-secret` | Send secret reminder mail | `quickcode project forgot-secret --name demo --email demo@quickcode.net` |
| `project verify-secret` | Validate email + secret combination | `quickcode project verify-secret --name demo --email demo@quickcode.net --secret-code SECRET123` |
| `project validate --name <project>` | Validate specific project configuration | `quickcode project validate --name demo` |
| `project get-dbmls --name <project>` | Download project modules, README.md, and templates to the project folder | `quickcode project get-dbmls --name demo` |
| `project update-dbmls --name <project>` | Upload all DBML files from project folder to API | `quickcode project update-dbmls --name demo` |
| `project remove --name <project>` | Remove stored credentials and delete local DBML folder | `quickcode project remove --name demo` |
| `module available` | List available templates | `quickcode module available` |
| `module list/add/remove/get-dbml/save-dbml` | Manage project modules | `quickcode module list --project demo` |
| `generate [--watch]` | Trigger generation and optionally stream progress | `quickcode generate demo --watch` |
| `status --session-id` | Query generation status once | `quickcode status --session-id <id>` |
| `<project> pull` | Clone or pull the project from GitHub | `quickcode demo pull` |
| `<project> push` | Push changes to the project on GitHub | `quickcode demo push` |

---

## Watcher Behavior
- SignalR hub: `/quickcodeHub?sessionId=...`.
- If no SignalR update arrives for 5 seconds, CLI automatically fetches status via HTTP to keep progress accurate.
- Once all actions finish, the CLI prints a success message and exits the watcher automatically.
- Duration labels mimic the web UI (e.g. `12.3s`, `5m 8s`, `3h 2m 1s`, or `...` when timing is unknown).

---

## Security Features
- **Encrypted Secrets**: Secret codes are automatically encrypted using AES-256 encryption before being stored in the configuration file.
- **Encryption Key**: The encryption key is stored at `~/.quickcode/.key` with restricted file permissions (600 on Unix/macOS).
- **Automatic Migration**: Existing plain-text secrets are automatically encrypted on first load.
- **Validation**: Use `config validate` or `project validate --name <project>` to check for missing credentials.

## Additional Notes
- Any change in credentials should be applied with `config --project`.
- Secret codes are never displayed in plain text; they appear as `********` when viewing config.
- For Turkish instructions read [`README.tr.md`](README.tr.md).
