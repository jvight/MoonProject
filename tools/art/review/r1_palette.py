"""
The R1 palette for the 3D review page, read from the game's own recipes (Assets/_Project/Scripts/Art/Editor/
R1MaterialRecipes.cs) so the page shows each slot in the colour, smoothness and glow the game uses, and a GLB's
materials are rewritten to it by slot name. Every model on the page then shares one look, whatever the exporter
wrote. The game takes metalness from each slot's mask texture, which a review GLB does not carry, so the page shows
the bare-metal slots (METAL_SLOTS) as metal and every painted, glass or rubber slot as non-metal.

Usage as a script: python r1_palette.py in.glb out.glb   (the repository is found from this file's location)
"""
import json
import re
import struct
import sys
from dataclasses import dataclass
from pathlib import Path

RECIPES = Path("Assets/_Project/Scripts/Art/Editor/R1MaterialRecipes.cs")
SLOT_SUFFIX = re.compile(r"\.\d{3}$")
CONST_RE = re.compile(r'public\s+const\s+string\s+(\w+)\s*=\s*"([^"]+)"\s*;')
COLOR_RE = re.compile(r"new\s+Color\s*\(\s*([-\d.]+)f?\s*,\s*([-\d.]+)f?\s*,\s*([-\d.]+)f?")
NAMED_COLORS = {"Color.black": (0.0, 0.0, 0.0), "Color.white": (1.0, 1.0, 1.0), "Color.clear": (0.0, 0.0, 0.0)}
SPEC_START = "new R1MaterialSpec("
# R1MaterialSpec(slot, slotName, materialAssetName, texturePrefix, intendedUse, baseColor, bumpScale, metallicScale,
# smoothnessMin, smoothnessMax, edgeWearColor, edgeWearAmount, edgeWearMetallic, edgeWearSmoothness, dustAmount,
# hasEmission, emissionColor, emissionStrength)
ARG_SLOT_NAME, ARG_BASE, ARG_SMOOTH_MIN, ARG_SMOOTH_MAX = 1, 5, 8, 9
ARG_HAS_EMISSION, ARG_EMISSION, ARG_EMISSION_STRENGTH = 15, 16, 17
SPEC_ARGS = 18
METAL_SLOTS = frozenset({"R1_BareSteel", "R1_GoldFoil", "R1_Copper", "R1_SilverFoil"})
GLB_MAGIC, GLB_VERSION, CHUNK_JSON = 0x46546C67, 2, 0x4E4F534A


@dataclass(frozen=True)
class SlotLook:
    base: tuple          # linear RGB
    metallic: float
    roughness: float
    emissive: tuple      # linear RGB, (0, 0, 0) when the slot does not glow


def repo_root():
    return Path(__file__).resolve().parents[3]


def srgb_to_linear(c):
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def split_args(text):
    """Top-level comma-separated arguments of a call whose body is <text> (without the outer parentheses)."""
    args, depth, current, quoted = [], 0, "", False
    for ch in text:
        if ch == '"':
            quoted = not quoted
        elif not quoted and ch in "([{":
            depth += 1
        elif not quoted and ch in ")]}":
            depth -= 1
        elif not quoted and ch == "," and depth == 0:
            args.append(current.strip())
            current = ""
            continue
        current += ch
    args.append(current.strip())
    return args


def call_bodies(text, start):
    """The argument text of every call beginning with <start>."""
    bodies = []
    index = text.find(start)
    while index >= 0:
        depth, cursor = 1, index + len(start)
        while depth and cursor < len(text):
            depth += {"(": 1, ")": -1}.get(text[cursor], 0)
            cursor += 1
        bodies.append(text[index + len(start):cursor - 1])
        index = text.find(start, cursor)
    return bodies


def parse_color(arg):
    if arg in NAMED_COLORS:
        return NAMED_COLORS[arg]
    match = COLOR_RE.search(arg)
    if not match:
        raise ValueError(f"cannot read a colour from '{arg}'")
    return tuple(float(v) for v in match.groups())


def parse_float(arg):
    return float(arg.rstrip("fF"))


def load_palette(root=None):
    """{slot name: SlotLook} for every R1 slot in the game's recipes."""
    path = Path(root or repo_root()) / RECIPES
    text = path.read_text(encoding="utf-8-sig")
    consts = dict(CONST_RE.findall(text))
    palette = {}
    for body in call_bodies(text, SPEC_START):
        args = split_args(body)
        if len(args) != SPEC_ARGS:
            raise ValueError(f"{path}: an R1MaterialSpec has {len(args)} arguments, expected {SPEC_ARGS}")
        name = consts.get(args[ARG_SLOT_NAME], args[ARG_SLOT_NAME].strip('"'))
        smoothness = (parse_float(args[ARG_SMOOTH_MIN]) + parse_float(args[ARG_SMOOTH_MAX])) / 2.0
        emission = (0.0, 0.0, 0.0)
        if args[ARG_HAS_EMISSION] == "true":
            strength = min(1.0, parse_float(args[ARG_EMISSION_STRENGTH]))
            emission = tuple(srgb_to_linear(c) * strength for c in parse_color(args[ARG_EMISSION]))
        palette[name] = SlotLook(tuple(srgb_to_linear(c) for c in parse_color(args[ARG_BASE])),
                                 1.0 if name in METAL_SLOTS else 0.0, round(1.0 - smoothness, 4), emission)
    if not palette:
        raise ValueError(f"{path}: no R1MaterialSpec found")
    return palette


def slot_of(material_name):
    """The R1 slot a material name stands for: Blender and FBX add '.001'-style suffixes to repeated names."""
    return SLOT_SUFFIX.sub("", material_name)


def read_glb(path):
    data = Path(path).read_bytes()
    magic, version, _ = struct.unpack_from("<III", data, 0)
    length, kind = struct.unpack_from("<II", data, 12)
    if magic != GLB_MAGIC or version != GLB_VERSION or kind != CHUNK_JSON:
        raise ValueError(f"{path} is not a glTF 2.0 binary")
    return json.loads(data[20:20 + length]), data[20 + length:]


def write_glb(path, gltf, rest):
    body = json.dumps(gltf, separators=(",", ":")).encode("utf-8")
    body += b" " * ((4 - len(body) % 4) % 4)
    header = struct.pack("<III", GLB_MAGIC, GLB_VERSION, 12 + 8 + len(body) + len(rest))
    Path(path).write_bytes(header + struct.pack("<II", len(body), CHUNK_JSON) + body + rest)


def apply_palette(src, dst, palette):
    """Rewrites every material of GLB <src> to its R1 slot's look and writes <dst>; returns the slots used.
    A material that names no R1 slot stops the round: the model breaks the slot contract."""
    gltf, rest = read_glb(src)
    used, unknown = set(), set()
    for material in gltf.get("materials", []):
        slot = slot_of(material.get("name", ""))
        if slot not in palette:
            unknown.add(material.get("name", ""))
            continue
        look = palette[slot]
        used.add(slot)
        material["name"] = slot
        material["pbrMetallicRoughness"] = {"baseColorFactor": list(look.base) + [1.0],
                                            "metallicFactor": look.metallic, "roughnessFactor": look.roughness}
        material.pop("extensions", None)
        material.pop("normalTexture", None)
        material.pop("occlusionTexture", None)
        material.pop("emissiveTexture", None)
        if any(look.emissive):
            material["emissiveFactor"] = list(look.emissive)
        else:
            material.pop("emissiveFactor", None)
    if unknown:
        raise ValueError(f"{src}: materials outside the R1 slots: {', '.join(sorted(unknown))}")
    if not any("extensions" in m for m in gltf.get("materials", [])):
        gltf.pop("extensionsUsed", None)
    write_glb(dst, gltf, rest)
    return sorted(used)


def main():
    if len(sys.argv) != 3:
        sys.exit(__doc__)
    used = apply_palette(sys.argv[1], sys.argv[2], load_palette())
    print(f"palette ok {sys.argv[2]}: {', '.join(used)}")


if __name__ == "__main__":
    main()
