"""Build script for the 10 R1 weathered lunar basalt/breccia rock models (`Assets/_Project/Art/R1/Models/Rocks/Rock_00..09.fbx`).

- `Rock_00..Rock_04`: 5 varied irregular rounded pebbles/cobbles (<= 180 triangles each).
- `Rock_05..Rock_09`: 5 varied weathered lunar boulders with soft rounded forms, a few fracture planes,
  vesicle/spall pits, fracture crevices, dust-blanketed tops, and a basal regolith skirt (<= 300 triangles each).
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import bpy

THIS_DIR = Path(__file__).resolve().parent
if str(THIS_DIR) not in sys.path:
    sys.path.insert(0, str(THIS_DIR))
sys.dont_write_bytecode = True

import r1kit  # noqa: E402


ROCK_SPECS: tuple[dict[str, object], ...] = (
    # Pebble 0: weathered rounded basalt cobble with shallow crown spall pit (0.38x -> ~0.37 m W)
    {
        "name": "Rock_00",
        "footprint_radius": 0.2128,
        "skirt_height_frac": 0.26,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (0.3420, 0.2128, 0.2964),
                "seed": 101,
                "lon_segs": 12,
                "lat_rings": 5,
                "skirt_flare": 0.11,
                "burial_frac": 0.18,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((0.52, 0.74, 0.42), 0.54, 0.48),
                    ((-0.64, 0.62, -0.45), 0.58, 0.42),
                ],
                "pits_u": [
                    ((0.25, 0.88, 0.40), 0.34, 0.12),
                    ((-0.72, 0.45, 0.52), 0.28, 0.10),
                    ((0.40, 0.38, -0.83), 0.30, 0.11),
                ],
                "crevices_u": [],
            },
        ],
    },
    # Pebble 1: asymmetric sub-rounded breccia lump with oblique cleavage face (0.38x -> ~0.39 m W)
    {
        "name": "Rock_01",
        "footprint_radius": 0.2204,
        "skirt_height_frac": 0.25,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (0.3572, 0.2356, 0.2812),
                "seed": 203,
                "lon_segs": 12,
                "lat_rings": 5,
                "skirt_flare": 0.12,
                "burial_frac": 0.18,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((0.68, 0.64, 0.35), 0.50, 0.52),
                    ((-0.42, 0.78, -0.46), 0.56, 0.44),
                    ((0.18, 0.55, -0.81), 0.60, 0.38),
                ],
                "pits_u": [
                    ((0.58, 0.65, 0.48), 0.32, 0.13),
                    ((-0.55, 0.62, -0.56), 0.30, 0.11),
                    ((-0.20, 0.92, 0.32), 0.26, 0.09),
                ],
                "crevices_u": [
                    ((0.82, 0.18, -0.54), 0.05, 0.18, 0.09),
                ],
            },
        ],
    },
    # Pebble 2: low-domed weathered regolith stone with stepped rim (0.38x -> ~0.41 m W)
    {
        "name": "Rock_02",
        "footprint_radius": 0.2356,
        "skirt_height_frac": 0.28,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (0.3724, 0.1748, 0.3192),
                "seed": 307,
                "lon_segs": 12,
                "lat_rings": 5,
                "skirt_flare": 0.13,
                "burial_frac": 0.18,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((0.22, 0.92, 0.32), 0.54, 0.46),
                    ((-0.68, 0.54, 0.49), 0.58, 0.42),
                ],
                "pits_u": [
                    ((0.12, 0.94, -0.32), 0.36, 0.13),
                    ((0.76, 0.48, 0.43), 0.28, 0.10),
                    ((-0.65, 0.52, -0.55), 0.30, 0.11),
                ],
                "crevices_u": [],
            },
        ],
    },
    # Pebble 3: squat potato-shaped lunar clast with weathered split seam (0.42x -> ~0.40 m W)
    {
        "name": "Rock_03",
        "footprint_radius": 0.2352,
        "skirt_height_frac": 0.25,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (0.3612, 0.2436, 0.3360),
                "seed": 409,
                "lon_segs": 12,
                "lat_rings": 5,
                "skirt_flare": 0.12,
                "burial_frac": 0.18,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((-0.58, 0.72, 0.38), 0.52, 0.48),
                    ((0.62, 0.58, -0.52), 0.56, 0.44),
                ],
                "pits_u": [
                    ((-0.45, 0.78, 0.42), 0.32, 0.12),
                    ((0.64, 0.55, -0.53), 0.30, 0.12),
                    ((0.15, 0.85, 0.50), 0.26, 0.09),
                ],
                "crevices_u": [
                    ((0.65, 0.20, 0.73), -0.04, 0.20, 0.11),
                ],
            },
        ],
    },
    # Pebble 4: pear-shaped impact-gardened basalt lump with spall pits (0.45x -> ~0.44 m W)
    {
        "name": "Rock_04",
        "footprint_radius": 0.2565,
        "skirt_height_frac": 0.26,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (0.4140, 0.2340, 0.3240),
                "seed": 503,
                "lon_segs": 12,
                "lat_rings": 5,
                "skirt_flare": 0.12,
                "burial_frac": 0.18,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((0.46, 0.78, -0.42), 0.52, 0.50),
                    ((-0.72, 0.52, 0.46), 0.58, 0.40),
                ],
                "pits_u": [
                    ((0.38, 0.82, -0.42), 0.34, 0.13),
                    ((-0.68, 0.56, 0.47), 0.28, 0.11),
                    ((-0.25, 0.68, -0.68), 0.30, 0.10),
                ],
                "crevices_u": [],
            },
        ],
    },
    # Medium Boulder 0 (Rock_05): broad weathered lunar breccia boulder (0.75x -> ~1.15 m W)
    {
        "name": "Rock_05",
        "footprint_radius": 0.6750,
        "skirt_height_frac": 0.25,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (1.0200, 0.6600, 0.9000),
                "seed": 607,
                "lon_segs": 18,
                "lat_rings": 9,
                "skirt_flare": 0.18,
                "burial_frac": 0.16,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((0.58, 0.70, 0.41), 0.50, 0.52),
                    ((-0.62, 0.64, -0.45), 0.54, 0.46),
                    ((0.24, 0.62, -0.74), 0.58, 0.40),
                ],
                "pits_u": [
                    ((0.48, 0.74, 0.46), 0.32, 0.15),
                    ((-0.66, 0.58, 0.47), 0.28, 0.13),
                    ((0.28, 0.64, -0.71), 0.34, 0.15),
                    ((-0.42, 0.82, -0.38), 0.26, 0.11),
                    ((0.82, 0.38, -0.42), 0.25, 0.12),
                ],
                "crevices_u": [
                    ((0.76, 0.18, -0.62), 0.06, 0.19, 0.13),
                ],
            },
        ],
    },
    # Medium Boulder 1 (Rock_06): asymmetric humped basalt erratic with companion spall cobble (0.80x -> ~1.22 m W)
    {
        "name": "Rock_06",
        "footprint_radius": 0.7360,
        "skirt_height_frac": 0.24,
        "shells": [
            {
                "center_u": (-0.0480, 0.0, 0.0160),
                "size_u": (1.0080, 0.8160, 0.9120),
                "seed": 709,
                "lon_segs": 16,
                "lat_rings": 8,
                "skirt_flare": 0.17,
                "burial_frac": 0.16,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((0.64, 0.66, 0.39), 0.48, 0.54),
                    ((-0.56, 0.72, -0.40), 0.54, 0.46),
                    ((-0.78, 0.42, 0.46), 0.58, 0.42),
                ],
                "pits_u": [
                    ((0.56, 0.68, 0.47), 0.32, 0.15),
                    ((-0.52, 0.74, -0.42), 0.30, 0.13),
                    ((0.22, 0.58, -0.78), 0.28, 0.13),
                    ((-0.76, 0.48, 0.43), 0.26, 0.12),
                ],
                "crevices_u": [
                    ((-0.68, 0.22, 0.70), -0.05, 0.18, 0.12),
                ],
            },
            {
                "center_u": (0.3360, 0.0, -0.1440),
                "size_u": (0.5280, 0.3520, 0.4800),
                "seed": 719,
                "lon_segs": 10,
                "lat_rings": 4,
                "skirt_flare": 0.14,
                "burial_frac": 0.22,
                "rot_deg_u": (4.0, 24.0, -5.0),
                "fractures_u": [
                    ((0.62, 0.70, 0.35), 0.52, 0.48),
                ],
                "pits_u": [
                    ((0.45, 0.78, 0.42), 0.34, 0.11),
                ],
                "crevices_u": [],
            },
        ],
    },
    # Medium Boulder 2 (Rock_07): split-crown lunar breccia boulder (0.80x -> ~1.24 m W)
    {
        "name": "Rock_07",
        "footprint_radius": 0.7680,
        "skirt_height_frac": 0.26,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (1.1360, 0.6560, 0.9920),
                "seed": 811,
                "lon_segs": 18,
                "lat_rings": 9,
                "skirt_flare": 0.19,
                "burial_frac": 0.16,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((0.32, 0.86, 0.39), 0.52, 0.48),
                    ((-0.68, 0.58, 0.44), 0.54, 0.46),
                    ((0.64, 0.56, -0.52), 0.56, 0.44),
                ],
                "pits_u": [
                    ((0.52, 0.72, 0.45), 0.30, 0.14),
                    ((-0.62, 0.64, 0.45), 0.32, 0.15),
                    ((0.58, 0.58, -0.57), 0.30, 0.14),
                    ((-0.48, 0.62, -0.62), 0.28, 0.13),
                    ((0.08, 0.92, -0.38), 0.26, 0.11),
                ],
                "crevices_u": [
                    ((0.88, 0.14, 0.45), 0.02, 0.22, 0.16),
                    ((-0.42, 0.24, 0.87), -0.08, 0.16, 0.11),
                ],
            },
        ],
    },
    # Large Boulder 0 (Rock_08): slanted crater-ejecta boulder with companion talus lump (1.65x -> ~2.43 m W)
    {
        "name": "Rock_08",
        "footprint_radius": 1.5510,
        "skirt_height_frac": 0.25,
        "shells": [
            {
                "center_u": (0.0825, 0.0, -0.0495),
                "size_u": (2.2110, 1.4850, 1.9470),
                "seed": 919,
                "lon_segs": 16,
                "lat_rings": 8,
                "skirt_flare": 0.18,
                "burial_frac": 0.16,
                "rot_deg_u": (3.0, -14.0, -4.0),
                "fractures_u": [
                    ((-0.62, 0.68, 0.39), 0.48, 0.54),
                    ((0.58, 0.66, -0.47), 0.52, 0.48),
                    ((0.76, 0.44, 0.47), 0.58, 0.42),
                ],
                "pits_u": [
                    ((-0.54, 0.72, 0.43), 0.34, 0.15),
                    ((0.52, 0.68, -0.51), 0.30, 0.14),
                    ((0.72, 0.52, 0.45), 0.28, 0.13),
                    ((-0.36, 0.58, -0.73), 0.30, 0.13),
                ],
                "crevices_u": [
                    ((0.62, 0.20, 0.76), 0.04, 0.19, 0.13),
                ],
            },
            {
                "center_u": (-0.6600, 0.0, 0.3630),
                "size_u": (1.1550, 0.6930, 1.0230),
                "seed": 929,
                "lon_segs": 10,
                "lat_rings": 4,
                "skirt_flare": 0.14,
                "burial_frac": 0.22,
                "rot_deg_u": (-4.0, -26.0, 5.0),
                "fractures_u": [
                    ((-0.58, 0.72, 0.38), 0.52, 0.48),
                ],
                "pits_u": [
                    ((-0.42, 0.80, 0.42), 0.34, 0.11),
                ],
                "crevices_u": [],
            },
        ],
    },
    # Large Boulder 1 (Rock_09): massive hummocky lunar boulder (1.95x -> ~2.95 m W)
    {
        "name": "Rock_09",
        "footprint_radius": 1.8720,
        "skirt_height_frac": 0.26,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (2.6910, 1.8330, 2.4960),
                "seed": 1031,
                "lon_segs": 18,
                "lat_rings": 9,
                "skirt_flare": 0.18,
                "burial_frac": 0.16,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((-0.54, 0.74, -0.40), 0.50, 0.50),
                    ((0.66, 0.60, 0.45), 0.54, 0.46),
                    ((-0.28, 0.64, 0.71), 0.58, 0.42),
                ],
                "pits_u": [
                    ((-0.48, 0.76, -0.43), 0.34, 0.15),
                    ((0.60, 0.64, 0.48), 0.32, 0.14),
                    ((-0.26, 0.66, 0.70), 0.30, 0.14),
                    ((0.42, 0.58, -0.69), 0.28, 0.13),
                    ((0.15, 0.92, 0.36), 0.26, 0.11),
                ],
                "crevices_u": [
                    ((0.54, 0.22, -0.81), -0.05, 0.18, 0.13),
                ],
            },
        ],
    },
    # Big Boulder 0 (Rock_10): ~3.6 m weathered basalt cover erratic with oblique cleavage face
    {
        "name": "Rock_10",
        "footprint_radius": 2.25,
        "skirt_height_frac": 0.26,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (3.28, 2.35, 2.92),
                "seed": 1103,
                "lon_segs": 18,
                "lat_rings": 9,
                "skirt_flare": 0.18,
                "burial_frac": 0.16,
                "rot_deg_u": (0.0, 0.0, 0.0),
                "fractures_u": [
                    ((0.58, 0.68, 0.44), 0.50, 0.52),
                    ((-0.62, 0.62, -0.48), 0.54, 0.46),
                    ((0.24, 0.66, -0.71), 0.58, 0.42),
                ],
                "pits_u": [
                    ((0.50, 0.74, 0.45), 0.34, 0.15),
                    ((-0.58, 0.64, -0.50), 0.32, 0.14),
                    ((0.26, 0.64, -0.72), 0.30, 0.14),
                    ((-0.42, 0.78, 0.46), 0.28, 0.13),
                    ((0.14, 0.92, -0.36), 0.26, 0.11),
                ],
                "crevices_u": [
                    ((0.68, 0.20, 0.70), 0.04, 0.19, 0.14),
                ],
            },
        ],
    },
    # Big Boulder 1 (Rock_11): ~4.8 m split-crown crater-rim basalt erratic with companion talus shoulder
    {
        "name": "Rock_11",
        "footprint_radius": 3.05,
        "skirt_height_frac": 0.25,
        "shells": [
            {
                "center_u": (-0.18, 0.0, 0.06),
                "size_u": (4.05, 3.15, 3.60),
                "seed": 1217,
                "lon_segs": 18,
                "lat_rings": 9,
                "skirt_flare": 0.18,
                "burial_frac": 0.16,
                "rot_deg_u": (2.0, 12.0, -3.0),
                "fractures_u": [
                    ((0.62, 0.66, 0.42), 0.48, 0.54),
                    ((-0.56, 0.72, -0.41), 0.52, 0.48),
                    ((-0.74, 0.46, 0.48), 0.56, 0.44),
                ],
                "pits_u": [
                    ((0.54, 0.70, 0.46), 0.34, 0.15),
                    ((-0.50, 0.74, -0.44), 0.32, 0.14),
                    ((0.24, 0.60, -0.76), 0.30, 0.13),
                    ((-0.72, 0.50, 0.48), 0.28, 0.12),
                ],
                "crevices_u": [
                    ((-0.64, 0.22, 0.73), -0.05, 0.18, 0.13),
                ],
            },
            {
                "center_u": (1.28, 0.0, -0.48),
                "size_u": (2.10, 1.45, 1.90),
                "seed": 1229,
                "lon_segs": 12,
                "lat_rings": 5,
                "skirt_flare": 0.15,
                "burial_frac": 0.20,
                "rot_deg_u": (4.0, -22.0, 5.0),
                "fractures_u": [
                    ((0.60, 0.70, 0.38), 0.52, 0.48),
                ],
                "pits_u": [
                    ((0.46, 0.78, 0.42), 0.34, 0.12),
                ],
                "crevices_u": [],
            },
        ],
    },
    # Big Boulder 2 (Rock_12): ~6.2 m ridged lunar breccia landmark outcrop with stepped fracture planes
    {
        "name": "Rock_12",
        "footprint_radius": 3.95,
        "skirt_height_frac": 0.26,
        "shells": [
            {
                "center_u": (0.22, 0.0, -0.10),
                "size_u": (5.25, 4.05, 4.65),
                "seed": 1307,
                "lon_segs": 20,
                "lat_rings": 9,
                "skirt_flare": 0.19,
                "burial_frac": 0.16,
                "rot_deg_u": (-3.0, -16.0, 4.0),
                "fractures_u": [
                    ((-0.60, 0.68, 0.42), 0.48, 0.54),
                    ((0.58, 0.66, -0.47), 0.52, 0.48),
                    ((0.74, 0.46, 0.48), 0.56, 0.44),
                ],
                "pits_u": [
                    ((-0.52, 0.72, 0.45), 0.34, 0.15),
                    ((0.50, 0.68, -0.53), 0.32, 0.14),
                    ((0.70, 0.52, 0.49), 0.30, 0.13),
                    ((-0.38, 0.60, -0.70), 0.28, 0.13),
                ],
                "crevices_u": [
                    ((0.60, 0.20, 0.77), 0.04, 0.20, 0.14),
                    ((-0.46, 0.24, -0.85), -0.06, 0.17, 0.12),
                ],
            },
            {
                "center_u": (-1.65, 0.0, 0.78),
                "size_u": (2.65, 1.85, 2.35),
                "seed": 1319,
                "lon_segs": 12,
                "lat_rings": 5,
                "skirt_flare": 0.15,
                "burial_frac": 0.20,
                "rot_deg_u": (-4.0, 24.0, -5.0),
                "fractures_u": [
                    ((-0.58, 0.72, 0.38), 0.52, 0.48),
                ],
                "pits_u": [
                    ((-0.44, 0.80, 0.40), 0.34, 0.12),
                ],
                "crevices_u": [],
            },
        ],
    },
    # Fresh Rockfall Boulder 0 (Rockfall_Boulder_00): 10.50 x 8.40 x 6.40 m fresh-cleaved lunar breccia mass
    {
        "name": "Rockfall_Boulder_00",
        "fresh_rockfall": True,
        "target_size_u": (10.50, 6.40, 8.40),
        "burial_frac": 0.16,
        "footprint_radius": 5.80,
        "skirt_height_frac": 0.22,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (8.60, 6.40, 7.20),
                "seed": 2101,
                "lon_segs": 22,
                "lat_rings": 11,
                "skirt_flare": 0.18,
                "burial_frac": 0.16,
                "rot_deg_u": (4.0, -12.0, 3.0),
                "fractures_u": [
                    ((0.52, 0.68, -0.51), 0.44, 0.58),
                    ((-0.64, 0.56, 0.52), 0.48, 0.54),
                    ((0.18, 0.62, 0.76), 0.50, 0.50),
                    ((-0.42, 0.74, -0.52), 0.52, 0.46),
                ],
                "pits_u": [],
                "crevices_u": [
                    ((0.68, 0.22, 0.70), 0.03, 0.18, 0.14),
                    ((-0.55, 0.26, -0.79), -0.05, 0.16, 0.13),
                ],
            },
            {
                "center_u": (2.95, -0.25, -1.10),
                "size_u": (4.80, 3.85, 4.40),
                "seed": 2113,
                "lon_segs": 14,
                "lat_rings": 7,
                "skirt_flare": 0.16,
                "burial_frac": 0.20,
                "rot_deg_u": (-5.0, 18.0, 6.0),
                "fractures_u": [
                    ((0.62, 0.64, -0.45), 0.46, 0.54),
                    ((-0.48, 0.70, 0.52), 0.50, 0.48),
                ],
                "pits_u": [],
                "crevices_u": [],
            },
            {
                "center_u": (-2.80, -0.35, 1.25),
                "size_u": (4.30, 3.20, 3.90),
                "seed": 2129,
                "lon_segs": 14,
                "lat_rings": 7,
                "skirt_flare": 0.16,
                "burial_frac": 0.22,
                "rot_deg_u": (3.0, -24.0, -4.0),
                "fractures_u": [
                    ((-0.58, 0.68, 0.44), 0.48, 0.52),
                ],
                "pits_u": [],
                "crevices_u": [],
            },
        ],
    },
    # Fresh Rockfall Boulder 1 (Rockfall_Boulder_01): 12.20 x 9.80 x 7.60 m stepped shear-face boulder
    {
        "name": "Rockfall_Boulder_01",
        "fresh_rockfall": True,
        "target_size_u": (12.20, 7.60, 9.80),
        "burial_frac": 0.16,
        "footprint_radius": 6.80,
        "skirt_height_frac": 0.22,
        "shells": [
            {
                "center_u": (-0.25, 0.0, 0.15),
                "size_u": (9.80, 7.60, 8.20),
                "seed": 2203,
                "lon_segs": 22,
                "lat_rings": 11,
                "skirt_flare": 0.18,
                "burial_frac": 0.16,
                "rot_deg_u": (-3.0, 14.0, -4.0),
                "fractures_u": [
                    ((-0.56, 0.64, -0.52), 0.44, 0.58),
                    ((0.60, 0.58, 0.55), 0.46, 0.54),
                    ((-0.24, 0.72, 0.65), 0.50, 0.50),
                    ((0.48, 0.68, -0.55), 0.52, 0.48),
                ],
                "pits_u": [],
                "crevices_u": [
                    ((-0.62, 0.24, 0.74), 0.04, 0.19, 0.15),
                    ((0.52, 0.20, -0.83), -0.04, 0.17, 0.13),
                ],
            },
            {
                "center_u": (3.45, -0.30, 1.35),
                "size_u": (5.60, 4.50, 4.90),
                "seed": 2219,
                "lon_segs": 14,
                "lat_rings": 7,
                "skirt_flare": 0.16,
                "burial_frac": 0.20,
                "rot_deg_u": (6.0, -22.0, 5.0),
                "fractures_u": [
                    ((0.58, 0.66, 0.47), 0.46, 0.54),
                    ((-0.52, 0.68, -0.51), 0.50, 0.48),
                ],
                "pits_u": [],
                "crevices_u": [],
            },
            {
                "center_u": (-3.30, -0.45, -1.45),
                "size_u": (5.10, 3.80, 4.50),
                "seed": 2237,
                "lon_segs": 14,
                "lat_rings": 7,
                "skirt_flare": 0.16,
                "burial_frac": 0.22,
                "rot_deg_u": (-4.0, 28.0, -6.0),
                "fractures_u": [
                    ((-0.60, 0.64, -0.48), 0.48, 0.52),
                ],
                "pits_u": [],
                "crevices_u": [],
            },
        ],
    },
    # Fresh Rockfall Boulder 2 (Rockfall_Boulder_02): 14.00 x 11.20 x 8.80 m multi-lobe avalanche block
    {
        "name": "Rockfall_Boulder_02",
        "fresh_rockfall": True,
        "target_size_u": (14.00, 8.80, 11.20),
        "burial_frac": 0.16,
        "footprint_radius": 7.80,
        "skirt_height_frac": 0.22,
        "shells": [
            {
                "center_u": (0.20, 0.0, -0.15),
                "size_u": (11.20, 8.80, 9.40),
                "seed": 2309,
                "lon_segs": 22,
                "lat_rings": 11,
                "skirt_flare": 0.19,
                "burial_frac": 0.16,
                "rot_deg_u": (5.0, -16.0, 4.0),
                "fractures_u": [
                    ((0.48, 0.66, -0.57), 0.43, 0.58),
                    ((-0.62, 0.58, 0.52), 0.46, 0.55),
                    ((0.32, 0.70, 0.64), 0.49, 0.50),
                    ((-0.46, 0.68, -0.57), 0.52, 0.48),
                ],
                "pits_u": [],
                "crevices_u": [
                    ((0.58, 0.22, 0.78), 0.05, 0.20, 0.15),
                    ((-0.66, 0.20, -0.72), -0.06, 0.18, 0.14),
                ],
            },
            {
                "center_u": (-3.95, -0.35, 1.50),
                "size_u": (6.40, 5.20, 5.60),
                "seed": 2327,
                "lon_segs": 14,
                "lat_rings": 7,
                "skirt_flare": 0.16,
                "burial_frac": 0.20,
                "rot_deg_u": (-6.0, 24.0, -5.0),
                "fractures_u": [
                    ((-0.56, 0.66, 0.50), 0.46, 0.54),
                    ((0.52, 0.68, -0.51), 0.50, 0.48),
                ],
                "pits_u": [],
                "crevices_u": [],
            },
            {
                "center_u": (3.85, -0.50, -1.65),
                "size_u": (5.80, 4.40, 5.10),
                "seed": 2341,
                "lon_segs": 14,
                "lat_rings": 7,
                "skirt_flare": 0.16,
                "burial_frac": 0.22,
                "rot_deg_u": (4.0, -26.0, 5.0),
                "fractures_u": [
                    ((0.60, 0.64, -0.48), 0.48, 0.52),
                ],
                "pits_u": [],
                "crevices_u": [],
            },
        ],
    },
    # Fresh Rockfall Boulder 3 (Rockfall_Boulder_03): 15.80 x 12.60 x 10.20 m Anchor Megalith of the Peak slide
    {
        "name": "Rockfall_Boulder_03",
        "fresh_rockfall": True,
        "target_size_u": (15.80, 10.20, 12.60),
        "burial_frac": 0.16,
        "footprint_radius": 8.90,
        "skirt_height_frac": 0.22,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (12.60, 10.20, 10.60),
                "seed": 2411,
                "lon_segs": 24,
                "lat_rings": 12,
                "skirt_flare": 0.19,
                "burial_frac": 0.16,
                "rot_deg_u": (-4.0, 10.0, 3.0),
                "fractures_u": [
                    ((-0.52, 0.66, -0.54), 0.42, 0.60),
                    ((0.60, 0.58, 0.55), 0.45, 0.56),
                    ((-0.28, 0.68, 0.67), 0.48, 0.52),
                    ((0.46, 0.70, -0.54), 0.51, 0.48),
                ],
                "pits_u": [],
                "crevices_u": [
                    ((0.55, 0.22, -0.80), -0.04, 0.20, 0.16),
                    ((-0.70, 0.20, 0.68), 0.05, 0.19, 0.15),
                ],
            },
            {
                "center_u": (4.45, -0.40, -1.75),
                "size_u": (7.20, 5.90, 6.30),
                "seed": 2423,
                "lon_segs": 14,
                "lat_rings": 7,
                "skirt_flare": 0.16,
                "burial_frac": 0.20,
                "rot_deg_u": (5.0, -20.0, 6.0),
                "fractures_u": [
                    ((0.58, 0.66, -0.47), 0.46, 0.54),
                    ((-0.50, 0.70, 0.51), 0.50, 0.48),
                ],
                "pits_u": [],
                "crevices_u": [],
            },
            {
                "center_u": (-4.30, -0.55, 1.85),
                "size_u": (6.60, 5.10, 5.80),
                "seed": 2437,
                "lon_segs": 14,
                "lat_rings": 7,
                "skirt_flare": 0.16,
                "burial_frac": 0.22,
                "rot_deg_u": (-4.0, 26.0, -5.0),
                "fractures_u": [
                    ((-0.58, 0.68, 0.44), 0.48, 0.52),
                ],
                "pits_u": [],
                "crevices_u": [],
            },
        ],
    },
    # Landslide Scree/Talus Slab (Rockfall_TalusSlab): 46.00 x 30.00 x 4.50 m rubble tongue with feathered edges and embedded broken slabs
    {
        "name": "Rockfall_TalusSlab",
        "talus_slab": True,
        "target_size_u": (46.00, 4.50, 30.00),
        "burial_frac": 0.20,
        "seed": 2503,
        "radial_rings": 15,
        "lon_segs": 48,
        "rubble_shells": [
            {"center_u": (-12.5, 0.85, -5.2), "size_u": (5.4, 2.1, 4.2), "seed": 2511, "rot_deg_u": (8.0, 24.0, -6.0)},
            {"center_u": (11.8, 0.90, -4.6), "size_u": (5.8, 2.2, 4.5), "seed": 2521, "rot_deg_u": (-6.0, -32.0, 9.0)},
            {"center_u": (-5.4, 1.35, -7.8), "size_u": (4.8, 1.9, 3.9), "seed": 2531, "rot_deg_u": (11.0, 15.0, -8.0)},
            {"center_u": (5.2, 1.30, -8.2), "size_u": (4.6, 1.8, 3.8), "seed": 2543, "rot_deg_u": (-9.0, -18.0, 7.0)},
            {"center_u": (-15.2, 0.95, 2.8), "size_u": (6.2, 2.3, 4.8), "seed": 2551, "rot_deg_u": (5.0, 42.0, -5.0)},
            {"center_u": (14.6, 1.05, 3.4), "size_u": (6.0, 2.2, 4.6), "seed": 2557, "rot_deg_u": (-7.0, -38.0, 6.0)},
            {"center_u": (-7.2, 1.65, 5.6), "size_u": (6.4, 2.4, 5.0), "seed": 2579, "rot_deg_u": (9.0, -22.0, 8.0)},
            {"center_u": (7.8, 1.60, 6.2), "size_u": (6.2, 2.3, 4.9), "seed": 2591, "rot_deg_u": (-8.0, 28.0, -7.0)},
        ],
    },
    # Big Boulder 3 (Rock_13): ~7.6 m towering crater-ejecta landmark megalith with twin talus spurs
    {
        "name": "Rock_13",
        "footprint_radius": 4.85,
        "skirt_height_frac": 0.26,
        "shells": [
            {
                "center_u": (0.0, 0.0, 0.0),
                "size_u": (6.40, 4.85, 5.55),
                "seed": 1409,
                "lon_segs": 20,
                "lat_rings": 9,
                "skirt_flare": 0.19,
                "burial_frac": 0.16,
                "rot_deg_u": (0.0, 8.0, 0.0),
                "fractures_u": [
                    ((-0.54, 0.74, -0.40), 0.48, 0.54),
                    ((0.64, 0.62, 0.45), 0.52, 0.48),
                    ((-0.30, 0.64, 0.70), 0.56, 0.44),
                ],
                "pits_u": [
                    ((-0.48, 0.76, -0.43), 0.34, 0.15),
                    ((0.58, 0.64, 0.50), 0.32, 0.14),
                    ((-0.28, 0.66, 0.69), 0.30, 0.14),
                    ((0.42, 0.58, -0.69), 0.28, 0.13),
                    ((0.14, 0.92, 0.36), 0.26, 0.11),
                ],
                "crevices_u": [
                    ((0.54, 0.22, -0.81), -0.05, 0.19, 0.14),
                    ((-0.72, 0.18, 0.66), 0.04, 0.18, 0.13),
                ],
            },
            {
                "center_u": (2.15, 0.0, -0.85),
                "size_u": (2.95, 2.05, 2.55),
                "seed": 1423,
                "lon_segs": 10,
                "lat_rings": 5,
                "skirt_flare": 0.15,
                "burial_frac": 0.20,
                "rot_deg_u": (4.0, -20.0, 5.0),
                "fractures_u": [
                    ((0.60, 0.70, 0.38), 0.52, 0.48),
                ],
                "pits_u": [
                    ((0.45, 0.78, 0.42), 0.32, 0.12),
                ],
                "crevices_u": [],
            },
            {
                "center_u": (-2.05, 0.0, 0.92),
                "size_u": (2.60, 1.75, 2.25),
                "seed": 1439,
                "lon_segs": 10,
                "lat_rings": 5,
                "skirt_flare": 0.15,
                "burial_frac": 0.22,
                "rot_deg_u": (-3.0, 26.0, -4.0),
                "fractures_u": [
                    ((-0.58, 0.72, 0.38), 0.52, 0.48),
                ],
                "pits_u": [
                    ((-0.42, 0.80, 0.42), 0.32, 0.12),
                ],
                "crevices_u": [],
            },
        ],
    },
)


def build_rock(spec: dict[str, object]) -> bpy.types.Object:
    """Build a single R1 weathered lunar basalt/breccia rock mesh object with UVs and baked vertex colours."""
    name = str(spec["name"])
    mb = r1kit.MeshBuilder(name, pivot_u=(0.0, 0.0, 0.0))
    if spec.get("talus_slab"):
        wx, hy, dz = spec["target_size_u"]
        burial_frac = float(spec["burial_frac"])
        mb.add_talus_slab(
            center_u=(0.0, 0.0, 0.0),
            size_u=(float(wx), float(hy), float(dz)),
            seed=int(spec["seed"]),
            radial_rings=int(spec["radial_rings"]),
            lon_segs=int(spec["lon_segs"]),
            burial_frac=burial_frac,
            mat="R1_Basalt",
        )
        min_z_bound = -float(hy) * burial_frac
        max_z_bound = float(hy) * (1.0 - burial_frac)
        base_vert_count = len(mb.bm.verts)
        for rshell in spec.get("rubble_shells", []):
            mb.add_weathered_rock(
                center_u=rshell["center_u"],
                size_u=rshell["size_u"],
                seed=int(rshell["seed"]),
                lon_segs=10,
                lat_rings=5,
                fractures_u=[
                    ((-0.52, 0.72, -0.45), 0.48, 0.52),
                    ((0.58, 0.65, 0.48), 0.50, 0.48),
                ],
                pits_u=[],
                crevices_u=[],
                skirt_flare=0.14,
                burial_frac=0.35,
                rot_deg_u=rshell["rot_deg_u"],
                mat="R1_Basalt",
            )
        all_verts = list(mb.bm.verts)
        for v in all_verts[base_vert_count:]:
            v.co.z = max(min_z_bound + 0.05, min(max_z_bound - 0.05, v.co.z))
        obj = mb.build_object(smooth_angle_deg=42.0)
        r1kit.apply_triplanar_uvs(obj, texel_scale=0.65)
        bpy.context.view_layer.update()
        r1kit.bake_talus_slab_vertex_colors(
            obj,
            half_width=float(wx) * 0.5,
            half_depth=float(dz) * 0.5,
        )
        return obj

    if spec.get("fresh_rockfall"):
        for shell in spec["shells"]:
            mb.add_weathered_rock(
                center_u=shell["center_u"],
                size_u=shell["size_u"],
                seed=int(shell["seed"]),
                lon_segs=int(shell["lon_segs"]),
                lat_rings=int(shell["lat_rings"]),
                fractures_u=shell["fractures_u"],
                pits_u=shell["pits_u"],
                crevices_u=shell["crevices_u"],
                skirt_flare=float(shell["skirt_flare"]),
                burial_frac=float(shell["burial_frac"]),
                rot_deg_u=shell["rot_deg_u"],
                mat="R1_Basalt",
            )
        wx, hy, dz = spec["target_size_u"]
        burial_frac = float(spec["burial_frac"])
        verts = list(mb.bm.verts)
        xs = [v.co.x for v in verts]
        ys = [v.co.y for v in verts]
        zs = [v.co.z for v in verts]
        min_x, max_x = min(xs), max(xs)
        min_y, max_y = min(ys), max(ys)
        min_z, max_z = min(zs), max(zs)
        span_x = max(1e-4, max_x - min_x)
        span_y = max(1e-4, max_y - min_y)
        span_z = max(1e-4, max_z - min_z)
        cx = 0.5 * (min_x + max_x)
        cy = 0.5 * (min_y + max_y)
        burial_z = float(hy) * burial_frac
        for v in verts:
            v.co.x = ((v.co.x - cx) / span_x) * float(wx)
            v.co.y = ((v.co.y - cy) / span_y) * float(dz)
            v.co.z = ((v.co.z - min_z) / span_z) * float(hy) - burial_z
        obj = mb.build_object(smooth_angle_deg=44.0)
        r1kit.apply_triplanar_uvs(obj, texel_scale=0.75)
        bpy.context.view_layer.update()
        r1kit.bake_fresh_rockfall_vertex_colors(
            obj,
            footprint_radius=float(spec["footprint_radius"]),
            skirt_height_frac=float(spec["skirt_height_frac"]),
        )
        return obj
    mb = r1kit.MeshBuilder(name, pivot_u=(0.0, 0.0, 0.0))
    for shell in spec["shells"]:
        mb.add_weathered_rock(
            center_u=shell["center_u"],
            size_u=shell["size_u"],
            seed=int(shell["seed"]),
            lon_segs=int(shell["lon_segs"]),
            lat_rings=int(shell["lat_rings"]),
            fractures_u=shell["fractures_u"],
            pits_u=shell["pits_u"],
            crevices_u=shell["crevices_u"],
            skirt_flare=float(shell["skirt_flare"]),
            burial_frac=float(shell["burial_frac"]),
            rot_deg_u=shell["rot_deg_u"],
            mat="R1_Basalt",
        )
    obj = mb.build_object(smooth_angle_deg=44.0)
    r1kit.apply_triplanar_uvs(obj, texel_scale=0.85)
    bpy.context.view_layer.update()
    r1kit.bake_rock_vertex_colors(
        obj,
        footprint_radius=float(spec["footprint_radius"]),
        skirt_height_frac=float(spec["skirt_height_frac"]),
    )
    return obj


def render_big_boulders_with_rover() -> None:
    """Render Rock_10..Rock_13 alongside Rover 07 under Day C and Night A lighting presets."""
    import math
    from mathutils import Euler, Vector
    from ruins import ruins_common as rc

    rc.reset_ruins_scene()
    scene_group = bpy.data.objects.new("_BigBouldersScaleScene", None)
    bpy.context.scene.collection.objects.link(scene_group)

    big_specs = {str(s["name"]): s for s in ROCK_SPECS if str(s["name"]) in ("Rock_10", "Rock_11", "Rock_12", "Rock_13")}
    placements = (
        ("Rock_10", (-4.8, -1.6, 0.0), 12.0),
        ("Rock_11", (-1.2, 1.8, 0.0), -18.0),
        ("Rock_12", (3.8, 2.4, 0.0), 22.0),
        ("Rock_13", (9.6, 0.8, 0.0), -14.0),
    )
    for rname, loc, yaw_deg in placements:
        robj = build_rock(big_specs[rname])
        if robj.data is not None:
            for v in robj.data.vertices:
                if v.co.z < 0.0:
                    v.co.z = 0.0
            robj.data.update()
        robj.parent = scene_group
        robj.location = Vector(loc)
        robj.rotation_euler = Euler((0.0, 0.0, math.radians(yaw_deg)), "XYZ")

    rover_fbx = Path("Assets/_Project/Art/R1/Models/Rover/RoverModel.fbx")
    if rover_fbx.exists():
        before = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=str(rover_fbx.resolve()))
        rover_objs = [o for o in bpy.data.objects if o not in before]
        for o in rover_objs:
            if o.type == "MESH" and o.data is not None:
                for slot in o.material_slots:
                    if slot.material is not None:
                        base_m = bpy.data.materials.get(slot.material.name.split(".")[0])
                        if base_m is not None:
                            slot.material = base_m
            if o.parent is None:
                o.parent = scene_group
                o.location = Vector((-1.4, -2.2, 0.0))
                o.rotation_euler = Euler((0.0, 0.0, math.radians(28.0)), "XYZ")

    rc.render_ruin_day_night(
        scene_group,
        slug="boulders_scale",
        cam_angle_deg=24.0,
        cam_elev_deg=20.0,
        res=(1024, 768),
    )


def main(argv: list[str] | None = None) -> None:
    parser = argparse.ArgumentParser(description="Build Rock_00..Rock_13 FBX models.")
    parser.add_argument(
        "--out-dir",
        type=Path,
        default=Path("Assets/_Project/Art/R1/Models/Rocks"),
    )
    parser.add_argument("--only-big", action="store_true", help="Only build and export Rock_10..Rock_13.")
    parser.add_argument("--only-rockfall", action="store_true", help="Only build and export Rockfall_Boulder_00..03 and Rockfall_TalusSlab.")
    parser.add_argument("--render-scale", action="store_true", help="Render boulders_scale_dawn/night.png with 07.")
    args = parser.parse_args(argv)

    args.out_dir.mkdir(parents=True, exist_ok=True)
    if args.only_rockfall:
        specs_to_build = [s for s in ROCK_SPECS if s.get("fresh_rockfall") or s.get("talus_slab")]
    elif args.only_big:
        specs_to_build = [s for s in ROCK_SPECS if str(s["name"]) in ("Rock_10", "Rock_11", "Rock_12", "Rock_13")]
    else:
        specs_to_build = list(ROCK_SPECS)
    if False:
        specs_to_build = (
        [s for s in ROCK_SPECS if str(s["name"]) in ("Rock_10", "Rock_11", "Rock_12", "Rock_13")]
        if args.only_big
        else ROCK_SPECS
    )
    for spec in specs_to_build:
        r1kit.reset_scene()
        rock_obj = build_rock(spec)
        tris = r1kit.count_triangles(rock_obj)
        verts = len(rock_obj.data.vertices)
        out_path = args.out_dir / f"{spec['name']}.fbx"
        r1kit.export_fbx(rock_obj, out_path)
        print(f"[{spec['name']}] Exported {out_path} ({verts} verts, {tris} triangles)")

    if args.render_scale:
        render_big_boulders_with_rover()


if __name__ == "__main__":
    cli_args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    main(cli_args)

