"""Generate Deliverable 2 R1 story props:
- Assets/_Project/Art/R1/Models/Props/LanderServicePort.fbx
- Assets/_Project/Art/R1/Models/Props/LanderBeacon.fbx

Run headless:
  D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/props/build_lander_port_and_beacon.py -- --skip-renders
"""

from __future__ import annotations

import math
import sys
from pathlib import Path

import bpy
from mathutils import Vector

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


def apply_screen_planar_uvs(
    obj: bpy.types.Object,
    x_min: float,
    x_max: float,
    y_u_min: float,
    y_u_max: float,
    u_range: tuple[float, float] = (0.0, 1.0),
    v_range: tuple[float, float] = (0.0, 1.0),
) -> None:
    """Map front-facing (-Z Unity / -Y Blender) screen polygons to clean planar UV0 coordinates."""
    mesh = obj.data
    if not mesh.uv_layers:
        mesh.uv_layers.new(name="UVMap")
    uv_layer = mesh.uv_layers.active.data
    world_mat = obj.matrix_world
    span_x = max(1e-5, x_max - x_min)
    span_y = max(1e-5, y_u_max - y_u_min)
    u0, u1 = u_range
    v0, v1 = v_range

    for poly in mesh.polygons:
        n_world = (world_mat.to_3x3() @ poly.normal).normalized()
        # Front face points toward -Z Unity (-Y Blender)
        if n_world.y < -0.55:
            for loop_idx in poly.loop_indices:
                v_idx = mesh.loops[loop_idx].vertex_index
                co_w = world_mat @ mesh.vertices[v_idx].co
                # co_w.x is Unity X; co_w.z is Unity Y (up)
                u_norm = max(0.0, min(1.0, (co_w.x - x_min) / span_x))
                v_norm = max(0.0, min(1.0, (co_w.z - y_u_min) / span_y))
                uv_layer[loop_idx].uv = (u0 + u_norm * (u1 - u0), v0 + v_norm * (v1 - v0))
        else:
            for loop_idx in poly.loop_indices:
                uv_layer[loop_idx].uv = (u0 + 0.01 * (u1 - u0), v0 + 0.01 * (v1 - v0))


def build_lander_service_port() -> bpy.types.Object:
    """Build CargoLander side service port (~0.88 W x 0.38 D x 1.18 H m) with pad cable socket & LANDER RESERVE readout."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("LanderServicePort", pivot_u=(0.0, 0.0, 0.0))

    # 1. Main hull-mounted service housing (back mounting plane at Z = +0.12, front face toward -Z)
    housing = r1kit.MeshBuilder("PortHousing", pivot_u=(0.0, 0.0, 0.0))

    # Rear structural hull-adapter flange & rubber vibration seal
    housing.add_bevel_box((0.0, 0.59, 0.095), (0.88, 1.18, 0.050), mat="R1_GreyMetal", bevel=0.014)
    housing.add_bevel_box((0.0, 0.59, 0.125), (0.84, 1.14, 0.018), mat="R1_Rubber", bevel=0.004)

    # Gold Kapton thermal insulation pillows tucked into upper & lower rear hull recesses
    housing.add_bevel_box((0.0, 1.04, 0.045), (0.76, 0.18, 0.075), mat="R1_GoldFoil", bevel=0.012)
    housing.add_bevel_box((0.0, 0.14, 0.045), (0.76, 0.18, 0.075), mat="R1_GoldFoil", bevel=0.012)

    # Main 1980s cream-enamel instrument & umbilical console body
    housing.add_bevel_box((0.0, 0.59, -0.025), (0.80, 0.96, 0.21), mat="R1_CreamEnamel", bevel=0.018)

    # Top and bottom orange hazard trim bands
    housing.add_bevel_box((0.0, 1.03, -0.035), (0.81, 0.055, 0.22), mat="R1_OrangeStripe", bevel=0.006)
    housing.add_bevel_box((0.0, 0.15, -0.035), (0.81, 0.055, 0.22), mat="R1_OrangeStripe", bevel=0.006)

    # Left & right vertical steel protective rub-rails + corner bolts
    for sx in (-1.0, 1.0):
        rx = sx * 0.415
        housing.add_bevel_box((rx, 0.59, -0.03), (0.038, 1.04, 0.23), mat="R1_GreyMetal", bevel=0.008)
        housing.add_tube_path(
            [
                (sx * 0.38, 0.22, -0.13),
                (sx * 0.38, 0.26, -0.21),
                (sx * 0.38, 0.92, -0.21),
                (sx * 0.38, 0.96, -0.13),
            ],
            radius=0.014,
            mat="R1_BareSteel",
            segments=8,
        )

    housing.add_bolts(
        [
            (sx * 0.415, y_b, -0.148)
            for sx in (-1.0, 1.0)
            for y_b in (0.20, 0.59, 0.98)
        ],
        radius=0.011,
        height=0.010,
        axis_u="Z",
        mat="R1_BareSteel",
    )

    # Upper readout sun-hood & recessed bezel framing the LANDER RESERVE screen (Y = 0.68..0.98)
    housing.add_bevel_box((0.0, 0.83, -0.145), (0.64, 0.31, 0.055), mat="R1_GreyMetal", bevel=0.008)
    housing.add_bevel_box((0.0, 0.975, -0.185), (0.66, 0.028, 0.095), mat="R1_GreyMetal", bevel=0.005)
    # Brass corner bezel screws around readout
    housing.add_bolts(
        [
            (sx * 0.285, 0.83 + sy * 0.125, -0.174)
            for sx in (-1.0, 1.0)
            for sy in (-1.0, 1.0)
        ],
        radius=0.008,
        height=0.008,
        axis_u="Z",
        mat="R1_Brass",
    )

    # Lower umbilical service bay recess (Y = 0.22..0.62)
    housing.add_bevel_box((0.0, 0.42, -0.138), (0.66, 0.38, 0.042), mat="R1_GreyMetal", bevel=0.008)
    # Field repair sage patch + rotary selector switch on right side of service bay
    housing.add_bevel_box((0.21, 0.48, -0.162), (0.15, 0.16, 0.014), mat="R1_SagePatch", bevel=0.003)
    housing.add_cylinder((0.21, 0.48, -0.175), 0.036, 0.022, mat="R1_Brass", axis_u="Z", segments=12, bevel=0.003)
    housing.add_bevel_box((0.21, 0.48, -0.192), (0.018, 0.058, 0.018), mat="R1_BareSteel", bevel=0.003, rot_deg_u=(0.0, 0.0, 28.0))

    housing.build_object(parent=root)

    # 2. Dedicated LANDER RESERVE readout screen surface (mapped cleanly to [0, 1] UVs)
    # Center at (0.0, 0.83, -0.172), width = 0.52 m, height = 0.22 m
    screen = r1kit.MeshBuilder("ReadoutScreen", pivot_u=(0.0, 0.83, -0.172))
    screen.add_bevel_box((0.0, 0.83, -0.172), (0.52, 0.22, 0.016), mat="R1_Signage", bevel=0.003)
    screen_obj = screen.build_object(parent=root)

    # 3. Heavy cable socket (where the ChargingPad / GroundCableEnd connects at Y = 0.35 m)
    # plus Rover 07's B3 emergency trickle receptacle alongside it
    socket = r1kit.MeshBuilder("CableSocket", pivot_u=(0.0, 0.35, -0.18))
    # Main ChargingPad / GroundCableEnd heavy bayonet socket at (-0.08, 0.35, -0.18..-0.25)
    socket.add_cylinder((-0.08, 0.35, -0.180), 0.095, 0.048, mat="R1_GreyMetal", axis_u="Z", segments=16, bevel=0.006)
    socket.add_cylinder((-0.08, 0.35, -0.216), 0.082, 0.034, mat="R1_Rubber", axis_u="Z", segments=16, bevel=0.005)
    socket.add_cylinder((-0.08, 0.35, -0.238), 0.072, 0.018, mat="R1_BareSteel", axis_u="Z", segments=16, bevel=0.003)
    socket.add_cylinder((-0.08, 0.35, -0.246), 0.054, 0.010, mat="R1_DarkGlass", axis_u="Z", segments=14, bevel=0.002)
    for angle_deg in (90.0, 210.0, 330.0):
        rad = math.radians(angle_deg)
        px = -0.08 + math.cos(rad) * 0.024
        py = 0.35 + math.sin(rad) * 0.024
        socket.add_cylinder((px, py, -0.252), 0.008, 0.016, mat="R1_Copper", axis_u="Z", segments=8, bevel=0.001)
        socket.add_cylinder((px, py, -0.248), 0.012, 0.008, mat="R1_Brass", axis_u="Z", segments=8, bevel=0.001)

    # Secondary B3 emergency rover trickle socket at (-0.23, 0.50, -0.19)
    socket.add_cylinder((-0.23, 0.50, -0.175), 0.048, 0.032, mat="R1_Brass", axis_u="Z", segments=12, bevel=0.004)
    socket.add_cylinder((-0.23, 0.50, -0.192), 0.036, 0.014, mat="R1_DarkGlass", axis_u="Z", segments=12, bevel=0.002)
    socket.add_cylinder((-0.23, 0.50, -0.198), 0.012, 0.014, mat="R1_Copper", axis_u="Z", segments=8, bevel=0.001)
    socket.build_object(parent=root)

    # 4. Small warm amber pilot indicator next to the readout
    pilot = r1kit.MeshBuilder("ReservePilotLamp", pivot_u=(0.21, 0.34, -0.175))
    pilot.add_cylinder((0.21, 0.34, -0.168), 0.028, 0.016, mat="R1_Brass", axis_u="Z", segments=10, bevel=0.002)
    pilot.add_cylinder((0.21, 0.34, -0.178), 0.019, 0.012, mat="R1_LampAmber", axis_u="Z", segments=10, bevel=0.002)
    pilot.build_object(parent=root)

    # Anchor empties for World placement & cable connection
    r1kit.create_empty("HullMount_Anchor", parent=root, pivot_u=(0.0, 0.59, 0.13))
    r1kit.create_empty("Socket_Cable", parent=root, pivot_u=(-0.08, 0.35, -0.25))
    r1kit.create_empty("Socket_Trickle", parent=root, pivot_u=(-0.23, 0.50, -0.20))
    r1kit.create_empty("Readout_Anchor", parent=root, pivot_u=(0.0, 0.83, -0.18))

    r1kit.apply_triplanar_uvs(root)
    # Override ReadoutScreen front face with clean [0, 1] planar UVs so dynamic or baked 2-line text maps 1:1
    apply_screen_planar_uvs(
        screen_obj,
        x_min=-0.26,
        x_max=0.26,
        y_u_min=0.72,
        y_u_max=0.94,
        u_range=(0.0, 1.0),
        v_range=(0.0, 1.0),
    )
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=0.50)

    fbx_path = PROPS_DIR / "LanderServicePort.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def build_lander_beacon() -> bpy.types.Object:
    """Build CargoLander emergency amber beacon (~0.52 W x 0.52 L x 0.64 H m) with bloom-safe outer dome and tiny hot core."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("LanderBeacon", pivot_u=(0.0, 0.0, 0.0))

    # 1. Base pedestal, gold thermal skirt, brass reflector cup, and 4-strut protective birdcage
    base = r1kit.MeshBuilder("BeaconHousing", pivot_u=(0.0, 0.0, 0.0))

    # Octagonal deck mounting flange + rubber pad
    base.add_oct_prism((0.0, 0.016, 0.0), 0.52, 0.032, 0.52, chamfer=0.10, mat="R1_Rubber", bevel=0.006)
    base.add_oct_prism((0.0, 0.052, 0.0), 0.48, 0.048, 0.48, chamfer=0.09, mat="R1_GreyMetal", bevel=0.010)

    # 4 corner hold-down bolts
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            base.add_cylinder(
                (sx * 0.175, 0.084, sz * 0.175),
                0.016,
                0.020,
                mat="R1_BareSteel",
                axis_u="Y",
                segments=6,
                bevel=0.002,
            )

    # Gold Kapton thermal blanket wrapped around the cylindrical riser column (Y = 0.075..0.21)
    base.add_cylinder((0.0, 0.145, 0.0), 0.175, 0.14, mat="R1_GoldFoil", axis_u="Y", segments=16, bevel=0.008)
    # Orange emergency identification band
    base.add_cylinder((0.0, 0.225, 0.0), 0.185, 0.042, mat="R1_OrangeStripe", axis_u="Y", segments=16, bevel=0.004)
    # Machined brass Fresnel base collar & reflector cup (Y = 0.245..0.295)
    base.add_cylinder(
        (0.0, 0.270, 0.0),
        0.195,
        0.050,
        mat="R1_Brass",
        axis_u="Y",
        segments=18,
        bevel=0.006,
        radius_top=0.175,
    )

    # 4 arched bare-steel guard cage ribs protecting the Fresnel dome
    for angle_deg in (45.0, 135.0, 225.0, 315.0):
        rad = math.radians(angle_deg)
        cx = math.cos(rad)
        cz = math.sin(rad)
        base.add_tube_path(
            [
                (cx * 0.182, 0.28, cz * 0.182),
                (cx * 0.205, 0.36, cz * 0.205),
                (cx * 0.195, 0.54, cz * 0.195),
                (cx * 0.125, 0.605, cz * 0.125),
            ],
            radius=0.012,
            mat="R1_BareSteel",
            segments=8,
        )

    # Mid-height guard ring around the dome
    for i in range(8):
        a0 = math.radians(i * 45.0)
        a1 = math.radians((i + 1) * 45.0)
        base.add_strut(
            (math.cos(a0) * 0.198, 0.43, math.sin(a0) * 0.198),
            (math.cos(a1) * 0.198, 0.43, math.sin(a1) * 0.198),
            width=0.014,
            thickness=0.014,
            mat="R1_BareSteel",
            bevel=0.002,
        )

    # Top cap plate over the beacon dome (Y = 0.59..0.64 m)
    base.add_cylinder(
        (0.0, 0.605, 0.0),
        0.145,
        0.028,
        mat="R1_GreyMetal",
        axis_u="Y",
        segments=16,
        bevel=0.005,
        radius_top=0.115,
    )
    base.add_cylinder((0.0, 0.628, 0.0), 0.065, 0.022, mat="R1_Brass", axis_u="Y", segments=12, bevel=0.004)

    base.build_object(parent=root)

    # 2. Stepped Fresnel amber beacon dome (R1_LampAmber) with central hot core ring
    lens = r1kit.MeshBuilder("BeaconLens", pivot_u=(0.0, 0.44, 0.0))
    # Lower and upper Fresnel lens drums (slightly narrower than the central hot-core waist)
    lens.add_cylinder(
        (0.0, 0.34, 0.0),
        0.142,
        0.095,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=18,
        bevel=0.006,
        radius_top=0.148,
    )
    # Central hot-core Fresnel bullseye ring at Y = 0.44 m
    lens.add_cylinder(
        (0.0, 0.44, 0.0),
        0.156,
        0.095,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=18,
        bevel=0.008,
    )
    lens.add_cylinder(
        (0.0, 0.535, 0.0),
        0.146,
        0.090,
        mat="R1_LampAmber",
        axis_u="Y",
        segments=18,
        bevel=0.008,
        radius_top=0.118,
    )
    lens_obj = lens.build_object(parent=root)

    r1kit.create_empty("Mount_Anchor", parent=root, pivot_u=(0.0, 0.0, 0.0))
    r1kit.create_empty("Light_Anchor", parent=root, pivot_u=(0.0, 0.44, 0.0))

    r1kit.apply_triplanar_uvs(root)
    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=0.35)

    # Shape vertex color AO (R) on BeaconLens so only the tiny center bullseye band hits peak core
    # while upper/lower Fresnel rings stay gently attenuated below bloom threshold 1.25.
    if lens_obj.data and "Col" in lens_obj.data.color_attributes:
        attr = lens_obj.data.color_attributes["Col"]
        world_mat = lens_obj.matrix_world
        for loop in lens_obj.data.loops:
            co_w = world_mat @ lens_obj.data.vertices[loop.vertex_index].co
            # Unity Y is Blender Z; center bullseye is around Z = 0.44
            dist_y = abs(co_w.z - 0.44)
            r, g, b, a = attr.data[loop.index].color
            if dist_y < 0.035:
                r_new = 1.0
                b_new = max(0.0, b * 0.25)
            else:
                r_new = max(0.55, r * 0.78)
                b_new = min(1.0, b + 0.10)
            attr.data[loop.index].color = (r_new, g, b_new, a)

    fbx_path = PROPS_DIR / "LanderBeacon.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def main() -> None:
    PROPS_DIR.mkdir(parents=True, exist_ok=True)
    build_lander_service_port()
    build_lander_beacon()


if __name__ == "__main__":
    main()
