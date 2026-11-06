# GFC Upload Engine — Backend API (.NET 8 & SQL Server)

Clean Architecture Web API (.NET 8) powering the generic, multi-workspace GFC Upload Engine. Reads real-world projected coordinates in construction drawings (DXF, DWG via converter stub, PDF, GeoJSON, Shapefile) and builds master maps bottom-up from approved uploads.

## Tech Stack
- **Framework**: ASP.NET Core Web API (.NET 8), C#
- **Architecture**: Clean Architecture (Domain, Application, Infrastructure, API, Tests)
- **Database / Spatial**: Entity Framework Core 8 with NetTopologySuite and SQL Server (`geometry` type with spatial indexing)
- **Background Processing**: Hangfire (SQL Server storage)
- **Authentication**: ASP.NET Core Identity + JWT; Workspace-scoped roles (WorkspaceAdmin, Uploader, Reviewer, Viewer)
- **Libraries**:
  - `netDxf`: DXF extraction (lines, 2D/3D polylines, circles, points, polygons, text annotations)
  - `ProjNet`: Coordinate Reference System (CRS) transformations (e.g. EPSG:32644 to EPSG:4326)
  - `NetTopologySuite.IO.GeoJSON`: GeoJSON streaming & export
  - `CsvHelper`: Layer mapping CSV import and clash CSV export
  - `PdfPig`: PDF document parsing
  - `Serilog`: Structured logging
  - `Swashbuckle`: Swagger API documentation

## Database Setup Options

The API automatically applies EF migrations on startup (`Database.Migrate()`) and runs a spatial check verifying `geometry::STGeomFromText`.

Provide one of the following connection strings in `appsettings.json` (see `appsettings.Example.json`):

### Option 1: SQL Server LocalDB (Default for Windows development)
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=GfcEngine;Trusted_Connection=True;TrustServerCertificate=True"
}
```

### Option 2: SQL Server Express
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=GfcEngine;Trusted_Connection=True;TrustServerCertificate=True"
}
```

### Option 3: Docker (SQL Server 2022 container)
Run `docker compose up -d` in the `MP-API` repository root. The SA password is read from the `MSSQL_SA_PASSWORD` environment variable.
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost,1433;Database=GfcEngine;User Id=sa;Password=<YOUR_SA_PASSWORD>;TrustServerCertificate=True"
}
```

## First-Run Seed Data
On initial startup, the database is seeded automatically:
1. **Default Workspace**: Created from `/gis_test_pack/docs/workspace_config.json` (EPSG: 32644, center: `[80.516, 16.513]`, zoom: 14, clash tolerance: 0.5m).
2. **CAD Layer Mappings**: Seeded from `/gis_test_pack/docs/layer_mapping.csv`.
3. **Categories & Dynamic Fields**: Road, Utility, Building configs.
4. **Default Admin User**:
   - Username: `admin@gfcengine.com`
   - Default Password: Configured in `appsettings.json` (`AdminUser:DefaultPassword`). Must be changed upon first login.

## Running the API

1. **Restore and Build**:
   ```bash
   dotnet restore GfcEngine.sln
   dotnet build GfcEngine.sln
   ```

2. **Run Migrations (if not using auto-migration)**:
   ```bash
   dotnet ef database update --project src/GfcEngine.Infrastructure --startup-project src/GfcEngine.API
   ```

3. **Start the API**:
   ```bash
   dotnet run --project src/GfcEngine.API
   ```
   API is accessible at `http://localhost:5000` (Swagger docs: `http://localhost:5000/swagger`, Hangfire dashboard: `http://localhost:5000/hangfire`).

4. **Run Automated Tests**:
   ```bash
   dotnet test GfcEngine.sln
   ```

## Test Data Pack (`/gis_test_pack`)
- `generate_test_dxf.py`: Generates 6 valid DXFs and 3 invalid DXFs for verification.
  ```bash
  cd gis_test_pack
  pip install ezdxf
  python generate_test_dxf.py
  ```
