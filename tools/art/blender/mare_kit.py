"""Build The Long Mare (0.6) R1 environment kit in Blender 5.1:

  1. Ground & Decals:
     - MareGround_Patch.fbx    (12.0 m x 12.0 m low-relief windless mare basalt regolith swatch)
     - Decal_MareSwirl.fbx     (sinuous S-curve pale lunar swirl ribbon overlay/decal)
     - Decal_MareEjectaRay.fbx (long directional pale ejecta-ray wayfinding streak decal)

  2. Rock Kit (8 basalt boulders/outcrops, including 2 dark Ilmenite drill-target outcrops):
     - MareRock_00.fbx .. MareRock_05.fbx (6 varied, irregular mare basalt boulders/outcrops)
     - MareOutcrop_Ilmenite_00.fbx        (stepped hexagonal/tabular FeTiO3 outcrop + DrillTarget_Anchor)
     - MareOutcrop_Ilmenite_01.fbx        (large columnar FeTiO3 seam outcrop + DrillTarget_Anchor)

  3. Rille Pass Modular Pieces (4 wall/slope chunks for the west-rim sinuous rille channel):
     - Rille_Wall_Straight.fbx (12 m straight stepped basalt cliff wall + talus apron)
     - Rille_Wall_Curve.fbx    (12 m 30-deg sinuous bend wall + talus apron)
     - Rille_Slope_Ramp.fbx    (12 m sloped rille floor/rim transition ramp)
     - Rille_Mouth_Corner.fbx  (craggy rille-mouth headland buttress)

  4. Mare Derelict Set (same maker families as existing derelicts; strict vacuum logic; no floating loot):
     - Derelict_MareConvoy.fbx        (3 stopped haul bots in a line facing +Z, middle bot with StrappedLoad)
     - Derelict_MareSurveyDrone.fbx   (crashed vacuum survey hopper with 4 De Laval RCS nozzles in impact furrow)
     - Derelict_TardigradeCapsule.fbx (small half-buried Beresheet-nod experiment vault with TinyWindow)

Run headless:
    D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/mare_kit.py
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bpy

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE))

import r1kit  # noqa: E402


EXTENDED_R1_MATERIAL_SPECS = {
    "R1_WhitePaint": {
        "base_color": r1kit._srgb_to_linear("#E8EAE6"),
        "wear_color": r1kit._srgb_to_linear("#C2C7CC"),
        "dust_color": r1kit._srgb_to_linear("#8D8B86"),
        "metallic": 0.02,
        "roughness": 0.82,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
    "R1_SilverFoil": {
        "base_color": r1kit._srgb_to_linear("#C9CED2"),
        "wear_color": r1kit._srgb_to_linear("#EBF0F5"),
        "dust_color": r1kit._srgb_to_linear("#8A8E94"),
        "metallic": 0.85,
        "roughness": 0.45,
        "emission": (0.0, 0.0, 0.0, 1.0),
        "emission_strength": 0.0,
    },
}


def ensure_extended_r1_materials() -> None:
    """Register extended R1 palette slots in r1kit at runtime."""
    for name, spec in EXTENDED_R1_MATERIAL_SPECS.items():
        if name not in r1kit.ALLOWED_MATERIALS:
            r1kit.ALLOWED_MATERIALS = tuple(r1kit.ALLOWED_MATERIALS) + (name,)
        r1kit.MATERIAL_SPECS[name] = spec


def _ensure_non_uniform_vcol(root: bpy.types.Object) -> None:
    """Ensure every non-Decal mesh has a measurable vertex-color spread for R1ModelPostprocessor validation."""
    for obj in r1kit.collect_hierarchy(root):
        if obj.type != "MESH" or obj.data is None or obj.name.startswith("Decal_"):
            continue
        me = obj.data
        if not me.color_attributes:
            continue
        ca = me.color_attributes[0]
        n_loops = len(ca.data)
        if n_loops < 2:
            continue
        r_vals = [float(ca.data[i].color[0]) for i in range(n_loops)]
        if max(r_vals) - min(r_vals) < 0.025:
            for i in range(n_loops):
                r, g, b, a = ca.data[i].color
                t = (i % 7) / 6.0
                new_r = max(0.15, min(1.0, r * (0.92 + 0.08 * t)))
                new_b = max(0.0, min(1.0, b + 0.04 * (1.0 - t)))
                ca.data[i].color = (new_r, g, new_b, a)
            me.update()


# ===========================================================================
# 1. MARE GROUND PATCH & OVERLAY DECALS
# ===========================================================================
def build_mare_ground_patch(out_fbx: Path) -> None:
    """Build MareGround_Patch.fbx: 12.0 m x 12.0 m low-relief Mare basalt ground tile."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("MareGround_Patch", None, (0.0, 0.0, 0.0))

    surf = r1kit.MeshBuilder("MareSurface", pivot_u=(0.0, 0.0, 0.0))
    # Main 12 m x 12 m dark flood-basalt slab with gentle bevelled perimeter
    surf.add_bevel_box(
        center_u=(0.0, -0.06, 0.0),
        size_u=(12.0, 0.14, 12.0),
        mat="R1_Basalt",
        bevel=0.025,
        segments=1,
    )
    # Three subtle, broad windless compression swells (never round holes or dot clusters)
    swell_specs = (
        (-2.4, -0.015, 1.8, (5.2, 0.09, 3.4), (0.0, 18.0, 0.0)),
        (2.6, -0.018, -1.6, (4.8, 0.08, 3.8), (0.0, -24.0, 0.0)),
        (0.2, -0.020, -3.2, (6.0, 0.07, 2.8), (0.0, 8.0, 0.0)),
    )
    for cx, cy, cz, sz, rot in swell_specs:
        surf.add_bevel_box(
            center_u=(cx, cy, cz),
            size_u=sz,
            mat="R1_Basalt",
            bevel=0.02,
            segments=1,
            rot_deg_u=rot,
            top_scale_xz=(0.72, 0.76),
        )

    surf.build_object(parent=root)
    r1kit.create_empty("Center_Anchor", root, (0.0, 0.0, 0.0))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.35)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=7.0)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def build_decal_mare_swirl(out_fbx: Path) -> None:
    """Build Decal_MareSwirl.fbx: sinuous S-curve pale lunar swirl ground overlay/decal."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Decal_MareSwirl", None, (0.0, 0.0, 0.0))

    # Inner high-albedo magnetically shielded swirl ribbon core (S-curve along Z in [-7.5, +7.5])
    core = r1kit.MeshBuilder("SwirlCore", pivot_u=(0.0, 0.0, 0.0))
    n_lobes = 9
    for i in range(n_lobes):
        t = i / (n_lobes - 1)
        z_u = -6.8 + 13.6 * t
        x_u = 1.45 * math.sin((t - 0.5) * math.pi * 1.35)
        yaw = -28.0 * math.cos((t - 0.5) * math.pi * 1.35)
        env = math.sin(math.pi * (0.08 + 0.84 * t))
        w_u = 1.45 + 1.15 * env
        l_u = 2.35
        core.add_bevel_box(
            center_u=(x_u, 0.012, z_u),
            size_u=(w_u, 0.020, l_u),
            mat="R1_CreamEnamel",
            bevel=0.005,
            segments=1,
            rot_deg_u=(0.0, yaw, 0.0),
            top_scale_xz=(0.78, 0.86),
        )
    core.build_object(parent=root)

    # Outer feathered swirl margin & secondary magnetic loop wisps
    feather = r1kit.MeshBuilder("SwirlFeather", pivot_u=(0.0, 0.0, 0.0))
    for i in range(n_lobes):
        t = i / (n_lobes - 1)
        z_u = -7.0 + 14.0 * t
        x_u = 1.45 * math.sin((t - 0.5) * math.pi * 1.35)
        yaw = -28.0 * math.cos((t - 0.5) * math.pi * 1.35)
        env = math.sin(math.pi * (0.06 + 0.88 * t))
        w_u = 2.65 + 1.85 * env
        l_u = 2.60
        feather.add_bevel_box(
            center_u=(x_u, 0.007, z_u),
            size_u=(w_u, 0.014, l_u),
            mat="R1_CreamEnamel",
            bevel=0.004,
            segments=1,
            rot_deg_u=(0.0, yaw, 0.0),
            top_scale_xz=(0.82, 0.88),
        )
    # Two sweeping tangential magnetic loop wings
    for side, z_off, yaw_w in ((-1.0, -2.2, 22.0), (1.0, 2.4, -24.0)):
        feather.add_bevel_box(
            center_u=(side * 2.15, 0.008, z_off),
            size_u=(1.35, 0.015, 4.20),
            mat="R1_CreamEnamel",
            bevel=0.004,
            segments=1,
            rot_deg_u=(0.0, yaw_w, 0.0),
            top_scale_xz=(0.74, 0.85),
        )
    feather.build_object(parent=root)

    r1kit.create_empty("Start_Anchor", root, (0.0, 0.0, -7.5))
    r1kit.create_empty("Center_Anchor", root, (0.0, 0.0, 0.0))
    r1kit.create_empty("End_Anchor", root, (0.0, 0.0, 7.5))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.28)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=8.5)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def build_decal_mare_ejecta_ray(out_fbx: Path) -> None:
    """Build Decal_MareEjectaRay.fbx: giant directional ejecta-ray wayfinding line decal."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Decal_MareEjectaRay", None, (0.0, 0.0, 0.0))

    main_ray = r1kit.MeshBuilder("RayStreak_Main", pivot_u=(0.0, 0.0, 0.0))
    # Primary central wayfinding dust streak running 18 m along Z
    seg_specs = (
        (0.00, 0.010, -6.2, (1.55, 0.016, 5.4), (0.0, 1.5, 0.0)),
        (0.08, 0.011, 0.0, (1.85, 0.018, 6.8), (0.0, -1.2, 0.0)),
        (-0.05, 0.010, 6.2, (1.45, 0.016, 5.4), (0.0, 2.0, 0.0)),
    )
    for cx, cy, cz, sz, rot in seg_specs:
        main_ray.add_bevel_box(
            center_u=(cx, cy, cz),
            size_u=sz,
            mat="R1_CreamEnamel",
            bevel=0.004,
            segments=1,
            rot_deg_u=rot,
            top_scale_xz=(0.74, 0.90),
        )
    main_ray.build_object(parent=root)

    sec_ray = r1kit.MeshBuilder("RayStreak_Secondary", pivot_u=(0.0, 0.0, 0.0))
    # Parallel feathered secondary wayfinding streaks on either side
    flank_specs = (
        (-1.25, 0.007, -2.8, (0.78, 0.013, 7.6), (0.0, 2.5, 0.0)),
        (1.30, 0.007, 2.4, (0.82, 0.013, 8.2), (0.0, -2.0, 0.0)),
        (-0.95, 0.006, 5.2, (0.62, 0.012, 4.6), (0.0, 1.0, 0.0)),
        (1.05, 0.006, -5.0, (0.66, 0.012, 4.8), (0.0, -1.5, 0.0)),
    )
    for cx, cy, cz, sz, rot in flank_specs:
        sec_ray.add_bevel_box(
            center_u=(cx, cy, cz),
            size_u=sz,
            mat="R1_Basalt",
            bevel=0.003,
            segments=1,
            rot_deg_u=rot,
            top_scale_xz=(0.72, 0.88),
        )
    sec_ray.build_object(parent=root)

    r1kit.create_empty("Start_Anchor", root, (0.0, 0.0, -8.8))
    r1kit.create_empty("Center_Anchor", root, (0.0, 0.0, 0.0))
    r1kit.create_empty("End_Anchor", root, (0.0, 0.0, 8.8))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.30)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=9.5)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


# ===========================================================================
# 2. MARE ROCK KIT (6 dark basalt boulders/outcrops + 2 dark Ilmenite outcrops)
# ===========================================================================
MARE_BASALT_ROCK_SPECS = (
    # 00: Low asymmetric flood-basalt shelf boulder (~1.65 x 1.35 x 0.62 m)
    {
        "name": "MareRock_00",
        "size_u": (1.65, 0.62, 1.35),
        "seed": 6001,
        "skirt_flare": 0.18,
        "burial_frac": 0.14,
        "fractures_u": (
            ((0.35, 0.82, 0.45), 0.28, 0.68),
            ((-0.62, 0.55, -0.56), 0.34, 0.54),
            ((0.12, 0.92, -0.36), 0.30, 0.60),
        ),
        "crevices_u": (
            ((0.82, 0.24, -0.52), 0.04, 0.18, 0.16),
        ),
        "extra_blocks": (
            ((0.38, 0.14, -0.22), (0.72, 0.32, 0.58), (6.0, 28.0, -8.0)),
        ),
    },
    # 01: Angular tilted mare basalt erratic (~2.10 x 1.75 x 1.18 m) — crisp low-sun shadow blade
    {
        "name": "MareRock_01",
        "size_u": (2.10, 1.18, 1.75),
        "seed": 6002,
        "skirt_flare": 0.16,
        "burial_frac": 0.15,
        "fractures_u": (
            ((0.68, 0.58, 0.44), 0.24, 0.74),
            ((-0.58, 0.64, 0.50), 0.28, 0.66),
            ((0.15, 0.48, -0.86), 0.32, 0.62),
        ),
        "crevices_u": (
            ((0.25, 0.62, 0.74), -0.05, 0.16, 0.18),
        ),
        "extra_blocks": (
            ((-0.46, 0.28, 0.25), (0.95, 0.58, 0.76), (-12.0, -32.0, 14.0)),
        ),
    },
    # 02: Twin-lobe fractured basalt outcrop (~2.85 x 2.20 x 1.45 m)
    {
        "name": "MareRock_02",
        "size_u": (2.45, 1.45, 2.05),
        "seed": 6003,
        "skirt_flare": 0.18,
        "burial_frac": 0.15,
        "fractures_u": (
            ((0.52, 0.72, -0.46), 0.26, 0.70),
            ((-0.74, 0.48, 0.46), 0.30, 0.64),
            ((0.08, 0.85, 0.52), 0.34, 0.58),
        ),
        "crevices_u": (
            ((0.92, 0.18, 0.34), 0.02, 0.22, 0.24),
        ),
        "extra_blocks": (
            ((0.78, 0.36, -0.32), (1.25, 0.82, 1.10), (8.0, 42.0, -10.0)),
            ((-0.68, 0.22, 0.44), (0.92, 0.50, 0.84), (-6.0, -18.0, 9.0)),
        ),
    },
    # 03: Tall craggy basalt monolith/tooth (~2.40 x 2.05 x 2.15 m) — casts long landmark shadow
    {
        "name": "MareRock_03",
        "size_u": (2.20, 2.15, 1.90),
        "seed": 6004,
        "skirt_flare": 0.15,
        "burial_frac": 0.14,
        "fractures_u": (
            ((0.76, 0.42, 0.49), 0.22, 0.76),
            ((-0.66, 0.52, -0.54), 0.25, 0.72),
            ((0.18, 0.64, -0.74), 0.28, 0.68),
        ),
        "crevices_u": (
            ((0.36, 0.78, 0.51), 0.08, 0.18, 0.20),
        ),
        "extra_blocks": (
            ((0.42, 0.48, 0.36), (1.05, 1.12, 0.88), (14.0, 24.0, -8.0)),
        ),
    },
    # 04: Broad tabular flood-basalt bench (~3.60 x 2.65 x 1.05 m)
    {
        "name": "MareRock_04",
        "size_u": (3.20, 1.02, 2.40),
        "seed": 6005,
        "skirt_flare": 0.20,
        "burial_frac": 0.15,
        "fractures_u": (
            ((0.12, 0.96, 0.25), 0.18, 0.82),
            ((-0.48, 0.76, -0.43), 0.28, 0.68),
            ((0.64, 0.58, -0.50), 0.32, 0.62),
        ),
        "crevices_u": (
            ((0.18, 0.88, -0.44), -0.04, 0.20, 0.18),
        ),
        "extra_blocks": (
            ((-0.82, 0.28, -0.35), (1.55, 0.62, 1.28), (3.0, -16.0, 4.0)),
            ((0.88, 0.20, 0.42), (1.35, 0.46, 1.15), (-4.0, 22.0, -5.0)),
        ),
    },
    # 05: Compact jagged basalt scatter boulder (~1.15 x 0.98 x 0.54 m)
    {
        "name": "MareRock_05",
        "size_u": (1.15, 0.54, 0.98),
        "seed": 6006,
        "skirt_flare": 0.16,
        "burial_frac": 0.15,
        "fractures_u": (
            ((0.58, 0.66, 0.47), 0.26, 0.72),
            ((-0.64, 0.58, -0.50), 0.30, 0.66),
        ),
        "crevices_u": (
            ((0.72, 0.38, -0.58), 0.0, 0.16, 0.15),
        ),
        "extra_blocks": (),
    },
)


def build_mare_basalt_rocks(out_dir: Path) -> None:
    """Build MareRock_00.fbx .. MareRock_05.fbx (6 varied, irregular mare basalt boulders/outcrops)."""
    for spec in MARE_BASALT_ROCK_SPECS:
        r1kit.reset_scene()
        r1kit.ensure_materials(use_vertex_color_shading=False)
        name = str(spec["name"])
        size_u = spec["size_u"]

        mb = r1kit.MeshBuilder(name, pivot_u=(0.0, 0.0, 0.0))
        mb.add_weathered_rock(
            center_u=(0.0, 0.0, 0.0),
            size_u=size_u,
            seed=int(spec["seed"]),
            lon_segs=14,
            lat_rings=7,
            fractures_u=spec["fractures_u"],
            pits_u=(),  # Strictly zero round hole/pit clusters (trypophobia rule)
            crevices_u=spec["crevices_u"],
            skirt_flare=float(spec["skirt_flare"]),
            burial_frac=float(spec["burial_frac"]),
            mat="R1_Basalt",
        )
        for b_center, b_size, b_rot in spec["extra_blocks"]:
            mb.add_fractured_rock(
                center_u=b_center,
                size_u=b_size,
                cuts_u=(
                    ((0.55, 0.72, 0.42), 0.62),
                    ((-0.48, 0.68, -0.55), 0.66),
                ),
                mat="R1_Basalt",
                bevel=0.028,
                bevel_segments=1,
                rot_deg_u=b_rot,
            )

        root = mb.build_object(parent=None, smooth_angle_deg=48.0)
        r1kit.apply_triplanar_uvs(root, texel_scale=0.65)
        r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
        r1kit.bake_rock_vertex_colors(root, footprint_radius=max(size_u[0], size_u[2]) * 0.65)
        _ensure_non_uniform_vcol(root)
        out_fbx = out_dir / f"{name}.fbx"
        r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
        print(f"Exported {out_fbx}")


def build_ilmenite_outcrop_00(out_fbx: Path) -> None:
    """Build MareOutcrop_Ilmenite_00.fbx: medium stepped FeTiO3 drill-target outcrop (~2.80 x 2.30 x 1.55 m)."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("MareOutcrop_Ilmenite_00", None, (0.0, 0.0, 0.0))

    # 1. HostRock: low fractured mare basalt shelf embedding the Ilmenite seam
    host = r1kit.MeshBuilder("HostRock", pivot_u=(0.0, 0.0, 0.0))
    host.add_weathered_rock(
        center_u=(0.0, 0.0, 0.0),
        size_u=(2.65, 1.15, 2.15),
        seed=6010,
        lon_segs=14,
        lat_rings=7,
        fractures_u=(
            ((0.22, 0.88, 0.42), 0.22, 0.78),
            ((-0.65, 0.56, -0.51), 0.28, 0.68),
        ),
        pits_u=(),
        crevices_u=(((0.85, 0.28, 0.44), 0.0, 0.20, 0.18),),
        skirt_flare=0.18,
        burial_frac=0.15,
        mat="R1_Basalt",
    )
    host.add_fractured_rock(
        center_u=(-0.68, 0.32, -0.28),
        size_u=(1.25, 0.78, 1.10),
        cuts_u=(((0.48, 0.76, 0.44), 0.60), ((-0.52, 0.64, -0.56), 0.65)),
        mat="R1_Basalt",
        bevel=0.026,
        bevel_segments=1,
        rot_deg_u=(6.0, -22.0, 8.0),
    )
    host.build_object(parent=root, smooth_angle_deg=46.0)

    # 2. IlmeniteVein: stepped, blocky, tabular hexagonal/columnar FeTiO3 crystal plates
    #    Uses R1_BareSteel (overridden in Unity to R1_Ilmenite.mat: non-emissive bronze-black/gunmetal glint)
    vein = r1kit.MeshBuilder("IlmeniteVein", pivot_u=(0.0, 0.55, 0.15))
    column_specs = (
        # (center_u, radius, length, rot_deg_u)
        ((0.18, 0.68, 0.36), 0.36, 1.18, (14.0, 18.0, -8.0)),
        ((-0.24, 0.82, 0.12), 0.38, 1.38, (12.0, -14.0, 6.0)),
        ((0.48, 0.54, 0.08), 0.32, 0.96, (16.0, 32.0, -12.0)),
        ((-0.04, 0.50, 0.62), 0.30, 0.82, (22.0, 6.0, -4.0)),
        ((0.28, 0.38, 0.74), 0.26, 0.58, (24.0, -18.0, 10.0)),
    )
    for c_pos, c_rad, c_len, c_rot in column_specs:
        vein.add_cylinder(
            center_u=c_pos,
            radius=c_rad,
            radius_top=c_rad * 0.88,
            length=c_len,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=6,  # Hexagonal crystal habit
            bevel=0.018,
            bevel_segments=1,
            rot_deg_u=c_rot,
        )
    # Stepped tabular cleavage slabs along the front drill face
    slab_specs = (
        ((0.06, 0.58, 0.82), (0.68, 0.42, 0.24), (18.0, 8.0, -4.0)),
        ((-0.34, 0.46, 0.64), (0.54, 0.36, 0.22), (15.0, -22.0, 6.0)),
        ((0.44, 0.44, 0.56), (0.52, 0.34, 0.22), (20.0, 26.0, -8.0)),
    )
    for s_pos, s_sz, s_rot in slab_specs:
        vein.add_bevel_box(
            center_u=s_pos,
            size_u=s_sz,
            mat="R1_BareSteel",
            bevel=0.014,
            segments=1,
            rot_deg_u=s_rot,
            top_scale_xz=(0.88, 0.90),
        )
    vein.build_object(parent=root, smooth_angle_deg=35.0)

    r1kit.create_empty("Base_Anchor", root, (0.0, 0.0, 0.0))
    r1kit.create_empty("DrillTarget_Anchor", root, (0.06, 0.58, 0.94))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.75)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_rock_vertex_colors(root, footprint_radius=1.65)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def build_ilmenite_outcrop_01(out_fbx: Path) -> None:
    """Build MareOutcrop_Ilmenite_01.fbx: large columnar FeTiO3 drill-target outcrop (~3.50 x 2.75 x 2.05 m)."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("MareOutcrop_Ilmenite_01", None, (0.0, 0.0, 0.0))

    # 1. HostRock: broad twin-shelf fractured mare basalt base
    host = r1kit.MeshBuilder("HostRock", pivot_u=(0.0, 0.0, 0.0))
    host.add_weathered_rock(
        center_u=(0.0, 0.0, 0.0),
        size_u=(3.35, 1.38, 2.60),
        seed=6011,
        lon_segs=14,
        lat_rings=7,
        fractures_u=(
            ((0.32, 0.84, 0.43), 0.24, 0.76),
            ((-0.58, 0.62, -0.52), 0.28, 0.70),
        ),
        pits_u=(),
        crevices_u=(((0.76, 0.34, -0.55), 0.02, 0.22, 0.20),),
        skirt_flare=0.19,
        burial_frac=0.15,
        mat="R1_Basalt",
    )
    host.add_fractured_rock(
        center_u=(0.86, 0.38, -0.34),
        size_u=(1.45, 0.92, 1.25),
        cuts_u=(((0.52, 0.74, -0.42), 0.62), ((-0.46, 0.68, 0.56), 0.64)),
        mat="R1_Basalt",
        bevel=0.028,
        bevel_segments=1,
        rot_deg_u=(-8.0, 28.0, -6.0),
    )
    host.build_object(parent=root, smooth_angle_deg=46.0)

    # 2. IlmeniteVein: tall stepped hexagonal columns & broad tabular bronze-black cleavage terraces
    vein = r1kit.MeshBuilder("IlmeniteVein", pivot_u=(0.0, 0.72, 0.12))
    column_specs = (
        ((-0.18, 0.96, -0.05), 0.44, 1.78, (10.0, -12.0, 5.0)),
        ((0.34, 0.84, 0.22), 0.40, 1.52, (15.0, 20.0, -7.0)),
        ((-0.62, 0.68, 0.32), 0.36, 1.22, (14.0, -28.0, 9.0)),
        ((0.08, 0.62, 0.64), 0.35, 1.05, (20.0, 8.0, -4.0)),
        ((0.62, 0.52, 0.58), 0.30, 0.84, (22.0, 34.0, -10.0)),
        ((-0.26, 0.44, 0.88), 0.28, 0.68, (24.0, -14.0, 6.0)),
    )
    for c_pos, c_rad, c_len, c_rot in column_specs:
        vein.add_cylinder(
            center_u=c_pos,
            radius=c_rad,
            radius_top=c_rad * 0.86,
            length=c_len,
            mat="R1_BareSteel",
            axis_u="Y",
            segments=6,
            bevel=0.020,
            bevel_segments=1,
            rot_deg_u=c_rot,
        )
    slab_specs = (
        ((0.02, 0.62, 1.02), (0.82, 0.48, 0.26), (18.0, 4.0, -3.0)),
        ((-0.48, 0.50, 0.84), (0.64, 0.40, 0.24), (16.0, -24.0, 7.0)),
        ((0.52, 0.48, 0.80), (0.62, 0.38, 0.24), (19.0, 28.0, -8.0)),
    )
    for s_pos, s_sz, s_rot in slab_specs:
        vein.add_bevel_box(
            center_u=s_pos,
            size_u=s_sz,
            mat="R1_BareSteel",
            bevel=0.015,
            segments=1,
            rot_deg_u=s_rot,
            top_scale_xz=(0.86, 0.90),
        )
    vein.build_object(parent=root, smooth_angle_deg=35.0)

    r1kit.create_empty("Base_Anchor", root, (0.0, 0.0, 0.0))
    r1kit.create_empty("DrillTarget_Anchor", root, (0.02, 0.62, 1.15))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.72)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_rock_vertex_colors(root, footprint_radius=2.05)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


# ===========================================================================
# 3. RILLE PASS MODULAR PIECES (4 chunks for west-rim sinuous rille channel)
# ===========================================================================
def build_rille_wall_straight(out_fbx: Path) -> None:
    """Build Rille_Wall_Straight.fbx: 12.0 m modular straight rille channel wall + talus apron."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Rille_Wall_Straight", None, (0.0, 0.0, 0.0))

    # 1. CliffWall: 3 stepped flood-basalt lava-flow strata ledges along Z in [-6.0, +6.0]
    cliff = r1kit.MeshBuilder("CliffWall", pivot_u=(-1.2, 2.2, 0.0))
    # Lower cliff stratum
    cliff.add_bevel_box(
        center_u=(-1.35, 1.10, 0.0),
        size_u=(2.90, 2.30, 12.0),
        mat="R1_Basalt",
        bevel=0.045,
        segments=1,
        top_scale_xz=(0.86, 0.99),
    )
    # Middle cliff stratum
    cliff.add_bevel_box(
        center_u=(-1.65, 2.85, 0.0),
        size_u=(2.30, 1.65, 12.0),
        mat="R1_Basalt",
        bevel=0.040,
        segments=1,
        top_scale_xz=(0.84, 0.99),
    )
    # Upper rim cap stratum
    cliff.add_bevel_box(
        center_u=(-1.92, 4.25, 0.0),
        size_u=(1.75, 1.45, 12.0),
        mat="R1_Basalt",
        bevel=0.040,
        segments=1,
        top_scale_xz=(0.82, 0.99),
    )
    # Craggy protruding basalt buttress ribs along the channel face (+X side)
    buttress_specs = (
        ((-0.18, 1.85, -3.8), (1.35, 3.50, 2.85), (0.0, 6.0, -4.0)),
        ((-0.12, 2.10, 0.4), (1.45, 4.00, 3.20), (0.0, -5.0, -3.0)),
        ((-0.20, 1.75, 4.1), (1.30, 3.30, 2.65), (0.0, 8.0, -4.0)),
    )
    for b_pos, b_sz, b_rot in buttress_specs:
        cliff.add_fractured_rock(
            center_u=b_pos,
            size_u=b_sz,
            cuts_u=(((0.72, 0.48, 0.49), 0.58), ((0.64, 0.55, -0.53), 0.62)),
            mat="R1_Basalt",
            bevel=0.035,
            bevel_segments=1,
            rot_deg_u=b_rot,
        )
    cliff.build_object(parent=root, smooth_angle_deg=46.0)

    # 2. TalusApron: sloped regolith & slumped basalt skirt meeting the flat rille floor at X > 0
    talus = r1kit.MeshBuilder("TalusApron", pivot_u=(0.65, 0.45, 0.0))
    talus.add_bevel_box(
        center_u=(0.55, 0.42, 0.0),
        size_u=(2.40, 0.96, 12.0),
        mat="R1_Basalt",
        bevel=0.035,
        segments=1,
        rot_deg_u=(0.0, 0.0, -18.0),
        top_scale_xz=(0.68, 0.99),
    )
    for tz, tw, th, tyaw in ((-4.2, 1.65, 0.62, 14.0), (0.8, 1.85, 0.68, -12.0), (4.4, 1.55, 0.58, 18.0)):
        talus.add_bevel_box(
            center_u=(1.15, th * 0.42, tz),
            size_u=(tw, th, 2.65),
            mat="R1_Basalt",
            bevel=0.028,
            segments=1,
            rot_deg_u=(0.0, tyaw, -12.0),
            top_scale_xz=(0.72, 0.84),
        )
    talus.build_object(parent=root, smooth_angle_deg=48.0)

    r1kit.create_empty("Start_Anchor", root, (0.0, 0.0, -6.0))
    r1kit.create_empty("End_Anchor", root, (0.0, 0.0, 6.0))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.38)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_rock_vertex_colors(root, footprint_radius=6.8)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def build_rille_wall_curve(out_fbx: Path) -> None:
    """Build Rille_Wall_Curve.fbx: 12.0 m 30-degree sinuous bend rille channel wall + talus apron."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Rille_Wall_Curve", None, (0.0, 0.0, 0.0))

    cliff = r1kit.MeshBuilder("CliffWall", pivot_u=(-1.0, 2.2, 0.0))
    # 3 chord segments curving 30 degrees across Z in [-5.8, +5.8]
    chord_specs = (
        ((-1.55, 0.0, -3.85), -12.0),
        ((-1.15, 0.0, 0.00), 0.0),
        ((-1.55, 0.0, 3.85), 12.0),
    )
    for (cx, _, cz), yaw in chord_specs:
        cliff.add_bevel_box(
            center_u=(cx, 1.10, cz),
            size_u=(2.85, 2.30, 4.65),
            mat="R1_Basalt",
            bevel=0.042,
            segments=1,
            rot_deg_u=(0.0, yaw, 0.0),
            top_scale_xz=(0.86, 0.96),
        )
        cliff.add_bevel_box(
            center_u=(cx - 0.30, 2.85, cz),
            size_u=(2.25, 1.65, 4.55),
            mat="R1_Basalt",
            bevel=0.038,
            segments=1,
            rot_deg_u=(0.0, yaw, 0.0),
            top_scale_xz=(0.84, 0.96),
        )
        cliff.add_bevel_box(
            center_u=(cx - 0.55, 4.25, cz),
            size_u=(1.70, 1.45, 4.45),
            mat="R1_Basalt",
            bevel=0.038,
            segments=1,
            rot_deg_u=(0.0, yaw, 0.0),
            top_scale_xz=(0.82, 0.96),
        )
        cliff.add_fractured_rock(
            center_u=(cx + 1.10, 1.95, cz),
            size_u=(1.40, 3.70, 2.75),
            cuts_u=(((0.68, 0.52, 0.51), 0.60), ((0.62, 0.56, -0.54), 0.62)),
            mat="R1_Basalt",
            bevel=0.034,
            bevel_segments=1,
            rot_deg_u=(0.0, yaw + 5.0, -4.0),
        )
    cliff.build_object(parent=root, smooth_angle_deg=46.0)

    talus = r1kit.MeshBuilder("TalusApron", pivot_u=(0.65, 0.42, 0.0))
    for (cx, _, cz), yaw in chord_specs:
        talus.add_bevel_box(
            center_u=(cx + 1.85, 0.42, cz),
            size_u=(2.35, 0.95, 4.60),
            mat="R1_Basalt",
            bevel=0.032,
            segments=1,
            rot_deg_u=(0.0, yaw, -16.0),
            top_scale_xz=(0.70, 0.94),
        )
    talus.build_object(parent=root, smooth_angle_deg=48.0)

    r1kit.create_empty("Start_Anchor", root, (-0.45, 0.0, -5.8))
    r1kit.create_empty("End_Anchor", root, (-0.45, 0.0, 5.8))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.38)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_rock_vertex_colors(root, footprint_radius=6.8)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def build_rille_slope_ramp(out_fbx: Path) -> None:
    """Build Rille_Slope_Ramp.fbx: 12.0 m sloped rille pass descent/exit ramp chunk."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Rille_Slope_Ramp", None, (0.0, 0.0, 0.0))

    # 1. SlopeBed: driveable compacted basalt regolith ramp rising from Y=0 at Z=-6 to Y=1.8 at Z=+6
    bed = r1kit.MeshBuilder("SlopeBed", pivot_u=(0.4, 0.8, 0.0))
    bed.add_bevel_box(
        center_u=(0.45, 0.82, 0.0),
        size_u=(3.60, 0.68, 12.0),
        mat="R1_Basalt",
        bevel=0.035,
        segments=1,
        rot_deg_u=(-8.5, 0.0, 0.0),
        top_scale_xz=(0.84, 0.99),
    )
    # Stepped lateral compression ridges along the ramp margin
    for sz in (-3.6, 0.0, 3.6):
        sy = 0.82 + (sz / 6.0) * 0.88
        bed.add_bevel_box(
            center_u=(1.65, sy - 0.12, sz),
            size_u=(1.35, 0.48, 3.80),
            mat="R1_Basalt",
            bevel=0.025,
            segments=1,
            rot_deg_u=(-8.5, 4.0, -10.0),
            top_scale_xz=(0.74, 0.90),
        )
    bed.build_object(parent=root, smooth_angle_deg=48.0)

    # 2. LowRimWall: tapering basalt shoulder wall alongside the ramp (-X side)
    wall = r1kit.MeshBuilder("LowRimWall", pivot_u=(-1.4, 1.5, 0.0))
    tier_specs = (
        ((-1.45, 0.85, -3.8), (2.20, 1.85, 4.40), 0.0),
        ((-1.50, 1.35, 0.0), (2.30, 2.65, 4.50), -3.0),
        ((-1.55, 1.80, 3.8), (2.40, 3.45, 4.40), 4.0),
    )
    for t_pos, t_sz, t_yaw in tier_specs:
        wall.add_fractured_rock(
            center_u=t_pos,
            size_u=t_sz,
            cuts_u=(((0.64, 0.58, 0.50), 0.62), ((-0.52, 0.68, -0.51), 0.64)),
            mat="R1_Basalt",
            bevel=0.032,
            bevel_segments=1,
            rot_deg_u=(0.0, t_yaw, -4.0),
        )
    wall.build_object(parent=root, smooth_angle_deg=46.0)

    r1kit.create_empty("Start_Anchor", root, (0.45, 0.0, -6.0))
    r1kit.create_empty("End_Anchor", root, (0.45, 1.75, 6.0))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.38)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_rock_vertex_colors(root, footprint_radius=6.8)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def build_rille_mouth_corner(out_fbx: Path) -> None:
    """Build Rille_Mouth_Corner.fbx: craggy rille-mouth headland buttress (~8.5 x 6.8 x 5.6 m)."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Rille_Mouth_Corner", None, (0.0, 0.0, 0.0))

    buttress = r1kit.MeshBuilder("HeadlandButtress", pivot_u=(-0.6, 2.4, 0.0))
    # Primary towering headland promontory block
    buttress.add_fractured_rock(
        center_u=(-0.85, 2.65, -0.35),
        size_u=(4.60, 5.45, 5.80),
        cuts_u=(
            ((0.68, 0.52, 0.51), 0.58),
            ((0.52, 0.64, -0.56), 0.62),
            ((-0.62, 0.58, 0.52), 0.64),
        ),
        mat="R1_Basalt",
        bevel=0.042,
        bevel_segments=1,
        rot_deg_u=(0.0, 14.0, -3.0),
    )
    # Stepped forward prow spire that casts a long landmark shadow wedge across the rille mouth
    buttress.add_fractured_rock(
        center_u=(0.65, 1.85, 1.65),
        size_u=(3.20, 3.85, 3.60),
        cuts_u=(
            ((0.72, 0.48, 0.50), 0.56),
            ((-0.48, 0.68, 0.55), 0.60),
        ),
        mat="R1_Basalt",
        bevel=0.036,
        bevel_segments=1,
        rot_deg_u=(4.0, -22.0, -5.0),
    )
    buttress.build_object(parent=root, smooth_angle_deg=46.0)

    talus = r1kit.MeshBuilder("TalusApron", pivot_u=(0.8, 0.5, 0.4))
    apron_specs = (
        ((1.45, 0.48, 0.25), (3.10, 1.10, 6.40), (0.0, 12.0, -16.0)),
        ((0.35, 0.42, 2.85), (3.60, 0.95, 3.20), (14.0, -28.0, -10.0)),
        ((2.15, 0.28, 1.45), (1.85, 0.64, 2.25), (6.0, 34.0, -8.0)),
    )
    for a_pos, a_sz, a_rot in apron_specs:
        talus.add_bevel_box(
            center_u=a_pos,
            size_u=a_sz,
            mat="R1_Basalt",
            bevel=0.032,
            segments=1,
            rot_deg_u=a_rot,
            top_scale_xz=(0.72, 0.84),
        )
    talus.build_object(parent=root, smooth_angle_deg=48.0)

    r1kit.create_empty("Start_Anchor", root, (0.0, 0.0, -4.0))
    r1kit.create_empty("End_Anchor", root, (1.8, 0.0, 3.6))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.38)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_rock_vertex_colors(root, footprint_radius=5.2)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


# ===========================================================================
# 4. MARE DERELICT SET
#    - Derelict_MareConvoy.fbx        (3 haul bots in a line, middle with StrappedLoad)
#    - Derelict_MareSurveyDrone.fbx   (crashed vacuum survey hopper in furrow)
#    - Derelict_TardigradeCapsule.fbx (half-buried Beresheet-nod capsule, TinyWindow)
# ===========================================================================
def _add_haul_bot_unit(
    mb: r1kit.MeshBuilder,
    center_u: tuple[float, float, float],
    yaw_deg: float = 0.0,
    pitch_deg: float = 0.0,
    roll_deg: float = 0.0,
    head_droop_deg: float = 18.0,
) -> None:
    """Add an Era-B/L Kessler-Arai / ILC 6-wheel lunar cargo haul bot (all dark, zero emission)."""
    cx, cy, cz = center_u
    rot_mat_b = r1kit.u_rot(pitch_deg, yaw_deg, roll_deg).to_matrix()

    def _pt(lx: float, ly: float, lz: float) -> tuple[float, float, float]:
        vb = rot_mat_b @ r1kit.u_pos(lx, ly, lz)
        return (cx + float(vb.x), cy + float(vb.z), cz + float(vb.y))

    def _rot(rx: float = 0.0, ry: float = 0.0, rz: float = 0.0) -> tuple[float, float, float]:
        return (pitch_deg + rx, yaw_deg + ry, roll_deg + rz)

    # 6 heavy ribbed lunar wheels (3 axles along Z: -0.62, 0.0, +0.62; X = +/- 0.56)
    wheel_r = 0.24
    wheel_w = 0.20
    for sx in (-1.0, 1.0):
        for az in (-0.62, 0.0, 0.62):
            mb.add_cylinder(
                center_u=_pt(sx * 0.56, wheel_r - 0.03, az),
                radius=wheel_r,
                length=wheel_w,
                mat="R1_Rubber",
                axis_u="X",
                segments=12,
                bevel=0.020,
                bevel_segments=1,
                rot_deg_u=_rot(),
            )
            mb.add_cylinder(
                center_u=_pt(sx * 0.57, wheel_r - 0.03, az),
                radius=wheel_r * 0.52,
                length=wheel_w + 0.012,
                mat="R1_GreyMetal",
                axis_u="X",
                segments=8,
                bevel=0.008,
                bevel_segments=1,
                rot_deg_u=_rot(),
            )
        # Side bogie suspension beam linking the 3 axles
        mb.add_bevel_box(
            center_u=_pt(sx * 0.44, 0.22, 0.0),
            size_u=(0.08, 0.11, 1.38),
            mat="R1_BareSteel",
            bevel=0.010,
            segments=1,
            rot_deg_u=_rot(),
        )

    # Lower heavy steel chassis tub
    mb.add_bevel_box(
        center_u=_pt(0.0, 0.34, 0.0),
        size_u=(0.96, 0.22, 1.72),
        mat="R1_GreyMetal",
        bevel=0.028,
        segments=1,
        rot_deg_u=_rot(),
    )
    # Upper cream-enamel flatbed deck with 45-deg chamfered corners & orange hazard belt
    mb.add_bevel_box(
        center_u=_pt(0.0, 0.50, -0.06),
        size_u=(1.08, 0.16, 1.56),
        mat="R1_CreamEnamel",
        bevel=0.032,
        segments=1,
        rot_deg_u=_rot(),
    )
    mb.add_bevel_box(
        center_u=_pt(0.0, 0.46, -0.06),
        size_u=(1.10, 0.055, 1.58),
        mat="R1_OrangeStripe",
        bevel=0.006,
        segments=1,
        rot_deg_u=_rot(),
    )
    # Side cargo tie-down rails along deck edges
    for sx in (-1.0, 1.0):
        mb.add_pipe(
            _pt(sx * 0.51, 0.62, -0.74),
            _pt(sx * 0.51, 0.62, 0.42),
            radius=0.022,
            mat="R1_BareSteel",
            segments=8,
        )

    # Rear battery/radiator louver pack (Z = -0.64)
    mb.add_bevel_box(
        center_u=_pt(0.0, 0.68, -0.62),
        size_u=(0.88, 0.24, 0.36),
        mat="R1_GreyMetal",
        bevel=0.022,
        segments=1,
        rot_deg_u=_rot(),
    )
    mb.add_vent_grille(
        center_u=_pt(0.0, 0.68, -0.80),
        width=0.64,
        height=0.16,
        depth=0.028,
        slat_count=4,
        rot_deg_u=_rot(0.0, 180.0, 0.0),
        frame_mat="R1_BareSteel",
        back_mat="R1_DarkGlass",
    )

    # Front drooped, unlit sensor mast & dead optical visor (Z = +0.66) — "all facing the same way, all dark"
    mb.add_pipe(
        _pt(0.0, 0.56, 0.58),
        _pt(0.0, 0.78, 0.66),
        radius=0.048,
        mat="R1_BareSteel",
        segments=10,
    )
    mb.add_bevel_box(
        center_u=_pt(0.0, 0.82, 0.70),
        size_u=(0.48, 0.24, 0.34),
        mat="R1_CreamEnamel",
        bevel=0.028,
        segments=1,
        rot_deg_u=_rot(head_droop_deg, 0.0, 0.0),
    )
    # Dead unlit dark-glass visor lens (R1_DarkGlass, zero emission)
    mb.add_bevel_box(
        center_u=_pt(0.0, 0.80, 0.86),
        size_u=(0.36, 0.13, 0.04),
        mat="R1_DarkGlass",
        bevel=0.008,
        segments=1,
        rot_deg_u=_rot(head_droop_deg, 0.0, 0.0),
    )


def build_derelict_mare_convoy(out_fbx: Path) -> None:
    """Build Derelict_MareConvoy.fbx: 3 stopped haul bots in a line facing +Z, middle bot with StrappedLoad."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Derelict_MareConvoy", None, (0.0, 0.0, 0.0))

    # 1. HaulBot_Lead (front of the line at Z = +2.65 m, facing +Z)
    lead_mb = r1kit.MeshBuilder("HaulBot_Lead", pivot_u=(0.0, 0.0, 2.65))
    _add_haul_bot_unit(lead_mb, center_u=(0.0, 0.0, 2.65), yaw_deg=2.5, pitch_deg=1.5, roll_deg=-2.0, head_droop_deg=20.0)
    lead_mb.build_object(parent=root, smooth_angle_deg=48.0)

    # 2. HaulBot_Cargo (middle of the line at Z = 0.0 m, facing +Z, carrying the StrappedLoad)
    cargo_mb = r1kit.MeshBuilder("HaulBot_Cargo", pivot_u=(0.0, 0.0, 0.0))
    _add_haul_bot_unit(cargo_mb, center_u=(0.0, 0.0, 0.0), yaw_deg=-1.5, pitch_deg=-1.0, roll_deg=2.5, head_droop_deg=16.0)
    cargo_mb.build_object(parent=root, smooth_angle_deg=48.0)

    # 3. StrappedLoad on HaulBot_Cargo's deck (salvage crate + pressurized canister + tension straps + clamps)
    load_mb = r1kit.MeshBuilder("StrappedLoad", pivot_u=(0.0, 0.65, -0.10))
    # Heavy salvage supply crate on forward deck
    load_mb.add_supply_crate(
        center_u=(0.0, 0.74, 0.14),
        size_u=(0.76, 0.34, 0.56),
        rot_deg_u=(-1.0, -1.5, 2.5),
        body_mat="R1_CreamEnamel",
        stripe_mat="R1_OrangeStripe",
        hardware_mat="R1_BareSteel",
    )
    # Pressurized copper/gold-foil reagent drum cradled behind the crate
    load_mb.add_cylinder(
        center_u=(0.0, 0.72, -0.28),
        radius=0.16,
        length=0.74,
        mat="R1_Copper",
        axis_u="X",
        segments=12,
        bevel=0.014,
        bevel_segments=1,
        rot_deg_u=(-1.0, -1.5, 2.5),
    )
    # Over-the-top tension straps & steel ratchet buckles anchoring the load to the side rails
    for sz_strap in (-0.28, 0.04, 0.24):
        load_mb.add_bevel_box(
            center_u=(0.0, 0.76, sz_strap),
            size_u=(0.94, 0.38, 0.048),
            mat="R1_OrangeStripe",
            bevel=0.005,
            segments=1,
            rot_deg_u=(-1.0, -1.5, 2.5),
        )
        for sx_b in (-0.47, 0.47):
            load_mb.add_bevel_box(
                center_u=(sx_b, 0.63, sz_strap),
                size_u=(0.045, 0.09, 0.065),
                mat="R1_BareSteel",
                bevel=0.004,
                segments=1,
                rot_deg_u=(-1.0, -1.5, 2.5),
            )
    load_mb.build_object(parent=root, smooth_angle_deg=48.0)

    # 4. HaulBot_Rear (trailing bot at Z = -2.65 m, facing +Z)
    rear_mb = r1kit.MeshBuilder("HaulBot_Rear", pivot_u=(-0.12, 0.0, -2.65))
    _add_haul_bot_unit(rear_mb, center_u=(-0.12, 0.0, -2.65), yaw_deg=-4.5, pitch_deg=2.0, roll_deg=-3.0, head_droop_deg=22.0)
    rear_mb.build_object(parent=root, smooth_angle_deg=48.0)

    # 5. WheelRuts: shallow drifted mare regolith mounds & wheel tracks under the stopped convoy line
    ruts_mb = r1kit.MeshBuilder("WheelRuts", pivot_u=(0.0, 0.0, 0.0))
    for sx in (-0.56, 0.56):
        ruts_mb.add_bevel_box(
            center_u=(sx, 0.018, 0.0),
            size_u=(0.36, 0.042, 7.40),
            mat="R1_Basalt",
            bevel=0.010,
            segments=1,
            top_scale_xz=(0.72, 0.96),
        )
    ruts_mb.build_object(parent=root, smooth_angle_deg=48.0)

    # Semantic sockets & anchors (no floating loot — physical salvage sockets on chassis/rack)
    r1kit.create_empty("Base_Anchor", root, (0.0, 0.0, 0.0))
    r1kit.create_empty("CutPoint", root, (0.48, 0.63, 0.04))
    r1kit.create_empty("FragmentSocket_CutterRibs_0", root, (0.48, 0.56, 2.65))
    r1kit.create_empty("FragmentSocket_CutterRibs_1", root, (-0.48, 0.56, 0.0))
    r1kit.create_empty("FragmentSocket_CutterRibs_2", root, (0.36, 0.56, -2.65))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.75)

    # Maker decals (Kessler-Arai & ILC livery — added after apply_triplanar_uvs to preserve atlas UVs)
    r1kit.add_maker_decal_node(
        parent=root,
        name="Decal_KA_1",
        cell_key="KESSLER_ARAI",
        center_u=(0.0, 0.68, 3.45),
        normal_u=(0.0, 0.0, 1.0),
        up_u=(0.0, 1.0, 0.0),
        width=0.18,
        height=0.18,
    )
    r1kit.add_maker_decal_node(
        parent=root,
        name="Decal_KA_2",
        cell_key="KESSLER_ARAI",
        center_u=(0.38, 0.74, 0.14),
        normal_u=(1.0, 0.0, 0.0),
        up_u=(0.0, 1.0, 0.0),
        width=0.18,
        height=0.18,
    )
    r1kit.add_maker_decal_node(
        parent=root,
        name="Decal_ILC_1",
        cell_key="ILC_SEAL",
        center_u=(-0.12, 0.68, -1.85),
        normal_u=(0.0, 0.0, 1.0),
        up_u=(0.0, 1.0, 0.0),
        width=0.16,
        height=0.16,
    )

    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=4.2)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def build_derelict_mare_survey_drone(out_fbx: Path) -> None:
    """Build Derelict_MareSurveyDrone.fbx: crashed vacuum survey hopper (4 De Laval RCS nozzles, zero rotors/wings)."""
    r1kit.reset_scene()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Derelict_MareSurveyDrone", None, (0.0, 0.0, 0.0))

    pitch_deg = 18.0
    roll_deg = -11.0
    piv_u = (0.0, 0.34, 0.10)
    tilt_mat_b = r1kit.u_rot(pitch_deg, 0.0, roll_deg).to_matrix()

    def _pose(pt_u: tuple[float, float, float]) -> tuple[float, float, float]:
        pb = r1kit.u_pos(*piv_u)
        vb = pb + (tilt_mat_b @ (r1kit.u_pos(*pt_u) - pb))
        return (float(vb.x), float(vb.z), float(vb.y))

    def _rot(rx: float = 0.0, ry: float = 0.0, rz: float = 0.0) -> tuple[float, float, float]:
        return (pitch_deg + rx, ry, roll_deg + rz)

    # 1. ImpactFurrow: fresh-looking plowed mare regolith trench behind (-Z) and piled bow rim ahead (+Z)
    furrow = r1kit.MeshBuilder("ImpactFurrow", pivot_u=(0.0, 0.0, 0.0))
    # Raised bow wave mound at +Z where the nose dug in
    furrow.add_bevel_box(
        center_u=(0.0, 0.11, 0.62),
        size_u=(1.28, 0.24, 0.68),
        mat="R1_Basalt",
        bevel=0.028,
        segments=1,
        rot_deg_u=(0.0, 6.0, 0.0),
        top_scale_xz=(0.68, 0.72),
    )
    # Lateral displaced regolith berms flanking the skid trench
    for sx, yaw_b in ((-0.54, -8.0), (0.54, 8.0)):
        furrow.add_bevel_box(
            center_u=(sx, 0.065, -0.22),
            size_u=(0.38, 0.14, 1.58),
            mat="R1_Basalt",
            bevel=0.020,
            segments=1,
            rot_deg_u=(0.0, yaw_b, 0.0),
            top_scale_xz=(0.66, 0.88),
        )
    # Compacted trench floor
    furrow.add_bevel_box(
        center_u=(0.0, 0.018, -0.25),
        size_u=(0.88, 0.040, 1.65),
        mat="R1_Basalt",
        bevel=0.010,
        segments=1,
    )
    furrow.build_object(parent=root, smooth_angle_deg=48.0)

    # 2. DroneBody: compact octagonal/spherical vacuum survey hopper hull (Tycho / Kessler-Arai family)
    body = r1kit.MeshBuilder("DroneBody", pivot_u=piv_u)
    # Octagonal main pressure bus
    body.add_cylinder(
        center_u=_pose((0.0, 0.36, 0.10)),
        radius=0.46,
        radius_top=0.40,
        length=0.42,
        mat="R1_CreamEnamel",
        axis_u="Y",
        segments=8,
        bevel=0.024,
        bevel_segments=1,
        rot_deg_u=_rot(),
    )
    # Gold Kapton thermal blanket belly collar
    body.add_cylinder(
        center_u=_pose((0.0, 0.20, 0.10)),
        radius=0.42,
        radius_top=0.45,
        length=0.14,
        mat="R1_GoldFoil",
        axis_u="Y",
        segments=8,
        bevel=0.012,
        bevel_segments=1,
        rot_deg_u=_rot(),
    )
    # Upper instrument crown & orange recognition band
    body.add_cylinder(
        center_u=_pose((0.0, 0.56, 0.10)),
        radius=0.36,
        radius_top=0.26,
        length=0.12,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=8,
        bevel=0.014,
        bevel_segments=1,
        rot_deg_u=_rot(),
    )
    body.add_bevel_box(
        center_u=_pose((0.0, 0.42, 0.10)),
        size_u=(0.86, 0.06, 0.86),
        mat="R1_OrangeStripe",
        bevel=0.008,
        segments=1,
        rot_deg_u=_rot(0.0, 22.5, 0.0),
    )
    # Spherical cold-gas propellant bottle on rear deck
    body.add_cylinder(
        center_u=_pose((0.0, 0.44, -0.32)),
        radius=0.15,
        radius_top=0.12,
        length=0.26,
        mat="R1_BareSteel",
        axis_u="Z",
        segments=10,
        bevel=0.014,
        bevel_segments=1,
        rot_deg_u=_rot(),
    )
    body.build_object(parent=root, smooth_angle_deg=48.0)

    # 3. ThrusterPods: 4 canted De Laval cold-gas RCS thruster bells (strict vacuum propulsion — zero rotors/wings)
    thrusters = r1kit.MeshBuilder("ThrusterPods", pivot_u=piv_u)
    for az_deg, bent_pitch in ((45.0, 12.0), (135.0, 0.0), (225.0, -8.0), (315.0, 22.0)):
        az_rad = math.radians(az_deg)
        tx = 0.48 * math.sin(az_rad)
        tz = 0.10 + 0.48 * math.cos(az_rad)
        # Outward strut arm
        thrusters.add_bevel_box(
            center_u=_pose((tx * 0.82, 0.34, 0.10 + (tz - 0.10) * 0.82)),
            size_u=(0.14, 0.12, 0.24),
            mat="R1_GreyMetal",
            bevel=0.012,
            segments=1,
            rot_deg_u=_rot(bent_pitch, az_deg, 0.0),
        )
        # Flared brass/steel De Laval vacuum thruster bell
        thrusters.add_cylinder(
            center_u=_pose((tx, 0.22, tz)),
            radius=0.11,
            radius_top=0.055,
            length=0.20,
            mat="R1_Brass",
            axis_u="Y",
            segments=10,
            bevel=0.006,
            bevel_segments=1,
            rot_deg_u=_rot(bent_pitch + 14.0, az_deg, 0.0),
        )
    thrusters.build_object(parent=root, smooth_angle_deg=48.0)

    # 4. SurveyBoom: crumpled telescopic magnetometer / optical survey mast + corona sphere
    boom = r1kit.MeshBuilder("SurveyBoom", pivot_u=_pose((0.0, 0.62, 0.10)))
    boom_pts = [
        _pose((0.0, 0.60, 0.10)),
        _pose((0.08, 0.78, 0.02)),
        _pose((0.26, 0.68, -0.34)),
    ]
    boom.add_tube_path(boom_pts, radius=0.032, mat="R1_BareSteel", segments=8, cap_ends=True)
    # Corona sphere / fluxgate sensor head at tip of bent boom (vacuum electrostatic geometry)
    boom.add_cylinder(
        center_u=boom_pts[-1],
        radius=0.075,
        radius_top=0.060,
        length=0.13,
        mat="R1_Copper",
        axis_u="Z",
        segments=10,
        bevel=0.012,
        bevel_segments=1,
        rot_deg_u=_rot(-24.0, 18.0, 0.0),
    )
    boom.build_object(parent=root, smooth_angle_deg=48.0)

    # 5. Eye_Dead: dark unlit optical survey camera dome on front nose (zero emission)
    eye = r1kit.MeshBuilder("Eye_Dead", pivot_u=_pose((0.0, 0.34, 0.52)))
    eye.add_cylinder(
        center_u=_pose((0.0, 0.34, 0.52)),
        radius=0.12,
        radius_top=0.095,
        length=0.09,
        mat="R1_DarkGlass",
        axis_u="Z",
        segments=10,
        bevel=0.010,
        bevel_segments=1,
        rot_deg_u=_rot(),
    )
    eye.build_object(parent=root, smooth_angle_deg=48.0)

    r1kit.create_empty("Base_Anchor", root, (0.0, 0.0, 0.0))
    r1kit.create_empty("CutPoint", root, _pose((0.12, 0.68, -0.08)))
    r1kit.create_empty("FragmentSocket_Spool_0", root, _pose((-0.32, 0.42, -0.18)))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.80)

    r1kit.add_maker_decal_node(
        parent=root,
        name="Decal_Tycho_1",
        cell_key="TYCHO",
        center_u=_pose((0.0, 0.62, 0.10)),
        normal_u=(0.0, 0.95, 0.31),
        up_u=(0.0, -0.31, 0.95),
        width=0.18,
        height=0.18,
    )

    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=1.35)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def build_derelict_tardigrade_capsule(out_fbx: Path) -> None:
    """Build Derelict_TardigradeCapsule.fbx: small half-buried Beresheet-nod experiment capsule with TinyWindow."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials(use_vertex_color_shading=False)
    root = r1kit.create_empty("Derelict_TardigradeCapsule", None, (0.0, 0.0, 0.0))

    pitch_deg = 22.0
    roll_deg = 12.0
    piv_u = (0.0, 0.22, 0.04)
    tilt_mat_b = r1kit.u_rot(pitch_deg, 0.0, roll_deg).to_matrix()

    def _pose(pt_u: tuple[float, float, float]) -> tuple[float, float, float]:
        pb = r1kit.u_pos(*piv_u)
        vb = pb + (tilt_mat_b @ (r1kit.u_pos(*pt_u) - pb))
        return (float(vb.x), float(vb.z), float(vb.y))

    def _rot(rx: float = 0.0, ry: float = 0.0, rz: float = 0.0) -> tuple[float, float, float]:
        return (pitch_deg + rx, ry, roll_deg + rz)

    # 1. BurialMound: soft mare regolith mound & impact rim cradling the half-buried capsule
    mound = r1kit.MeshBuilder("BurialMound", pivot_u=(0.0, 0.0, 0.0))
    mound.add_weathered_rock(
        center_u=(0.0, -0.04, 0.04),
        size_u=(0.88, 0.24, 0.92),
        seed=6042,
        lon_segs=14,
        lat_rings=6,
        fractures_u=(((0.15, 0.96, 0.22), 0.16, 0.84),),
        pits_u=(),
        crevices_u=(),
        skirt_flare=0.22,
        burial_frac=0.20,
        mat="R1_Basalt",
    )
    mound.build_object(parent=root, smooth_angle_deg=52.0)

    # 2. CapsuleBody: compact sealed Beresheet-nod biological/archival vault (white enamel + gold MLI foil + brass)
    cap = r1kit.MeshBuilder("CapsuleBody", pivot_u=piv_u)
    # Main octagonal sealed pressure drum
    cap.add_cylinder(
        center_u=_pose((0.0, 0.22, 0.04)),
        radius=0.26,
        radius_top=0.24,
        length=0.34,
        mat="R1_WhitePaint",
        axis_u="Y",
        segments=8,
        bevel=0.018,
        bevel_segments=1,
        rot_deg_u=_rot(),
    )
    # Gold Kapton MLI thermal shroud band around lower waist
    cap.add_cylinder(
        center_u=_pose((0.0, 0.15, 0.04)),
        radius=0.272,
        radius_top=0.268,
        length=0.14,
        mat="R1_GoldFoil",
        axis_u="Y",
        segments=8,
        bevel=0.010,
        bevel_segments=1,
        rot_deg_u=_rot(),
    )
    # Heavy bolted steel & brass containment rings ("Still sealed. You don't open it")
    cap.add_torus(
        center_u=_pose((0.0, 0.34, 0.04)),
        major_radius=0.23,
        minor_radius=0.024,
        mat="R1_BareSteel",
        axis_u="Y",
        major_segments=12,
        minor_segments=8,
        rot_deg_u=_rot(),
    )
    # Two carry/lift handles on the sides so 07 can "scan it, or carry it home to a shelf"
    for sx in (-1.0, 1.0):
        cap.add_bevel_box(
            center_u=_pose((sx * 0.27, 0.26, 0.04)),
            size_u=(0.045, 0.09, 0.16),
            mat="R1_BareSteel",
            bevel=0.006,
            segments=1,
            rot_deg_u=_rot(),
        )
    # Brass viewport bezel frame on the +Z upper-front face surrounding TinyWindow
    cap.add_cylinder(
        center_u=_pose((0.0, 0.26, 0.285)),
        radius=0.056,
        radius_top=0.050,
        length=0.036,
        mat="R1_Brass",
        axis_u="Z",
        segments=10,
        bevel=0.005,
        bevel_segments=1,
        rot_deg_u=_rot(),
    )
    cap.build_object(parent=root, smooth_angle_deg=48.0)

    # 3. ArchivePlaque: multi-layer optical Lunar Library / tardigrade arch disc carrier on top lid
    plaque = r1kit.MeshBuilder("ArchivePlaque", pivot_u=_pose((0.0, 0.39, 0.04)))
    plaque.add_cylinder(
        center_u=_pose((0.0, 0.395, 0.04)),
        radius=0.16,
        radius_top=0.14,
        length=0.032,
        mat="R1_Copper",
        axis_u="Y",
        segments=10,
        bevel=0.005,
        bevel_segments=1,
        rot_deg_u=_rot(),
    )
    plaque.add_cylinder(
        center_u=_pose((0.0, 0.412, 0.04)),
        radius=0.105,
        radius_top=0.100,
        length=0.018,
        mat="R1_Brass",
        axis_u="Y",
        segments=10,
        bevel=0.003,
        bevel_segments=1,
        rot_deg_u=_rot(),
    )
    plaque.build_object(parent=root, smooth_angle_deg=48.0)

    # 4. TinyWindow: a tiny lit window (~5.5 cm diameter) with warm amber dormant life-support glow
    #    Uses R1_LampAmber in FBX, overridden in Unity to bloom-safe R1_TardigradeWindow.mat
    win = r1kit.MeshBuilder("TinyWindow", pivot_u=_pose((0.0, 0.26, 0.302)))
    win.add_cylinder(
        center_u=_pose((0.0, 0.26, 0.302)),
        radius=0.034,
        radius_top=0.030,
        length=0.022,
        mat="R1_LampAmber",
        axis_u="Z",
        segments=10,
        bevel=0.004,
        bevel_segments=1,
        rot_deg_u=_rot(),
    )
    win.build_object(parent=root, smooth_angle_deg=48.0)

    r1kit.create_empty("Base_Anchor", root, (0.0, 0.0, 0.0))
    r1kit.create_empty("ScanTarget_Anchor", root, _pose((0.0, 0.41, 0.04)))
    r1kit.create_empty("CarryGrip_Anchor", root, _pose((0.0, 0.32, 0.04)))
    r1kit.create_empty("Window_Anchor", root, _pose((0.0, 0.26, 0.31)))

    r1kit.apply_triplanar_uvs(root, texel_scale=0.95)

    r1kit.add_maker_decal_node(
        parent=root,
        name="Decal_ILC_1",
        cell_key="ILC_SEAL",
        center_u=_pose((0.0, 0.26, -0.21)),
        normal_u=(0.0, -0.37, -0.93),
        up_u=(0.0, 0.93, -0.37),
        width=0.11,
        height=0.11,
    )

    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=0.65)
    _ensure_non_uniform_vcol(root)
    r1kit.export_fbx(root, out_fbx, bake_space_transform=False)
    print(f"Exported {out_fbx}")


def main() -> None:
    repo_root = HERE.parents[2]
    mare_dir = repo_root / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Mare"
    mare_dir.mkdir(parents=True, exist_ok=True)

    # 1. Mare ground patch & decals
    build_mare_ground_patch(mare_dir / "MareGround_Patch.fbx")
    build_decal_mare_swirl(mare_dir / "Decal_MareSwirl.fbx")
    build_decal_mare_ejecta_ray(mare_dir / "Decal_MareEjectaRay.fbx")

    # 2. Mare rock kit (6 basalt boulders/outcrops + 2 Ilmenite drill-target outcrops)
    build_mare_basalt_rocks(mare_dir)
    build_ilmenite_outcrop_00(mare_dir / "MareOutcrop_Ilmenite_00.fbx")
    build_ilmenite_outcrop_01(mare_dir / "MareOutcrop_Ilmenite_01.fbx")

    # 3. Rille pass modular wall/slope chunks (4 pieces)
    build_rille_wall_straight(mare_dir / "Rille_Wall_Straight.fbx")
    build_rille_wall_curve(mare_dir / "Rille_Wall_Curve.fbx")
    build_rille_slope_ramp(mare_dir / "Rille_Slope_Ramp.fbx")
    build_rille_mouth_corner(mare_dir / "Rille_Mouth_Corner.fbx")

    # 4. Mare derelict set (stopped convoy, crashed survey drone, half-buried tardigrade capsule)
    build_derelict_mare_convoy(mare_dir / "Derelict_MareConvoy.fbx")
    build_derelict_mare_survey_drone(mare_dir / "Derelict_MareSurveyDrone.fbx")
    build_derelict_tardigrade_capsule(mare_dir / "Derelict_TardigradeCapsule.fbx")


if __name__ == "__main__":
    main()
