# FRA Service Request Portal / FRA Employee Support Portal — Final Summary

This document describes what was built and where the project stands after the latest work.

## High-level architecture
- **Single ASP.NET Core (.NET 8) codebase** with behavior controlled by configuration.
- **Two Azure App Service deployment slots** on the same app:
  - **Backend (Service Desk)**: slot `staging`
    - Runs the **system-of-record API** + DB-backed storage.
    - **Swagger enabled** in `Development`/`Staging` for backend slot only.
  - **Portal (Employee Support)**: slot `phase2`
    - **UI-only Razor Pages** “frontend” that calls the backend API.
    - No `/api/*` endpoints intended for portal usage; it acts as a client.

## Backend (Service Desk) capabilities
### Authentication
- `POST /api/Auth/login`
  - Validates credentials from `AuthUsers` configuration.
  - Returns a **JWT** when credentials are valid.
- `GET /api/Auth/me`
  - **Requires JWT** (`Authorization: Bearer <token>`).
  - Returns the authenticated user’s email and roles.

### Case management (SOC-aligned)
- `GET /api/Cases` (filterable list)
- `POST /api/Cases` (create)
- `GET /api/Cases/{id}` (read)
- `PUT /api/Cases/{id}` (update)
- `DELETE /api/Cases/{id}` (**role-gated**: `SOCLead` or `Admin`)
- Immutable audit trail:
  - `GET /api/Cases/{id}/events`
  - `POST /api/Cases/{id}/events`
- Evidence management:
  - `POST /api/Cases/{id}/evidence` (multipart upload with validation + hashing)
  - `GET /api/Cases/{id}/evidence` (list)
  - `GET /api/Cases/{id}/evidence/{evidenceId}` (download)

### Ticket system (employee-facing system of record)
- `POST /api/tickets` (create ticket)
- `GET /api/tickets/mine` (list tickets for requester; uses authenticated email when available)
- `GET /api/tickets/{id}` (ticket detail)
- Ticket entity includes `TranscriptJson` (optional) so portal-submitted context can be stored.

### Health
- `GET /health`
- `GET /health/db`

## Backend security model (current)
- **JWT auth** (Bearer token) is used for:
  - `/api/Auth/me`
  - `/api/Cases*` (role-gated at controller/action level)
  - `/api/tickets*` (controller has `[Authorize]`)
- **API key** (header `X-API-Key`) is enforced **only for**:
  - `/api/tickets*`
- **Important**: API key middleware does **not** apply to:
  - `/api/Auth/*`
  - `/swagger/*`
  - `/health*`

## Phase 2 portal (Employee Support) capabilities
- Razor Pages UI with navigation and branding for “Employee Support Portal”.
- Support landing page links to:
  - **Submit Ticket** (`/tickets/new`)
  - **My Tickets** (`/tickets`)
- Ticket submission:
  - Employee enters **Email + Title + Description + Category + Priority**
  - Optional **Transcript** text box (paste any context)
  - Portal calls backend via `BackendApiClient` using `BackendApi:BaseUrl`
  - Backend creates the ticket in the service desk DB.

## What we intentionally did NOT build / do
- No DB/EF migrations owned by Phase 2 portal (portal remains UI-only client).
- No AI chat “assistant” in Phase 2 (removed and replaced with ticket submission only).
- No Copilot Studio integration.

## Automated tests added
- `ServiceDeskBackend.Tests` (xUnit) using `WebApplicationFactory` / in-memory test host
  - Smoke/contract tests for:
    - Swagger JSON accessible
    - Auth login works and `/api/Auth/me` works with token
    - Tickets require API key and work with API key + JWT
    - Cases require JWT and work with JWT
  - Tests run via:
    - `dotnet test .\ServiceDeskBackend.Tests\ServiceDeskBackend.Tests.csproj`

## Azure deployment rules (critical)
- **Never deploy portal artifacts to `staging`**.
- **Never deploy backend artifacts to `phase2`**.
- Always deploy with explicit slot:
  - `--slot staging` for backend
  - `--slot phase2` for portal

## Configuration (how it’s wired)
- Portal behavior toggled by:
  - `BackendApi:BaseUrl` (when set, portal mode; when empty, backend mode)
- Backend auth requires:
  - `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpiresMinutes`
  - `AuthUsers` list (email/password/roles)
- Tickets API key (optional gate):
  - `ServiceDesk:ApiKey`

