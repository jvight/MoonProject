#!/usr/bin/env python3
"""
model_lint: checks an R1 FBX against its per-model contract JSON, so agy iterates against numbers instead of its own
renders. Headless Blender dumps the model's facts (blender_inspect.py); this script judges them.

    python tools/art/qa/model_lint.py --fbx Assets/_Project/Art/R1/Models/Rover/RoverModel.fbx \
        --contract docs/art/contracts/rover.json [--out Logs/qa/rover-lint.json]

Prints a short summary, writes the full report JSON (default <worktree>/Logs/qa/<model>-lint.json) and exits 0 on
PASS, 1 on FAIL. Contract keys (all optional except "model"; docs/art/model-qa.md has an example):
  model                 id used in reports
  nodes                 {"Name": {"pivot": [x, y, z], "tolerance": m}} required node names; a pivot, when given, is
                        the node's world origin in Blender space (Z up, metres) within tolerance
  pivot_tolerance       default pivot tolerance in metres (0.01)
  slots                 allowed material slots (default: the 12 R1 slots, from R1MaterialRecipes.cs)
  require_vertex_colors every mesh carries a colour attribute (R = AO, G = edge wear, B = cavity dust); default true
  max_triangles, max_meshes, max_draw_calls   budgets; a draw call is one mesh x one material slot
  allow_rotation        node name patterns (fnmatch) that may carry a world rotation; others must be unrotated
  scale_tolerance       how far world and local scale may stray from 1 (0.001)
  allow_boundary_edges  open borders allowed (default true; cards and decals have them)
Always checked: no non-manifold edges, no inconsistently wound faces (a flipped normal), no inside-out closed parts.
Tests: python -m unittest discover -s tools/tests -t tools
"""
import argparse
import fnmatch
import json
import math
import subprocess
import sys
import tempfile
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "review"))

import r1_palette  # noqa: E402

HERE = Path(__file__).resolve().parent
BLENDER = Path("D:/Blender/blender.exe")
INSPECT = HERE / "blender_inspect.py"
INSPECT_TIMEOUT = 600
DEFAULT_PIVOT_TOLERANCE = 0.01
DEFAULT_SCALE_TOLERANCE = 0.001
DEFAULT_BOUNDS_TOLERANCE = 0.05
ROTATION_TOLERANCE_DEG = 0.01
LISTED = 20


def check(name, failures, detail):
    return {"check": name, "pass": not failures, "detail": detail, "items": failures[:LISTED],
            "count": len(failures)}


def model_bounds(facts, meshes):
    if "bounds" in facts and facts["bounds"] is not None:
        return facts["bounds"]
    node_bounds = [m["bounds"] for m in meshes if "bounds" in m and m["bounds"] is not None]
    if not node_bounds:
        return None
    bmin = [round(min(b["min"][i] for b in node_bounds), 4) for i in range(3)]
    bmax = [round(max(b["max"][i] for b in node_bounds), 4) for i in range(3)]
    return {"min": bmin, "max": bmax, "size": [round(bmax[i] - bmin[i], 4) for i in range(3)]}


def evaluate(facts, contract, default_slots):
    """The lint report for <facts> (blender_inspect.py output) under <contract>."""
    nodes = {n["name"]: n for n in facts["nodes"]}
    meshes = [n for n in facts["nodes"] if n["type"] == "MESH"]
    checks = []

    required = contract.get("nodes", {})
    missing = [name for name in required if name not in nodes]
    checks.append(check("required_nodes", missing, f"{len(required) - len(missing)} of {len(required)} present"))

    default_tol = contract.get("pivot_tolerance", DEFAULT_PIVOT_TOLERANCE)
    off = []
    for name, spec in required.items():
        if name in nodes and "pivot" in spec:
            distance = math.dist(nodes[name]["pivot"], spec["pivot"])
            if distance > spec.get("tolerance", default_tol):
                off.append(f"{name}: {nodes[name]['pivot']} is {distance:.3f} m from {spec['pivot']}")
    checks.append(check("pivots", off, "pivots within tolerance"))

    allowed = set(contract.get("slots", default_slots))
    bad_slots = [f"{m['name']}: {slot}" for m in meshes for slot in m["slots"] if slot not in allowed]
    bad_slots += [f"{m['name']}: {m['empty_slots']} empty slot(s)" for m in meshes if m["empty_slots"]]
    checks.append(check("slots", bad_slots, f"only the {len(allowed)} allowed slots"))

    if contract.get("require_vertex_colors", True):
        bare = [m["name"] for m in meshes if not m["color_attributes"]]
        checks.append(check("vertex_colors", bare, "every mesh has a colour attribute (AO, wear, dust)"))

    triangles = sum(m["triangles"] for m in meshes)
    draw_calls = sum(max(1, len(m["slots"])) for m in meshes)
    for key, value, label in (("max_triangles", triangles, "triangles"), ("max_meshes", len(meshes), "meshes"),
                              ("max_draw_calls", draw_calls, "draw calls")):
        if key in contract:
            over = [f"{value} {label} > {contract[key]}"] if value > contract[key] else []
            checks.append(check(key.removeprefix("max_"), over, f"{value} {label} (budget {contract[key]})"))

    scale_tol = contract.get("scale_tolerance", DEFAULT_SCALE_TOLERANCE)
    may_rotate = contract.get("allow_rotation", [])
    transforms = []
    for node in facts["nodes"]:
        if any(abs(s - 1.0) > scale_tol for s in node["scale"] + node["local_scale"]):
            transforms.append(f"{node['name']}: scale {node['scale']} (local {node['local_scale']})")
        rotated = any(abs(a) > ROTATION_TOLERANCE_DEG for a in node["rotation"])
        if rotated and not any(fnmatch.fnmatchcase(node["name"], p) for p in may_rotate):
            transforms.append(f"{node['name']}: rotation {node['rotation']} deg")
    checks.append(check("transforms", transforms, "transforms applied (unit scale, no stray rotation)"))

    topology = [f"{m['name']}: {m['non_manifold_edges']} non-manifold edge(s)" for m in meshes
                if m["non_manifold_edges"]]
    topology += [f"{m['name']}: {m['inconsistent_edges']} inconsistently wound edge(s)" for m in meshes
                 if m["inconsistent_edges"]]
    topology += [f"{m['name']}: {m['inverted_shells']} inside-out part(s)" for m in meshes if m["inverted_shells"]]
    if not contract.get("allow_boundary_edges", True):
        topology += [f"{m['name']}: {m['boundary_edges']} open border edge(s)" for m in meshes if m["boundary_edges"]]
    checks.append(check("topology", topology, "manifold, consistently wound, no inside-out parts"))

    bounds = model_bounds(facts, meshes)
    if "bounds" in contract:
        spec = contract["bounds"]
        bounds_off = []
        if bounds is None:
            bounds_off.append("no mesh bounds in facts")
        else:
            size = bounds["size"]
            tol = spec.get("tolerance", DEFAULT_BOUNDS_TOLERANCE)
            tols = [tol, tol, tol] if isinstance(tol, (int, float)) else list(tol)
            if "size" in spec:
                for axis, got, exp, t in zip(("X", "Y", "Z"), size, spec["size"], tols):
                    if abs(got - exp) > t + 1e-6:
                        bounds_off.append(f"size {axis}: {got:.3f} m outside {exp:.3f} +/- {t:.3f} m")
            if "min_size" in spec:
                for axis, got, lo in zip(("X", "Y", "Z"), size, spec["min_size"]):
                    if got < lo - 1e-6:
                        bounds_off.append(f"size {axis}: {got:.3f} m < min {lo:.3f} m")
            if "max_size" in spec:
                for axis, got, hi in zip(("X", "Y", "Z"), size, spec["max_size"]):
                    if got > hi + 1e-6:
                        bounds_off.append(f"size {axis}: {got:.3f} m > max {hi:.3f} m")
        detail = f"size {bounds['size']} m within bounds" if bounds else "missing bounds"
        checks.append(check("bounds", bounds_off, detail))

    totals = {"nodes": len(facts["nodes"]), "meshes": len(meshes), "triangles": triangles, "draw_calls": draw_calls,
              "boundary_edges": sum(m["boundary_edges"] for m in meshes),
              "slots": sorted({s for m in meshes for s in m["slots"]})}
    if bounds is not None:
        totals["bounds"] = bounds
    return {"model": contract.get("model", ""), "fbx": facts.get("source", ""),
            "pass": all(c["pass"] for c in checks), "checks": checks, "totals": totals}


def summary(report):
    failed = [c for c in report["checks"] if not c["pass"]]
    lines = [f"model_lint {report['model']}: {'PASS' if report['pass'] else 'FAIL'} "
             f"({len(report['checks']) - len(failed)} of {len(report['checks'])} checks; "
             f"{report['totals']['triangles']} triangles, {report['totals']['meshes']} meshes, "
             f"{report['totals']['draw_calls']} draw calls)"]
    for c in report["checks"]:
        lines.append(f"- {'PASS' if c['pass'] else 'FAIL'} {c['check']}: {c['detail']}")
        lines += [f"    {item}" for item in c["items"]]
        if c["count"] > len(c["items"]):
            lines.append(f"    ... {c['count'] - len(c['items'])} more")
    return "\n".join(lines)


def inspect(fbx):
    with tempfile.TemporaryDirectory() as tmp:
        target = Path(tmp) / "facts.json"
        res = subprocess.run([str(BLENDER), "-b", "--factory-startup", "--python", str(INSPECT), "--", str(fbx),
                              str(target)], capture_output=True, text=True, encoding="utf-8", errors="replace",
                             timeout=INSPECT_TIMEOUT)
        if res.returncode != 0 or not target.exists():
            raise SystemExit(f"Blender could not inspect {fbx}:\n{res.stdout[-2000:]}{res.stderr[-2000:]}")
        return json.loads(target.read_text(encoding="utf-8"))


def worktree_root():
    res = subprocess.run(["git", "-c", "safe.directory=*", "rev-parse", "--show-toplevel"], capture_output=True,
                         text=True)
    return Path(res.stdout.strip()) if res.returncode == 0 else Path.cwd()


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--fbx", required=True, help="the FBX to check")
    ap.add_argument("--contract", required=True, help="the model's contract JSON")
    ap.add_argument("--out", help="report JSON (default <worktree>/Logs/qa/<model>-lint.json)")
    args = ap.parse_args()
    contract = json.loads(Path(args.contract).read_text(encoding="utf-8"))
    root = worktree_root()
    report = evaluate(inspect(Path(args.fbx).resolve()), contract, sorted(r1_palette.load_palette(root)))
    out = Path(args.out) if args.out else root / "Logs" / "qa" / f"{contract.get('model', 'model')}-lint.json"
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(json.dumps(report, indent=1), encoding="utf-8")
    print(summary(report))
    print(f"report: {out}")
    return 0 if report["pass"] else 1


if __name__ == "__main__":
    sys.exit(main())
