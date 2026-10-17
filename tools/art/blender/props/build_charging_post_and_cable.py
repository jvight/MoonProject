"""Generate Deliverable 1 R1 story props:
- Assets/_Project/Art/R1/Models/Props/DeadChargingPost.fbx
- Assets/_Project/Art/R1/Models/Props/GroundCableSegment.fbx
- Assets/_Project/Art/R1/Models/Props/GroundCableEnd.fbx

Run headless:
  D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/props/build_charging_post_and_cable.py -- --skip-renders
"""

from __future__ import annotations

import math
import sys
from pathlib import Path
from typing import List, Tuple

import bpy

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
REPO_ROOT = BLENDER_DIR.parents[2]

if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))

import r1kit  # noqa: E402


PROPS_DIR = REPO_ROOT / "Assets" / "_Project" / "Art" / "R1" / "Models" / "Props"


def ensure_extended_r1_materials() -> None:
    """Register R1_Screen, R1_Brass, and R1_Signage into r1kit at runtime."""
    extra_specs = {
        "R1_Screen": {
            "base_color": r1kit._srgb_to_linear("#1A140E"),
            "wear_color": r1kit._srgb_to_linear("#34281C"),
            "dust_color": r1kit._srgb_to_linear("#5E564C"),
            "metallic": 0.05,
            "roughness": 0.12,
            "emission": r1kit._srgb_to_linear("#FFB24A"),
            "emission_strength": 1.0,
        },
        "R1_Brass": {
            "base_color": r1kit._srgb_to_linear("#DBB052"),
            "wear_color": r1kit._srgb_to_linear("#F7D98C"),
            "dust_color": r1kit._srgb_to_linear("#8A8270"),
            "metallic": 0.92,
            "roughness": 0.24,
            "emission": (0.0, 0.0, 0.0, 1.0),
            "emission_strength": 0.0,
        },
        "R1_Signage": {
            "base_color": r1kit._srgb_to_linear("#EDE3CE"),
            "wear_color": r1kit._srgb_to_linear("#464952"),
            "dust_color": r1kit._srgb_to_linear("#8D8B86"),
            "metallic": 0.04,
            "roughness": 0.36,
            "emission": (0.0, 0.0, 0.0, 1.0),
            "emission_strength": 0.0,
        },
    }
    allowed = list(r1kit.ALLOWED_MATERIALS)
    for name, spec in extra_specs.items():
        if name not in allowed:
            allowed.append(name)
        r1kit.MATERIAL_SPECS[name] = spec
    r1kit.ALLOWED_MATERIALS = tuple(allowed)


def boost_dust_and_weathering(root: bpy.types.Object, extra_dust: float = 0.18, darken_ao: float = 0.88) -> None:
    """Enhance vertex-color dust (B) and cavity AO darkening (R) for cold, dusty story props."""
    for obj in r1kit.collect_hierarchy(root):
        if obj.type != "MESH" or obj.data is None:
            continue
        mesh = obj.data
        if "Col" not in mesh.color_attributes:
            continue
        attr = mesh.color_attributes["Col"]
        for item in attr.data:
            r, g, b, a = item.color
            r_new = max(0.15, min(1.0, r * darken_ao))
            g_new = max(0.0, min(1.0, g * 1.12))
            b_new = max(0.0, min(1.0, b + extra_dust))
            item.color = (r_new, g_new, b_new, a)


def build_dead_charging_post() -> bpy.types.Object:
    """Build 07's waist-high 1980s dead charging post (~0.66 W x 0.78 L x 0.99 H m)."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("DeadChargingPost", pivot_u=(0.0, 0.0, 0.0))

    # 1. Main post body, plinth, cowls, dead indicator brow, breaker box
    body = r1kit.MeshBuilder("PostBody", pivot_u=(0.0, 0.0, 0.0))

    # Heavy rubber ground isolation pad + chamfered cast-aluminium anchor plinth
    body.add_oct_prism((0.0, 0.022, -0.02), 0.66, 0.044, 0.62, chamfer=0.09, mat="R1_Rubber", bevel=0.010)
    body.add_oct_prism((0.0, 0.085, -0.02), 0.60, 0.090, 0.56, chamfer=0.08, mat="R1_GreyMetal", bevel=0.014)

    # 4 corner anchor bolts with washers on plinth
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            bx = sx * 0.23
            bz = -0.02 + sz * 0.21
            body.add_cylinder((bx, 0.138, bz), 0.026, 0.016, mat="R1_GreyMetal", axis_u="Y", segments=8, bevel=0.003)
            body.add_cylinder((bx, 0.154, bz), 0.016, 0.022, mat="R1_BareSteel", axis_u="Y", segments=6, bevel=0.003)

    # Lower pedestal skirt + orange hazard stripe band
    body.add_bevel_box((0.0, 0.20, -0.02), (0.48, 0.16, 0.44), mat="R1_GreyMetal", bevel=0.016)
    body.add_bevel_box((0.0, 0.22, -0.02), (0.492, 0.055, 0.452), mat="R1_OrangeStripe", bevel=0.006)

    # Main waist-high service column core (Y = 0.28 .. 0.76)
    body.add_bevel_box((0.0, 0.52, -0.02), (0.40, 0.48, 0.34), mat="R1_GreyMetal", bevel=0.018)

    # 1980s cream enamel side & rear service cowls (chipped/weathered via vertex colors)
    body.add_bevel_box((-0.195, 0.53, -0.02), (0.032, 0.42, 0.28), mat="R1_CreamEnamel", bevel=0.008)
    body.add_bevel_box((0.195, 0.53, -0.02), (0.032, 0.42, 0.28), mat="R1_CreamEnamel", bevel=0.008)
    body.add_bevel_box((0.0, 0.54, 0.155), (0.32, 0.40, 0.032), mat="R1_CreamEnamel", bevel=0.008)

    # Field repair sage patch on left cowl + 4 corner rivets
    body.add_bevel_box((-0.212, 0.46, 0.02), (0.012, 0.14, 0.13), mat="R1_SagePatch", bevel=0.003)
    body.add_bolts(
        [
            (-0.220, 0.46 + sy * 0.052, 0.02 + sz * 0.048)
            for sy in (-1.0, 1.0)
            for sz in (-1.0, 1.0)
        ],
        radius=0.006,
        height=0.008,
        axis_u="X",
        mat="R1_BareSteel",
    )

    # Right-side manual breaker box with tripped (DOWN/OFF) disconnect lever
    body.add_bevel_box((0.235, 0.54, -0.03), (0.065, 0.20, 0.15), mat="R1_GreyMetal", bevel=0.008)
    body.add_bevel_box((0.270, 0.58, -0.03), (0.018, 0.045, 0.055), mat="R1_OrangeStripe", bevel=0.004)
    # Tripped handle pointing down
    body.add_cylinder((0.275, 0.52, -0.03), 0.014, 0.036, mat="R1_BareSteel", axis_u="X", segments=8, bevel=0.002)
    body.add_bevel_box((0.290, 0.465, -0.03), (0.020, 0.11, 0.026), mat="R1_Rubber", bevel=0.004)

    # Upper sloped instrument head & sun visor hood (Y = 0.74 .. 0.96 m, waist-high to Rover 07)
    body.add_bevel_box((0.0, 0.83, -0.02), (0.43, 0.18, 0.36), mat="R1_CreamEnamel", bevel=0.016)
    body.add_bevel_box((0.0, 0.935, -0.02), (0.45, 0.050, 0.39), mat="R1_GreyMetal", bevel=0.012)
    # Protective top hood visor over front readout
    body.add_bevel_box((0.0, 0.945, -0.18), (0.42, 0.028, 0.09), mat="R1_GreyMetal", bevel=0.006)
    body.add_bevel_box((0.0, 0.905, -0.18), (0.42, 0.016, 0.06), mat="R1_OrangeStripe", bevel=0.004)

    # Side tubular steel protection bars / grab handles (left & right)
    for sx in (-1.0, 1.0):
        hx = sx * 0.22
        body.add_tube_path(
            [
                (hx, 0.32, -0.11),
                (hx * 1.24, 0.36, -0.11),
                (hx * 1.24, 0.86, -0.06),
                (hx, 0.89, -0.06),
            ],
            radius=0.015,
            mat="R1_BareSteel",
            segments=8,
        )

    # Dead unlit indicator cluster on upper front face (-Z)
    # Recessed dark bezel
    body.add_bevel_box((0.0, 0.82, -0.202), (0.34, 0.12, 0.024), mat="R1_GreyMetal", bevel=0.005)
    # Left dead status window (dark glass, unlit)
    body.add_bevel_box((-0.075, 0.82, -0.216), (0.13, 0.072, 0.012), mat="R1_DarkGlass", bevel=0.003)
    # Right dead analog charge-current gauge (brass bezel + dark glass face, zeroed needle)
    body.add_cylinder((0.095, 0.82, -0.214), 0.042, 0.016, mat="R1_Brass", axis_u="Z", segments=14, bevel=0.003)
    body.add_cylinder((0.095, 0.82, -0.220), 0.033, 0.010, mat="R1_DarkGlass", axis_u="Z", segments=14, bevel=0.002)
    # Cold dead beacon dome on very top (dark glass in brass cage - NO R1_LampAmber!)
    body.add_cylinder((0.0, 0.962, 0.04), 0.046, 0.018, mat="R1_Brass", axis_u="Y", segments=12, bevel=0.003)
    body.add_cylinder(
        (0.0, 0.985, 0.04),
        0.036,
        0.036,
        mat="R1_DarkGlass",
        axis_u="Y",
        segments=12,
        bevel=0.004,
        radius_top=0.024,
    )

    # Rear cooling louver ribs on +Z panel
    for i in range(4):
        ly = 0.64 + i * 0.042
        body.add_bevel_box((0.0, ly, 0.174), (0.24, 0.018, 0.016), mat="R1_GreyMetal", bevel=0.003)

    body.build_object(parent=root)

    # 2. Empty charging socket on front face (-Z) at Y = 0.46 m (matches 07's umbilical reach)
    socket = r1kit.MeshBuilder("EmptySocket", pivot_u=(0.0, 0.46, -0.19))
    # Rectangular mounting plate
    socket.add_bevel_box((0.0, 0.46, -0.195), (0.26, 0.26, 0.028), mat="R1_GreyMetal", bevel=0.008)
    # Heavy rubber weather boot collar protruding toward -Z
    socket.add_cylinder(
        (0.0, 0.46, -0.218),
        0.096,
        0.032,
        mat="R1_Rubber",
        axis_u="Z",
        segments=16,
        bevel=0.005,
        radius_top=0.088,
    )
    socket.add_cylinder(
        (0.0, 0.46, -0.244),
        0.086,
        0.026,
        mat="R1_Rubber",
        axis_u="Z",
        segments=16,
        bevel=0.005,
        radius_top=0.076,
    )
    # Bayonet locking ring
    socket.add_cylinder(
        (0.0, 0.46, -0.262),
        0.082,
        0.016,
        mat="R1_BareSteel",
        axis_u="Z",
        segments=16,
        bevel=0.003,
    )
    # Dark interior socket well + 3 dead copper/brass contact pins inside the empty receptacle
    socket.add_cylinder((0.0, 0.46, -0.268), 0.062, 0.012, mat="R1_DarkGlass", axis_u="Z", segments=14, bevel=0.002)
    for angle_deg in (90.0, 210.0, 330.0):
        rad = math.radians(angle_deg)
        px = math.cos(rad) * 0.026
        py = 0.46 + math.sin(rad) * 0.026
        socket.add_cylinder((px, py, -0.278), 0.008, 0.022, mat="R1_Copper", axis_u="Z", segments=8, bevel=0.001)
        socket.add_cylinder((px, py, -0.272), 0.013, 0.008, mat="R1_Brass", axis_u="Z", segments=8, bevel=0.001)

    # Open hinged dust cap hanging down below the empty socket so it clearly reads as disconnected
    socket.add_bevel_box((0.0, 0.345, -0.225), (0.070, 0.028, 0.032), mat="R1_BareSteel", bevel=0.004)
    socket.add_cylinder((0.0, 0.275, -0.242), 0.068, 0.022, mat="R1_GreyMetal", axis_u="Z", segments=14, bevel=0.004)
    socket.add_cylinder((0.0, 0.275, -0.252), 0.052, 0.010, mat="R1_OrangeStripe", axis_u="Z", segments=14, bevel=0.002)
    socket.build_object(parent=root)

    # 3. Heavy feeder cable exiting rear (+Z) down into the dust toward the lander
    cable_tail = r1kit.MeshBuilder("CableTail", pivot_u=(0.0, 0.042, 0.22))
    # Rear strain-relief gland housing on lower +Z face
    cable_tail.add_bevel_box((0.0, 0.16, 0.19), (0.16, 0.15, 0.08), mat="R1_GreyMetal", bevel=0.010)
    cable_tail.add_cylinder(
        (0.0, 0.145, 0.245),
        0.058,
        0.065,
        mat="R1_BareSteel",
        axis_u="Z",
        segments=12,
        bevel=0.004,
        radius_top=0.046,
    )
    # Heavy ribbed rubber trunk cable sweeping onto the ground to (0.0, 0.042, 0.46)
    cable_tail.add_tube_path(
        [
            (0.0, 0.145, 0.25),
            (0.0, 0.105, 0.32),
            (0.0, 0.056, 0.39),
            (0.0, 0.042, 0.46),
        ],
        radius=0.042,
        mat="R1_Rubber",
        segments=12,
    )
    # Output coupling collar at CableOrigin_Anchor (Z = +0.46)
    cable_tail.add_cylinder((0.0, 0.042, 0.445), 0.048, 0.030, mat="R1_GreyMetal", axis_u="Z", segments=12, bevel=0.003)
    cable_tail.build_object(parent=root)

    # Child anchor empties for World placement & cable routing
    r1kit.create_empty("Socket_Umbilical", parent=root, pivot_u=(0.0, 0.46, -0.27))
    r1kit.create_empty("StatusWindow_Dead", parent=root, pivot_u=(0.0, 0.82, -0.22))
    r1kit.create_empty("CableOrigin_Anchor", parent=root, pivot_u=(0.0, 0.042, 0.46))

    r1kit.apply_triplanar_uvs(root)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=0.45)
    boost_dust_and_weathering(root, extra_dust=0.22, darken_ao=0.86)

    fbx_path = PROPS_DIR / "DeadChargingPost.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def build_ground_cable_segment() -> bpy.types.Object:
    """Build tileable 8.0 m heavy ground cable segment with faint inline amber marker."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("GroundCableSegment", pivot_u=(0.0, 0.0, 0.0))

    cable = r1kit.MeshBuilder("CableRun", pivot_u=(0.0, 0.0, 0.0))

    # Subtle S-curve path from Z = -4.0 m to Z = +4.0 m with zero tangent offset at both ends
    # so segments tile seamlessly end-to-end along the ~160 m path.
    pts: List[Tuple[float, float, float]] = []
    num_steps = 24
    for i in range(num_steps + 1):
        t = i / num_steps
        z = -4.0 + 8.0 * t
        # Smooth full-wave sine meander with zero value and zero slope at t=0, 0.5, 1.0
        meander = 0.036 * math.sin(2.0 * math.pi * t) * math.sin(math.pi * t)
        pts.append((meander, 0.042, z))

    cable.add_tube_path(pts, radius=0.042, mat="R1_Rubber", segments=10)

    # End coupling collars at Z = -4.0 and Z = +4.0 so tiling joints look engineered
    for z_end in (-3.96, 3.96):
        cable.add_cylinder((0.0, 0.042, z_end), 0.048, 0.075, mat="R1_GreyMetal", axis_u="Z", segments=10, bevel=0.003)
        cable.add_cylinder((0.0, 0.042, z_end), 0.051, 0.028, mat="R1_BareSteel", axis_u="Z", segments=10, bevel=0.002)

    # Intermediate ground armor clamps at Z = -2.0 and Z = +2.0
    for z_clamp, x_off in ((-2.0, -0.025), (2.0, 0.025)):
        cable.add_bevel_box((x_off, 0.036, z_clamp), (0.13, 0.072, 0.14), mat="R1_GreyMetal", bevel=0.008)
        cable.add_bevel_box((x_off, 0.074, z_clamp), (0.10, 0.012, 0.08), mat="R1_BareSteel", bevel=0.003)

    # Center 8 m inline repeater / marker collar at Z = 0.0
    cable.add_bevel_box((0.0, 0.044, 0.0), (0.20, 0.088, 0.34), mat="R1_Rubber", bevel=0.012)
    cable.add_bevel_box((0.0, 0.052, 0.0), (0.17, 0.086, 0.24), mat="R1_GreyMetal", bevel=0.010)
    cable.add_bevel_box((0.0, 0.054, -0.075), (0.176, 0.088, 0.032), mat="R1_OrangeStripe", bevel=0.004)
    cable.add_bevel_box((0.0, 0.054, 0.075), (0.176, 0.088, 0.032), mat="R1_OrangeStripe", bevel=0.004)
    # Hooded brass guard over the dim marker slit so it only peeks out as a tiny breadcrumb
    cable.add_bevel_box((0.0, 0.098, 0.0), (0.084, 0.018, 0.096), mat="R1_Brass", bevel=0.004)

    cable.build_object(parent=root)

    # Tiny recessed marker pip as separate child node MarkerLamp so emission can be dimmed via material/MPB
    marker = r1kit.MeshBuilder("MarkerLamp", pivot_u=(0.0, 0.098, 0.0))
    marker.add_bevel_box((0.0, 0.101, 0.0), (0.038, 0.012, 0.052), mat="R1_LampAmber", bevel=0.002)
    marker.build_object(parent=root)

    # Tiling anchors at exact start (-4.0 m) and end (+4.0 m)
    r1kit.create_empty("Start_Anchor", parent=root, pivot_u=(0.0, 0.042, -4.0))
    r1kit.create_empty("End_Anchor", parent=root, pivot_u=(0.0, 0.042, 4.0))

    r1kit.apply_triplanar_uvs(root)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=0.35)
    boost_dust_and_weathering(root, extra_dust=0.20, darken_ao=0.88)

    fbx_path = PROPS_DIR / "GroundCableSegment.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def build_ground_cable_end() -> bpy.types.Object:
    """Build 1.60 m terminal ground cable piece with strain-relief anchor block and bayonet plug."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("GroundCableEnd", pivot_u=(0.0, 0.0, 0.0))

    lead = r1kit.MeshBuilder("CableLead", pivot_u=(0.0, 0.0, 0.0))

    # Input collar at Z = 0.0 matching GroundCableSegment end
    lead.add_cylinder((0.0, 0.042, 0.038), 0.048, 0.075, mat="R1_GreyMetal", axis_u="Z", segments=10, bevel=0.003)
    lead.add_cylinder((0.0, 0.042, 0.038), 0.051, 0.028, mat="R1_BareSteel", axis_u="Z", segments=10, bevel=0.002)

    # Heavy ground anchor block with strain-relief clamp at Z = 0.65 m
    lead.add_bevel_box((0.0, 0.045, 0.65), (0.34, 0.090, 0.28), mat="R1_GreyMetal", bevel=0.012)
    lead.add_bevel_box((0.0, 0.088, 0.65), (0.26, 0.024, 0.20), mat="R1_OrangeStripe", bevel=0.006)
    for sx in (-1.0, 1.0):
        lead.add_cylinder((sx * 0.12, 0.095, 0.65), 0.016, 0.024, mat="R1_BareSteel", axis_u="Y", segments=6, bevel=0.003)

    # Armored rubber cable sweeping from ground (Z=0.0) through anchor block up to service port socket height (Y=0.35, Z=1.42)
    lead.add_tube_path(
        [
            (0.0, 0.042, 0.0),
            (0.02, 0.042, 0.34),
            (0.0, 0.046, 0.65),
            (-0.02, 0.052, 0.96),
            (0.0, 0.14, 1.20),
            (0.0, 0.28, 1.34),
            (0.0, 0.35, 1.44),
        ],
        radius=0.042,
        mat="R1_Rubber",
        segments=12,
    )

    # Heavy 1980s bayonet umbilical plug head at (0.0, 0.35, 1.44..1.58)
    lead.add_cylinder(
        (0.0, 0.35, 1.41),
        0.046,
        0.065,
        mat="R1_Rubber",
        axis_u="Z",
        segments=12,
        bevel=0.004,
        radius_top=0.062,
    )
    lead.add_cylinder((0.0, 0.35, 1.475), 0.066, 0.065, mat="R1_GreyMetal", axis_u="Z", segments=12, bevel=0.004)
    lead.add_cylinder((0.0, 0.35, 1.515), 0.070, 0.022, mat="R1_Brass", axis_u="Z", segments=12, bevel=0.002)
    lead.add_cylinder((0.0, 0.35, 1.545), 0.054, 0.038, mat="R1_BareSteel", axis_u="Z", segments=12, bevel=0.003)
    # Top locking latch handle on plug
    lead.add_bevel_box((0.0, 0.428, 1.48), (0.036, 0.026, 0.085), mat="R1_OrangeStripe", bevel=0.004)

    lead.build_object(parent=root)

    r1kit.create_empty("Start_Anchor", parent=root, pivot_u=(0.0, 0.042, 0.0))
    r1kit.create_empty("Plug_Anchor", parent=root, pivot_u=(0.0, 0.35, 1.56))

    r1kit.apply_triplanar_uvs(root)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=0.35)
    boost_dust_and_weathering(root, extra_dust=0.20, darken_ao=0.88)

    fbx_path = PROPS_DIR / "GroundCableEnd.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def main() -> None:
    PROPS_DIR.mkdir(parents=True, exist_ok=True)
    build_dead_charging_post()
    build_ground_cable_segment()
    build_ground_cable_end()


if __name__ == "__main__":
    main()
