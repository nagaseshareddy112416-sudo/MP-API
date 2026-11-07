import os
import ezdxf

E0, N0 = 448350, 1825744   # UTM 44N origin near 16.513N, 80.516E
OUT = os.path.join(os.path.dirname(__file__), "")
os.makedirs(OUT + "gfc_dxf", exist_ok=True)
os.makedirs(OUT + "gfc_dxf_invalid", exist_ok=True)
os.makedirs(OUT + "asbuilt_dxf", exist_ok=True)

def L(x, y):
    return (E0 + x, N0 + y)

def new_doc(layers):
    doc = ezdxf.new("R2010")
    doc.units = ezdxf.units.M
    for n in layers:
        doc.layers.add(n)
    return doc, doc.modelspace()

# 1. Road R1, chainage 0+000 to 1+000
doc, m = new_doc(["ROAD_CL", "ROAD_EDGE", "CHAINAGE", "TEXT"])
m.add_lwpolyline([L(-500, 0), L(500, 0)], dxfattribs={"layer": "ROAD_CL"})
m.add_lwpolyline([L(-500, 15), L(500, 15)], dxfattribs={"layer": "ROAD_EDGE"})
m.add_lwpolyline([L(-500, -15), L(500, -15)], dxfattribs={"layer": "ROAD_EDGE"})
for k in range(0, 1001, 100):
    p = L(-500 + k, 0)
    m.add_point(p, dxfattribs={"layer": "CHAINAGE"})
    m.add_text(f"{k//1000}+{k%1000:03d}",
               dxfattribs={"layer": "CHAINAGE", "height": 4}).set_placement((p[0], p[1] + 20))
m.add_text("GFC R1 MAIN AVENUE CH 0+000 TO 1+000 REV R0",
           dxfattribs={"layer": "TEXT", "height": 6}).set_placement(L(-500, 60))
doc.saveas(OUT + "gfc_dxf/GFC_ROAD_R1_CH0000-1000_R0.dxf")

# 2. Water main, depth 1.5 m
doc, m = new_doc(["WATER_MAIN", "TEXT"])
m.add_polyline3d([(*L(-500, 8), -1.5), (*L(500, 8), -1.5)], dxfattribs={"layer": "WATER_MAIN"})
m.add_text("WATER MAIN DN300 DI DEPTH 1.5m",
           dxfattribs={"layer": "TEXT", "height": 5}).set_placement(L(-500, 40))
doc.saveas(OUT + "gfc_dxf/GFC_WATER_R1_CH0000-1000_R0.dxf")

# 3. Sewer, depth 1.7 m, crosses water main at (0, 8) -> CLASH expected
doc, m = new_doc(["SEWER_LINE", "TEXT"])
m.add_polyline3d([(*L(0, -200), -1.7), (*L(0, 200), -1.7)], dxfattribs={"layer": "SEWER_LINE"})
m.add_text("SEWER DN400 RCC DEPTH 1.7m",
           dxfattribs={"layer": "TEXT", "height": 5}).set_placement(L(10, 200))
doc.saveas(OUT + "gfc_dxf/GFC_SEWER_R2_CH0000-0400_R0.dxf")

# 4. Storm drain, depth 3.5 m, crosses sewer at (0, -12) -> NO clash (gap 1.8 m)
doc, m = new_doc(["STORM_DRAIN", "TEXT"])
m.add_polyline3d([(*L(-300, -12), -3.5), (*L(300, -12), -3.5)], dxfattribs={"layer": "STORM_DRAIN"})
doc.saveas(OUT + "gfc_dxf/GFC_STORM_R1_CH0200-0800_R0.dxf")

# 5. Power cable, depth 0.9 m, crosses sewer at (0, -8) -> NO clash (gap 0.8 m)
doc, m = new_doc(["POWER_CABLE"])
m.add_polyline3d([(*L(-500, -8), -0.9), (*L(500, -8), -0.9)], dxfattribs={"layer": "POWER_CABLE"})
doc.saveas(OUT + "gfc_dxf/GFC_POWER_R1_CH0000-1000_R0.dxf")

# 6. Building footprint (closed polyline)
doc, m = new_doc(["BLDG_FOOTPRINT", "TEXT"])
x, y = -1400, -1100
m.add_lwpolyline([L(x+10, y+10), L(x+90, y+10), L(x+90, y+100), L(x+10, y+100)],
                 close=True, dxfattribs={"layer": "BLDG_FOOTPRINT"})
m.add_text("G+2 RESIDENTIAL BLOCK PLOT P-001",
           dxfattribs={"layer": "TEXT", "height": 4}).set_placement(L(x+10, y+105))
doc.saveas(OUT + "gfc_dxf/GFC_BUILDING_P-001_R0.dxf")

# INVALID A: local grid near (0,0), no real georeference
doc, m = new_doc(["ROAD_CL"])
m.add_lwpolyline([(0, 0), (1000, 0)], dxfattribs={"layer": "ROAD_CL"})
doc.saveas(OUT + "gfc_dxf_invalid/INVALID_local_grid_no_coordinates.dxf")

# INVALID B: valid UTM coordinates but ~30 km away from the other data
doc, m = new_doc(["ROAD_CL"])
m.add_lwpolyline([(E0 + 30000, N0), (E0 + 31000, N0)], dxfattribs={"layer": "ROAD_CL"})
doc.saveas(OUT + "gfc_dxf_invalid/INVALID_far_from_project_area.dxf")

# INVALID C: layer not present in the layer mapping
doc, m = new_doc(["XYZ_UNKNOWN"])
m.add_lwpolyline([L(-100, 300), L(100, 300)], dxfattribs={"layer": "XYZ_UNKNOWN"})
doc.saveas(OUT + "gfc_dxf_invalid/INVALID_unmapped_layer.dxf")

# ============================================================
# PHASE 9 AS-BUILT DXF GENERATION
# ============================================================

# 1. Road R1 As-Built: Centerline and north edge match GFC; south edge offset by 0.05m -> Within Tolerance
doc, m = new_doc(["ROAD_CL", "ROAD_EDGE", "CHAINAGE", "TEXT"])
m.add_lwpolyline([L(-500, 0), L(500, 0)], dxfattribs={"layer": "ROAD_CL"})
m.add_lwpolyline([L(-500, 15), L(500, 15)], dxfattribs={"layer": "ROAD_EDGE"})
m.add_lwpolyline([L(-500, -15.05), L(500, -15.05)], dxfattribs={"layer": "ROAD_EDGE"}) # 0.05m deviation
for k in range(0, 1001, 100):
    p = L(-500 + k, 0)
    m.add_point(p, dxfattribs={"layer": "CHAINAGE"})
    m.add_text(f"{k//1000}+{k%1000:03d}",
               dxfattribs={"layer": "CHAINAGE", "height": 4}).set_placement((p[0], p[1] + 20))
m.add_text("AS-BUILT R1 MAIN AVENUE CH 0+000 TO 1+000",
           dxfattribs={"layer": "TEXT", "height": 6}).set_placement(L(-500, 60))
doc.saveas(OUT + "asbuilt_dxf/ASBUILT_ROAD_R1_CH0000-1000.dxf")

# 2. Water Main As-Built: Shifted 0.6 m sideways (y = 8.6 instead of 8.0) and depth 1.9 m (Z = -1.9 instead of -1.5) -> Deviation
doc, m = new_doc(["WATER_MAIN", "TEXT"])
m.add_polyline3d([(*L(-500, 8.6), -1.9), (*L(500, 8.6), -1.9)], dxfattribs={"layer": "WATER_MAIN"})
m.add_text("AS-BUILT WATER MAIN DN300 DI DEPTH 1.9m",
           dxfattribs={"layer": "TEXT", "height": 5}).set_placement(L(-500, 40))
doc.saveas(OUT + "asbuilt_dxf/ASBUILT_WATER_R1_CH0000-1000.dxf")

# 3. Sewer As-Built: Identical to GFC at (0, -200) to (0, 200), depth 1.7 m -> Compliant
doc, m = new_doc(["SEWER_LINE", "TEXT"])
m.add_polyline3d([(*L(0, -200), -1.7), (*L(0, 200), -1.7)], dxfattribs={"layer": "SEWER_LINE"})
m.add_text("AS-BUILT SEWER DN400 RCC DEPTH 1.7m",
           dxfattribs={"layer": "TEXT", "height": 5}).set_placement(L(10, 200))
doc.saveas(OUT + "asbuilt_dxf/ASBUILT_SEWER_R2_CH0000-0400.dxf")

# 4. Storm Drain As-Built: Intentionally OMITTED -> GFC Storm Drain will be classified as 'Missing'

# 5. Power Cable As-Built: Identical main run (y = -8, depth 0.9m), plus one extra short branch from (100, -8) to (100, 30) -> 'Extra'
doc, m = new_doc(["POWER_CABLE"])
m.add_polyline3d([(*L(-500, -8), -0.9), (*L(500, -8), -0.9)], dxfattribs={"layer": "POWER_CABLE"})
m.add_polyline3d([(*L(100, -8), -0.9), (*L(100, 30), -0.9)], dxfattribs={"layer": "POWER_CABLE"}) # Extra branch
doc.saveas(OUT + "asbuilt_dxf/ASBUILT_POWER_R1_CH0000-1000.dxf")

print("All GFC and Phase 9 As-Built test DXF files created successfully.")

