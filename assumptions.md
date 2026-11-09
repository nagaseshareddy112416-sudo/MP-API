# Architecture & Technical Assumptions - MP-API (Backend)

This document tracks assumptions made in the architecture and implementation of the backend.

## 1. Geospatial & Database Engine
- **Fixed Technology Stack**: The core database is Microsoft SQL Server with spatial types (`geometry`) and NetTopologySuite (`NetTopologySuite.IO.SqlServerBytes`).
- **No Third-Party Spatial DB Dependencies**: PostGIS, GeoServer, and GDAL/OGR are not used unless explicitly authorized. All spatial operations (containment, intersection, buffering, coordinate transformation) are handled by NetTopologySuite and ProjNet.
- **Coordinate Systems**: Drawings can be uploaded in arbitrary projected coordinate reference systems (e.g. UTM zones, state plane) or WGS84 geographic coordinates. The default workspace reference system is EPSG:32644 (UTM Zone 44N), but the system is global and not bound to any single region.

## 2. Authentication & Multi-Tenancy
- **Workspaces**: All engineering drawing data, reference layers, and user permissions are strictly scoped to a `WorkspaceId`.
- **Identity & Tokens**: ASP.NET Core Identity with JWT bearer tokens. Tokens include user identity, email, and workspace roles.
- **Roles**: Supported roles per workspace are `WorkspaceAdmin`, `Reviewer`, `Uploader`, and `Viewer`.

## 3. Background Processing & Scalability
- **Hangfire**: Async drawing ingestion, geometry extraction, and clash detection are executed via Hangfire background jobs backed by SQL Server storage.
- **File Storage**: Drawing originals (DXF, DWG, PDF, IFC, etc.) are preserved byte-for-byte in configured local/cloud storage; extraction reads from the stored file without modifying it.
