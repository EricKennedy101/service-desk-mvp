## What this app is
FRA Service Request Portal is a SOC-aligned case management API that tracks security incidents from intake through closure. It provides structured workflows, evidence handling, and immutable audit events for compliance.

## Features to demo
- Login (analyst vs lead)
- Create case
- Upload evidence (allowed extensions)
- Events/audit trail
- Soft delete retention (`includeDeleted`)

## 5-minute demo script
1) Open Swagger UI and log in as analyst; copy the JWT token and authorize.
2) Create a case with a title and description.
3) Upload a small `.txt` file as evidence; confirm the upload succeeds.
4) List evidence for the case and download it to verify content.
5) View case events to show Created and EvidenceUploaded entries.
6) Log in as lead, authorize, and soft delete the case.
7) Show the case is hidden from the default list, then visible with `includeDeleted=true`.
8) Re-open events for the deleted case to show retention and the Deleted event.

## What success looks like
- Analyst can create cases and upload evidence.
- Evidence list and download work end-to-end.
- Audit trail shows Created, EvidenceUploaded, and Deleted events with actor emails.
- Lead can delete; analyst cannot.
- Soft-deleted cases are hidden by default but retrievable when requested.

## Known limitations
- Local file storage for evidence (Azure Blob planned later).
- Minimal UI (Swagger demo only).
- Hardcoded AuthUsers for demo.
