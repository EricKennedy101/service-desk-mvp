# Slices Overview

This document summarizes each implementation slice for the FRA Service Request Portal.

## Slice 1 — Baseline API + EF Core
- Initial ASP.NET Core Web API setup.
- EF Core wired with database connection and migrations.
- Swagger enabled for Development.

## Slice 2 — CRUD + DTOs + Validation
- Full CRUD for the core entity with DTOs.
- Request validation and server-controlled fields.
- Swagger verified for create/read/update/delete flows.

## Slice 3 — SOC Refactor + Enterprise Structure
- Renamed Ticket -> Case and aligned routes to `/api/Cases`.
- Introduced enterprise folder structure (Api/Domain/Contracts/Infrastructure).
- Migrations updated for schema rename.

## Slice 4 — SOC Workflow + Audit Trail
- Added enums for Status/Priority/Severity.
- Added triage fields and tags.
- Added CaseEvent audit trail with per-field updates.
- Added filters + pagination on list endpoint.

## Slice 5 — Actor Attribution via Header
- Added `X-Actor-Email` header support for audit events.
- Updated event creation to include actor when provided.

## Slice 6 — JWT Auth + RBAC
- Added JWT auth and role-based authorization.
- Added Auth controller with login + `me` endpoint.
- Actor attribution sourced from JWT claims.
- Swagger configured for Bearer auth.

## Slice 7 — Soft Delete + Audit Retention
- Soft delete fields added to Case.
- Delete endpoint now soft deletes and logs audit event.
- `includeDeleted` support on list and get by id.

## Slice 8 — Evidence Management
- Added CaseEvidence entity and upload/list/download endpoints.
- File validation and audit event on evidence upload.
- Evidence storage path configurable.

## Slice 9 — Azure Deploy Readiness
- Forwarded headers and static files enabled.
- CORS policy added for dev.
- Swagger gated to Development.
- Evidence storage path configuration.

## Slice 10 — Azure App Service Deployment
- Two-slot deployment guidance (Production/Stage).
- Azure SQL settings and environment configuration.
- Verification checklist for staging vs production.

## Slice 11 — Azure SQL Connection (Dev)
- Development connection string guidance for Azure SQL.
- Reminder to keep secrets out of source control.

## Slice 12 — Minimal Portal UI + Health Checks
- Razor Pages homepage with environment and health status.
- `/health` and `/health/db` endpoints.
- Swagger enabled for Staging only.

## Slice 13 — Portal UI Shell + Login Screen
- Portal layout with navigation and session-based login.
- Login screen with environment badge and status panel.
- Protected dashboard/cases pages and logout flow.
