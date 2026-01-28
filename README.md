# FRAServiceRequestPortal

## Deployment

See `docs/deploy-azure.md` for Azure production + staging slot setup.

## Slices

See `docs/slices.md` for the slice-by-slice implementation summary.

## Local Dev (Azure SQL)

Paste your Azure SQL ADO.NET connection string into `appsettings.Development.json` under `ConnectionStrings:DefaultConnection`. Replace `{your_password}` with the actual password, and do not commit secrets (the file is already in `.gitignore`).

## Local Run

Restore: `dotnet restore`
Apply EF migrations to LocalDB: `dotnet ef database update --context SqlServerAppDbContext`
Run the API: `dotnet run`
Open Swagger: `http://localhost:5249/swagger`

## Azure Deployment

Production runs on Azure App Service. Configure the Azure SQL connection via the App Service setting `ConnectionStrings__DefaultConnection`. Local development uses LocalDB via `appsettings.Development.json`.
