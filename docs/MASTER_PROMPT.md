# MASTER IMPLEMENTATION PROMPT (CORRECTED)
## Global Construction Intelligence & Digital Twin Platform

> Keep this file in the repository as `docs/MASTER_PROMPT.md`. At the start of every phase, re-read it in full before doing anything.

---

# 1. YOUR ROLE

Act as a combined team of:

- Principal Software Architect
- Senior .NET Backend Engineer
- Senior Angular Frontend Engineer
- GIS and Geospatial Specialist
- CAD/BIM Integration Engineer
- 3D Digital Twin Engineer
- Drone Navigation and Photogrammetry Specialist
- Database Architect
- Cybersecurity Engineer
- Senior Product Designer and UI/UX Architect
- QA Automation Engineer

Your responsibility is to transform my existing application into a world-class, production-grade, global construction intelligence and digital twin platform.

This is an existing application enhancement project, NOT a greenfield rewrite.

Quality, stability, security, performance and engineering accuracy are more important than speed.

---

# 2. PRODUCT VISION

Build a GLOBAL CONSTRUCTION DIGITAL TWIN PLATFORM.

The application must work for construction projects anywhere in the world. It must not be restricted to any particular city, country, construction company, project type or engineering discipline.

## 2.1 Priority scope for the first release

The first release focuses on:

- Roads
- Buildings
- Bridges
- Underground utilities (water supply, sewerage, stormwater drainage, power, telecommunications)

All other categories (railways, metro, airports, ports, gas, industrial facilities, government infrastructure, parks and landscaping, and any other civil construction) must be supported through CONFIGURATION ONLY (categories, sub-types, layer mapping, styles, tolerances). Do not write category-specific code for them until I explicitly request it.

## 2.2 Core concept

Every construction project has engineering drawings.

When a user uploads an approved GFC drawing, the application must identify its real geographical position and integrate the drawing into the corresponding project master plan.

The master plan is created and progressively developed from geographically positioned and approved engineering information. There is NO hardcoded master plan for any city or country.

The platform must support:

1. Individual project master plans.
2. An optional globally connected geographical view of participating projects, subject to permissions.

The system must keep these clearly distinct:

- Official reference master plans (optional reference layers uploaded by an admin).
- Proposed designs.
- Approved GFC designs.
- Construction progress.
- Actual as-built infrastructure.

An uploaded drawing must never silently modify an official statutory master plan.

---

# 3. TECHNOLOGY STACK (FIXED)

Preserve the existing architecture wherever practical.

## Backend
- ASP.NET Core Web API, .NET 8, C#
- Clean Architecture (Domain, Application, Infrastructure, API)
- Entity Framework Core with NetTopologySuite
- SQL Server (geometry type, spatial indexes with explicit bounding boxes)
- Hangfire (SQL Server storage)
- ASP.NET Core Identity, JWT with refresh tokens
- FluentValidation, Mapster or AutoMapper, Serilog, Swagger/OpenAPI

## Frontend
- Angular (latest stable version compatible with the repo), standalone components, strict TypeScript, Angular Signals
- Angular Material with a custom premium design system built on design tokens
- OpenLayers for 2D maps
- CesiumJS for 3D digital twin visualization

## Geospatial and engineering libraries
- NetTopologySuite, ProjNet
- netDxf (DXF), PdfPig (PDF)
- NetTopologySuite.IO.GeoJSON and NetTopologySuite.IO.Esri.Shapefile
- xBIM (IFC processing), glTF/GLB
- ODA File Converter behind an `IDwgConverter` abstraction for DWG (optional; must fail gracefully with a clear message if not installed)

**The geospatial stack is FIXED: SQL Server spatial + NetTopologySuite + ProjNet.**
Do NOT introduce PostGIS, GeoServer or GDAL/OGR unless the Phase 0 audit proves a specific requirement cannot be met otherwise, and then only with my written approval.

## Testing
- xUnit, FluentAssertions, Testcontainers (SQL Server)
- Angular unit tests (Jest or Karma, whichever the repo uses)
- Playwright (end-to-end)
- axe (accessibility)

Do not upgrade or replace dependencies blindly. Verify compatibility with the existing repositories first.

---

# 4. GLOBAL GIS AND COORDINATE SYSTEMS

Requirements:

1. Global geographical positioning.
2. Project boundary management (optional per workspace).
3. Country and regional coordinate systems.
4. Latitude/longitude support.
5. UTM and other projected coordinate reference systems.
6. Local engineering coordinate systems (via control points).
7. Coordinate transformation and validation.
8. Base maps: satellite, OpenStreetMap, terrain, engineering/plain, none, and custom (tile URLs and attribution come from configuration).
9. 2D and 3D visualization.
10. Multiple infrastructure layers.
11. Spatial search and project/location navigation.

Never assume all uploaded drawings use WGS84 or latitude/longitude.

If the coordinate system cannot be determined reliably, request manual CRS selection or control points.

Do not place an incorrectly positioned drawing on the master plan.

---

# 5. UNIVERSAL GFC UPLOAD ENGINE

This is the core feature.

## 5.1 Supported inputs
DWG (via conversion), DXF, PDF, GeoJSON, Shapefile, IFC, glTF, GLB, OBJ where supported.

## 5.2 Drawing stages (configurable, ordered per workspace)
Concept, Preliminary Design, Detailed Design, Tender, GFC, Shop Drawing, As-Built, Handover.

Keep a separate Construction Status per feature (Not Started, Under Construction, Completed).

## 5.3 Required drawing data
Workspace, project, construction category, discipline, drawing number, drawing title, revision, drawing stage, approval status, CRS, geographical extent, uploading user, upload date, responsible organization, contractor, consultant, source file, related drawings, and "source of coordinates" (Survey / Redrawn / Unknown).

## 5.4 Upload workflow (replace the existing long form)

**Step 1: File**: drag and drop, multiple files, validation, preview, progress.
**Step 2: Drawing information**: project, category, discipline, drawing number, revision, stage, metadata (fields depend on category, configured per workspace).
**Step 3: Placement and preview**: auto-detect coordinates, show extracted geometry on the map, manual placement or control points where required, show CRS and project boundary, validate positioning.
**Step 4: Review and submit**: validation summary, warnings, errors, drawing preview, submit for review and approval.

## 5.5 Drawing processing
The upload engine must:

1. Validate the file (extension, MIME, magic bytes, size).
2. Detect its format.
3. Extract drawing metadata.
4. Detect CRS where possible.
5. Transform coordinates.
6. Extract geometry: LINE, LWPOLYLINE, POLYLINE (2D and 3D), CIRCLE, POINT, ARC and SPLINE (approximated), closed polylines as polygons, TEXT and MTEXT.
7. Read CAD layers and classify them using the workspace layer-mapping table; report unmapped layers clearly instead of silently ignoring them.
8. Extract labels and engineering attributes (chainage, depth from negative Z, diameter, material, floors such as "G+10" or "STILT+G+10").
9. Generate spatial features.
10. Create map previews.
11. Store the original drawing unchanged.
12. Queue expensive operations in Hangfire (retryable, idempotent, with progress).
13. Display processing status, including failure reasons.
14. Allow preview before final approval.

## 5.6 Validation with clear, specific messages
- Missing or wrong CRS.
- Coordinates near (0,0) or otherwise implausible.
- Far from existing workspace data (configurable distance, warning).
- Outside the workspace boundary (if one is defined).
- Unreadable or corrupt file.
- No mapped layers found.
- Unsupported entity types (list them).

Never treat a PDF preview or a simple map marker as an accurate digital representation of an engineering drawing. Preserve the original drawing and its geometry. Provide clear processing errors for unsupported or incomplete files.

---

# 6. DRAWING-TO-MASTER-PLAN INTEGRATION

When a GFC drawing is uploaded and approved:

- Extract the geographical geometry and determine its position.
- Match it to the project, category and discipline.
- Add its geometry to the corresponding master-plan layer.
- Link the original drawing and store revision information.
- Update spatial indexes and refresh relevant map data.

Examples: a building GFC appears at its actual footprint; a water pipeline along its actual alignment; a bridge at its actual position; a road along its actual alignment.

Do not display drawings as simple pins when real geometry is available. Use points, lines, polygons, 3D models and volumetric features. Support both drawing-level and individual feature-level positioning. A simple pin is allowed only as the "Level 1" fallback when a drawing has no coordinates and the user places it manually.

---

# 7. ADVANCED GIS MAP VISUALIZATION

Current problem: uploaded drawings appear only as thin single-colour outlines. Required:

- Layer-based symbology from the layer-mapping table: fill colour, fill opacity, stroke colour, stroke width, dash pattern, label field, min/max zoom, z-order.
- Semi-transparent polygon fills; zoom-aware line widths.
- TEXT/MTEXT rendered as map labels with rotation, zoom-based visibility and collision avoidance.
- Level of detail: outlines when zoomed out, full detail when zoomed in, and full detail for a focused (selected) drawing.
- Feature selection with highlight (fade others), hover tooltips, legend built from the layer mapping.
- Measurement tools, scale bar, live coordinates in WGS84 and UTM.
- Layer filters, search, bounding-box-based data loading, geometry simplification.
- Base maps: satellite, street, terrain, engineering, none.

Provide a professional CAD-style drawing viewer: zoom, pan, layer toggles, distance measurement, feature inspection, drawing attributes, revision comparison, open original drawing. For georeferenced PDFs, show the page as an overlay and in the viewer.

---

# 8. 3D DIGITAL TWIN

This is one of the most important requirements. Do not implement 3D merely as a building extruded from a marker. Build a real geographical 3D experience with CesiumJS so the user feels they are virtually visiting the actual construction location.

## 8.1 2D/3D synchronization
A seamless 2D/3D switch that preserves location, selected project, selected asset, visible layers and filters, using the same underlying data.

## 8.2 Terrain and environment
3D terrain, satellite imagery, buildings, roads, bridges, underground infrastructure, uploaded 3D models, photogrammetry, point clouds where supported, 3D Tiles, survey data. Use configurable providers and credentials. Never hardcode Cesium tokens.

## 8.3 Buildings
Support footprints, heights, floor count, structural and architectural models, interior models where available, elevation, foundation information.

If a building has only a 2D footprint and floor count, allow approximate extrusion (floors x floor height, default 3 m, configurable). **Clearly mark estimated heights.**

Support IFC, glTF, GLB, OBJ where supported. IFC is processed in background jobs (xBIM behind an `IModelConverter` interface) and converted for viewing.

Provide a model placement interface (position, rotation, scale, elevation, CRS, alignment). Use model georeferencing automatically when available.

Do not claim an approximate extruded building is a fully accurate BIM model.

## 8.4 Underground utilities
Support water, sewerage, stormwater, electrical ducts, gas, communications, manholes, valves, chambers, other underground structures. Use depth and elevation data. Render as 3D geometry with diameter, material and category styling.

Provide: ground-transparency slider, underground visibility controls, depth information, vertical positioning, clash markers, feature inspection.

Never infer accurate vertical positioning from latitude and longitude alone.

---

# 9. DRONE-STYLE VIRTUAL NAVIGATION

Users must be able to virtually fly around a construction site like a drone.

**A. Free-fly mode:** W/S forward/back, A/D left/right, Q/E down/up, mouse look, mouse wheel for speed or zoom, speed slider, smooth movement, adjustable acceleration and sensitivity, no movement below terrain, configurable altitude limits.

**B. Orbit mode:** orbit around a selected building (horizontal and vertical), zoom, reset camera, focus on selected asset.

**C. Walk mode:** first-person camera at a configurable eye height, terrain collision, building/ground interaction where supported.

**D. Fly-to:** clicking a project, building or asset flies the camera there smoothly and opens the asset panel.

**E. Drone HUD:** altitude, heading, speed, latitude, longitude, elevation, compass, current mode.

**F. Saved viewpoints:** camera position and orientation, selected project and building, visible layers, zoom; shareable with authorized users.

**G. Fly-through tours:** add, reorder and edit waypoints; altitude, speed, duration; preview, play, pause, resume, loop; save and share; optional video export if technically feasible.

**H. Touch support:** pinch zoom, touch orbit, mobile-friendly controls.

These are virtual drone-style camera controls, NOT control of a physical UAV. Physical drone integration is a separate optional module requiring explicit device authorization, aviation compliance and safety controls.

---

# 10. REAL DRONE AND PHOTOGRAMMETRY INTEGRATION

Design for importing: drone photographs, geotagged photographs, orthomosaic imagery, photogrammetry models, point clouds, 3D meshes, 3D Tiles, survey outputs.

Support capture date, survey details, CRS, accuracy metadata, before/after comparison, historical site views, and comparison of drone-captured conditions against approved GFC designs.

Do not claim a collection of drone photographs automatically forms an accurate 3D model. Photogrammetry processing may need external software or a dedicated service.

---

# 11. TIME-BASED DIGITAL TWIN (4D)

A construction timeline with: design date, GFC approval date, construction start, construction progress, survey capture dates, as-built date, handover date.

A timeline slider showing how the site changes over time using available historical models, imagery and approved records. Planned vs actual comparison, historical drawing revisions, construction and completion status.

Do not fabricate historical 3D geometry when historical data is unavailable.

---

# 12. GFC VS AS-BUILT COMPARISON

Link each as-built drawing to the GFC revision that was current at construction time.

- Feature matching: category, sub-type, chainage range, optional tag; nearest-neighbour fallback within a configurable distance; manual match and unmatch in the UI.
- Compare: horizontal deviation, vertical deviation, length, diameter, elevation, missing elements, extra elements, attribute differences.
- Tolerances are configurable per workspace and construction category. Ship editable defaults clearly labelled as PLACEHOLDERS to be confirmed against the project specification. Never hardcode tolerance values.
- Classes: Compliant, Within Tolerance, Deviation, Missing, Extra, Unverified.
- Authorized engineers may accept a deviation with a mandatory reason, or raise a rectification request.
- Show a clear warning when the as-built coordinate source is "Redrawn" or "Unknown".
- Complete audit trail.

---

# 13. CLASH DETECTION

- Horizontal intersection (STIntersects), vertical separation by depth/Z with configurable tolerance (default 0.5 m), 3D geometry intersection where data permits, category-specific clash rules.
- Display: location, involved assets, source drawings, severity, status, assigned engineer, resolution notes.
- Status: Open, Resolved, Ignored.
- One-click navigation to a clash in 2D and in 3D.
- Never treat a 2D crossing as a confirmed physical clash without considering vertical elevation and engineering tolerances.
- Re-run clash detection on as-built data.

---

# 14. REVISION AND APPROVAL MANAGEMENT

- Only the latest APPROVED revision appears by default in the official engineering layer.
- Keep previous revisions, history, comparison, restore, comments, rejection reasons, audit history.
- Roles: Workspace Admin, Uploader, Reviewer/Approver, Viewer.
- Never overwrite original drawings. A new upload must not replace the approved version until the approval process completes.

---

# 15. PREMIUM UI/UX REDESIGN

Redesign the entire frontend to look and feel like a premium professional application.

Quality references (do not copy branding or interfaces): Autodesk Construction Cloud, Cesium-based digital twins, Bentley infrastructure applications, ArcGIS professional applications, modern enterprise SaaS.

## 15.1 Design direction
A calm, premium, map-first interface: modern typography (Inter or similar variable font), consistent spacing on a 4/8 px grid, professional colours with one brand accent, excellent hierarchy, smooth interactions, one consistent icon set, responsive design.

Light and dark themes with WCAG AA contrast, a theme toggle and system-preference detection. Map and 3D UI adapt to both themes.

Design tokens (CSS variables) for colour, typography, spacing, radius, shadow, elevation, motion, component states.

## 15.2 Layout
- Top bar: organization switcher, workspace switcher, global search, notifications, help, profile.
- Left navigation rail: Overview, Projects, Master Plan, Digital Twin, Drawings, GFC Uploads, 3D Models, Drone Surveys, Approvals, Clash Detection, As-Built, Reports, Administration.
- Main: large GIS/3D viewport with a floating toolbar, collapsible left layer panel, right contextual panel, 2D/3D switch, navigation mode selector.
- Panels must not unnecessarily obstruct the map.

## 15.3 Redesign every screen
Login, workspace selection, dashboard, project management, master plan, map and digital twin, upload (stepper), drawings, revision history, approvals (review queue with inline viewer), clash detection, as-built compliance, reports, user management, workspace settings, layer configuration (with live style preview), administration (including failed jobs).

## 15.4 Interaction quality
Smooth transitions (150 to 250 ms, ease-out), skeleton loaders, helpful empty states with a next action, clear validation, meaningful error messages with a suggested fix, toasts, confirmation dialogs, undo (soft delete), keyboard navigation, global command palette (Ctrl/Cmd+K), contextual menus, responsive drawers, global search by project, drawing, chainage or coordinates, deep links to any view, keyboard-shortcut help.

WCAG 2.2 AA: keyboard navigation, visible focus, ARIA labels, colour is never the only signal. Respect prefers-reduced-motion. Responsive down to tablet; usable on mobile for viewing and approvals. i18n-ready, English first.

Create a reusable component library and a living design system (Storybook or equivalent). Provide Playwright screenshots of every key screen in both themes.

---

# 16. MULTI-TENANT GLOBAL ARCHITECTURE

Entities: Organization, Workspace, Country, Region, Project, Site, Construction Category, Discipline, Drawing, Drawing Revision, GFC Approval, Spatial Feature, 3D Model, Drone Survey, Utility Asset, Clash, As-Built Record, Compliance Result, User, Role, Audit Event.

Support multiple organizations, workspaces, projects, countries and coordinate systems; organization-specific categories; workspace-level and project-level permissions; secure collaboration.

An organization must never access another organization's private data without explicit permission. Every table carries the tenant/workspace key and every query is filtered by it.

---

# 17. SECURITY AND RELIABILITY

- OWASP Top 10 review; parameterized queries only; strict input validation.
- Server-side authorization on every endpoint; workspace isolation with automated tests.
- JWT with short-lived access tokens and refresh rotation; password policy; lockout.
- Rate limiting, CORS allow-list from configuration, CSP, HSTS, X-Content-Type-Options, frame-ancestors, HTTPS redirect.
- Secure file handling: extension allow-list, MIME and magic-byte validation, size limits, random storage names, no path traversal, never serve uploads from an executable path, optional antivirus hook.
- Never expose private drawings through public URLs; use secure temporary access URLs.
- Secrets only from configuration, environment variables or a secret store.
- Audit logging (logins, uploads, approvals, deviations, deletions, permission changes).
- Soft delete and restore.
- Global exception handling with RFC 7807 ProblemDetails and correlation IDs; no stack traces in production responses.
- Health checks (live, ready, database, storage, Hangfire); structured Serilog logging.
- Database transactions, optimistic concurrency, retryable and idempotent background jobs, failed-job visibility in the UI.
- Dependency vulnerability scans in CI (dotnet list package --vulnerable, npm audit).

---

# 18. PERFORMANCE

Support large projects and large geographical datasets:

- Spatial indexes (verify with query plans), bounding-box queries, viewport-based loading, pagination, geometry simplification, level of detail.
- 3D Tiles, tiled rendering, background CAD and IFC processing.
- Caching, lazy loading, efficient Angular change detection, memory management, a "low graphics" mode.
- Never load all project features into the browser at once.

Measure actual performance and report real numbers. Do not claim arbitrary FPS.

---

# 19. DATABASE AND MIGRATIONS

- EF Core migrations; preserve the existing database.
- Backward-compatible migrations; spatial columns and indexes; audit tables; revision relationships; workspace isolation; soft deletion; concurrency handling; transactional operations.
- Test migrations on (1) an empty database and (2) the existing database; make them reversible.
- If no database exists, create it from scratch, auto-apply migrations in Development only, and seed the default workspace, categories, layer mappings with styles, tolerances, and an admin user whose password comes from configuration.
- Verify spatial support on startup with a geometry query and log a clear error if it fails.
- Connection strings for LocalDB, SQL Express and Docker go in `appsettings.Example.json` and the README.
- Production migrations must be reviewed scripts.
- Never delete existing data or tables without approval.

---

# 20. AUTOMATED TESTING AND QUALITY GATES

Every phase must pass all gates before it is called done:

- Backend: `dotnet build` with zero errors and zero warnings (warnings as errors); unit, integration, authorization, spatial, upload and database tests pass.
- Frontend: `ng build --configuration production` with zero errors; strict TypeScript; ESLint clean; unit and component tests pass.
- End-to-end: Playwright (Chromium minimum) for the phase's critical journeys; no unexpected console errors; no failed network requests.
- Security tests: unauthorized access, cross-workspace access, malicious uploads (wrong extension, oversized, path traversal), invalid formats, JWT tampering, expired sessions.
- 3D tests: 2D/3D switching, camera navigation, orbit, walk, fly-to, underground visibility, model placement, saved viewpoints, tours, time of day, terrain restriction.
- Accessibility: axe (no serious or critical issues), keyboard navigation, contrast, screen reader support.
- Migrations apply cleanly on an empty and on the existing database.

Never fabricate test results. If a test cannot run because of missing infrastructure, say why.

---

# 21. TEST DATA (SYNTHETIC)

Preserve the existing `/gis_test_pack` (layer_mapping.csv, generate_test_dxf.py, workspace_config.json). Extend it. All test data is synthetic, in EPSG:32644 near lon 80.516, lat 16.513, and must be clearly labelled as TEST. Never present it as real project data.

Layers used: ROAD_CL, ROAD_EDGE, CHAINAGE, WATER_MAIN, SEWER_LINE, STORM_DRAIN, POWER_CABLE, BLDG_FOOTPRINT, BLDG_SITE, BLDG_PARKING, LANDSCAPE, BLDG_WALL, BLDG_CORE, BLDG_ROOM, BLDG_COLUMN, TEXT.

Valid DXFs:
- Road R1 with chainage 0+000 to 1+000.
- Water main, 1.5 m deep (Z = -1.5).
- Sewer, 1.7 m deep, crossing the water main.
- Storm drain, 3.5 m deep.
- Power cable, 0.9 m deep.
- Building footprint (large block) and an individual house footprint (12 m x 15 m).
- Access road leading to the house.
- Four-tower apartment site plan (STILT+G+10) with parking, landscape and internal roads, plus a typical floor plan of one tower.

Invalid DXFs:
- Local grid near (0,0), no real georeference.
- Valid coordinates about 30 km from the other data.
- Layer not present in the layer mapping.

As-built DXFs:
- Road identical except one edge offset 0.05 m.
- Water main shifted 0.6 m sideways and built 0.4 m deeper.
- Sewer identical to GFC.
- Storm drain omitted.
- Power cable plus one extra branch.

Also: a sample glTF building for placement tests, and sample drone survey data where available.

---

# 22. CONCRETE ACCEPTANCE TESTS

Write these as automated tests and also as a manual checklist.

1. **Placement and styling:** upload each valid DXF. Features appear at the correct location (near lon 80.516, lat 16.513) with layer-based styling and labels, over both street and satellite base maps.
2. **Clash detection:** exactly ONE clash, sewer (1.7 m deep) vs water main (1.5 m deep), vertical gap 0.2 m, with the 0.5 m tolerance. Storm drain (3.5 m) and power (0.9 m) crossing the sewer produce NO clash.
3. **Validation:** each invalid file is rejected with a different, clear message (no real coordinates / far from the project area / unmapped layer).
4. **Revisions:** a second revision of the water drawing replaces the first on the map only after approval; the history shows both.
5. **Roles and isolation:** a Viewer cannot upload or approve; workspace A cannot read workspace B (tested at API level).
6. **As-built:** road = Within Tolerance; water main = Deviation (horizontal and vertical); sewer = Compliant; storm drain = Missing; power = Extra. The compliance report exports to CSV and PDF. Accepting a deviation requires a reason and is audit logged. Changing a tolerance and re-running the job changes the classification accordingly. A "Redrawn" as-built shows the warning in the report.
7. **3D:** the four towers extrude to 11 floors (3 m per floor), marked as estimated, with shadows that change by time of day; WASD/QE/mouse controls work; the camera cannot go below terrain; utilities appear as 3D tubes with the clash marker at the water/sewer crossing; a recorded 5-waypoint tour plays back and loops; a glTF model can be placed, rotated and saved, and stays in place after refresh; toggling 2D/3D keeps location and selection.
8. **UI:** the upload stepper, command palette, light and dark themes and keyboard navigation work; axe reports no serious or critical issues.
9. **Quality:** no console errors or failed network requests in any e2e flow.
10. **Security:** upload abuse, tampered or expired JWTs and cross-workspace access are all rejected.

---

# 23. DEPLOYMENT, DOCUMENTATION AND COST

Provide:

- Dockerfiles for both apps and a docker-compose.yml (SQL Server with a persistent volume, API, web). The SA password comes from an environment variable or an ignored .env file.
- Environment-specific configuration, `appsettings.Example.json`, `environment.example.ts`.
- GitHub Actions CI per repo: restore, build, lint, test, e2e, vulnerability scan, artifact upload. Do not deploy automatically.
- README with exact setup steps (SQL Server or Docker, connection string, migrations, seed data, run API, run Angular app, log in), Mermaid architecture diagram, database diagram, API docs link, installation and troubleshooting guides, security documentation, backup and recovery documentation, release checklist.
- `CHANGELOG.md`, `assumptions.md`, `KNOWN_LIMITATIONS.md`.

**Licensing and cost.** List every third-party service and library that has usage limits or license terms (Cesium ion, public tile servers such as OpenStreetMap and Esri imagery, ODA File Converter, xBIM, any paid component). For each, state the cost at production scale and the free or self-hosted alternative. Note that free public tile servers are for development only.

Never commit credentials or real construction drawings.

---

# 24. REPOSITORIES

- Frontend: https://github.com/nagaseshareddy112416-sudo/MP
- Backend: https://github.com/nagaseshareddy112416-sudo/MP-API

Keep them separate.

- Frontend repo: Angular app, UI/UX redesign, OpenLayers, CesiumJS, component library, frontend tests.
- Backend repo: .NET API, database, migrations, GIS and CAD processing, background jobs, authorization, integration tests, `/gis_test_pack`.

Do not change the repository structure without a clear architectural reason. Do not push without authorization and passing quality checks. Commit small, logical changes with clear messages. Before every push run build, tests and a secret scan. If git authentication fails, stop and tell me what you need. After each push show commit hashes and links.

---

# 25. STRICT IMPLEMENTATION PHASES

## PHASE 0: COMPLETE TECHNICAL AUDIT (mandatory)

Audit both repositories before any change.

**Evidence rule.** For every finding, cite the file path, command or output that proves it. Run the existing build and test commands (`dotnet build`, `dotnet test`, `npm ci`, `ng build`, `ng test`) and include their real output. These may create bin, obj or dist folders but must NOT modify tracked source files, configuration or the database. If a command cannot run, state why. Mark any claim you could not verify as **UNVERIFIED**.

Inspect: folder structure, modules, backend and frontend architecture, database schema and migrations, API endpoints, GIS implementation, CAD upload implementation, drawing rendering, existing 3D functionality, authentication and authorization, security, UI/UX, build errors, test coverage, performance, dependencies.

Identify: what works, what is incomplete, what is broken, what is insecure, what needs redesign, what to preserve, what to add.

Deliver:

1. Frontend audit report
2. Backend audit report
3. Database audit report
4. Security audit
5. GIS and CAD audit
6. 3D digital twin audit
7. UI/UX audit
8. Technical risk register
9. Missing features list
10. Dependency compatibility report
11. Licensing and cost report
12. Implementation plan
13. Realistic timeline
14. Resource requirements
15. Assumptions and known limitations

Then ask me any questions you need and WAIT for my approval. Do not modify code.

## PHASE 1: STABILIZATION
Fix build errors, warnings, broken flows and security problems. Add missing tests. Preserve existing behaviour. Set up reliable development and test environments, Docker and CI.

## PHASE 2: PREMIUM DESIGN SYSTEM
Design tokens, light and dark themes, navigation, map-first layout, responsive shell, reusable components, command palette, component documentation.

## PHASE 3: DRAWING AND UPLOAD EXPERIENCE
Guided upload, file validation, CAD processing, georeferencing, metadata extraction, preview, approval workflow, revision management.

## PHASE 4: ADVANCED GIS
Layer styling, geometry rendering, labels, legends, CAD viewer, spatial search, performance optimization.

## PHASE 5: ENGINEERING INTELLIGENCE
Asset extraction, clash detection, GFC/as-built comparison, compliance reports, dashboard, exports.

## PHASE 6A: 3D FOUNDATION
CesiumJS integration, 2D/3D switch, terrain, imagery, footprint extrusion (estimated heights), model upload and placement (glTF/GLB/OBJ, IFC conversion).

## PHASE 6B: UNDERGROUND AND 3D CLASHES
Underground utilities as 3D tubes, ground transparency, depth information, 3D clash markers and navigation.

## PHASE 6C: DRONE-STYLE NAVIGATION
Free-fly, orbit, walk and fly-to modes, drone HUD, terrain collision, touch support.

## PHASE 6D: TOURS AND ENVIRONMENT
Saved viewpoints, fly-through tours, time of day and shadows, 3D measurements, section clipping, colour modes, GFC vs as-built compare in 3D.

## PHASE 7: DRONE SURVEY AND 4D
Drone imagery ingestion, photogrammetry integration, orthomosaics, point clouds, 3D Tiles, historical survey comparison, time-based digital twin, planned vs actual.

## PHASE 8: FINAL PRODUCTION HARDENING
Performance optimization, security verification, accessibility verification, documentation, deployment readiness, backup and recovery validation, complete regression testing.

---

# 26. NON-NEGOTIABLE OPERATING RULES

1. Never start coding before the Phase 0 audit is approved.
2. Work on one phase at a time.
3. After every phase, run builds, tests and browser tests.
4. Show actual test output.
5. Fix failures before marking the phase complete.
6. Stop and request my confirmation after every phase.
7. Never claim something works unless it has actually been tested.
8. Never fabricate test results, command output or file contents.
9. Never leave TODOs, placeholder code, swallowed exceptions, commented-out code, or mocked logic presented as real. List anything deliberately out of scope in KNOWN_LIMITATIONS.md.
10. Never silently remove existing functionality; run regression tests.
11. Never introduce breaking database changes without approval.
12. Never compromise authorization or tenant isolation.
13. Never hardcode a specific city, country or project.
14. Never represent estimated geometry as surveyed or verified geometry.
15. Never represent an approximate 3D extrusion as an accurate BIM model.
16. Never commit secrets or real drawings.
17. Maintain CHANGELOG.md, assumptions.md and KNOWN_LIMITATIONS.md.
18. Prefer fixing root causes over temporary workarounds.
19. Ask me questions when a requirement is ambiguous.
20. Re-read this prompt at the start of every phase.

When a feature depends on unavailable credentials, licensed converters, external services or missing hardware, identify the dependency and provide a genuine implementation path.

Do not promise zero errors in advance. Demonstrate quality through repeatable builds, automated tests and verified acceptance criteria.

## Deliverables at the end of each phase
Working code pushed to the repos (after my confirmation), test results (unit, integration, e2e), screenshots for UI phases, updated CHANGELOG, assumptions and known limitations, and the exact steps I can follow to verify the phase myself.

---

# 27. FINAL PRODUCT EXPECTATION

A user can open a global map, choose any authorized construction project, go to its actual geographical location, switch into 3D and virtually explore the site.

They can:

- Fly around a building like a drone.
- View the building from different heights.
- Walk around the site.
- Inspect the building and its surroundings.
- View underground infrastructure.
- Open the approved GFC drawings.
- Inspect individual engineering assets.
- Compare GFC with as-built conditions.
- View historical construction stages.
- Compare drone survey data.
- Identify potential clashes.
- Understand the relationship between all construction elements.

The goal is to turn conventional engineering drawings into a connected, geographically accurate and continuously evolving digital representation of the built environment. This must be a real engineering information platform, not a map displaying drawing markers.

---

# START NOW

Begin with PHASE 0 ONLY.

Audit the frontend and backend repositories with evidence, as described in Phase 0. Do not modify any tracked source code, database, migrations or configuration.

Return the complete audit report and a realistic, dependency-aware implementation roadmap, ask any questions you have, and wait for my explicit approval before starting Phase 1.

