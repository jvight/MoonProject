# R1 model QA gates

Two numeric gates for every R1 model round, run on the FBX in headless Blender (`D:/Blender/blender.exe`). An agy
brief names both as acceptance gates, so agy iterates against numbers instead of judging its own renders. Both work on
any FBX and write their report under `<worktree>/Logs/qa/` unless `--out` says otherwise.

The owner then reviews the round on the 3D review page (see "Owner review" below).

## model_lint: the contract check

```
python tools/art/qa/model_lint.py --fbx Assets/_Project/Art/R1/Models/Rover/RoverModel.fbx --contract rover.contract.json
```

It prints a short summary, writes `Logs/qa/<model>-lint.json`, and exits 0 on PASS, 1 on FAIL.

What it checks:

| Check | Rule |
|---|---|
| required_nodes | every node named in the contract exists |
| pivots | each node's world origin is where the contract says, within tolerance |
| slots | only R1 material slots (the 12 in `R1MaterialRecipes.cs`, or the contract's list); no empty slots |
| vertex_colors | every mesh has a colour attribute (R = AO, G = edge wear, B = cavity dust) |
| triangles / meshes / draw_calls | the budgets, when the contract sets them; a draw call is one mesh × one slot |
| transforms | unit world and local scale; no rotation except on the nodes the contract allows |
| topology | no non-manifold edges, no inconsistently wound faces (a flipped normal), no inside-out closed parts; open borders only when allowed |

Coordinates are Blender world space after a default FBX import: Z up, metres, the space agy authors in. 07 faces +Y.

Contract example:

```json
{
  "model": "rover",
  "nodes": {
    "Body": {},
    "Arm_FL": {"pivot": [-0.44, 0.42, 0.48]},
    "AntennaTip": {"pivot": [-0.33, -0.52, 1.58], "tolerance": 0.03}
  },
  "pivot_tolerance": 0.01,
  "max_triangles": 30000,
  "max_meshes": 100,
  "max_draw_calls": 160,
  "allow_rotation": ["Antenna*"],
  "allow_boundary_edges": true
}
```

## concept_compare: the silhouette check

```
python tools/art/qa/concept_compare.py --fbx Assets/_Project/Art/R1/Models/Rover/RoverModel.fbx --config rover.concept.json
```

Steps:

1. Renders orthographic front, side, back and top silhouettes of the FBX at a fixed metres-per-pixel.
2. Cuts each view out of the concept turnaround with its crop box and keys out the background. A painted ground
   shadow (the background's hue, darker) also counts as background.
3. Calibrates the sheet's scale from one reference dimension, then compares each view after aligning the two masks:
   elevations at the ground line, the top view at its centroid.

Output:

- `concept_compare.json`: per view, IoU, the model's and the concept's width × height in metres, and the size ratio.
- `concept_compare.png`: one heatmap panel per view. Grey is both, red is model only (too much), cyan is concept only
  (missing).

It exits 0 when every view reaches `min_iou`.

Config example (07's v3 turnaround):

```json
{
  "model": "rover",
  "concept": "D:/Project/MoonProject-wt/agy-face/Logs/v3/turnaround.png",
  "views": {"front": [0, 190, 318, 565], "side": [318, 200, 705, 565], "back": [705, 200, 1032, 565], "top": [1032, 170, 1376, 625]},
  "reference": {"view": "front", "axis": "height", "metres": 1.59},
  "front_axis": "+Y",
  "metres_per_pixel": 0.004,
  "min_iou": 0.85
}
```

The sheet's conventions:

- **Views:** front faces the model; side is the model's right, with the front pointing to the image's right; back is
  from behind; top is seen from above with the front pointing down.
- **Crop boxes:** pixel boxes `[x0, y0, x1, y1]` on the sheet. Keep each box clear of the other views and of labels.
- **Reference:** the design's intended size of one dimension, e.g. 07 is 1.59 m tall. Every view uses the scale it
  gives, so a model that is the right height but too long shows as a side-view ratio above 1.
- **Background:** taken from the sheet's border unless `background` is given.
  - `tolerance` (default 12) absorbs the background's own noise.
  - Set `shadow_cos` to `null` when the model's dark paint shares the background's hue.

Reading the numbers: on 07 round 2 against the v3 turnaround, front and back score about 0.87, while the side scores
0.63 with a length ratio of 1.28. The model's wheelbase is about 28% longer than the concept's, which is the
"wrong proportions" the owner saw. A round that only changes surface detail barely moves IoU. Silhouette checks catch
proportion, size and placement, not detail.

## Owner review

The owner reviews each round on the 3D review page (`tools/art/review/index.html`, published as a private artifact):

- the turntable model;
- the approved concept and that round's captures;
- numbered pins with notes on the model;
- a verdict, Duyệt or Cần sửa.

Stage a round in one command, then publish it with the single Artifact call the script prints:

```
python tools/art/review/stage_round.py --model rover --round 3 --fbx <fbx> --concept <img> --capture <img|folder> --changed "<one line>" --out <dir>
```

Some rules for staging:

- Pass `--name` the first time a model is staged.
- `--out` must be inside the publishing session's working directory or scratchpad.
- The page URL and the round registry live in `MoonProject-wt/.review3d/`, so every session sees every round.
- The model goes as GLB without vertex colours, in the R1 palette read from `R1MaterialRecipes.cs`. A material
  outside the R1 slots stops the round.

Pins and verdicts are kept in the page's database:

- `reviews/{model}-r{round}` holds `{verdict: "approved" | "changes", note, updatedAt}`.
- `pins/{id}` holds `{key: "{model}-r{round}", position, normal, text, createdAt}`.

Read them with `ArtifactData`.
