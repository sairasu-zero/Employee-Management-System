# ERMS Development Checklist

## Phase 1 – Dependency and build blockers

Status: verified complete in the current environment. Backend and frontend production builds pass, and the NuGet vulnerability review reports no vulnerable packages. The previously reported Vite `spawn EPERM` could not be reproduced; its original trigger remains undetermined because the original full error trace and matching execution environment are unavailable.

### Repository and package state

- The actual repository is nested in `Employee-Management-System-master/Employee-Management-System-master`.
- The previously referenced checklist was absent, so this file records the current verification state.
- `README.md` was read. It describes the original foundation scaffold and its dependency setup, so it does not accurately describe the current application modules.
- Backend has the API project `backend/ERMS.API/ERMS.API.csproj` and the Phase 2 security test project `backend/ERMS.API.Tests/ERMS.API.Tests.csproj`. No solution, `NuGet.Config`, or `global.json` was found.
- NuGet uses the enabled official `nuget.org` source. Restore assets exist under `backend/ERMS.API/obj/`.
- Frontend manifest and lockfile exist. `frontend/node_modules` is populated and `npm ls --depth=0` reports all declared packages installed.

### Verification performed

- Environment: .NET SDK `8.0.427`; Node.js `v26.13.0`; npm `11.20.0`.
- `npm.cmd config get offline`: `false`; `npm.cmd config get proxy` and `https-proxy`: `null`; registry: `https://registry.npmjs.org/`. No custom proxy/offline override appeared in `npm config list`.
- Node child-process probe via `spawnSync(process.execPath, ...)`: no spawn error; exit status `0`.
- Inspected Vite launcher/entry-point ACLs: the current Administrator account and Administrators group have full control. Inspected `frontend/vite.config.ts`: it configures the React plugin and dev-server host/port only; it does not customize process spawning.
- `npm.cmd --prefix .\\Employee-Management-System-master\\frontend run build`: succeeded after Phase 1 and again after the Phase 2 auth changes; latest run passed `tsc -b`, transformed 1,624 modules, and emitted production assets.
- `dotnet list .\\Employee-Management-System-master\\backend\\ERMS.API\\ERMS.API.csproj package --vulnerable --include-transitive`: completed against `https://api.nuget.org/v3/index.json`; reported no vulnerable packages.
- Final Phase 3 `dotnet test .\\Employee-Management-System-master\\backend\\ERMS.API.Tests\\ERMS.API.Tests.csproj --verbosity minimal`: 25 passed, 0 failed, 0 skipped. Final standalone `dotnet build .\\Employee-Management-System-master\\backend\\ERMS.API\\ERMS.API.csproj --no-restore --verbosity minimal`: succeeded with 0 warnings and 0 errors.
- `dotnet list .\\Employee-Management-System-master\\backend\\ERMS.API.Tests\\ERMS.API.Tests.csproj package --vulnerable --include-transitive`: reported no vulnerable packages from `nuget.org`.
- `dotnet list .\\Employee-Management-System-master\\backend\\ERMS.API.Tests\\ERMS.API.Tests.csproj package --vulnerable --include-transitive`: reported no vulnerable packages from `nuget.org`; test native SQLite is pinned to `SQLitePCLRaw.lib.e_sqlite3` 3.53.3.
- The earlier `spawn EPERM` is not reproducible with current settings. Current evidence does not indicate a persistent ACL denial, generic Node child-process restriction, npm offline/proxy setting, or Vite configuration issue. Without the original stack trace and matching environment, the historical trigger cannot be identified.
- The backend test project was added in Phase 2 using xUnit, `WebApplicationFactory`, and in-memory SQLite; frontend `node_modules` were retained. No frontend package manifests or lockfiles were changed.

## Phase 2 - Cookie and CSRF security
## Phase 2 - Cookie and CSRF security

Status: cookie, origin-validation, SignalR WebSocket, and cross-origin Development behavior verified by automated tests.

- Unsafe methods and SignalR WebSocket handshakes require an exact configured `Origin`, or an allowed `Referer` when `Origin` is absent. Missing, malformed, and unconfigured origins receive `403` before authentication or controller execution. Ordinary `GET`, `HEAD`, and CORS preflight `OPTIONS` remain available.
- CORS remains credentialed and exact-origin. Development origins remain in `appsettings.Development.json`; production `appsettings.json` has an empty allowlist and must be configured with the deployed frontend origin, for example `Cors__AllowedOrigins__0=https://app.example.com`.
- Session cookies are `HttpOnly` and `SameSite=Lax`; they use `SecurePolicy.Always` outside Development and `SameAsRequest` in Development. Frontend and API must be same-site for Lax cookies to accompany cross-origin requests; localhost Development meets this requirement.
- Login/MFA issue cookies, logout clears the cookie, and API auth failures return `401`/`403` rather than browser redirects. The MFA flow carries the remember-me choice through TOTP and recovery-code verification. Frontend mock login, session restore, MFA, and registration fallbacks are Development-only.

## Phase 3 - Password reset

Status: reset flow implemented; 25 security and SQLite-backed lifecycle tests pass. Production delivery remains unavailable until an email/recovery delivery mechanism is configured.

- Reset requests use a generic response and audit entry for known and unknown accounts. Production does not create, return, or log reset tokens because no email sender exists. No external email provider was added.
- Development creates a 32-byte cryptographically random token, stores only its SHA-256 hash with a 30-minute UTC expiry, and returns the raw token only in the Development API response. Never put this response token in logs or source control.
- Reset validates the token and expiry, hashes the new password with the existing Argon2id hasher, and atomically consumes the token, updates the password, clears lockout state, increments `SessionVersion`, and revokes existing refresh-token rows. Cookie validation rejects older session-version claims.
- Existing cookies without a session-version claim are rejected after deployment; users must sign in again.
- Development workflow: run the API with `ASPNETCORE_ENVIRONMENT=Development`; POST an account email to `/api/auth/forgot-password`; copy the response's `developmentResetToken` for an active account; POST that token and the new password to `/api/auth/reset-password`. The token expires after 30 minutes and works once.
- Migration `20261107101703_PasswordResetSecurity` was generated and its idempotent SQL script was produced successfully. It also includes outstanding model/schema changes for leave requests and employee documents discovered in the existing EF snapshot. The migration has not been applied to a PostgreSQL database; apply it before using the new columns in a deployed database.
- Tests cover generic known/unknown responses, hashed storage, expiry, replay rejection, Argon2id password replacement, audit entries, session-cookie invalidation, and refresh-token revocation.

### Remaining blockers

- Production password reset cannot complete until a delivery channel is configured; production requests remain generic and issue no usable token. The migration must also be applied to the target database.
- Successful credential verification against a live PostgreSQL database was not tested; reset lifecycle tests use SQLite in memory. The historical Vite `spawn EPERM` remains unreproduced and is not a current blocker. No Phase 4 or later work was started.
