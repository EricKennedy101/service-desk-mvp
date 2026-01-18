# FRAServiceRequestPortal

## Deployment

See `docs/deploy-azure.md` for Azure production + staging slot setup.

## Slices

See `docs/slices.md` for the slice-by-slice implementation summary.

## Local Dev (Azure SQL)

Paste your Azure SQL ADO.NET connection string into `appsettings.Development.json` under `ConnectionStrings:DefaultConnection`. Replace `{your_password}` with the actual password, and do not commit secrets (the file is already in `.gitignore`).
