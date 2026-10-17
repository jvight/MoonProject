"""Build R1 Lumen cargo crates (`Crate_S.fbx`, `Crate_M.fbx`, `Crate_L.fbx`).

Dimensions (`docs/design/scale.md` §5.3):
- Crate_S: 0.36 x 0.28 x 0.24 m (W x D x H)
- Crate_M: 0.60 x 0.44 x 0.38 m (W x D x H)
- Crate_L: 0.82 x 0.56 x 0.50 m (W x D x H)

Hierarchy contract (compatible with `SealedContainer` / `CrewCrate`):
- Root (`Crate_S` / `Crate_M` / `Crate_L`): MESH at ground pivot (0, 0, 0), +Z forward.
- `Lid`: child MESH pivoted on the rear hinge `(0, hinge_y, -hinge_z)` so negative X rotation lifts it open.
- `Lid/CutPoint`: child EMPTY at the front seam `(0, hinge_y, +hinge_z)` (+Z forward).
- `Lid/Seal`: child MESH (`R1_LampAmber`) along the lid seam.
- `ItemSocket`: child EMPTY inside the crate cavity `(0, item_y, 0)`.
"""

from __future__ import annotations

import argparse
import math
import sys
from pathlib import Path
from typing import Any

import bpy
from mathutils import Euler, Vector

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402
from ruins import ruins_common  # noqa: E402


CRATE_SPECS: dict[str, dict[str, Any]] = {
    "Crate_S": {
        "slug": "crate_s",
        "size_u": (0.36, 0.24, 0.28),  # (width_X, height_Y, depth_Z)
        "hinge_y": 0.168,
        "hinge_z": 0.128,
        "item_y": 0.060,
        "body_mat": "R1_CreamEnamel",
        "lid_mat": "R1_CreamEnamel",
        "side_accent_mat": "R1_CreamEnamel",
        "bumper_mat": "R1_GreyMetal",
        "liner_mat": "R1_GoldFoil",
        "rib_count_x": 3,
        "rib_count_z": 2,
        "latch_offsets_x": (0.0,),
        "strap_offsets_x": (0.0,),
        "signage_cell": "LUMEN",
        "signage_size": (0.14, 0.056),
        "corner_w": 0.036,
        "skid_h": 0.022,
        "wall_inset": 0.012,
        "bevel_main": 0.008,
    },
    "Crate_M": {
        "slug": "crate_m",
        "size_u": (0.60, 0.38, 0.44),  # (width_X, height_Y, depth_Z)
        "hinge_y": 0.266,
        "hinge_z": 0.204,
        "item_y": 0.095,
        "body_mat": "R1_CreamEnamel",
        "lid_mat": "R1_CreamEnamel",
        "side_accent_mat": "R1_SagePatch",
        "bumper_mat": "R1_GreyMetal",
        "liner_mat": "R1_GoldFoil",
        "rib_count_x": 5,
        "rib_count_z": 3,
        "latch_offsets_x": (-0.14, 0.14),
        "strap_offsets_x": (-0.17, 0.17),
        "signage_cell": "SEC_04",
        "signage_size": (0.18, 0.076),
        "corner_w": 0.048,
        "skid_h": 0.030,
        "wall_inset": 0.016,
        "bevel_main": 0.011,
    },
    "Crate_L": {
        "slug": "crate_l",
        "size_u": (0.82, 0.50, 0.56),  # (width_X, height_Y, depth_Z)
        "hinge_y": 0.350,
        "hinge_z": 0.260,
        "item_y": 0.125,
        "body_mat": "R1_SagePatch",
        "lid_mat": "R1_CreamEnamel",
        "side_accent_mat": "R1_CreamEnamel",
        "bumper_mat": "R1_GreyMetal",
        "liner_mat": "R1_GoldFoil",
        "rib_count_x": 7,
        "rib_count_z": 4,
        "latch_offsets_x": (-0.21, 0.21),
        "strap_offsets_x": (-0.24, 0.24),
        "signage_cell": "SEC04_HABITAT",
        "signage_size": (0.24, 0.096),
        "corner_w": 0.058,
        "skid_h": 0.038,
        "wall_inset": 0.020,
        "bevel_main": 0.014,
    },
}


def build_crate(name: str) -> bpy.types.Object:
    """Build a single R1 cargo crate hierarchy (`Crate_S`, `Crate_M`, or `Crate_L`) in the active Blender scene."""
    spec = CRATE_SPECS[name]
    wx, hy, dz = spec["size_u"]
    hx = wx * 0.5
    hz = dz * 0.5
    hinge_y = float(spec["hinge_y"])
    hinge_z = float(spec["hinge_z"])
    item_y = float(spec["item_y"])
    cw = float(spec["corner_w"])
    skid_h = float(spec["skid_h"])
    inset = float(spec["wall_inset"])
    bev = float(spec["bevel_main"])

    body_mat = spec["body_mat"]
    lid_mat = spec["lid_mat"]
    side_accent_mat = spec["side_accent_mat"]
    bumper_mat = spec["bumper_mat"]
    liner_mat = spec["liner_mat"]

    mb_body = r1kit.MeshBuilder(name, pivot_u=(0.0, 0.0, 0.0))

    # 1. Exact outer footprint base skids (y = 0 .. skid_h, spanning full [-hx, +hx] and [-hz, +hz])
    skid_w = cw * 1.15
    for sx in (-1.0, 1.0):
        mb_body.add_bevel_box(
            center_u=(sx * (hx - skid_w * 0.5), skid_h * 0.5, 0.0),
            size_u=(skid_w, skid_h, dz),
            mat=bumper_mat,
            bevel=min(0.004, skid_h * 0.25),
            segments=1,
        )
    # Cross fork-skid bars at front and back
    for sz in (-1.0, 1.0):
        mb_body.add_bevel_box(
            center_u=(0.0, skid_h * 0.5, sz * (hz - skid_w * 0.5)),
            size_u=(wx - skid_w * 1.6, skid_h * 0.85, skid_w * 0.9),
            mat=bumper_mat,
            bevel=min(0.004, skid_h * 0.22),
            segments=1,
        )

    # 2. Lower tub walls & floor with recessed interior cavity (y = skid_h .. hinge_y)
    tub_bot = skid_h * 0.85
    tub_top = hinge_y - 0.004
    tub_h = tub_top - tub_bot
    tub_cy = 0.5 * (tub_bot + tub_top)
    shell_w = wx - inset * 2.0
    shell_d = dz - inset * 2.0
    wall_t = max(0.024, min(wx, dz) * 0.085)

    # Floor slab
    floor_h = max(0.022, tub_h * 0.24)
    mb_body.add_bevel_box(
        center_u=(0.0, tub_bot + floor_h * 0.5, 0.0),
        size_u=(shell_w, floor_h, shell_d),
        mat=body_mat,
        bevel=bev,
        segments=2,
    )
    # Interior cushioned foil/metal cargo bed around ItemSocket
    liner_h = 0.014
    mb_body.add_bevel_box(
        center_u=(0.0, tub_bot + floor_h + liner_h * 0.5 - 0.002, 0.0),
        size_u=(shell_w - wall_t * 1.8, liner_h, shell_d - wall_t * 1.8),
        mat=liner_mat,
        bevel=0.003,
        segments=1,
    )

    # Front (+Z) and Rear (-Z) tub walls
    for sz in (-1.0, 1.0):
        mb_body.add_bevel_box(
            center_u=(0.0, tub_cy, sz * (shell_d * 0.5 - wall_t * 0.5)),
            size_u=(shell_w, tub_h, wall_t),
            mat=body_mat,
            bevel=bev * 0.85,
            segments=1,
        )
    # Left (-X) and Right (+X) tub walls
    for sx in (-1.0, 1.0):
        mb_body.add_bevel_box(
            center_u=(sx * (shell_w * 0.5 - wall_t * 0.5), tub_cy, 0.0),
            size_u=(wall_t, tub_h, shell_d - wall_t * 1.6),
            mat=side_accent_mat,
            bevel=bev * 0.85,
            segments=1,
        )

    # 3. Corner bumper columns on lower tub (spanning out to exact [-hx, +hx] and [-hz, +hz])
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            mb_body.add_bevel_box(
                center_u=(sx * (hx - cw * 0.5), (skid_h + tub_top) * 0.5, sz * (hz - cw * 0.5)),
                size_u=(cw, tub_top - skid_h * 0.8, cw),
                mat=bumper_mat,
                bevel=min(0.006, cw * 0.16),
                segments=1,
            )

    # 4. Warm-orange horizontal waist band (`R1_OrangeStripe`) around the lower tub for 30 m readability
    band_h = max(0.032, hy * 0.14)
    band_y = tub_bot + tub_h * 0.52
    for sz in (-1.0, 1.0):
        mb_body.add_bevel_box(
            center_u=(0.0, band_y, sz * (shell_d * 0.5 + 0.003)),
            size_u=(wx - cw * 1.85, band_h, 0.010),
            mat="R1_OrangeStripe",
            bevel=0.003,
            segments=1,
        )
    for sx in (-1.0, 1.0):
        mb_body.add_bevel_box(
            center_u=(sx * (shell_w * 0.5 + 0.003), band_y, 0.0),
            size_u=(0.010, band_h, dz - cw * 1.85),
            mat="R1_OrangeStripe",
            bevel=0.003,
            segments=1,
        )

    # 5. Vertical stiffening ribs along front, back, and sides (recessed within outer [wx, dz] envelope)
    rib_t = min(inset * 0.72, 0.012)
    rib_w = max(0.016, wx * 0.036)
    rib_h = tub_h * 0.82
    n_rx = int(spec["rib_count_x"])
    usable_x = wx - cw * 2.4
    for i in range(n_rx):
        rx = 0.0 if n_rx == 1 else (-usable_x * 0.5 + usable_x * (i / (n_rx - 1)))
        # Leave center clear on front (+Z) for the stencil signage plate
        for sz in (-1.0, 1.0):
            if sz > 0.0 and abs(rx) < spec["signage_size"][0] * 0.62:
                continue
            mb_body.add_bevel_box(
                center_u=(rx, tub_cy, sz * (shell_d * 0.5 + rib_t * 0.45)),
                size_u=(rib_w, rib_h, rib_t),
                mat=bumper_mat,
                bevel=0.0025,
                segments=1,
            )

    n_rz = int(spec["rib_count_z"])
    usable_z = dz - cw * 2.4
    for i in range(n_rz):
        rz = 0.0 if n_rz == 1 else (-usable_z * 0.5 + usable_z * (i / (n_rz - 1)))
        for sx in (-1.0, 1.0):
            mb_body.add_bevel_box(
                center_u=(sx * (shell_w * 0.5 + rib_t * 0.45), tub_cy, rz),
                size_u=(rib_t, rib_h, rib_w),
                mat=bumper_mat,
                bevel=0.0025,
                segments=1,
            )

    # 6. Recessed side bail handles (`R1_BareSteel` + `R1_Rubber` grip) on -X and +X
    handle_d = min(0.14, dz * 0.36)
    handle_out = min(inset * 0.88, 0.014)
    for sx in (-1.0, 1.0):
        mb_body.add_bevel_box(
            center_u=(sx * (shell_w * 0.5 + handle_out * 0.5), tub_top - 0.028, 0.0),
            size_u=(handle_out, 0.026, handle_d),
            mat="R1_BareSteel",
            bevel=0.003,
            segments=1,
        )
        mb_body.add_bevel_box(
            center_u=(sx * (shell_w * 0.5 + handle_out * 0.72), tub_top - 0.028, 0.0),
            size_u=(handle_out * 0.55, 0.018, handle_d * 0.62),
            mat="R1_Rubber",
            bevel=0.002,
            segments=1,
        )

    # 7. Stencilled code plate (`R1_Signage`) on front (+Z) face of lower tub
    sig_w, sig_h = spec["signage_size"]
    sig_z = shell_d * 0.5 + 0.004
    ruins_common.add_signage_plate(
        mb_body,
        center_u=(0.0, band_y, sig_z),
        width=sig_w,
        height=sig_h,
        cell_name=spec["signage_cell"],
        rot_deg_u=(0.0, 0.0, 0.0),
        thickness=0.008,
        frame_mat=bumper_mat,
        rivets=(sig_w >= 0.16),
    )

    # 8. Rear hinge barrel knuckles on the lower tub at (x, hinge_y, -hinge_z)
    knuckle_r = min(0.012, inset * 0.85)
    knuckle_len = min(0.08, wx * 0.18)
    for hx_off in (-wx * 0.24, wx * 0.24):
        mb_body.add_cylinder(
            center_u=(hx_off, hinge_y, -hinge_z),
            radius=knuckle_r,
            length=knuckle_len,
            mat=bumper_mat,
            axis_u="X",
            segments=8,
            bevel=0.002,
            bevel_segments=1,
        )
        mb_body.add_cylinder(
            center_u=(hx_off, hinge_y, -hinge_z),
            radius=knuckle_r * 0.55,
            length=knuckle_len * 1.18,
            mat="R1_BareSteel",
            axis_u="X",
            segments=6,
            bevel=0.001,
            bevel_segments=1,
        )

    # Lower latch keeper plates on front (+Z) tub face
    for lx in spec["latch_offsets_x"]:
        mb_body.add_bevel_box(
            center_u=(lx, hinge_y - 0.022, shell_d * 0.5 + rib_t * 0.5),
            size_u=(0.036, 0.028, rib_t),
            mat="R1_BareSteel",
            bevel=0.002,
            segments=1,
        )

    root_obj = ruins_common.to_ruin_object(mb_body, name, parent=None, origin_u=(0.0, 0.0, 0.0))

    # =========================================================================
    # Child `Lid` (pivoted on rear hinge `(0.0, hinge_y, -hinge_z)`)
    # =========================================================================
    lid_pivot_u = (0.0, hinge_y, -hinge_z)
    mb_lid = r1kit.MeshBuilder("Lid", pivot_u=lid_pivot_u)

    lid_bot = hinge_y + 0.004
    lid_rib_h = max(0.012, hy * 0.045)
    lid_shell_top = hy - lid_rib_h
    lid_shell_h = lid_shell_top - lid_bot
    lid_shell_cy = 0.5 * (lid_bot + lid_shell_top)

    # Main bevelled lid cap
    mb_lid.add_bevel_box(
        center_u=(0.0, lid_shell_cy, 0.0),
        size_u=(wx - inset * 1.2, lid_shell_h, dz - inset * 1.2),
        mat=lid_mat,
        bevel=bev,
        segments=2,
    )

    # Lid corner bumpers matching the lower tub corners out to exact [-hx, +hx] and [-hz, +hz]
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            mb_lid.add_bevel_box(
                center_u=(sx * (hx - cw * 0.5), (lid_bot + hy) * 0.5, sz * (hz - cw * 0.5)),
                size_u=(cw, hy - lid_bot, cw),
                mat=bumper_mat,
                bevel=min(0.005, cw * 0.15),
                segments=1,
            )

    # Top longitudinal stacking ribs reaching exact `y = hy`
    for i in range(n_rx):
        rx = 0.0 if n_rx == 1 else (-usable_x * 0.5 + usable_x * (i / (n_rx - 1)))
        mb_lid.add_bevel_box(
            center_u=(rx, hy - lid_rib_h * 0.5, 0.0),
            size_u=(rib_w * 1.15, lid_rib_h, dz - cw * 1.6),
            mat=bumper_mat,
            bevel=min(0.003, lid_rib_h * 0.25),
            segments=1,
        )

    # Warm-orange over-the-top cargo strap(s) (`R1_OrangeStripe`)
    strap_w = max(0.036, wx * 0.09)
    for st_x in spec["strap_offsets_x"]:
        mb_lid.add_bevel_box(
            center_u=(st_x, lid_shell_cy + lid_shell_h * 0.15, 0.0),
            size_u=(strap_w, lid_shell_h * 0.78, dz - inset * 0.8),
            mat="R1_OrangeStripe",
            bevel=0.003,
            segments=1,
        )

    # Underside lid inner pressure-pad liner (visible when lid opens)
    mb_lid.add_bevel_box(
        center_u=(0.0, lid_bot + 0.006, 0.0),
        size_u=(shell_w - wall_t * 1.9, 0.012, shell_d - wall_t * 1.9),
        mat=bumper_mat,
        bevel=0.002,
        segments=1,
    )

    # Front mechanical toggle latches (`R1_BareSteel` clasp + `R1_Brass` release tab) reaching down across seam
    latch_z = shell_d * 0.5 + rib_t * 0.55
    for lx in spec["latch_offsets_x"]:
        mb_lid.add_bevel_box(
            center_u=(lx, hinge_y + 0.004, latch_z),
            size_u=(0.042, 0.056, rib_t * 1.1),
            mat="R1_BareSteel",
            bevel=0.003,
            segments=1,
        )
        mb_lid.add_bevel_box(
            center_u=(lx, hinge_y - 0.012, latch_z + 0.003),
            size_u=(0.028, 0.022, 0.008),
            mat="R1_Brass",
            bevel=0.002,
            segments=1,
        )

    lid_obj = ruins_common.to_ruin_object(mb_lid, "Lid", parent=root_obj, origin_u=lid_pivot_u)

    # =========================================================================
    # Child `Lid/Seal` (`R1_LampAmber` glowing seam gasket + front lock indicator)
    # =========================================================================
    mb_seal = r1kit.MeshBuilder("Seal", pivot_u=lid_pivot_u)
    seal_h = 0.010
    # Front and rear seam bead
    for sz in (-1.0, 1.0):
        mb_seal.add_bevel_box(
            center_u=(0.0, hinge_y, sz * (shell_d * 0.5 - 0.002)),
            size_u=(wx - cw * 1.6, seal_h, 0.012),
            mat="R1_LampAmber",
            bevel=0.002,
            segments=1,
        )
    # Left and right seam bead
    for sx in (-1.0, 1.0):
        mb_seal.add_bevel_box(
            center_u=(sx * (shell_w * 0.5 - 0.002), hinge_y, 0.0),
            size_u=(0.012, seal_h, dz - cw * 1.6),
            mat="R1_LampAmber",
            bevel=0.002,
            segments=1,
        )
    # Front central cutter-target seal bar on the front seam
    mb_seal.add_bevel_box(
        center_u=(0.0, hinge_y, hinge_z),
        size_u=(min(0.11, wx * 0.26), 0.018, 0.016),
        mat="R1_LampAmber",
        bevel=0.003,
        segments=1,
    )
    ruins_common.to_ruin_object(mb_seal, "Seal", parent=lid_obj, origin_u=lid_pivot_u)

    # =========================================================================
    # Child `Lid/CutPoint` (Empty on front seam) & `ItemSocket` (Empty inside)
    # =========================================================================
    r1kit.create_empty("CutPoint", parent=lid_obj, pivot_u=(0.0, hinge_y, hinge_z))
    r1kit.create_empty("ItemSocket", parent=root_obj, pivot_u=(0.0, item_y, 0.0))

    return root_obj


def export_all_crates(
    out_dir: Path = Path("Assets/_Project/Art/R1/Models/Crates"),
    renders_dir: Path = Path("Logs/renders"),
    skip_renders: bool = False,
) -> list[Path]:
    """Build, bake vertex colours, and export Crate_S, Crate_M, and Crate_L to FBX."""
    out_dir = Path(out_dir).resolve()
    out_dir.mkdir(parents=True, exist_ok=True)
    exported: list[Path] = []

    for crate_name, spec in CRATE_SPECS.items():
        ruins_common.reset_ruins_scene()
        root = build_crate(crate_name)
        wx, hy, dz = spec["size_u"]
        fp_rad = max(wx, dz) * 0.85

        r1kit.apply_triplanar_uvs(root, texel_scale=0.85)
        ruins_common.apply_signage_uvs(root)
        ruins_common.bake_ruins_vertex_colors(root, footprint_radius=fp_rad)

        fbx_path = out_dir / f"{crate_name}.fbx"
        r1kit.export_fbx(root, fbx_path)
        exported.append(fbx_path)

        if not skip_renders:
            scene_group = bpy.data.objects.new("_CrateScaleScene", None)
            bpy.context.scene.collection.objects.link(scene_group)
            root.parent = scene_group

            # Build an open-lid companion copy beside the closed crate to showcase the rear hinge + interior cavity
            open_root = build_crate(crate_name)
            r1kit.apply_triplanar_uvs(open_root, texel_scale=0.85)
            ruins_common.apply_signage_uvs(open_root)
            ruins_common.bake_ruins_vertex_colors(open_root, footprint_radius=fp_rad)
            open_root.parent = scene_group
            open_root.location = Vector((wx * 1.25 + 0.12, 0.06, 0.0))
            open_root.rotation_euler = Euler((0.0, 0.0, math.radians(-14.0)), "XYZ")
            for child in open_root.children:
                if child.name.startswith("Lid"):
                    child.rotation_euler = Euler((math.radians(-58.0), 0.0, 0.0), "XYZ")

            # Import Rover 07 beside the crates for scale verification
            rover_fbx = Path("Assets/_Project/Art/R1/Models/Rover/RoverModel.fbx")
            if rover_fbx.exists():
                before_objs = set(bpy.data.objects)
                bpy.ops.import_scene.fbx(filepath=str(rover_fbx.resolve()))
                imported = [o for o in bpy.data.objects if o not in before_objs]
                for o in imported:
                    if o.type == "MESH" and o.data is not None:
                        for slot in o.material_slots:
                            if slot.material is not None:
                                base_m = bpy.data.materials.get(slot.material.name.split(".")[0])
                                if base_m is not None:
                                    slot.material = base_m
                    if o.parent is None:
                        o.parent = scene_group
                        o.location = Vector((-(wx * 0.65 + 1.05), -0.25, 0.0))
                        o.rotation_euler = Euler((0.0, 0.0, math.radians(158.0)), "XYZ")

            ruins_common.render_ruin_day_night(
                scene_group,
                slug=spec["slug"],
                cam_angle_deg=148.0,
                cam_elev_deg=22.0,
                res=(960, 720),
            )
        print(f"[crates] Exported {fbx_path.name} ({wx:.2f} x {dz:.2f} x {hy:.2f} m)")

    return exported


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build and export R1 Crate_S, Crate_M, and Crate_L FBX models.")
    parser.add_argument(
        "--out-dir",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Crates"),
    )
    parser.add_argument(
        "--renders-dir",
        type=Path,
        default=Path("Logs/renders"),
    )
    parser.add_argument("--skip-renders", action="store_true")
    args = parser.parse_args(argv)
    export_all_crates(out_dir=args.out_dir, renders_dir=args.renders_dir, skip_renders=args.skip_renders)


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)
