import os
import struct
import json
import math
from PIL import Image, ImageDraw, ImageFont
import ezdxf

PACK_DIR = os.path.join(os.path.dirname(__file__), "house_test_pack")
os.makedirs(PACK_DIR, exist_ok=True)

E0, N0 = 448350, 1825744   # UTM 44N Amaravati origin

# -------------------------------------------------------------
# 1. GENERATE DXF SITE PLAN: house_site_plan.dxf
# -------------------------------------------------------------
def generate_dxf():
    dxf_path = os.path.join(PACK_DIR, "TEST_house_site_plan.dxf")
    doc = ezdxf.new("R2010")
    doc.units = ezdxf.units.M

    for layer_name in ["BLDG_FOOTPRINT", "PAVING", "LAWN", "WATER_FEATURE", "TREE_BLOCK", "TEXT"]:
        doc.layers.add(layer_name)

    msp = doc.modelspace()

    # House Footprint: 10m (East-West) x 8m (North-South)
    # Centered at (E0 - 200, N0 + 100)
    hx, hy = E0 - 200, N0 + 100
    footprint_pts = [
        (hx, hy),
        (hx + 10, hy),
        (hx + 10, hy + 8),
        (hx, hy + 8),
        (hx, hy) # closed
    ]
    msp.add_lwpolyline(footprint_pts, dxfattribs={"layer": "BLDG_FOOTPRINT", "closed": True})
    msp.add_text("TWO-STOREY RESIDENCE GFC R0 (10m x 8m)", dxfattribs={"layer": "TEXT", "height": 1.2}).set_placement((hx, hy + 9))

    # Paving / Driveway: 6m x 14m leading south
    paving_pts = [
        (hx + 2, hy),
        (hx + 8, hy),
        (hx + 8, hy - 14),
        (hx + 2, hy - 14),
        (hx + 2, hy)
    ]
    msp.add_lwpolyline(paving_pts, dxfattribs={"layer": "PAVING", "closed": True})

    # Lawn: 30m x 25m boundary enclosing the plot
    lawn_pts = [
        (hx - 10, hy - 15),
        (hx + 20, hy - 15),
        (hx + 20, hy + 18),
        (hx - 10, hy + 18),
        (hx - 10, hy - 15)
    ]
    msp.add_lwpolyline(lawn_pts, dxfattribs={"layer": "LAWN", "closed": True})

    # Water Feature: 4m x 3m ornamental pond in the east lawn
    water_pts = [
        (hx + 13, hy + 3),
        (hx + 17, hy + 3),
        (hx + 17, hy + 6),
        (hx + 13, hy + 6),
        (hx + 13, hy + 3)
    ]
    msp.add_lwpolyline(water_pts, dxfattribs={"layer": "WATER_FEATURE", "closed": True})

    # Tree Blocks: 4 trees around the perimeter
    trees = [
        (hx - 6, hy - 8),
        (hx - 6, hy + 12),
        (hx + 16, hy + 12),
        (hx + 16, hy - 8)
    ]
    for tx, ty in trees:
        msp.add_point((tx, ty), dxfattribs={"layer": "TREE_BLOCK"})
        msp.add_circle((tx, ty), radius=2.5, dxfattribs={"layer": "TREE_BLOCK"})

    doc.saveas(dxf_path)
    print(f"Generated {dxf_path}")

# -------------------------------------------------------------
# 2. GENERATE VALID BINARY glTF 2.0 (GLB): house_model.glb
# -------------------------------------------------------------
def build_glb(vertices, indices, materials, filename):
    # vertices: list of [x, y, z]
    # indices: list of ints (triangles)
    vertex_bytes = bytearray()
    min_x, max_x = float('inf'), float('-inf')
    min_y, max_y = float('inf'), float('-inf')
    min_z, max_z = float('inf'), float('-inf')

    for v in vertices:
        x, y, z = v
        min_x, max_x = min(min_x, x), max(max_x, x)
        min_y, max_y = min(min_y, y), max(max_y, y)
        min_z, max_z = min(min_z, z), max(max_z, z)
        vertex_bytes.extend(struct.pack('<fff', x, y, z))

    index_bytes = bytearray()
    min_idx, max_idx = min(indices), max(indices)
    for idx in indices:
        index_bytes.extend(struct.pack('<H', idx))

    # Pad vertex bytes to 4-byte boundary
    while len(vertex_bytes) % 4 != 0:
        vertex_bytes.append(0)
    # Pad index bytes to 4-byte boundary
    while len(index_bytes) % 4 != 0:
        index_bytes.append(0)

    bin_data = vertex_bytes + index_bytes

    gltf_json = {
        "asset": {"version": "2.0", "generator": "GfcEngine Synthetic House Generator"},
        "scenes": [{"nodes": [0]}],
        "nodes": [{"mesh": 0, "name": "Synthetic_House_Structure"}],
        "meshes": [{
            "name": "HouseMesh",
            "primitives": [{
                "attributes": {"POSITION": 0},
                "indices": 1,
                "material": 0
            }]
        }],
        "materials": [{
            "name": "PBR_Finished_Look",
            "pbrMetallicRoughness": {
                "baseColorFactor": [0.85, 0.72, 0.60, 1.0],
                "metallicFactor": 0.1,
                "roughnessFactor": 0.65
            }
        }],
        "accessors": [
            {
                "bufferView": 0,
                "byteOffset": 0,
                "componentType": 5126, # FLOAT
                "count": len(vertices),
                "type": "VEC3",
                "max": [max_x, max_y, max_z],
                "min": [min_x, min_y, min_z]
            },
            {
                "bufferView": 1,
                "byteOffset": 0,
                "componentType": 5123, # UNSIGNED_SHORT
                "count": len(indices),
                "type": "SCALAR",
                "max": [max_idx],
                "min": [min_idx]
            }
        ],
        "bufferViews": [
            {
                "buffer": 0,
                "byteOffset": 0,
                "byteLength": len(vertex_bytes),
                "target": 34962 # ARRAY_BUFFER
            },
            {
                "buffer": 0,
                "byteOffset": len(vertex_bytes),
                "byteLength": len(index_bytes),
                "target": 34963 # ELEMENT_ARRAY_BUFFER
            }
        ],
        "buffers": [{
            "byteLength": len(bin_data)
        }]
    }

    json_str = json.dumps(gltf_json, separators=(',', ':'))
    json_bytes = json_str.encode('utf-8')
    while len(json_bytes) % 4 != 0:
        json_bytes += b' '

    chunk0_type = 0x4E4F534A # 'JSON'
    chunk0_len = len(json_bytes)

    chunk1_type = 0x004E4942 # 'BIN\0'
    chunk1_len = len(bin_data)

    total_len = 12 + 8 + chunk0_len + 8 + chunk1_len

    header = struct.pack('<4sII', b'glTF', 2, total_len)
    chunk0_header = struct.pack('<II', chunk0_len, chunk0_type)
    chunk1_header = struct.pack('<II', chunk1_len, chunk1_type)

    out_path = os.path.join(PACK_DIR, filename)
    with open(out_path, 'wb') as f:
        f.write(header)
        f.write(chunk0_header)
        f.write(json_bytes)
        f.write(chunk1_header)
        f.write(bin_data)
    print(f"Generated GLB: {out_path} ({total_len} bytes)")

def generate_house_glb():
    # 10m (X: -5 to +5), 8m (Z: -4 to +4), 6m walls (Y: 0 to 6), 2.5m pitched roof (Y: 8.5)
    w = 5.0  # half-width 10m
    d = 4.0  # half-depth 8m
    h_wall = 6.0
    h_roof = 8.5

    # 10 vertices
    # 0-3: Ground floor base
    # 4-7: Eaves level
    # 8-9: Roof ridge apex
    verts = [
        [-w, 0.0, -d], # 0
        [ w, 0.0, -d], # 1
        [ w, 0.0,  d], # 2
        [-w, 0.0,  d], # 3
        [-w, h_wall, -d], # 4
        [ w, h_wall, -d], # 5
        [ w, h_wall,  d], # 6
        [-w, h_wall,  d], # 7
        [-w, h_roof, 0.0], # 8 ridge west
        [ w, h_roof, 0.0]  # 9 ridge east
    ]

    indices = [
        # Front wall (South, +Z)
        3, 2, 6,  3, 6, 7,
        # Back wall (North, -Z)
        1, 0, 4,  1, 4, 5,
        # West wall (X = -w)
        0, 3, 7,  0, 7, 4,
        # West gable triangle
        7, 8, 4,
        # East wall (X = +w)
        2, 1, 5,  2, 5, 6,
        # East gable triangle
        5, 9, 6,
        # South roof pitch (between eaves 7-6 and ridge 8-9)
        7, 6, 9,  7, 9, 8,
        # North roof pitch (between eaves 4-5 and ridge 8-9)
        5, 4, 8,  5, 8, 9,
        # Floor base
        0, 1, 2,  0, 2, 3
    ]

    build_glb(verts, indices, None, "TEST_house_model.glb")

def generate_tree_glb():
    # Simple low-poly tree: trunk + 2 conical canopy tiers
    verts = [
        # Trunk (height 2m, radius 0.3)
        [-0.3, 0.0, -0.3], [0.3, 0.0, -0.3], [0.3, 0.0, 0.3], [-0.3, 0.0, 0.3],
        [-0.2, 2.0, -0.2], [0.2, 2.0, -0.2], [0.2, 2.0, 0.2], [-0.2, 2.0, 0.2],
        # Lower Canopy (height 2m to 5m, radius 2.0)
        [-2.0, 2.0, -2.0], [2.0, 2.0, -2.0], [2.0, 2.0, 2.0], [-2.0, 2.0, 2.0],
        [ 0.0, 5.0,  0.0], # Apex 1
        # Upper Canopy (height 4m to 7m, radius 1.4)
        [-1.4, 4.0, -1.4], [1.4, 4.0, -1.4], [1.4, 4.0, 1.4], [-1.4, 4.0, 1.4],
        [ 0.0, 7.0,  0.0]  # Apex 2
    ]
    indices = [
        # Trunk
        0, 1, 5, 0, 5, 4,  1, 2, 6, 1, 6, 5,  2, 3, 7, 2, 7, 6,  3, 0, 4, 3, 4, 7,
        # Lower Canopy Cone
        8, 9, 12,  9, 10, 12,  10, 11, 12,  11, 8, 12,
        # Upper Canopy Cone
        13, 14, 17,  14, 15, 17,  15, 16, 17,  16, 13, 17
    ]
    build_glb(verts, indices, None, "TEST_tree_model.glb")

# -------------------------------------------------------------
# 3. GENERATE RENDERS: Top-view, Elevation, Perspective
# -------------------------------------------------------------
def generate_images():
    # Top-view plan render
    w, h = 600, 500
    top = Image.new("RGBA", (w, h), (76, 175, 80, 255)) # Lush Green lawn base
    draw = ImageDraw.Draw(top)

    # Driveway / paving in stone grey
    draw.rectangle([220, 260, 380, 480], fill=(160, 160, 165, 255), outline=(120, 120, 125, 255), width=3)
    # Garden pond in azure blue
    draw.ellipse([430, 140, 530, 220], fill=(33, 150, 243, 240), outline=(2, 119, 189, 255), width=4)
    # House Roof in terracotta
    draw.rectangle([180, 100, 420, 260], fill=(185, 80, 55, 255), outline=(130, 40, 20, 255), width=4)
    draw.line([300, 100, 300, 260], fill=(130, 40, 20, 255), width=3) # Ridge line
    # Trees
    for tx, ty in [(100, 100), (100, 400), (500, 380), (500, 80)]:
        draw.ellipse([tx-35, ty-35, tx+35, ty+35], fill=(46, 125, 50, 230), outline=(27, 94, 32, 255), width=3)

    top_path = os.path.join(PACK_DIR, "TEST_plot_top_render.png")
    top.save(top_path)
    print(f"Generated top-view render: {top_path}")

    # Elevation South
    elev = Image.new("RGBA", (700, 450), (225, 245, 254, 255)) # Sky
    edraw = ImageDraw.Draw(elev)
    # Ground
    edraw.rectangle([0, 360, 700, 450], fill=(76, 175, 80, 255))
    # Wall
    edraw.rectangle([150, 160, 550, 360], fill=(222, 184, 135, 255), outline=(139, 69, 19, 255), width=3)
    # Roof
    edraw.polygon([(130, 160), (350, 70), (570, 160)], fill=(185, 80, 55, 255), outline=(130, 40, 20, 255))
    # Door
    edraw.rectangle([320, 260, 380, 360], fill=(101, 67, 33, 255))
    # Windows
    edraw.rectangle([200, 200, 260, 260], fill=(179, 229, 252, 255), outline=(60, 60, 60, 255), width=2)
    edraw.rectangle([440, 200, 500, 260], fill=(179, 229, 252, 255), outline=(60, 60, 60, 255), width=2)
    edraw.rectangle([200, 280, 260, 340], fill=(179, 229, 252, 255), outline=(60, 60, 60, 255), width=2)
    edraw.rectangle([440, 280, 500, 340], fill=(179, 229, 252, 255), outline=(60, 60, 60, 255), width=2)
    elev_path = os.path.join(PACK_DIR, "TEST_elevation_south.png")
    elev.save(elev_path)
    print(f"Generated elevation render: {elev_path}")

    # Perspective render
    persp = Image.new("RGBA", (700, 450), (200, 230, 255, 255))
    pdraw = ImageDraw.Draw(persp)
    pdraw.rectangle([0, 330, 700, 450], fill=(90, 160, 70, 255))
    # Angular house facade
    pdraw.polygon([(180, 230), (360, 170), (360, 350), (180, 380)], fill=(210, 175, 130, 255))
    pdraw.polygon([(360, 170), (540, 220), (540, 360), (360, 350)], fill=(235, 195, 150, 255))
    # Roof
    pdraw.polygon([(160, 230), (360, 90), (560, 220), (360, 170)], fill=(185, 75, 45, 255))
    persp_path = os.path.join(PACK_DIR, "TEST_perspective_cam1.png")
    persp.save(persp_path)
    print(f"Generated perspective render: {persp_path}")

if __name__ == "__main__":
    generate_dxf()
    generate_house_glb()
    generate_tree_glb()
    generate_images()
    print("FINISHED-LOOK SYNTHETIC HOUSE TEST PACK COMPLETE.")
