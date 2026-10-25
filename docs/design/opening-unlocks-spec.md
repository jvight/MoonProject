# Opening unlocks: specs for the missing maker-bar rewards

> **For:** S3 (immediate unlocks and the maker bar; `maker-understanding.md` §2.1). Story & flow builds the gameplay;
> Art (agy) builds the models.
>
> **Author:** the game design session, 2026-10-25.
>
> **Checked on main 1610932d:**
> - structures that exist: pad, solar array, battery bank, power pylon, antenna mast;
> - kit pieces that exist: HoverCoils, LampBar, CapacitorDrums, CargoRack, SolarCell, FreshPaint;
> - the rack has bays (the headlamp, the cell pack, the hex adapter outline);
> - charge points are pads and lit relays.
>
> **Already real:** the Kessler–Arai first unlock, the SSR-3 Headlamp (rack bay plus lamp).
>
> **Missing, specced here (the minimum the opening's bars need):**
> 1. the **Crate Rack** (VIS);
> 2. the **Reflector Beacon** (ILC);
> 3. the **Hex Charge Adapter** (Tycho, but as a fragment blueprint; see §3's fix).

## 1. Crate Rack: VIS 4/4 (reached at the tower scan, near the opening's end)
- **What:** the base's first **home storage**: an open frame of three salvaged VIS crate shells. 07's hold stays 07's
  own (memory: the hold is 07's only; home storage is separate).
- **Why the player wants it:**
  - after the release, salvage outgrows the hold (`ui.cargo.waiting`: "left behind: no room aboard");
  - docking within the rack's reach **unloads what the next build doesn't need** into it;
  - it **fills visibly**: one crate lid swings shut per filled stack;
  - a full rack is a visible measure of what 07 has gathered (visible progression).
- **Recipe:** Metal 3 (VIS shells, cut and re-welded).
- **Where:** a base structure, inside the base's reach (normal placement), no power needed.
- **Use rules:** 4 slots of the hold's stack size at first. It unloads on dock, never mid-drive. Taking things back
  happens from the Tab screen's home page only when docked (existing D3 ruling: the auto-unload stays).
- **Model brief (agy):**
  - About 1.6 m wide × 1.2 m tall × 0.9 m deep (07 is 1.5 m tall).
  - A cream tube frame with **three VIS crate shells** in two tiers: their old white enamel chalked, the `VIS`
    stencils half cut away, bright **weld beads** where 07 joined them, and a small cut `07` tag.
  - Lids hinge up. A shut lid shows a small amber latch lamp.
  - Soft, chamfered corners (07's language); no grilles of identical holes (no-hole-patterns).
- **Vacuum logic:** lids keep dust off, not air in. No seals and no doors that imply pressure.

## 2. Reflector Beacon: ILC 3/3 (the airlock read, the seismic station, the retroreflector or flag)
- **What:** a slim post topped with **one corner-cube reflector**, the 1971 idea (the retroreflector) reused as a road
  marker.
- **Why the player wants it:**
  - a corner cube returns light **straight back to its source**, so **only 07's lamps make it shine**. At night a
    line of beacons flashes back one after another as 07 drives along them: a lit trail home that costs no power.
    That is real optics, and it is lovely.
  - each beacon shows on the **range ring** and on 07's CRT map as a small diamond: **player-placed waypoints**
    (genre doc C5 rule 6: the player pins their own goals).
  - typical use: mark the way to Relay 1, the rille notch, or a wreck to come back to.
- **Recipe:** Metal 1 · Optics 1 (cheap: it is placed many times).
- **Where:** **a field build, anywhere** 07 stands on drivable ground. It is the one structure exempt from "outside
  the base's reach". No power. It costs a short weld (≤ 0.1 cell).
- **Use rules:**
  - up to 12 beacons (enough for two routes);
  - dismantling one refunds its Metal;
  - beacons never count as charge points.
- **Model brief (agy):**
  - About 1.3 m tall. A cream post on **three short foot spikes** driven into the regolith.
  - A cap with **one coral band** and a single **corner-cube prism**: a faceted three-sided hollow that catches
    light, not an array of dots (no-hole-patterns).
  - Weld beads and a cut `07` tag.
  - In 07's lamp, a sharp warm star in the corner cube; otherwise dull.
- **Vacuum logic:** passive, no lamp of its own, no flag or cloth.

## 3. Hex Charge Adapter: the Kestrel-3 fragment hunt (hour two), and a fix to the Tycho bar
- **The conflict:**
  - `maker-understanding.md` §2 lists the adapter as **Tycho level 1 at n = 3**, but the basin holds only **1** Tycho
    point (Kestrel-3), so the bar could never reach it in the opening area.
  - `opening-v3-purpose.md` §5 already makes the adapter **the reward of Kestrel-3's two-site fragment hunt**.
  - Rule 2: an adapted blueprint is never also a fragment blueprint.
- **Fix:** the adapter is a **fragment blueprint** (2 fragments in the wreck, 1 at the debris field). The **Tycho
  bar's first unlock becomes the Power Bridge** (formerly level 2), so the bar in the basin reads
  `TYCHO ▮▯▯ · next: Power Bridge`. That is a name only: the bridge is due in 0.6/0.7. (`maker-understanding.md` is
  updated to match.)
- **What:** a **kit piece**: a teal hexagonal contact plate welded over 07's charge port, so 07 can draw power from
  Tycho hex contacts.
- **Why the player wants it:**
  - Kestrel-3's solar boom still makes power behind a hex plate (`CONTACT · HEX · NO ADAPTER`);
  - fitted, **Kestrel-3 becomes a charge point, and the ring grows toward the north and the Peak**;
  - later, every Meridian dock is a charger.
- **Recipe:** Metal 2 · Wiring 2 · Optics 1.
- **Where:** fitted at the pad's rack (its bay already exists and shows its outline after the first fragment). It is
  a big upgrade, so it fits at the bay, not in the field (memory: equipment-07).
- **Model brief (agy):**
  - A flat hexagonal plate about 0.18 m across, in **Tycho white composite with a muted teal rim**, and six small
    contact pins **arranged as one hexagon outline**, not a dot field.
  - It is **welded onto 07's cream flank** over the charge port with bright beads: a foreign part on a home-made
    machine (`tech-eras` era N: the donor keeps its livery).
  - When charging at a hex contact, a soft teal light runs around its rim.
- **Vacuum logic:** a contact charger, not induction. Nothing sparks or arcs.

## 4. What each needs, by domain
| Item | Gameplay (story & flow) | Art (agy) | UI | Audio |
|---|---|---|---|---|
| Crate Rack | a structure id and recipe; storage with 4 slots; unload on dock; a home page in Tab when docked | the model plus the lid open and shut states | the home page and the slot count | a lid clunk; an unload rattle |
| Reflector Beacon | a field-build structure (exempt from the reach rule), a cap of 12, the refund; a map diamond plus a ring marker; a lamp-return glint shader hook | the model plus the corner-cube glint material | a diamond on the ring and the CRT map | a tiny glass "tink" when the glint first catches |
| Hex Charge Adapter | a kit piece `HexAdapter` (a new enum value, landed with its consumers: memory core-land-full-tests); a fragment set ×3; Kestrel's hex contact as an `IChargePointSource` while fitted | the plate on 07's flank (a rig node); Kestrel's hex contact prop | the rack bay fill; `CONTACT · HEX · NO ADAPTER` | a contact click; a teal charge hum |
