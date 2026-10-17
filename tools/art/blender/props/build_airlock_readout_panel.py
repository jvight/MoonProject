"""Generate Deliverable 4 R1 story prop:
- Assets/_Project/Art/R1/Models/Props/AirlockReadoutPanel.fbx

Run headless:
  D:/Blender/blender.exe -b --factory-startup --python-exit-code 1 -P tools/art/blender/props/build_airlock_readout_panel.py -- --skip-renders
"""

from __future__ import annotations

import sys
from pathlib import Path

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


def apply_screen_planar_uvs(
    obj: bpy.types.Object,
    x_min: float,
    x_max: float,
    y_u_min: float,
    y_u_max: float,
    u_range: tuple[float, float] = (0.0, 1.0),
    v_range: tuple[float, float] = (0.0, 1.0),
) -> None:
    """Map front-facing (-Z Unity / -Y Blender) screen polygons to clean planar UV coordinates."""
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
        if n_world.y < -0.55:
            for loop_idx in poly.loop_indices:
                v_idx = mesh.loops[loop_idx].vertex_index
                co_w = world_mat @ mesh.vertices[v_idx].co
                u_norm = max(0.0, min(1.0, (co_w.x - x_min) / span_x))
                v_norm = max(0.0, min(1.0, (co_w.z - y_u_min) / span_y))
                uv_layer[loop_idx].uv = (u0 + u_norm * (u1 - u0), v0 + v_norm * (v1 - v0))
        else:
            for loop_idx in poly.loop_indices:
                uv_layer[loop_idx].uv = (u0 + 0.01 * (u1 - u0), v0 + 0.01 * (v1 - v0))


def build_airlock_readout_panel() -> bpy.types.Object:
    """Build B7 Habitat airlock readout panel (~0.68 W x 0.16 D x 0.54 H m) with 2-line amber CRT/LED screen."""
    r1kit.reset_scene()
    ensure_extended_r1_materials()
    r1kit.ensure_materials()

    root = r1kit.create_empty("AirlockReadoutPanel", pivot_u=(0.0, 0.0, 0.0))

    # 1. PanelHousing: bulkhead mounting flange, 1980s cream enamel casing, sun visor hood, tactile keypad strip
    housing = r1kit.MeshBuilder("PanelHousing", pivot_u=(0.0, 0.0, 0.0))

    # Rear rubber wall gasket & grey-metal mounting flange (Z = +0.02..+0.06)
    housing.add_bevel_box((0.0, 0.27, 0.052), (0.68, 0.54, 0.016), mat="R1_Rubber", bevel=0.003)
    housing.add_bevel_box((0.0, 0.27, 0.026), (0.66, 0.52, 0.038), mat="R1_GreyMetal", bevel=0.010)

    # 4 corner bulkhead mounting bolts
    housing.add_bolts(
        [
            (sx * 0.295, 0.27 + sy * 0.225, 0.005)
            for sx in (-1.0, 1.0)
            for sy in (-1.0, 1.0)
        ],
        radius=0.010,
        height=0.010,
        axis_u="Z",
        mat="R1_BareSteel",
    )

    # Main 1980s cream-enamel instrument enclosure (Z = -0.06..+0.01)
    housing.add_bevel_box((0.0, 0.27, -0.022), (0.60, 0.46, 0.072), mat="R1_CreamEnamel", bevel=0.012)
    # Orange identification stripe across bottom lip
    housing.add_bevel_box((0.0, 0.065, -0.026), (0.605, 0.036, 0.076), mat="R1_OrangeStripe", bevel=0.004)

    # Upper CRT/LED recessed screen bezel + protective top sun hood (Y = 0.16..0.46)
    housing.add_bevel_box((0.0, 0.31, -0.068), (0.56, 0.30, 0.036), mat="R1_GreyMetal", bevel=0.008)
    housing.add_bevel_box((0.0, 0.452, -0.088), (0.57, 0.022, 0.052), mat="R1_GreyMetal", bevel=0.004)
    # Brass bezel frame around the readout window
    housing.add_bevel_box((0.0, 0.31, -0.082), (0.51, 0.25, 0.016), mat="R1_Brass", bevel=0.004)

    # Lower 1980s tactile membrane/mechanical key row & proximity sensor window (Y = 0.09..0.15)
    housing.add_bevel_box((-0.07, 0.118, -0.064), (0.36, 0.048, 0.024), mat="R1_GreyMetal", bevel=0.004)
    for i in range(5):
        kx = -0.20 + i * 0.065
        mat_k = "R1_OrangeStripe" if i == 4 else "R1_CreamEnamel"
        housing.add_bevel_box((kx, 0.118, -0.078), (0.044, 0.028, 0.014), mat=mat_k, bevel=0.002, segments=1)

    # Proximity wake sensor dark glass lens on lower right
    housing.add_cylinder((0.20, 0.118, -0.066), 0.024, 0.018, mat="R1_Brass", axis_u="Z", segments=12, bevel=0.002)
    housing.add_cylinder((0.20, 0.118, -0.074), 0.017, 0.010, mat="R1_DarkGlass", axis_u="Z", segments=12, bevel=0.002)

    housing.build_object(parent=root)

    # 2. ReadoutScreen: rectangular CRT/LED panel surface (0.48 m W x 0.22 m H) mapped cleanly to [0, 1] UVs
    # Shows 2 lines of amber text:
    #   HABITAT SEAL · LOCKED · 28 OCT 1988
    #   SUIT 01-04 · OFFLINE · 29 OCT 1988 · 04:12
    screen = r1kit.MeshBuilder("ReadoutScreen", pivot_u=(0.0, 0.31, -0.088))
    screen.add_bevel_box((0.0, 0.31, -0.088), (0.48, 0.22, 0.014), mat="R1_Signage", bevel=0.002, segments=1)
    screen_obj = screen.build_object(parent=root)

    # 3. WakeIndicator: small amber pilot LED that wakes alongside the panel
    wake_led = r1kit.MeshBuilder("WakeIndicator", pivot_u=(0.245, 0.118, -0.072))
    wake_led.add_cylinder((0.245, 0.118, -0.072), 0.011, 0.012, mat="R1_LampAmber", axis_u="Z", segments=10, bevel=0.002)
    wake_led.build_object(parent=root)

    r1kit.create_empty("WallMount_Anchor", parent=root, pivot_u=(0.0, 0.27, 0.06))
    r1kit.create_empty("Readout_Anchor", parent=root, pivot_u=(0.0, 0.31, -0.095))

    r1kit.apply_triplanar_uvs(root)
    # Map ReadoutScreen front face to full [0, 1] UV space (and AIRLOCK_STATUS in UV1 if needed)
    apply_screen_planar_uvs(
        screen_obj,
        x_min=-0.24,
        x_max=0.24,
        y_u_min=0.20,
        y_u_max=0.42,
        u_range=(0.0, 1.0),
        v_range=(0.0, 1.0),
    )

    r1kit.prune_unused_materials(r1kit.collect_hierarchy(root))
    r1kit.bake_vertex_colors(root, footprint_radius=0.40)

    fbx_path = PROPS_DIR / "AirlockReadoutPanel.fbx"
    r1kit.export_fbx(root, fbx_path, bake_space_transform=False)
    print(f"Exported {fbx_path}")
    return root


def main() -> None:
    PROPS_DIR.mkdir(parents=True, exist_ok=True)
    build_airlock_readout_panel()


if __name__ == "__main__":
    main()
