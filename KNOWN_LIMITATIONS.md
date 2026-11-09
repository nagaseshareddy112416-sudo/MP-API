# Known Limitations - MP-API (Backend)

The following known limitations and planned enhancements are tracked here:

1. **DWG Converter Requirement**:
   - Native DWG binary files require the Teigha / ODA File Converter CLI installed on the host system. If not installed, DWG files are rejected with a clear user-facing error advising conversion to DXF.

2. **Spatial Index Bounding Boxes**:
   - SQL Server spatial indexes require an explicit bounding box for projected coordinate systems. Workspace boundaries should be defined to optimize spatial index performance.

3. **Background Job Persistence**:
   - In development environments using LocalDB or in-memory job queues, jobs do not survive app restarts. In production with SQL Server Hangfire storage, full persistence and retries are active.

4. **3D Volumetric Extrusion**:
   - Phase 1 currently processes 2D/3D polylines and footprints. Full 3D mesh generation (IFC/glTF) is scheduled for Phase 7.

