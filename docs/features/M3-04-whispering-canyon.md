# M3-04 — Whispering Canyon

> Owner: Director (design). The first region gated by a rover ability (`docs/DESIGN.md` "Regions"). Rulings in
> `docs/VISION.md` apply: no fail states, no stranding, generous.

## The promise (visible from the first session)
From the base, looking along the rim, the player can see a **narrow canyon cutting into the crater wall**, with a
**chasm** across its mouth and, beyond it, a **glinting ledge** and a faint warm light deep inside. Without Hover-Jump
the chasm is clearly uncrossable (too wide, too deep-looking), but never dangerous: the near lip is a gentle ramp-up,
and driving into the chasm's floor just slides you softly back out (no pit, no fall).

## The space
- **Mouth & chasm**: the canyon opens off the basin floor 180–260 m from the base, on a bearing visible from the base
  edge (not behind the lander or the tower), away from The Peak's bearing so both read separately.
- **Chasm**: ~18–22 m wide at the take-off lip (uncrossable by normal hops ≈ 1 m apex; easily crossed by a
  full-charge leap ≈ 36 m at speed, comfortably by a ~¾ charge), with a slightly raised take-off ramp on the near side
  and a wide, flat landing apron on the far side. The chasm floor is a shallow, smooth trough that can always be driven
  back out of (≤ 20° walls) — it reads deep thanks to darkness, fog and scale, not geometry that traps.
- **Inside**: a winding, narrow (12–25 m), high-walled canyon (~250–350 m long) that climbs gently, with cool blue
  ambient (the "whispering" — wind-like ambience is audio's job), a few alcoves, two or three gentle jump-able ledges
  as optional shortcuts, and a terminus chamber where Bell (M3-05) will lie with a crew log cache. Driveable
  everywhere, max slope ~20°.
- **Return**: a gentle exit back to the basin that doesn't require a jump (a ramp down to the chasm floor's far end
  or a side slope), so 07 is never stuck inside without the ability state mattering.

## Content anchors (Core contract addition via the Director)
World exposes the canyon's key points so gameplay can place things there: `IWorldLayout` gains
`IReadOnlyList<WorldAnchor> Anchors` with ids like `canyon.mouth`, `canyon.ledge`, `canyon.terminus`,
`canyon.alcove_<n>` (position + forward + radius). Proposal to the Director first.

## Split
| Box | Delivers |
|---|---|
| world | canyon + chasm carved into the height function (deterministic, smooth normals), floor paint (cooler, darker swatches inside), scatter rules (boulders on walls, clear driving line), the glinting ledge read from the base, anchors; captures from the base, the lip, mid-leap, inside, terminus |
| art | canyon set dressing later (old cable runs, a toppled antenna, the crew's trail markers) |
| gameplay | content placement at the anchors (scrap trail toward the lip, a relic on the ledge, Bell's site later) |
| audio | canyon ambience (wind-whisper bed, echo/reverb zone inside) |
