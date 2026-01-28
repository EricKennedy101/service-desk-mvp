# FRAServiceRequestPortal

## Deployment

See `docs/deploy-azure.md` for Azure production + staging slot setup.

## Slices

See `docs/slices.md` for the slice-by-slice implementation summary.

## Local Dev (LocalDB)

Local development uses SQL Server LocalDB.

Apply migrations and run:
- Restore: `dotnet restore`
- Apply EF migrations: `dotnet ef database update --context SqlServerAppDbContext`
- Run API: `dotnet run`
- Open Swagger: `http://localhost:5249/swagger`

A development-only seed user is available for local testing (see Auth section).

## Local Run

Restore: `dotnet restore`
Apply EF migrations to LocalDB: `dotnet ef database update --context SqlServerAppDbContext`
Run the API: `dotnet run`
Open Swagger: `http://localhost:5249/swagger`

## Azure Deployment

Production runs on Azure App Service. Configure the Azure SQL connection via the App Service setting `ConnectionStrings__DefaultConnection`. Local development uses LocalDB via `appsettings.Development.json`.
