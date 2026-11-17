"""
Generates two small demo "site scans" as .glb (binary glTF) files, with no dependencies.

  samples/site-scan-week1.glb  - an empty room (floor + walls)
  samples/site-scan-week2.glb  - the same room two weeks later: a column and a partition wall built

Upload both to one site in ScanVault to see the scan history and compare progress.
Run:  python3 tools/make_sample_scans.py
"""
import json
import struct
from pathlib import Path


def box(center, size, color):
    """Returns (positions, normals, colors, indices) for an axis-aligned box."""
    cx, cy, cz = center
    sx, sy, sz = (s / 2 for s in size)
    faces = [
        ((1, 0, 0), [(sx, -sy, -sz), (sx, sy, -sz), (sx, sy, sz), (sx, -sy, sz)]),
        ((-1, 0, 0), [(-sx, -sy, sz), (-sx, sy, sz), (-sx, sy, -sz), (-sx, -sy, -sz)]),
        ((0, 1, 0), [(-sx, sy, -sz), (-sx, sy, sz), (sx, sy, sz), (sx, sy, -sz)]),
        ((0, -1, 0), [(-sx, -sy, sz), (-sx, -sy, -sz), (sx, -sy, -sz), (sx, -sy, sz)]),
        ((0, 0, 1), [(sx, -sy, sz), (sx, sy, sz), (-sx, sy, sz), (-sx, -sy, sz)]),
        ((0, 0, -1), [(-sx, -sy, -sz), (-sx, sy, -sz), (sx, sy, -sz), (sx, -sy, -sz)]),
    ]
    positions, normals, colors, indices = [], [], [], []
    for normal, corners in faces:
        start = len(positions)
        for x, y, z in corners:
            positions.append((cx + x, cy + y, cz + z))
            normals.append(normal)
            colors.append(color)
        indices += [start, start + 2, start + 1, start, start + 3, start + 2]
    return positions, normals, colors, indices


def room(progress):
    concrete, wall, column, partition = (0.62, 0.62, 0.6), (0.85, 0.83, 0.78), (0.55, 0.57, 0.62), (0.76, 0.6, 0.45)
    parts = [
        box((0, -0.05, 0), (8, 0.1, 6), concrete),          # floor slab
        box((0, 1.4, -3), (8, 2.8, 0.15), wall),            # back wall
        box((-4, 1.4, 0), (0.15, 2.8, 6), wall),            # left wall
        box((4, 1.4, 0), (0.15, 2.8, 6), wall),             # right wall
    ]
    if progress:
        parts += [
            box((-1.5, 1.4, -0.5), (0.4, 2.8, 0.4), column),   # new column
            box((1.5, 1.4, -0.5), (0.4, 2.8, 0.4), column),    # new column
            box((2.2, 0.9, 1.2), (3.4, 1.8, 0.12), partition), # half-built partition wall
        ]
    return parts


def write_glb(path, parts):
    positions, normals, colors, indices = [], [], [], []
    for p, n, c, i in parts:
        offset = len(positions)
        positions += p
        normals += n
        colors += c
        indices += [offset + k for k in i]

    def pack(fmt, rows):
        return b"".join(struct.pack(fmt, *r) for r in rows)

    pos_bytes = pack("<3f", positions)
    nrm_bytes = pack("<3f", normals)
    col_bytes = pack("<3f", colors)
    idx_bytes = struct.pack(f"<{len(indices)}H", *indices)
    idx_bytes += b"\x00" * ((4 - len(idx_bytes) % 4) % 4)
    blob = pos_bytes + nrm_bytes + col_bytes + idx_bytes

    views, offset = [], 0
    for data, target in [(pos_bytes, 34962), (nrm_bytes, 34962), (col_bytes, 34962), (idx_bytes, 34963)]:
        views.append({"buffer": 0, "byteOffset": offset, "byteLength": len(data), "target": target})
        offset += len(data)

    mins = [min(p[k] for p in positions) for k in range(3)]
    maxs = [max(p[k] for p in positions) for k in range(3)]
    gltf = {
        "asset": {"version": "2.0", "generator": "ScanVault sample generator"},
        "scene": 0,
        "scenes": [{"nodes": [0]}],
        "nodes": [{"mesh": 0, "name": "SiteScan"}],
        "meshes": [{"primitives": [{
            "attributes": {"POSITION": 0, "NORMAL": 1, "COLOR_0": 2},
            "indices": 3,
            "material": 0
        }]}],
        "materials": [{"pbrMetallicRoughness": {"baseColorFactor": [1, 1, 1, 1], "metallicFactor": 0, "roughnessFactor": 0.9}}],
        "buffers": [{"byteLength": len(blob)}],
        "bufferViews": views,
        "accessors": [
            {"bufferView": 0, "componentType": 5126, "count": len(positions), "type": "VEC3", "min": mins, "max": maxs},
            {"bufferView": 1, "componentType": 5126, "count": len(normals), "type": "VEC3"},
            {"bufferView": 2, "componentType": 5126, "count": len(colors), "type": "VEC3"},
            {"bufferView": 3, "componentType": 5123, "count": len(indices), "type": "SCALAR"},
        ],
    }

    json_bytes = json.dumps(gltf, separators=(",", ":")).encode()
    json_bytes += b" " * ((4 - len(json_bytes) % 4) % 4)
    total = 12 + 8 + len(json_bytes) + 8 + len(blob)
    with open(path, "wb") as f:
        f.write(struct.pack("<4sII", b"glTF", 2, total))
        f.write(struct.pack("<I4s", len(json_bytes), b"JSON") + json_bytes)
        f.write(struct.pack("<I4s", len(blob), b"BIN\x00") + blob)


if __name__ == "__main__":
    out = Path(__file__).resolve().parent.parent / "samples"
    out.mkdir(exist_ok=True)
    write_glb(out / "site-scan-week1.glb", room(progress=False))
    write_glb(out / "site-scan-week2.glb", room(progress=True))
    print(f"Wrote sample scans to {out}")
