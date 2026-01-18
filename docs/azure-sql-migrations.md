# Azure SQL Migrations

This project keeps the original PostgreSQL (Npgsql) migrations for history, but uses a **separate SQL Server migrations folder** when targeting Azure SQL.

## Add a SQL Server migration
```
dotnet ef migrations add InitialSqlServer --output-dir Infrastructure/Persistence/MigrationsSqlServer --context AppDbContext
```

## Apply SQL Server migrations
```
dotnet ef database update --context AppDbContext
```

## Notes
- Keep existing Npgsql migrations untouched.
- Use `Infrastructure/Persistence/MigrationsSqlServer` for all SQL Server migrations.
