#!/usr/bin/env python3
"""
Procedural generator for the R1 Earth equirectangular albedo + elevation/mask texture:
  Assets/_Project/Art/R1/Textures/T_R1_Earth_Albedo.png (2048x1024 RGBA, 8-bit sRGB)

Channels:
  RGB: "Blue Marble" surface albedo (deep abyssal & pelagic ocean, turquoise coastal shelves,
       emerald tropical forests, temperate woodlands, golden savannahs, Saharan/Arabian/Outback
       deserts, craggy mountain ranges, boreal tundra, and polar ice sheets).
  A:   Continuous normalized elevation & surface mask packed in [0.88 .. 1.00] (bytes 224..255)
       so the PNG displays at near-full opacity in standard viewers while preserving a clean
       shader mask:
         0.880 .. 0.935 : Ocean bathymetry (0.88 = deep abyss, 0.92..0.935 = shallow shelf)
         0.940          : Exact coastline
         0.945 .. 0.985 : Land elevation (0.945 = lowland, 0.97..0.985 = high mountain ranges)
         0.988 .. 1.000 : Permanent snow / polar ice cap

Deterministic (fixed seeds, no external downloads).
"""

from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

WIDTH = 2048
HEIGHT = 1024


def lonlat_to_px(pts, w=WIDTH, h=HEIGHT):
    """Convert [(lon_deg, lat_deg), ...] to pixel coordinates [(x, y), ...] in equirectangular projection."""
    out = []
    for lon, lat in pts:
        x = (lon + 180.0) / 360.0 * w
        y = (90.0 - lat) / 180.0 * h
        out.append((x, y))
    return out


# High-recognition continental & island polygons in (lon_deg, lat_deg).
LAND_POLYGONS = [
    # Africa
    [
        (-17.5, 14.7), (-17.0, 21.0), (-13.2, 27.8), (-9.8, 31.5), (-5.8, 35.9),
        (3.0, 36.8), (10.2, 37.3), (11.5, 33.2), (20.0, 32.8), (25.2, 31.6),
        (32.5, 31.3), (34.0, 28.0), (37.2, 22.0), (41.0, 15.5), (43.4, 12.6),
        (51.4, 11.9), (49.5, 6.5), (41.5, -1.5), (39.5, -6.8), (40.6, -11.0),
        (40.5, -15.5), (35.0, -19.8), (35.5, -24.0), (32.8, -26.0), (28.5, -32.8),
        (25.0, -34.0), (20.0, -34.8), (18.4, -33.9), (17.5, -29.0), (14.5, -22.6),
        (11.8, -17.0), (13.2, -9.0), (12.2, -6.0), (9.4, 0.5), (9.6, 4.0),
        (5.5, 4.3), (2.0, 6.3), (-3.0, 5.1), (-7.7, 4.4), (-11.5, 6.8),
        (-13.6, 9.6), (-16.6, 12.3),
    ],
    # Madagascar
    [
        (49.3, -12.0), (50.5, -15.3), (49.5, -19.0), (47.0, -25.0),
        (44.0, -25.2), (43.3, -21.0), (44.3, -16.2), (47.5, -13.5),
    ],
    # Eurasia (Mainland Europe + Asia)
    [
        (-9.5, 37.0), (-9.2, 43.0), (-1.8, 43.4), (-4.7, 48.4), (-1.6, 49.6),
        (1.8, 51.0), (4.5, 52.5), (8.5, 54.0), (8.6, 57.1), (10.5, 57.7),
        (10.0, 54.4), (14.0, 54.0), (18.6, 54.4), (21.0, 56.0), (24.0, 59.4),
        (28.0, 60.0), (21.5, 61.0), (22.0, 65.5), (14.0, 67.5), (18.0, 69.6),
        (25.5, 71.1), (31.0, 69.8), (40.5, 67.8), (38.0, 64.5), (43.0, 66.5),
        (44.0, 68.5), (54.0, 68.2), (60.0, 69.8), (68.5, 69.0), (72.5, 72.8),
        (70.0, 67.0), (78.0, 72.2), (86.0, 74.0), (104.0, 77.6), (114.0, 73.6),
        (128.0, 72.0), (140.0, 72.8), (150.0, 71.2), (162.0, 69.6), (178.0, 68.8),
        (179.8, 65.2), (171.0, 60.2), (163.0, 59.5), (162.5, 55.0), (156.8, 51.0),
        (155.8, 53.5), (160.0, 59.0), (150.0, 59.6), (142.0, 59.0), (136.0, 54.5),
        (140.5, 51.5), (135.5, 44.0), (131.0, 42.5), (129.5, 35.2), (126.5, 34.5),
        (126.2, 37.5), (124.2, 39.8), (121.5, 38.8), (119.0, 39.8), (118.0, 38.2),
        (122.5, 37.0), (119.5, 34.5), (121.8, 31.2), (121.5, 28.0), (117.5, 23.5),
        (114.2, 22.2), (109.8, 20.2), (108.0, 21.5), (105.8, 19.0), (109.2, 13.0),
        (104.8, 8.6), (100.2, 13.4), (99.3, 8.0), (103.8, 1.3), (101.2, 3.0),
        (98.5, 8.0), (97.5, 16.5), (94.2, 16.0), (92.2, 21.5), (89.0, 21.8),
        (85.2, 19.5), (80.3, 16.0), (80.2, 13.0), (77.5, 8.1), (74.8, 12.8),
        (72.8, 19.0), (72.5, 21.5), (69.0, 22.3), (67.0, 24.8), (61.5, 25.2),
        (56.5, 27.1), (50.5, 30.0), (48.0, 29.9), (50.2, 26.2), (51.5, 24.2),
        (55.5, 25.5), (56.4, 26.3), (59.8, 22.5), (55.0, 17.0), (48.0, 14.0),
        (43.5, 12.6), (42.5, 16.5), (39.1, 21.5), (34.8, 28.0), (32.6, 30.0),
        (34.5, 31.5), (35.8, 35.8), (36.2, 36.8), (32.8, 36.2), (27.4, 36.8),
        (26.2, 40.0), (29.0, 41.2), (36.0, 41.5), (41.5, 41.8), (39.0, 44.5),
        (36.5, 45.2), (33.5, 44.5), (30.5, 46.2), (28.8, 44.0), (26.0, 40.8),
        (23.8, 38.0), (22.5, 36.5), (20.0, 39.5), (19.0, 42.2), (13.8, 45.5),
        (12.3, 45.4), (14.0, 42.5), (18.5, 40.1), (16.5, 38.0), (15.6, 38.2),
        (13.5, 41.2), (10.3, 43.7), (8.9, 44.4), (5.0, 43.3), (3.2, 42.4),
        (-0.4, 39.5), (-0.8, 37.6), (-5.4, 36.0),
    ],
    # Scandinavia south (Sweden/Norway closure)
    [
        (5.2, 60.4), (6.5, 58.1), (10.8, 59.9), (11.9, 57.7), (12.9, 55.4),
        (16.0, 56.2), (18.1, 59.3), (17.5, 62.5), (21.5, 65.0), (18.5, 68.5),
        (14.0, 67.2), (10.4, 63.4), (5.0, 62.0),
    ],
    # Great Britain
    [
        (-5.7, 50.1), (-5.2, 51.8), (-3.1, 51.5), (-4.8, 53.3), (-3.0, 53.8),
        (-5.0, 54.8), (-5.8, 56.8), (-5.0, 58.6), (-3.1, 58.6), (-1.8, 57.5),
        (-2.8, 56.0), (-0.1, 53.6), (1.8, 52.5), (1.4, 51.3), (-1.2, 50.7),
    ],
    # Ireland
    [
        (-10.2, 51.8), (-9.8, 54.3), (-7.3, 55.3), (-5.5, 54.5), (-6.1, 52.2), (-8.5, 51.6),
    ],
    # Iceland
    [
        (-24.0, 65.5), (-22.0, 66.4), (-16.0, 66.5), (-13.6, 65.0), (-15.0, 64.2), (-19.5, 63.4), (-22.7, 63.8),
    ],
    # Sicily, Sardinia, Corsica, Crete, Cyprus
    [(12.4, 37.8), (15.6, 38.2), (15.1, 36.7), (13.5, 37.2)],
    [(8.2, 41.2), (9.8, 41.0), (9.6, 39.0), (8.4, 39.0)],
    [(8.6, 43.0), (9.5, 42.8), (9.2, 41.4), (8.6, 41.6)],
    [(23.5, 35.5), (26.3, 35.2), (26.2, 34.9), (23.6, 35.1)],
    [(32.3, 35.1), (34.6, 35.7), (34.0, 34.6), (32.4, 34.6)],
    # Sri Lanka
    [(79.8, 9.8), (81.8, 7.5), (81.2, 6.0), (79.8, 6.8)],
    # Sumatra
    [(95.3, 5.6), (99.5, 3.5), (104.5, -1.0), (105.8, -5.9), (104.0, -5.8), (100.3, -1.0), (96.0, 3.8)],
    # Java
    [(105.2, -6.0), (110.5, -6.6), (114.5, -7.8), (114.2, -8.7), (108.5, -7.8), (105.5, -6.8)],
    # Borneo
    [(109.0, 2.0), (111.5, 4.5), (117.5, 7.0), (119.0, 4.5), (117.5, 1.0), (116.0, -3.5), (111.5, -3.2), (109.2, -0.5)],
    # Sulawesi
    [(119.0, 1.2), (125.0, 1.5), (122.5, -0.5), (123.0, -4.5), (120.5, -5.5), (119.2, -3.5)],
    # Philippines (Luzon + Mindanao)
    [(120.0, 18.5), (122.4, 18.2), (122.0, 14.0), (124.0, 13.0), (120.2, 14.5)],
    [(122.0, 8.5), (126.2, 9.5), (126.5, 6.2), (124.0, 5.8), (122.0, 6.8)],
    # Taiwan & Hainan
    [(120.1, 23.0), (121.5, 25.3), (121.9, 24.5), (120.8, 21.9)],
    [(108.6, 19.2), (111.0, 20.0), (110.5, 18.2), (108.8, 18.3)],
    # Japan (Honshu, Hokkaido, Kyushu)
    [(130.9, 34.0), (136.5, 35.8), (139.8, 39.5), (141.5, 41.4), (142.0, 39.6), (140.8, 35.8), (136.8, 34.5), (133.0, 33.5)],
    [(140.0, 41.5), (141.8, 45.4), (145.5, 44.0), (143.2, 42.0), (140.5, 41.8)],
    [(129.6, 33.5), (131.8, 33.6), (131.2, 31.2), (130.2, 31.3)],
    # Sakhalin & Novaya Zemlya & Svalbard
    [(141.8, 54.2), (143.5, 50.0), (143.2, 46.0), (141.8, 47.0)],
    [(52.0, 71.0), (57.5, 70.5), (68.5, 77.0), (65.0, 76.8), (54.5, 73.5)],
    [(11.0, 79.8), (22.0, 80.2), (24.0, 77.5), (14.5, 76.8)],
    # North & Central America
    [
        (-168.0, 65.6), (-162.0, 70.2), (-156.5, 71.3), (-140.0, 69.6), (-128.0, 70.5),
        (-114.0, 68.5), (-96.0, 68.5), (-86.0, 68.0), (-82.0, 64.0), (-90.0, 63.5),
        (-94.5, 59.0), (-89.0, 57.0), (-82.0, 55.0), (-79.5, 51.5), (-78.0, 55.5),
        (-77.5, 62.5), (-70.0, 61.0), (-64.5, 60.2), (-55.6, 52.0), (-59.0, 50.5),
        (-65.5, 50.2), (-70.5, 47.2), (-64.5, 48.8), (-61.0, 45.5), (-66.0, 43.5),
        (-70.0, 41.8), (-74.0, 40.5), (-75.5, 38.0), (-75.5, 35.2), (-80.8, 32.0),
        (-80.1, 25.2), (-81.5, 25.2), (-82.8, 28.5), (-85.0, 29.8), (-89.2, 29.2),
        (-94.5, 29.5), (-97.2, 26.0), (-97.5, 21.5), (-94.5, 18.2), (-90.5, 19.8),
        (-87.0, 21.5), (-87.5, 18.0), (-83.5, 15.0), (-83.0, 10.0), (-79.5, 9.2),
        (-77.5, 7.5), (-79.0, 8.5), (-83.0, 8.5), (-85.8, 10.5), (-87.8, 13.2),
        (-92.2, 14.5), (-96.5, 15.7), (-105.5, 20.5), (-109.0, 25.5), (-113.0, 31.2),
        (-114.8, 31.8), (-112.5, 26.5), (-109.8, 23.0), (-112.2, 24.8), (-115.5, 29.5),
        (-117.2, 32.5), (-120.5, 34.5), (-122.5, 37.8), (-124.4, 40.4), (-124.7, 48.4),
        (-130.0, 54.0), (-136.5, 58.5), (-141.0, 60.0), (-148.0, 60.8), (-152.0, 59.0),
        (-161.5, 55.5), (-165.0, 54.5), (-160.0, 58.8), (-166.0, 61.5), (-162.0, 63.5),
        (-166.0, 64.5),
    ],
    # Greenland
    [
        (-44.0, 59.8), (-41.5, 65.5), (-32.0, 68.2), (-22.0, 70.5), (-18.0, 76.5),
        (-12.0, 81.5), (-32.0, 83.5), (-52.0, 82.2), (-62.0, 81.0), (-72.0, 78.0),
        (-68.0, 76.0), (-58.0, 74.5), (-55.0, 70.0), (-52.0, 65.0), (-48.0, 61.0),
    ],
    # Baffin, Victoria, Ellesmere, Newfoundland
    [(-85.0, 73.5), (-76.0, 72.5), (-65.0, 66.5), (-62.0, 62.0), (-72.0, 63.5), (-78.0, 68.0), (-84.0, 70.0)],
    [(-118.0, 72.5), (-104.0, 71.5), (-102.0, 68.8), (-116.0, 68.8)],
    [(-88.0, 81.5), (-64.0, 82.5), (-74.0, 77.5), (-88.0, 76.5)],
    [(-59.4, 47.6), (-56.0, 51.5), (-52.7, 47.5), (-55.8, 46.8)],
    # Cuba & Hispaniola
    [(-84.9, 21.8), (-79.5, 22.8), (-74.2, 20.2), (-77.5, 19.8), (-82.5, 21.5)],
    [(-74.4, 18.4), (-71.5, 19.9), (-68.4, 18.6), (-71.8, 17.6)],
    # South America
    [
        (-77.5, 8.2), (-75.5, 10.8), (-71.8, 12.2), (-68.0, 11.2), (-62.0, 10.5),
        (-58.0, 6.8), (-51.5, 4.2), (-49.0, -0.5), (-44.0, -2.5), (-38.5, -3.8),
        (-34.8, -7.2), (-38.5, -13.0), (-39.0, -18.0), (-41.0, -22.0), (-44.5, -23.2),
        (-48.5, -26.5), (-52.0, -32.0), (-53.5, -34.0), (-58.0, -35.5), (-57.5, -38.0),
        (-62.0, -39.0), (-65.0, -41.5), (-65.5, -45.0), (-67.5, -49.5), (-68.5, -52.5),
        (-65.5, -54.8), (-70.0, -55.2), (-74.5, -52.5), (-75.0, -46.5), (-73.5, -41.5),
        (-73.2, -37.0), (-71.5, -33.0), (-71.2, -27.0), (-70.2, -18.5), (-76.2, -14.0),
        (-79.0, -8.0), (-81.3, -4.7), (-80.0, -2.5), (-80.8, -0.5), (-78.8, 1.5),
        (-77.5, 5.0),
    ],
    # Australia & Tasmania
    [
        (113.5, -22.0), (114.2, -26.0), (115.0, -34.2), (118.0, -35.0), (123.5, -34.0),
        (128.5, -32.0), (132.5, -32.2), (135.5, -34.8), (137.5, -34.5), (139.5, -36.0),
        (143.5, -38.8), (146.5, -39.0), (150.0, -37.5), (151.3, -33.8), (153.6, -28.6),
        (153.2, -25.0), (149.2, -21.0), (145.5, -16.5), (142.5, -10.8), (141.5, -13.5),
        (140.8, -17.5), (136.5, -15.5), (136.8, -12.2), (132.5, -11.5), (129.8, -14.5),
        (126.5, -14.0), (122.2, -17.5), (119.5, -20.0),
    ],
    [(144.8, -40.7), (148.3, -41.0), (147.8, -43.5), (145.5, -43.4)],
    # New Guinea
    [(131.0, -1.5), (135.0, -3.5), (141.0, -2.6), (145.8, -4.5), (148.0, -8.0), (150.8, -10.5),
     (147.0, -9.8), (143.5, -8.2), (138.5, -8.2), (137.5, -4.8), (133.5, -4.0)],
    # New Zealand (North & South Island)
    [(172.8, -34.5), (175.8, -36.8), (178.5, -37.7), (176.8, -39.8), (174.8, -41.4), (173.8, -39.2), (174.2, -36.8)],
    [(172.7, -40.6), (174.2, -41.5), (172.8, -43.8), (170.5, -46.0), (167.5, -46.6), (166.5, -45.5), (169.5, -43.5)],
    # Antarctica
    [
        (-180.0, -76.0), (-150.0, -75.0), (-125.0, -73.5), (-100.0, -72.5), (-75.0, -70.0),
        (-62.0, -64.0), (-57.0, -63.5), (-60.0, -68.0), (-62.0, -74.5), (-40.0, -77.5),
        (-20.0, -73.0), (0.0, -70.0), (25.0, -69.5), (50.0, -67.0), (70.0, -68.5),
        (95.0, -66.5), (120.0, -66.8), (145.0, -67.5), (168.0, -71.5), (170.0, -77.5),
        (180.0, -76.0), (180.0, -90.0), (-180.0, -90.0),
    ],
]

# Inland seas / big lakes carved out of continental landmasses.
INLAND_WATER_POLYGONS = [
    # Caspian Sea
    [(47.0, 45.8), (50.5, 46.8), (53.5, 45.0), (52.8, 41.0), (54.0, 37.5), (50.5, 37.0), (48.8, 38.8), (50.2, 40.4), (47.5, 43.0)],
    # Black Sea
    [(27.6, 42.2), (28.6, 44.5), (31.0, 46.5), (35.0, 45.8), (39.0, 44.5), (41.6, 41.8), (37.0, 41.0), (31.5, 41.2), (28.8, 41.2)],
    # Red Sea
    [(32.6, 29.8), (34.5, 27.8), (37.2, 22.0), (41.2, 16.5), (43.3, 12.7), (41.8, 14.5), (38.8, 21.5), (35.2, 27.5), (33.8, 29.5)],
    # Persian Gulf
    [(48.0, 30.0), (50.5, 29.8), (54.0, 27.0), (56.2, 26.5), (54.5, 24.2), (51.2, 24.5), (50.0, 26.5), (48.2, 29.0)],
    # Great Lakes (Superior, Michigan-Huron, Erie-Ontario)
    [(-92.1, 46.8), (-88.0, 48.8), (-84.5, 46.5), (-88.0, 46.4)],
    [(-87.8, 45.8), (-84.8, 45.8), (-80.0, 45.2), (-82.2, 43.0), (-86.2, 41.8), (-87.8, 42.5)],
    [(-83.2, 41.8), (-78.8, 42.9), (-76.2, 44.0), (-77.5, 43.2), (-81.5, 41.5)],
    # Lake Victoria & Lake Baikal
    [(31.6, -0.5), (34.2, -0.2), (34.0, -2.6), (31.8, -2.5)],
    [(104.0, 51.6), (109.8, 55.6), (109.2, 54.0), (104.8, 51.4)],
]


def hash3(ix, iy, iz, seed):
    """Fast vectorized integer 3D hash -> float32 in [0, 1]."""
    h = (ix * 73856093) ^ (iy * 19349663) ^ (iz * 83492791) ^ (seed * 2654435761)
    h = ((h >> 13) ^ h) * 1274126177
    h = (h >> 16) ^ h
    return (h & 0x7FFFFFFF).astype(np.float32) / float(0x7FFFFFFF)


def value_noise_3d(x, y, z, seed):
    """Smooth 3D value noise evaluated on spherical coordinates (seam-free across antimeridian & poles)."""
    ix = np.floor(x).astype(np.int64)
    iy = np.floor(y).astype(np.int64)
    iz = np.floor(z).astype(np.int64)
    fx = (x - ix).astype(np.float32)
    fy = (y - iy).astype(np.float32)
    fz = (z - iz).astype(np.float32)
    ux = fx * fx * fx * (fx * (fx * 6.0 - 15.0) + 10.0)
    uy = fy * fy * fy * (fy * (fy * 6.0 - 15.0) + 10.0)
    uz = fz * fz * fz * (fz * (fz * 6.0 - 15.0) + 10.0)

    n000 = hash3(ix, iy, iz, seed)
    n100 = hash3(ix + 1, iy, iz, seed)
    n010 = hash3(ix, iy + 1, iz, seed)
    n110 = hash3(ix + 1, iy + 1, iz, seed)
    n001 = hash3(ix, iy, iz + 1, seed)
    n101 = hash3(ix + 1, iy, iz + 1, seed)
    n011 = hash3(ix, iy + 1, iz + 1, seed)
    n111 = hash3(ix + 1, iy + 1, iz + 1, seed)

    nx00 = n000 + ux * (n100 - n000)
    nx10 = n010 + ux * (n110 - n010)
    nx01 = n001 + ux * (n101 - n001)
    nx11 = n011 + ux * (n111 - n011)
    nxy0 = nx00 + uy * (nx10 - nx00)
    nxy1 = nx01 + uy * (nx11 - nx01)
    return nxy0 + uz * (nxy1 - nxy0)


def fbm_sphere(sx, sy, sz, base_freq, octaves, seed):
    val = np.zeros_like(sx, dtype=np.float32)
    amp = 0.5
    freq = base_freq
    norm = 0.0
    for i in range(octaves):
        n = value_noise_3d(sx * freq + i * 13.7, sy * freq + i * 7.3, sz * freq + i * 19.1, seed + i * 31)
        val += amp * ((n - 0.5) * 1.85 + 0.5)
        norm += amp
        freq *= 2.07
        amp *= 0.5
    return np.clip(val / norm, 0.0, 1.0)


def smoothstep(edge0, edge1, x):
    t = np.clip((x - edge0) / np.maximum(edge1 - edge0, 1e-6), 0.0, 1.0)
    return t * t * (3.0 - 2.0 * t)


def gaussian_region(lon_deg, lat_deg, c_lon, c_lat, r_lon, r_lat):
    dlon = ((lon_deg - c_lon + 180.0) % 360.0) - 180.0
    dlat = lat_deg - c_lat
    d2 = (dlon / r_lon) ** 2 + (dlat / r_lat) ** 2
    return np.exp(-d2).astype(np.float32)


def generate_earth_albedo():
    # 1. Rasterize land & inland-water polygons at 2x resolution then downsample
    hi_w, hi_h = WIDTH * 2, HEIGHT * 2
    mask_img = Image.new("L", (hi_w, hi_h), 0)
    draw = ImageDraw.Draw(mask_img)
    for poly in LAND_POLYGONS:
        draw.polygon(lonlat_to_px(poly, hi_w, hi_h), fill=255)
    for water_poly in INLAND_WATER_POLYGONS:
        draw.polygon(lonlat_to_px(water_poly, hi_w, hi_h), fill=0)

    mask_1x = mask_img.resize((WIDTH, HEIGHT), Image.Resampling.BILINEAR)
    # Multi-scale blurs to build signed coastal distance / bathymetry / inland elevation
    blur_sharp = np.asarray(mask_1x.filter(ImageFilter.GaussianBlur(radius=1.4)), dtype=np.float32) / 255.0
    blur_fine = np.asarray(mask_1x.filter(ImageFilter.GaussianBlur(radius=4.5)), dtype=np.float32) / 255.0
    blur_mid = np.asarray(mask_1x.filter(ImageFilter.GaussianBlur(radius=14.0)), dtype=np.float32) / 255.0
    blur_wide = np.asarray(mask_1x.filter(ImageFilter.GaussianBlur(radius=36.0)), dtype=np.float32) / 255.0
    blur_continental = np.asarray(mask_1x.filter(ImageFilter.GaussianBlur(radius=76.0)), dtype=np.float32) / 255.0

    # 2. Build spherical 3D coordinate grid (x, y, z) on the unit sphere
    u = (np.arange(WIDTH, dtype=np.float32) + 0.5) / WIDTH
    v = (np.arange(HEIGHT, dtype=np.float32) + 0.5) / HEIGHT
    lon_deg = (u * 360.0 - 180.0)[None, :]
    lat_deg = (90.0 - v * 180.0)[:, None]
    lon_rad = np.deg2rad(lon_deg)
    lat_rad = np.deg2rad(lat_deg)

    cos_lat = np.cos(lat_rad)
    sx = cos_lat * np.cos(lon_rad)
    sy = np.broadcast_to(np.sin(lat_rad), (HEIGHT, WIDTH)).copy()
    sz = cos_lat * np.sin(lon_rad)

    # 3. Domain-warped 3D spherical fractal noise for natural fractal coastlines, islands, and mountain ridges
    warp_x = fbm_sphere(sx, sy, sz, 3.2, 3, 101) - 0.5
    warp_y = fbm_sphere(sx, sy, sz, 3.2, 3, 202) - 0.5
    warp_z = fbm_sphere(sx, sy, sz, 3.2, 3, 303) - 0.5
    wsx = sx + warp_x * 0.20
    wsy = sy + warp_y * 0.20
    wsz = sz + warp_z * 0.20

    coast_detail = fbm_sphere(wsx, wsy, wsz, 11.5, 5, 404) - 0.5
    fine_detail = fbm_sphere(wsx, wsy, wsz, 30.0, 4, 505) - 0.5
    micro_tex = fbm_sphere(sx, sy, sz, 54.0, 3, 909) - 0.5
    ridge_raw = 1.0 - np.abs(fbm_sphere(wsx, wsy, wsz, 16.0, 4, 606) * 2.0 - 1.0)
    ridge_noise = ridge_raw * ridge_raw * ridge_raw

    # Combine polygon masks with fractal coastline detail so coastlines have organic bays, fjords, and islands
    base_field = blur_sharp * 0.48 + blur_fine * 0.34 + blur_mid * 0.18
    coast_band = 4.0 * blur_fine * (1.0 - blur_fine)
    signed_land = (base_field - 0.47) + (coast_detail * 0.26 + fine_detail * 0.15 + micro_tex * 0.07) * (0.18 + 0.82 * coast_band)

    is_land = smoothstep(-0.010, 0.010, signed_land)

    # 4. Real mountain ranges (Himalayas/Tibet, Andes, Rockies, Alps, Ethiopian Highlands, Zagros, Atlas, Urals)
    himalaya = gaussian_region(lon_deg, lat_deg, 86.0, 31.5, 13.0, 6.5) * 1.10
    pamir_tianshan = gaussian_region(lon_deg, lat_deg, 74.0, 39.0, 9.5, 4.5) * 0.85
    zagros_caucasus = gaussian_region(lon_deg, lat_deg, 47.0, 36.0, 8.5, 5.5) * 0.70
    alps = gaussian_region(lon_deg, lat_deg, 10.5, 46.5, 6.0, 2.6) * 0.75
    andes_n = gaussian_region(lon_deg, lat_deg, -75.0, -8.0, 3.8, 13.5) * 0.90
    andes_s = gaussian_region(lon_deg, lat_deg, -69.5, -28.0, 3.5, 17.5) * 0.95
    rockies = gaussian_region(lon_deg, lat_deg, -111.5, 43.0, 7.5, 14.5) * 0.82
    sierra_madre = gaussian_region(lon_deg, lat_deg, -103.0, 23.0, 5.0, 7.5) * 0.65
    ethiopia_rift = gaussian_region(lon_deg, lat_deg, 38.5, 8.5, 5.0, 7.0) * 0.72
    atlas_mts = gaussian_region(lon_deg, lat_deg, -4.0, 32.5, 7.0, 2.5) * 0.62
    urals = gaussian_region(lon_deg, lat_deg, 59.5, 60.0, 2.8, 8.5) * 0.52

    mountain_env = np.clip(
        himalaya + pamir_tianshan + zagros_caucasus + alps + andes_n + andes_s
        + rockies + sierra_madre + ethiopia_rift + atlas_mts + urals,
        0.0, 1.25,
    )
    mountain_height = is_land * (mountain_env * (0.32 + 0.88 * ridge_noise) + ridge_noise * 0.20 * blur_mid)

    # 5. Ocean bathymetry (deep trenches, mid-ocean ridges, continental shelves, coral reefs)
    ocean_ridge = fbm_sphere(wsx, wsy, wsz, 6.5, 4, 707)
    shelf_mask = smoothstep(0.10, 0.54, blur_wide + coast_detail * 0.14) * (1.0 - is_land)
    coral_shoals = smoothstep(0.32, 0.56, blur_mid + fine_detail * 0.18) * (1.0 - is_land) * smoothstep(38.0, 12.0, np.abs(lat_deg))

    deep_ocean = np.array([0.045, 0.125, 0.315], dtype=np.float32)
    mid_ocean = np.array([0.088, 0.228, 0.485], dtype=np.float32)
    shelf_ocean = np.array([0.135, 0.435, 0.600], dtype=np.float32)
    reef_turquoise = np.array([0.215, 0.605, 0.700], dtype=np.float32)

    ocean_t = smoothstep(0.20, 0.78, ocean_ridge * 0.68 + blur_continental * 0.32)
    ocean_col = deep_ocean[None, None, :] * (1.0 - ocean_t[:, :, None]) + mid_ocean[None, None, :] * ocean_t[:, :, None]
    ocean_col = ocean_col * (1.0 - shelf_mask[:, :, None] * 0.72) + shelf_ocean[None, None, :] * (shelf_mask[:, :, None] * 0.72)
    ocean_col = ocean_col * (1.0 - coral_shoals[:, :, None] * 0.60) + reef_turquoise[None, None, :] * (coral_shoals[:, :, None] * 0.60)

    # 6. Continental biomes (Blue Marble Next Generation colours)
    moisture_noise = fbm_sphere(wsx, wsy, wsz, 5.5, 4, 808)

    # Real-world desert & dry plateau envelopes
    sahara = gaussian_region(lon_deg, lat_deg, 12.0, 23.5, 24.0, 8.5) * 1.05
    arabia = gaussian_region(lon_deg, lat_deg, 46.0, 23.5, 11.5, 8.5) * 1.00
    iran_thar = gaussian_region(lon_deg, lat_deg, 61.0, 29.5, 12.5, 6.5) * 0.90
    gobi_taklamakan = gaussian_region(lon_deg, lat_deg, 92.0, 41.0, 18.0, 7.5) * 0.92
    central_asia_steppe = gaussian_region(lon_deg, lat_deg, 64.0, 45.0, 15.0, 6.5) * 0.72
    outback = gaussian_region(lon_deg, lat_deg, 132.0, -24.5, 14.5, 7.5) * 0.96
    kalahari_namib = gaussian_region(lon_deg, lat_deg, 19.0, -23.0, 7.0, 8.0) * 0.88
    sonora_mojave = gaussian_region(lon_deg, lat_deg, -110.5, 32.5, 9.5, 9.5) * 0.86
    great_plains = gaussian_region(lon_deg, lat_deg, -101.0, 39.0, 7.5, 11.0) * 0.52
    atacama_patagonia = gaussian_region(lon_deg, lat_deg, -68.5, -28.0, 4.5, 14.0) * 0.82
    somali_arid = gaussian_region(lon_deg, lat_deg, 46.5, 8.0, 6.0, 6.5) * 0.82

    desert_env = np.clip(
        sahara + arabia + iran_thar + gobi_taklamakan + central_asia_steppe + outback
        + kalahari_namib + sonora_mojave + great_plains + atacama_patagonia + somali_arid,
        0.0, 1.05,
    )
    desert_mask = smoothstep(0.16, 0.58, desert_env + (0.5 - moisture_noise) * 0.34)

    # Tropical rainforest envelope (Amazon, Congo, SE Asia / Indonesia)
    amazon = gaussian_region(lon_deg, lat_deg, -62.0, -4.0, 13.0, 9.5)
    congo = gaussian_region(lon_deg, lat_deg, 21.0, 0.5, 10.0, 7.0)
    se_asia_tropics = gaussian_region(lon_deg, lat_deg, 112.0, 1.0, 18.0, 11.0)
    tropical_mask = smoothstep(0.15, 0.65, (amazon + congo + se_asia_tropics) + (moisture_noise - 0.45) * 0.42) * (1.0 - desert_mask * 0.85)

    abs_lat = np.abs(lat_deg)
    savannah_mask = smoothstep(0.22, 0.66, (1.0 - np.abs(abs_lat - 18.0) / 22.0) * (1.0 - tropical_mask * 0.7) * (1.0 - desert_mask * 0.5) + (0.5 - moisture_noise) * 0.28)
    boreal_mask = smoothstep(48.0, 64.0, abs_lat) * (1.0 - smoothstep(66.0, 74.0, abs_lat))
    tundra_mask = smoothstep(61.0, 72.0, abs_lat)

    # Biome sRGB colours
    col_rainforest = np.array([0.115, 0.285, 0.145], dtype=np.float32)
    col_temperate = np.array([0.205, 0.375, 0.195], dtype=np.float32)
    col_savannah = np.array([0.455, 0.465, 0.255], dtype=np.float32)
    col_desert_sand = np.array([0.805, 0.655, 0.455], dtype=np.float32)
    col_desert_rock = np.array([0.610, 0.450, 0.315], dtype=np.float32)
    col_boreal = np.array([0.165, 0.275, 0.190], dtype=np.float32)
    col_tundra = np.array([0.415, 0.425, 0.375], dtype=np.float32)
    col_mountain_rock = np.array([0.465, 0.390, 0.315], dtype=np.float32)
    col_snow = np.array([0.935, 0.955, 0.985], dtype=np.float32)

    land_col = np.broadcast_to(col_temperate[None, None, :], (HEIGHT, WIDTH, 3)).copy()
    land_col = land_col * (1.0 - tropical_mask[:, :, None]) + col_rainforest[None, None, :] * tropical_mask[:, :, None]
    land_col = land_col * (1.0 - savannah_mask[:, :, None] * 0.78) + col_savannah[None, None, :] * (savannah_mask[:, :, None] * 0.78)

    dune_var = smoothstep(-0.22, 0.24, coast_detail * 0.65 + fine_detail * 0.55)
    desert_col = col_desert_rock[None, None, :] * (1.0 - dune_var[:, :, None]) + col_desert_sand[None, None, :] * dune_var[:, :, None]
    land_col = land_col * (1.0 - desert_mask[:, :, None]) + desert_col * desert_mask[:, :, None]

    land_col = land_col * (1.0 - boreal_mask[:, :, None] * 0.82) + col_boreal[None, None, :] * (boreal_mask[:, :, None] * 0.82)
    land_col = land_col * (1.0 - tundra_mask[:, :, None] * 0.85) + col_tundra[None, None, :] * (tundra_mask[:, :, None] * 0.85)

    # Mountain rock & snow caps (rocky brown/ochre ranges with snow only on the highest dendritic crests)
    rock_blend = smoothstep(0.14, 0.48, mountain_height)
    alpine_snow = smoothstep(0.74, 1.05, mountain_height + smoothstep(50.0, 76.0, abs_lat) * 0.16)
    land_col = land_col * (1.0 - rock_blend[:, :, None] * 0.84) + col_mountain_rock[None, None, :] * (rock_blend[:, :, None] * 0.84)
    land_col = land_col * (1.0 - alpine_snow[:, :, None] * 0.85) + col_snow[None, None, :] * (alpine_snow[:, :, None] * 0.85)

    # Natural micro-albedo variation & topographic relief shading on continents
    relief_grad = np.gradient(mountain_height * 0.65 + coast_detail * 0.25, axis=1)
    land_col *= np.clip(1.0 + micro_tex[:, :, None] * 0.18 + fine_detail[:, :, None] * 0.12 + relief_grad[:, :, None] * 1.6, 0.74, 1.26)

    # Polar ice sheets (Antarctica, Greenland interior, high Arctic)
    greenland_box = smoothstep(-62.0, -52.0, lon_deg) * (1.0 - smoothstep(-22.0, -14.0, lon_deg)) * smoothstep(59.5, 63.5, lat_deg)
    greenland_ice = greenland_box * smoothstep(0.35, 0.72, blur_fine + coast_detail * 0.18) * is_land
    antarctic_ice = smoothstep(62.0, 70.0, -lat_deg + coast_detail * 4.5) * is_land
    arctic_ice = smoothstep(77.5, 84.5, lat_deg + coast_detail * 5.0) * is_land
    ice_mask = np.clip(np.maximum(np.maximum(greenland_ice * 0.95, antarctic_ice), np.maximum(arctic_ice, alpine_snow * 0.9)), 0.0, 1.0)

    # Combine ocean, land, and polar ice
    rgb = ocean_col * (1.0 - is_land[:, :, None]) + land_col * is_land[:, :, None]
    rgb = rgb * (1.0 - ice_mask[:, :, None]) + col_snow[None, None, :] * ice_mask[:, :, None]

    # Encode continuous elevation / mask in [0.0..1.0], then pack into [0.88..1.00] (bytes 224..255):
    ocean_norm = np.clip(0.10 + 0.24 * ocean_t + 0.14 * shelf_mask, 0.0, 0.48)
    land_norm = np.clip(0.52 + 0.14 * np.clip(blur_mid - 0.45, 0.0, 0.55) + 0.20 * np.clip(mountain_height, 0.0, 1.0), 0.52, 0.86)
    elev_norm = ocean_norm * (1.0 - is_land) + land_norm * is_land
    elev_norm = np.maximum(elev_norm, ice_mask * 0.98)
    alpha = 0.88 + 0.12 * elev_norm

    rgba = np.zeros((HEIGHT, WIDTH, 4), dtype=np.uint8)
    rgba[:, :, :3] = np.clip(np.rint(rgb * 255.0), 0, 255).astype(np.uint8)
    rgba[:, :, 3] = np.clip(np.rint(alpha * 255.0), 224, 255).astype(np.uint8)
    return Image.fromarray(rgba, mode="RGBA")


def main():
    repo_root = Path(__file__).resolve().parents[3]
    out_path = repo_root / "Assets" / "_Project" / "Art" / "R1" / "Textures" / "T_R1_Earth_Albedo.png"
    out_path.parent.mkdir(parents=True, exist_ok=True)
    img = generate_earth_albedo()
    img.save(out_path, format="PNG", optimize=False)
    print(f"Generated {out_path} ({img.size[0]}x{img.size[1]} RGBA)")


if __name__ == "__main__":
    main()
