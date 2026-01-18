## Azure App Service (Windows) staging deployment

## Publish the API to Azure App Service
1) Build and publish locally:
   - `dotnet publish -c Release -o publish`
2) Deploy to the App Service (Windows) using one of:
   - Visual Studio Publish
   - Zip deploy in the Azure Portal
   - Azure CLI: `az webapp deploy --resource-group <rg> --name <app> --src-path .\publish`

## Publish to the STAGING slot
1) In Azure Portal, open your App Service.
2) Go to **Deployment slots** → select `staging`.
3) Deploy the same published output to the staging slot.
4) Confirm slot settings are set:
   - `ASPNETCORE_ENVIRONMENT=Development`
   - `ConnectionStrings__DefaultConnection` = Azure SQL connection string
   - `Jwt__Key`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpiresMinutes`
   - `EvidenceUpload__RootPath` (Windows: `%HOME%\EvidenceUploads`)
   - `EvidenceUpload__MaxSizeBytes`

## URLs to test (staging)
- `https://<app>-staging.azurewebsites.net/swagger`
- `POST https://<app>-staging.azurewebsites.net/api/Auth/login`
- `POST https://<app>-staging.azurewebsites.net/api/Cases`
- `POST https://<app>-staging.azurewebsites.net/api/Cases/{caseId}/evidence`
- `GET  https://<app>-staging.azurewebsites.net/api/Cases/{caseId}/events`

## Expected results
- Swagger UI loads (HTTP 200).
- Auth login returns 200 and a JWT token.
- Create case returns 201 with `id`.
- Upload evidence returns 200 with evidence metadata.
- Events endpoint returns 200 and includes `Created` and `EvidenceUploaded`.
