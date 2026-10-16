"""Build ScatterSet.fbx (R1 Lumen Station ruins — 5 calm cassette-futurist field props).

Includes 5 distinct prop nodes under ScatterSet:
- SealedCrates: Smooth bevelled aerospace cargo crates (`R1_CreamEnamel`, `R1_GreyMetal`) with
  one warm-orange horizontal stripe (`R1_OrangeStripe`).
- CableReels: Heavy A-frame umbilical cable reel + spare flat drum wound with smooth 28-segment
  `R1_Rubber` and `R1_Copper` coils.
- ToppledAntenna: Portable tripod S-band field relay antenna toppled on its side with smooth
  28-segment microwave drum dish and warm amber beacon (`R1_LampAmber`).
- InstrumentCase: Ruggedized 1980s field telemetry case (`R1_SagePatch`) with hinged lid open,
  glowing amber CRT display (`R1_Screen`), and brass rotary dials (`R1_Brass`).
- OxygenBottleRack: 4-cylinder EVA oxygen-bottle rack with smooth 28-segment domed tanks,
  brass manifold valves, copper pigtails, and one `SEC-04` stencil plate (`R1_Signage`).
- Scan_1, Scan_2, Scan_3: Empty scan target nodes.
"""

from __future__ import annotations

import sys
from pathlib import Path

THIS_DIR = Path(__file__).resolve().parent
BLENDER_DIR = THIS_DIR.parent
if str(BLENDER_DIR) not in sys.path:
    sys.path.insert(0, str(BLENDER_DIR))

import r1kit  # noqa: E402
from ruins import ruins_common  # noqa: E402


def _build_sealed_crates(root) -> None:
    mb = r1kit.MeshBuilder()
    cx, cz = -1.65, 0.20

    # Steel pallet skid at y = 0.0 .. 0.12
    mb.add_bevel_box((cx, 0.06, cz), (1.42, 0.12, 1.08), bevel=0.015, mat="R1_GreyMetal", segs=1)
    for sx in (-0.52, 0.0, 0.52):
        mb.add_bevel_box((cx + sx, 0.04, cz), (0.14, 0.08, 1.12), bevel=0.01, mat="R1_BareSteel", segs=1)

    # Large primary sealed cargo crate (y = 0.12 .. 0.88) — clean rounded cream body + ONE orange band
    mb.add_bevel_box((cx - 0.08, 0.50, cz), (1.18, 0.76, 0.88), bevel=0.045, mat="R1_CreamEnamel", segs=3)
    mb.add_bevel_box((cx - 0.08, 0.64, cz), (1.20, 0.12, 0.90), bevel=0.025, mat="R1_OrangeStripe", segs=2)

    # Dark-grey corner bumpers & recessed toggle latches along front (-Z)
    for sx in (-1.0, 1.0):
        for sz in (-1.0, 1.0):
            mb.add_bevel_box(
                (cx - 0.08 + sx * 0.56, 0.50, cz + sz * 0.41),
                (0.10, 0.77, 0.10),
                bevel=0.016,
                mat="R1_GreyMetal",
                segs=1,
            )
    for lx in (-0.36, 0.20):
        mb.add_bevel_box((cx + lx, 0.64, cz - 0.45), (0.07, 0.15, 0.04), bevel=0.006, mat="R1_BareSteel", segs=1)

    # Smaller secondary sealed crate stacked on top, slightly angled (y = 0.88 .. 1.30)
    mb.add_bevel_box(
        (cx - 0.14, 1.08, cz + 0.04),
        (0.76, 0.42, 0.62),
        bevel=0.035,
        mat="R1_CreamEnamel",
        rot_deg_u=(0.0, 14.0, 0.0),
        segs=2,
    )
    mb.add_bevel_box(
        (cx - 0.14, 1.29, cz + 0.04),
        (0.78, 0.05, 0.64),
        bevel=0.012,
        mat="R1_GreyMetal",
        rot_deg_u=(0.0, 14.0, 0.0),
        segs=1,
    )

    ruins_common.to_ruin_object(mb, "SealedCrates", parent=root)


def _build_cable_reels(root) -> None:
    mb = r1kit.MeshBuilder()
    rx, rz = -0.45, -0.78

    # Heavy A-frame stand at y = 0.0 .. 0.62
    mb.add_bevel_box((rx, 0.04, rz), (0.76, 0.08, 0.72), bevel=0.012, mat="R1_GreyMetal", segs=1)
    for sx in (-0.30, 0.30):
        mb.add_bevel_box((rx + sx, 0.32, rz), (0.07, 0.56, 0.14), bevel=0.01, mat="R1_GreyMetal", segs=1)

    # Smooth 28-segment Upright Cable Spool (axis='x', center y = 0.48)
    for sx in (-0.22, 0.22):
        mb.add_cylinder((rx + sx, 0.48, rz), radius=0.38, height=0.04, segs=28, axis="x", mat="R1_CreamEnamel", bevel=0.008)
        mb.add_ring((rx + sx, 0.48, rz), r_outer=0.39, r_inner=0.33, height=0.05, segs=28, axis="x", mat="R1_GreyMetal")
    # Wound dark rubber umbilical drum & copper telemetry coil
    mb.add_cylinder((rx, 0.48, rz), radius=0.31, height=0.38, segs=28, axis="x", mat="R1_Rubber", bevel=0.014)
    mb.add_cylinder((rx + 0.08, 0.48, rz), radius=0.32, height=0.12, segs=28, axis="x", mat="R1_Copper", bevel=0.010)
    mb.add_cylinder((rx, 0.48, rz), radius=0.042, height=0.72, segs=16, axis="x", mat="R1_BareSteel")

    # Secondary smaller cable reel lying flat on the ground beside it at (rx - 0.58, 0.14, rz - 0.18)
    mb.add_cylinder((rx - 0.58, 0.03, rz - 0.18), radius=0.26, height=0.05, segs=24, axis="y", mat="R1_GreyMetal", bevel=0.006)
    mb.add_cylinder((rx - 0.58, 0.14, rz - 0.18), radius=0.20, height=0.18, segs=24, axis="y", mat="R1_Rubber", bevel=0.010)
    mb.add_cylinder((rx - 0.58, 0.25, rz - 0.18), radius=0.26, height=0.05, segs=24, axis="y", mat="R1_CreamEnamel", bevel=0.006)

    ruins_common.to_ruin_object(mb, "CableReels", parent=root)


def _build_toppled_antenna(root) -> None:
    mb = r1kit.MeshBuilder()

    # Ground rest pads under tripod hub & tip so y >= 0 everywhere
    mb.add_bevel_box((0.92, 0.12, 0.78), (0.36, 0.24, 0.36), bevel=0.015, mat="R1_GreyMetal", segs=1)
    mb.add_bevel_box((1.78, 0.08, 0.22), (0.28, 0.16, 0.28), bevel=0.012, mat="R1_GreyMetal", segs=1)

    # Canted tripod hub at (0.92, 0.34, 0.78)
    mb.add_cylinder(
        (0.92, 0.34, 0.78),
        radius=0.14,
        height=0.26,
        segs=20,
        axis="x",
        mat="R1_GreyMetal",
        bevel=0.015,
        rot_deg_u=(0.0, 32.0, -14.0),
    )
    leg_configs = [
        ((0.58, 0.18, 0.98), (0.05, 0.78, 0.05), (48.0, -35.0, 52.0)),
        ((0.72, 0.16, 0.42), (0.05, 0.78, 0.05), (-52.0, 28.0, 44.0)),
        ((0.68, 0.48, 0.82), (0.05, 0.74, 0.05), (18.0, 12.0, 68.0)),
    ]
    for pos, sz, rot in leg_configs:
        mb.add_bevel_box(pos, sz, bevel=0.008, mat="R1_BareSteel", rot_deg_u=rot, segs=1)

    # Toppled mast boom extending from hub to (1.88, 0.20, 0.16)
    mb.add_bevel_box(
        (1.40, 0.27, 0.47),
        (1.22, 0.06, 0.06),
        bevel=0.008,
        mat="R1_CreamEnamel",
        rot_deg_u=(0.0, 32.0, -9.0),
        segs=1,
    )
    for t_el in (0.25, 0.50, 0.75, 0.95):
        ex = 0.92 + t_el * 0.96
        ey = 0.34 - t_el * 0.14
        ez = 0.78 - t_el * 0.62
        el_len = 0.56 - t_el * 0.18
        mb.add_bevel_box(
            (ex, max(0.08, ey + 0.04), ez),
            (0.026, 0.026, el_len),
            bevel=0.004,
            mat="R1_BareSteel",
            rot_deg_u=(0.0, 32.0, 0.0),
            segs=1,
        )
    # Smooth 28-segment microwave drum dish & amber beacon on the mast
    mb.add_cylinder(
        (1.18, 0.38, 0.62),
        radius=0.24,
        height=0.12,
        segs=28,
        axis="z",
        mat="R1_CreamEnamel",
        bevel=0.018,
        rot_deg_u=(15.0, 32.0, 0.0),
    )
    mb.add_cylinder((1.88, 0.24, 0.16), radius=0.052, height=0.10, segs=16, axis="y", mat="R1_LampAmber", bevel=0.010)

    ruins_common.to_ruin_object(mb, "ToppledAntenna", parent=root)


def _build_instrument_case(root) -> None:
    mb = r1kit.MeshBuilder()
    ix, iz = 0.22, 0.22

    # Ruggedized 1980s Field Telemetry Case lower tub at y = 0.0 .. 0.32
    mb.add_bevel_box((ix, 0.16, iz), (0.74, 0.32, 0.54), bevel=0.03, mat="R1_SagePatch", segs=2)
    for sx in (-0.35, 0.35):
        for sz in (-0.25, 0.25):
            mb.add_bevel_box((ix + sx, 0.16, iz + sz), (0.08, 0.34, 0.08), bevel=0.012, mat="R1_Rubber", segs=1)

    # Interior cream-enamel faceplate & glowing CRT screen (`R1_Screen`) + brass dials
    mb.add_bevel_box((ix, 0.32, iz), (0.66, 0.04, 0.46), bevel=0.008, mat="R1_CreamEnamel", segs=1)
    mb.add_bevel_box((ix - 0.14, 0.34, iz - 0.02), (0.28, 0.03, 0.26), bevel=0.005, mat="R1_Screen", segs=1)
    for dx in (0.10, 0.22):
        for dz in (-0.10, 0.06):
            mb.add_cylinder((ix + dx, 0.35, iz + dz), radius=0.032, height=0.04, segs=14, axis="y", mat="R1_Brass", bevel=0.004)
    mb.add_cylinder((ix + 0.22, 0.35, iz - 0.18), radius=0.024, height=0.04, segs=12, axis="y", mat="R1_LampAmber", bevel=0.004)

    # Hinged Upper Lid propped open at the rear (z = iz + 0.32)
    mb.add_bevel_box(
        (ix, 0.56, iz + 0.32),
        (0.74, 0.52, 0.14),
        bevel=0.025,
        mat="R1_SagePatch",
        rot_deg_u=(-18.0, 0.0, 0.0),
        segs=2,
    )
    mb.add_bevel_box(
        (ix, 0.56, iz + 0.25),
        (0.62, 0.40, 0.03),
        bevel=0.006,
        mat="R1_CreamEnamel",
        rot_deg_u=(-18.0, 0.0, 0.0),
        segs=1,
    )

    ruins_common.to_ruin_object(mb, "InstrumentCase", parent=root)


def _build_oxygen_bottle_rack(root) -> None:
    mb = r1kit.MeshBuilder()
    ox, oz = 1.48, -0.68

    # Heavy structural steel rack base & 4 corner uprights (y = 0.0 .. 1.38)
    mb.add_bevel_box((ox, 0.07, oz), (1.16, 0.14, 0.56), bevel=0.015, mat="R1_GreyMetal", segs=1)
    for sx in (-0.52, 0.52):
        for sz in (-0.22, 0.22):
            mb.add_bevel_box((ox + sx, 0.72, oz + sz), (0.07, 1.28, 0.07), bevel=0.01, mat="R1_GreyMetal", segs=1)

    # Mid-height clamping strap bar & top warm-orange header frame
    mb.add_bevel_box((ox, 0.68, oz), (1.18, 0.12, 0.52), bevel=0.012, mat="R1_GreyMetal", segs=1)
    mb.add_bevel_box((ox, 1.32, oz), (1.20, 0.14, 0.56), bevel=0.015, mat="R1_OrangeStripe", segs=1)

    # 4 Smooth 24-segment Domed EVA Oxygen Cylinders inside the rack
    for bi in range(4):
        bx = ox - 0.36 + bi * 0.24
        ruins_common.add_domed_cylinder(
            mb,
            center_u=(bx, 0.64, oz),
            radius=0.102,
            total_length=1.06,
            dome_depth=0.12,
            mat="R1_CreamEnamel",
            axis="y",
            segs=24,
            barrel_rings=1,
            cap_rings=3,
        )
        # Brass pressure regulator valve & copper manifold pigtail
        mb.add_cylinder((bx, 1.20, oz), radius=0.030, height=0.10, segs=12, axis="y", mat="R1_Brass", bevel=0.004)
        mb.add_cylinder((bx, 1.25, oz + 0.06), radius=0.014, height=0.14, segs=10, axis="z", mat="R1_Copper")

    # Top copper collector manifold bar across all 4 cylinders
    mb.add_cylinder((ox, 1.25, oz + 0.12), radius=0.022, height=0.92, segs=14, axis="x", mat="R1_Copper", bevel=0.004)

    # ONE clean stencil plate on the front clamping strap of the Oxygen Bottle Rack
    ruins_common.add_signage_plate(
        mb,
        center_u=(ox, 0.68, oz - 0.27),
        width=0.64,
        height=0.22,
        cell_name="SEC04_HABITAT",
        rot_deg_u=(0.0, 180.0, 0.0),
    )

    ruins_common.to_ruin_object(mb, "OxygenBottleRack", parent=root)


def build(skip_renders: bool = False) -> None:
    ruins_common.reset_ruins_scene()
    root = r1kit.create_root("ScatterSet")

    _build_sealed_crates(root)
    _build_cable_reels(root)
    _build_toppled_antenna(root)
    _build_instrument_case(root)
    _build_oxygen_bottle_rack(root)

    # Scan points as empties named Scan_<n>
    r1kit.add_empty("Scan_1", (0.22, 0.45, 0.22), parent=root)    # Open 1980s instrument case
    r1kit.add_empty("Scan_2", (1.48, 0.85, -0.68), parent=root)   # Stencilled oxygen-bottle rack
    r1kit.add_empty("Scan_3", (-1.65, 0.65, 0.20), parent=root)   # Sealed cargo crates

    fbx_path, dawn_path, night_path = ruins_common.finalize_ruin_and_export(
        root,
        slug="scatter_set",
        fbx_name="ScatterSet.fbx",
        footprint_radius=2.8,
        cam_angle_deg=26.0,
        cam_elev_deg=22.0,
        skip_renders=skip_renders,
    )
    print(f"[scatter_set] Exported {fbx_path} and rendered {dawn_path.name}, {night_path.name}")


if __name__ == "__main__":
    build()
