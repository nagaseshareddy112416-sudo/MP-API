# Changelog - MP-API (Backend)

All notable changes to the backend service will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.4.0] - 2026-11-09 - Phase 3: Drawing & Upload Backend Integration
### Added
- Verified spatial pre-check endpoint (`POST /api/workspaces/{id}/drawings/preview`) returning GeoJSON features, display envelope, layer classification, and unmapped layer rejection.
- Verified drawing deletion (`DELETE /api/workspaces/{id}/drawings/{id}`) with cascade deletion across extracted features, clashes, compliance results, revision history, and physical storage files.

## [0.3.0] - 2026-11-09 - Phase 2: Design System Integration
### Added
- Maintained backend endpoint readiness and workspace/role DTOs aligning with Phase 2 design system navigation and command palette.

## [0.2.0] - 2026-11-09 - Phase 1: Stabilization
### Added
- Multi-stage production `Dockerfile` for ASP.NET Core 8 API.
- GitHub Actions CI workflow in `.github/workflows/ci.yml` with restore, warnings-as-errors build, test runner, and vulnerability scanning.
- Root docker-compose configuration (`docker-compose.yml`) including SQL Server 2022 developer edition with volume persistence and health checks.
- `appsettings.Example.json` with multi-environment database connection templates (LocalDB, SQLExpress, Docker, Remote SQL Server).
- `assumptions.md` and `KNOWN_LIMITATIONS.md` architectural tracking.

### Fixed
- Fixed nullable CS8604 warning in `GfcEngine.Tests/Phase9ComplianceTests.cs` (line 370).
- Fixed nullable CS8601 warnings in `DrawingsController.cs` (lines 253 & 299) by ensuring default fallback for stage names.
- Resolved zero compiler warnings under `/warnaserror` mode across all 5 projects.
- Extended JWT token validity to 720 hours (30 days) and verified 401 handling.

### Verified
- `dotnet build /warnaserror` passed with 0 warnings and 0 errors.
- `dotnet test` passed 23 of 23 unit & integration tests (100% pass rate).
- `dotnet list package --vulnerable` reported zero vulnerable dependencies.

