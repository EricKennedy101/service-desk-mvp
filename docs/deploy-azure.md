# Azure Deployment (Production + Staging Slots)

This guide sets up FRAServiceRequestPortal on Azure App Service with two slots:
- **Production** (Swagger OFF)
- **Staging** (Swagger ON)

It includes Portal steps and optional Azure CLI commands.

---

## Prerequisites
- Azure subscription
- Azure CLI installed (optional)
- .NET 8 SDK locally
- Azure SQL Server + Database created

---

## 1) Create App Service + Staging Slot

### Azure Portal
1. Create **App Service** (Runtime: .NET 8).
2. After creation, go to **Deployment slots** → **Add Slot**.
3. Name the slot `staging` and create it.

### Azure CLI (optional)
```
az group create -n fra-rg -l eastus
az appservice plan create -g fra-rg -n fra-plan --is-linux --sku B1
az webapp create -g fra-rg -p fra-plan -n <app-name> --runtime "DOTNETCORE:8.0"
az webapp deployment slot create -g fra-rg -n <app-name> -s staging
```

---

## 2) Slot Environment Settings

**Production slot**
- `ASPNETCORE_ENVIRONMENT=Production` (Swagger OFF)

**Staging slot**
- `ASPNETCORE_ENVIRONMENT=Development` (Swagger ON)

### Portal
App Service → **Configuration** → **Application settings**  
Add settings per slot and mark them as **slot settings**.

### CLI (optional)
```
az webapp config appsettings set -g fra-rg -n <app-name> --settings ASPNETCORE_ENVIRONMENT=Production
az webapp config appsettings set -g fra-rg -n <app-name> -s staging --settings ASPNETCORE_ENVIRONMENT=Development
```

---

## 3) Required App Settings (per slot)

Set these in **Configuration → Application settings** (use slot settings):

- `ConnectionStrings__DefaultConnection` = Azure SQL connection string
- `Jwt__Key`
- `Jwt__Issuer`
- `Jwt__Audience`
- `Jwt__ExpiresMinutes`
- `EvidenceUpload__RootPath`
- `EvidenceUpload__MaxSizeBytes`
- `EvidenceUpload__AllowedExtensions` (optional; defaults are fine)

### EvidenceUpload RootPath
- **Linux**: `/home/EvidenceUploads`
- **Windows**: `%HOME%\EvidenceUploads`

---

## 4) Deploy

Use any deployment method (GitHub Actions, ZIP deploy, Visual Studio publish).

Example ZIP deploy (optional):
```
dotnet publish -c Release -o publish
az webapp deploy -g fra-rg -n <app-name> --src-path ./publish
```

---

## 5) Database Migration (manual)

Run migrations **locally** using the Azure SQL connection string:
```
dotnet ef database update \
  --project "C:\dev\service-desk-mvp\FRAServiceRequestPortal.csproj" \
  --startup-project "C:\dev\service-desk-mvp\FRAServiceRequestPortal.csproj" \
  -- --ConnectionStrings:DefaultConnection "<AZURE_SQL_CONNECTION_STRING>"
```

Note: Do **not** enable automatic migrations in Production.

---

## 6) Verification Checklist

**Production URL**
- `https://<app>.azurewebsites.net` loads
- `https://<app>.azurewebsites.net/swagger` **not accessible**

**Staging URL**
- `https://<app>-staging.azurewebsites.net/swagger` loads
- Auth/Cases/Events/Evidence endpoints work end-to-end

**Evidence persistence**
- Files persist across app restarts via configured `EvidenceUpload__RootPath`

---

## Optional: Azure Storage Mount (Enterprise)

For long-term evidence storage, consider mounting Azure Files or Blob Storage to a path like `/home/EvidenceUploads`. This is optional and not required for Slice 10.

---

## Slice 10 wrap-up
- Azure SQL Server created: `fra-mysql-server.database.windows.net`
- Azure SQL Database created: `FRA_Service_Request_Portal_Db`
- EF migration applied via `SqlServerAppDbContext`
