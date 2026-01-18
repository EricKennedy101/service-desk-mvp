# Project Status

## Architecture
- Single codebase with two Azure App Service slots.
- Phase 1 backend lives on the `staging` slot (Service Request Portal API + Swagger).
- Phase 2 portal lives on the `phase2` slot (Employee Support Portal UI only).

## Backend Endpoints (Phase 1)
- `POST /api/Auth/login`
- `GET /api/Cases`
- `POST /api/Cases`
- `GET /api/Cases/{id}`
- `POST /api/Cases/{id}/events`
- `POST /api/Cases/{id}/evidence`
- `POST /api/tickets`
- `GET /api/tickets/mine`
- `GET /api/tickets/{id}`
- `GET /health`
- `GET /health/db`
- Swagger: `/swagger/index.html` (staging only)

## Portal-to-Backend Integration (Phase 2)
- Portal uses `BackendApi:BaseUrl` to call the Phase 1 backend.
- The portal submits tickets with `BackendApiClient.CreateTicketAsync` to `POST /api/tickets`.
- The Phase 2 portal does not expose any `/api/*` endpoints of its own.

## AI Tool Removed
- The chat/AI assistant UI and providers were removed from Phase 2.
- Reason: Phase 2 is UI-only ticket submission and should not host AI tooling.

## Remaining Work
- Backend database validation and cleanup (if any pending schema updates exist).
- Authentication/authorization tests across staging + phase2 slots.

## Deployment Rules (Critical)
- Never deploy Phase 2 to the `staging` slot.
- Never deploy Phase 1 backend to the `phase2` slot.
- Always deploy with explicit `--slot` (no implicit slot deploys).
