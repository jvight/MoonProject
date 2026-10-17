# MASTER GAME DESIGN DOCUMENT: LOFI LUNAR (Working Title)
**Genre:** Cozy / Driving Sim / ASMR Exploration / Base Decorating
**Engine:** Unity URP (Universal Render Pipeline)
**Visual Vibe:** Low-poly, dream-like, stylized, midnight blue/purple tones, warm lighting. "WALL-E meets Journey".

## 1. CRITICAL RULES (THE ZERO-STRESS MANDATE)
- NO survival mechanics (No health, oxygen, hunger, enemies, or fail states). Superseded 2026-10-17: 07 has a wear
  meter, dangerous Reclaimer machines exist and 07 can die (`docs/VISION.md` ruling 17). Still no oxygen or hunger.
- NO complex realistic physics (NEVER use Unity's built-in `WheelCollider`).
- NO terrain deformation (voxel digging). Excavation is strictly visual using particles and lighting.
- NO free-form grid building (Use automatic Snap-Points for base decorating to prevent physics bugs).

## 2. PLAYER CONTROLLER (THE ROVER)
- **Physics:** Runs on a hidden Sphere Rigidbody (`AddForce`). Visual car mesh tracks the sphere and aligns rotation to the ground normal via Raycasts.
- **Gravity:** Lunar gravity set to `-1.62`. Movement must feel floaty, bouncy, and relaxing.
- **Camera:** `Cinemachine FreeLook` with high damping and Auto-Recenter. Floaty cinematic drone feel.
- **VFX:** `TrailRenderer` for tire tracks, `ParticleSystem` for low-gravity moon dust. Warm yellow spotlight in front.

## 3. CORE GAMEPLAY SYSTEMS
**A. Scrap Collection (Drive-by Magnetism):**
- Small glowing scrap pieces (currency) scattered around. Collected automatically when the rover drives near them (they float and Lerp into the rover). Emits a pleasant ASMR chime sound.

**B. Relic Excavation (Sonar & Tractor Beam):**
- **Sonar Ping:** Press [Space] to ping. Hidden relics ping back with a Kalimba note and glowing ground decal.
- **Extraction:** Park over the spot, hold [E]. A glowing Tractor Beam cone points down. The hidden relic smoothly floats up into the air amidst swirling dust particles.

**C. Physical Cargo Transport (Soft Friction):**
- Extracted relics physically drop into the rover's open truck bed. Driving recklessly over bumps can make them bounce out, encouraging careful, cozy driving back to base.
**D. Physics Drag & Drop (The Energy Tether):**
- **Purpose:** A tactile, physics-based method for transporting Relics, inspired by games like "REPO". Replaces traditional invisible inventory systems.
- **The Mechanic:** The player aims at an excavated Relic and holds [Right Mouse Button]. A glowing energy beam (`LineRenderer`) shoots from the Rover and attaches to the object.
- **Physics Behavior (Crucial):** The tether uses an elastic `SpringJoint`. The object floats, slides, and bounces playfully behind the rover in low gravity.
- **Progression Flow:**
  - *Early Game (Towing):* Without a Cargo Bed, the player tethers the item and physically drags it behind the rover across the dunes.
  - *Mid Game (Crane Loading):* With the Cargo Bed attached, the player uses the tether to lift items up and drop them carefully into the back of the rover (Physics Tetris).
- **Anti-Frustration Rule:** The joint must have high damping. If an object gets stuck and the distance between the rover and the object exceeds a `maxTetherDistance`, the connection smoothly breaks (SNAPS) to prevent the heavy Rover from flipping over.

## 4. PROGRESSION & HOME BASE
**A. The Base (Lofi Oasis):**
- An abandoned lander. Players deposit collected relics onto predefined invisible Snap-Points (museum shelves).
- Upgrading the Base Radio Tower with Scrap expands a "Cozy Signal Radius" (better lighting, clearer Lofi music).
- Ultimate Base Goal: Grow a massive, bioluminescent alien tree (The Biodome).

**B. Upgrades (Metroidvania Traversal):**
- Spend Scrap to upgrade the Rover: e.g., `Hover-Jump` (charged leap for huge chasms) or `Magnetic Treads` (grip for steep craters).

**C. Dynamic Events & Endgame:**
- Occasional "Eclipses" reveal hidden bioluminescent paths. "Solar Flares" temporarily overcharge the rover for fast driving.
- **Endgame Goal:** Find audio logs of the past. Reach the highest peak, repair a massive satellite dish, and broadcast the final Lofi track to Earth.