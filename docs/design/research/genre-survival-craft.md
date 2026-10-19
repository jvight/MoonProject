# Genre deep dive: survival-craft and cozy-craft openings and progression

Status: research input, 2026-10-19. Second research doc for the Lunarion opening, after
`research/player-guidance.md`. That doc covered general guidance (Zelda, Journey, Portal, Outer Wilds, Stardew)
and the basics of Subnautica's pod and radio, Astroneer's tether, Planet Crafter's index and Factorio FFF #241. This
doc does not repeat those. It goes deeper into games in our own genre: open-world exploration with crafting, scanning
and base building. It then tests `opening-v3-purpose.md` (branch `box/gd-opening-purpose`) against them.

**Confidence.** Claims with a link come from that source. Claims marked *(memory)* come from my knowledge of the
games and are standard, but I did not confirm them in a source this session. Claims marked *(unverified)* are details
I am unsure of (exact counts and timings). Recheck these before using them in a pitch.

---

## Part A. Case studies

Each entry covers the same eight questions:
- **10 min:** the first ten minutes.
- **Goal:** how the goal is communicated.
- **Build why:** what motivates building.
- **Tech:** how blueprints and tech are discovered.
- **First base:** what the first base does for the player.
- **Economy:** items in the first hour.
- **Pull out:** what pulls the player out of the starter area.
- **Hook:** the session-end "one more thing".

### A1. Subnautica (Unknown Worlds, 2018): the genre's reference

- **10 min:** pod fire, PDA, fabricator *(see player-guidance A1)*. Then the Safe Shallows: bright, shallow, harmless.
  Every early resource lies within about 30 m of the pod. These are titanium from limestone outcrops, copper, quartz,
  salt and coral *(memory)*.
- **Goal:** there is no quest list. The PDA keeps a log of received messages. Each repaired radio call adds a
  **beacon with a distance** on the HUD, which the player can hide. Lead designer David Kalina said of the sequel:
  "We really don't want to explicitly tell you what to do, ever… But of course, we do give you signals"
  ([Inverse](https://www.inverse.com/gaming/subnautica-below-zero-interview-twofer)).
  Cleveland's GDC 2019 talk lists "adding structure to a sandbox through radio signals" as a core topic
  ([GDC](https://gdconf.com/news/dive-deep-design-subnautica-gdc-2019)).
- **Signals as quests:** the radio calls escalate across the first hours *(memory)*:
  - a lifepod nearby (an easy first trip);
  - pods farther out, which teach new biomes;
  - Second Officer Keen's rendezvous call;
  - the **Sunbeam**: a rescue ship that promises pickup on a countdown, then gets shot down by an alien gun. That
    reveal reframes the whole game.

  Pods give the location; the Sunbeam gives a time. Each signal is a person with a reason.
- **Tech:** almost all early vehicles and tools are **fragments** of broken machines lying in the world
  *(see A1 and part C3 below)*. **Databoxes** in wrecks give a whole blueprint in one find. The best ones sit behind
  sealed doors: the laser cutter opens them, and the repair tool fixes a broken panel *(memory)*. A gate inside a
  wreck is a small lesson in "gate before key".
- **Build why:** early fabrication answers thirst, hunger and oxygen. Base building is **optional**: the lifepod
  is enough to survive. Players build a first base because they want storage, a home with a view, grow beds, and
  later a forward base at depth *(memory)*.
- **First base:** storage and a sense of ownership. Later bases are oxygen and charging stations that push the
  depth frontier outward.
- **Economy:** a handful of materials, each tied to a want the player already has. A scan of a fragment whose
  blueprint you already know gives a little titanium *(memory, unverified amount)*, so no scan is wasted.
- **Pull out:** depth limits plus wrecks you can see from the shallows. The Aurora is always visible. It explodes
  after a set time *(unverified)* and then becomes a radiation-gated goal.
- **Hook:** "one more wreck" and "one more fragment". The next depth band is always visible in the dark below.

### A2. Subnautica: Below Zero (2021): what it did worse, and why

Below Zero kept the systems and changed the frame. The reasons it lands lower with many fans, roughly in order of
evidence:

1. **The story stalled and was rewritten mid-development.** Unknown Worlds brought in a new writer after more than a
   year of Early Access ([TechRaptor](https://techraptor.net/gaming/news/subnautica-below-zero-scrapping-original-story-hires-new-writer);
   [GameStar](https://www.gamestar.de/artikel/subnautica-below-zero-preview,3358632.html)). The new opening starts in
   space, not at a base ([KeenGamer](https://www.keengamer.com/articles/features/opinion-pieces/subnautica-below-zero-will-the-new-story-be-better-than-the-old-one/)).
2. **The land did not carry the game's strengths.** The studio wrote that "the surface isn't up to the quality
   standards of our fans or the team" and that much of what works underwater "doesn't automatically translate onto
   the surface" ([Unknown Worlds](https://unknownworlds.com/news/subnautica-below-zero-story-changes-and-save-games)).
   Reviews repeat it: land sections are "not fun to explore" or "dilute the experience", and IGN noted less room to
   explore ([OpenCritic](https://opencritic.com/game/11360/view/reviews)).
3. **A voiced, defined protagonist.** Robin is "a speaking player protagonist with a lot to think and say"
   ([Unknown Worlds, Jill Murray](https://unknownworlds.com/en/news/how-we-approach-story)). Players say it weakened
   the self-projection of the silent original. They also call her snark flat
   ([Steam forum](https://steamcommunity.com/app/848450/discussions/0/1751274167972463506)). This is player sentiment,
   not a critic consensus.
4. **A smaller, denser map with less mystery.** The original's dread came from scale and the unknown. The sequel's
   story, about a missing sister and an alien companion, explains more and leaves less to infer *(my reading of
   reviews)*.

**Lesson for us.** The first game worked because the player was a silent stranger. Signals were people far away,
the world was large and mostly unexplained, and every reason was a want. When the sequel explained more, gave the
hero a running voice and moved play to a weaker surface, it lost some of that. Lunarion's 07 is wordless; keep it
that way. Our surface is our whole game, so driving on it must be as good as Subnautica's swimming.

### A3. The Planet Crafter (Miju Games, 2022)

- **10 min:** a convict wakes in a crashed pod on a dead, dusty planet with limited oxygen
  ([Wikipedia](https://en.wikipedia.org/wiki/The_Planet_Crafter)). The first actions are mining iron and building a
  tiny pod habitat, then the first oxygen and heat machines *(memory)*.
- **Goal:** the Terraformation Index *(see player-guidance A7)*. A dedicated screen shows the current values and
  **the next unlock to reach** ([Pieuvre review](https://www.pieuvre.ca/?p=60372)). The goal is a number, but the
  sky, ice and later water make it visible.
- **Build why:** every machine raises one of the index components. Power is the gate. One small solar panel is
  enough at first, then several, then reactors ([Pieuvre](https://www.pieuvre.ca/?p=60372)).
- **Tech:** most unlocks come at index thresholds. Microchips in wrecks and containers give extra blueprints
  *(memory)*. Both channels are visible: you know what the next threshold gives.
- **First base:** air and storage, so you can stay out longer. Then it becomes a showroom of your progress.
- **Economy:** iron, cobalt, silicon, titanium, magnesium, ice *(memory)*. There are few types, and each is named
  by a machine you already want.
- **Pull out:** wrecks and rarer ores farther away. Biomes visibly change as the index rises.
- **Hook:** "I'm two thresholds from the next stage". Each stage changes the world (first rain, first lake, first
  grass).
- **What failed:** critics flagged **sparse early-game content** and little variety in places
  ([Wikipedia](https://en.wikipedia.org/wiki/The_Planet_Crafter)). The co-founder framed it as "a chill, non-violent
  survival experience" ([Galaxus](https://www.galaxus.fr/fr/page/creer-un-monde-vivant-dans-the-planet-crafter-32696)).
  It is our closest tonal cousin, and its weakest part is the first hour.

### A4. Astroneer (System Era, 2016–2019)

- **10 min:** land, step out of a small shelter with a printer, dig, print a tether *(see player-guidance A7)*.
  The tether mission "Breathing Space" asks you to print tethers and connect to oxygen
  ([Astroneer wiki](https://astroneer.wiki.gg/wiki/Tether)). Tethers are blue when connected and black when not,
  which is a one-glance safety readout.
- **Goal:** a short mission list from the landing pad gives first tasks
  ([review summary via search](https://www.shacknews.com/article/104089/astroneer-gameplay-interview)). The long
  goal, waking the planet cores, came only with 1.0 *(memory)*. Before that the game had a strong toybox and a weak
  "why".
- **Build why:** oxygen and power reach. Lead designer Joe Tirado described the focus as "removal of friction" and
  the game as "a toybox for the players, not work"
  ([ButWhyTho](https://butwhytho.net/2019/09/03/interview-astroneer-exploration-and-community-with-joe-tirado/)).
- **Tech:** research items found in the world and in wrecks go into a research chamber and produce bytes, which buy
  blueprints from a catalog *(memory)*. The player chooses what to unlock.
- **First base:** a hub that the tether lines run back to. Platforms snap on, and power is visibly shared.
- **Economy:** resin, compound, organic and a few ores. The first craft uses all of them.
- **Pull out:** caves and research items glinting in the dark, plus rover and shuttle unlocks.
- **Hook:** "the next byte unlock is close", and deeper caves.
- **Lesson:** a visible line back home is both tutorial and comfort. Without a long goal, a toybox drifts.

### A5. Satisfactory (Coffee Stain, 2019–2024)

- **10 min:** a drop pod lands. ADA, a dry corporate AI voice, opens "Onboarding" with objectives. Hand-mine iron,
  then build **the HUB**: a terminal plus a small habitat
  ([Satisfactory wiki](https://satisfactory.wiki.gg/wiki/Tutorial)).
- **Goal:** the HUB upgrades are six steps, done strictly in order. Each step costs items and unlocks the next
  machines:
  1. a storage box and a portable miner;
  2. a smelter and a biomass burner;
  3. a constructor and power poles;
  4. conveyors;
  5. Miner Mk.1;
  6. the Space Elevator ([wiki](https://satisfactory.wiki.gg/wiki/Tutorial);
     [Milestone](https://satisfactory.wiki.gg/wiki/Milestone)).

  After that, milestones are picked freely within a tier. The Space Elevator asks for big shipments that unlock new
  tiers (phases).
- **Build why:** the HUB terminal always shows the cost of the next milestone. Feeding it is the goal. Hand-feeding
  is tedious on purpose, so automation is the reward.
- **Tech:** two channels:
  - **milestones**, chosen from a visible list;
  - **the MAM**, which researches things you **find** (ores, plants, Hard Drives from crash sites). A Hard Drive
    offers a choice of two alternate recipes ([MAM wiki](https://satisfactory.wiki.gg/wiki/MAM)).

  Exploration feeds the second channel. Crash sites are visible lures, and they often need power or items to open
  *(memory)*.
- **First base:** the HUB is a home, a goal terminal and the first power and storage. It is a bed with a to-do
  list built in.
- **Economy:** iron only for the first steps, then copper, limestone and biomass. It is tightly staged.
- **Pull out:** the next milestone needs a resource that is not near the HUB.
- **Hook:** "one more milestone". The cost is shown, and the factory keeps running while you plan.
- **Lesson:** a goal terminal at home makes the base the quest-giver. Its weakness is that it is a list, which a
  factory game can afford and a cozy game should hide.

### A6. No Man's Sky (Hello Games, 2016 → NEXT 2018 → Waypoint 2022)

- **2016, 10 min:** you wake beside your crashed ship on a random, often hostile planet *(memory)*. You repair the
  scanner, launch thrusters and pulse engine with gathered elements. A red orb, the Atlas, offers a path.
- **2016, what failed:** progress required "hard grinding" and the universe felt empty
  ([The Spinoff](https://thespinoff.co.nz/featured/20-12-2016/back-from-the-abyss-the-story-of-no-mans-skys-death-and-resurrection/)).
  Inventory slots were tight *(memory)*. Procedural planets gave variety without places that mean something.
- **Atlas Rises (1.3, 2017):** added a story campaign and procedural missions
  ([Gematsu](https://gematsu.com/2017/08/no-mans-sky-atlas-rises-update-now-available)). The story is carried by a
  distant voice in distress, Artemis, reaching out across space *(memory)*.

  This is the genre's closest match to our "call": a signal that is a person with a plea, not an abstract tone.
- **NEXT (2018):** a "completely overhauled tutorial", in which you gather, repair equipment and find the ship
  ([search summary of NEXT coverage](https://press-start.com.au/?p=62834)). Some found it clearer. PCGamesN wrote
  that they "wandered about like a bewildered old man" and had "no idea what I should be doing"
  ([PCGamesN](https://www.pcgamesn.com/no-mans-sky/no-mans-sky-next-pc-review)). Longer scripted steps alone did not
  fix confusion.
- **Waypoint (4.0, 2022): friction and choice.** The patch notes
  ([DSOGaming](https://www.dsogaming.com/patches/no-mans-sky-waypoint-update-4-0-released-full-patch-notes-revealed/)):
  - Guide, Catalogue and Milestones were merged into one portal.
  - "Milestones and medals can now be pinned as a mission, giving guidance on how to progress."
  - Tutorial missions and pre-known recipes became difficulty options.
  - Stack sizes grew a lot.
  - Creative mode starts with "a short and non-mandatory guidance mission".
  - A new Relaxed mode.
- **Tech:** blueprints come from a research station, salvaged modules and rewards *(memory)*.
- **Pull out:** signal boosters and planetary charts reveal points of interest *(memory)*. A signal is a
  consumable lure.
- **Hook:** "one more system". Freighter and base growth.
- **Lesson:** the rework that worked was cutting friction (inventory, saving, difficulty) and letting the player pin
  their own goal. A longer tutorial did not help.

### A7. Valheim (Iron Gate, 2021)

- **10 min:** the raven Hugin drops you at a ring of **sacrificial stones**. They show boss silhouettes, with empty
  places where trophies will hang ([Inven Global guide](https://www.invenglobal.com/articles/13389/valheim-guide-beginners-tips-tricks-basics)).
  You punch trees, gather stone and make a club and an axe. Hugin appears when you meet something new or reach a
  milestone. His tips are kept in the compendium
  ([ThumbCulture](https://www.thumbculture.co.uk/valheim-early-access-build-your-saga)).
- **Goal:** the stones are a permanent, wordless goal board at spawn. **Vegvisir** runestones reveal a boss altar
  on the map. The altar asks for an offering ("hunt his kin", meaning deer trophies)
  ([GGRecon](https://www.ggrecon.com/guides/how-to-summon-defeat-the-first-boss-eikthyr-in-valheim)).
- **Build why:** the **Rested** buff *(memory)*:
  - Sitting near a fire under a roof gives a stamina buff.
  - Comfort items make it last longer.
  - The workbench needs a roof.

  Shelter is a mechanical reward, and decorating is too. A cozy home is power.
- **Tech:** recipes unlock when you **pick up** a new material *(memory)*. The workbench shows what its upgrades
  allow. Boss drops are keys: Eikthyr's antlers make the pickaxe that mines copper and tin, which opens the Bronze
  Age ([GGRecon](https://www.ggrecon.com/guides/how-to-summon-defeat-the-first-boss-eikthyr-in-valheim)).
- **First base:** rest, safety, a spawn point, a place to smelt.
- **Economy:** wood, stone, flint, resin, leather. Every early item is a craft you can already see in the menu.
- **Pull out:** the next biome is visible but dangerous. Swamps need a key from the second boss; mountains need
  cold protection *(memory)*.
- **Hook:** "one more upgrade to the house", and the next trophy slot that stays empty at spawn.
- **Lesson:** goals are **physical objects at home**: empty trophy hooks. Hugin is a calm, skippable voice that
  speaks only on first contact. Iron Gate cited Breath of the Wild as an inspiration
  ([PCGamesN](https://www.pcgamesn.com/valheim/ps1-graphics)). Combat aside, the stones-and-altar pattern transfers
  directly.

### A8. Raft (Redbeet, 2018–2022) and Grounded (Obsidian, 2020–2022)

**Raft.**
- **10 min:** a 2×2 raft, a hook, debris floating past, and a shark. Hook planks and plastic, build out, purify
  water. There is "very little tutorial" ([Shacknews](https://www.shacknews.com/article/131150/raft-review-steam)).
- **Design:** Redbeet credit "the limitation of freedom". The raft is "base and home", and islands are short stops.
  The shark's job is to keep you from settling
  ([Game Developer Q&A](https://www.gamedeveloper.com/design/q-a-how-steam-hit-i-raft-i-stands-out-in-the-crowded-survival-genre)).
- **Mysterious objective:** a **Receiver with antennas** picks up signals and points to story islands: a radio
  tower, then a ship and towns *(memory)*. Building the radio is what turns survival into a journey.
- **Lesson:** the base moves with you, and building a listening device is the moment the game gains direction.
  That is our mast beat, already proven.

**Grounded.**
- **10 min:** you wake shrunk in a backyard. A lab and a robot, BURG.L, give tasks and buy tech with collected
  data. An **analyzer** teaches recipes from items you bring it *(memory)*. Note that Grounded has combat.
- **Lesson:** a mystery ("who shrank us?") plus a place-bound quest-giver works. The analyzer is a scan-to-learn
  cousin, but at a station, not in the field.

### A9. Slime Rancher (Monomi Park, 2016)

- **10 min:** you arrive at a run-down ranch. You vacuum pink slimes, put them in a corral, feed them carrots and
  sell their plorts *(memory)*. A design club analysis says the tutorial "didn't need to cover everything"
  ([Make Games SA](https://makegamessa.com/discussion/5705/game-design-club-1-slime-rancher)).
- **Goal:** money to grow the ranch. Plots and corrals are visible and empty, waiting to be filled. Locked
  **ranch expansions** are visible gates that open with money *(memory)*. **Gordo** slimes block paths and pop when
  fed, which is a feeding-as-key gate *(memory)*.
- **Build why:** every corral upgrade solves a problem you just had: escapes, slimes eating each other, plort
  loss.
- **Notes and letters:** Hobson's notes lie in the world as lore
  ([wiki via search](https://slimerancher.fandom.com/wiki/The_Ranch)). Mail and ranchers' requests arrive over days
  *(memory)*.
- **Economy:** four VAC slots force trips home. This ties exploring to the ranch
  ([Make Games SA](https://makegamessa.com/discussion/5705/game-design-club-1-slime-rancher)).
- **Hook:** "hidden timers keep the player checking on things without creating stress". Missing one costs an
  opportunity, not progress (same source).
- **What failed:** repetitive feeding before automation; fetch quests with poor rewards; "empty nooks" in
  exploration; late progression that didn't match early growth (same source).
- **Lesson:** a cozy loop needs home and exploration to feed each other, and rewards that keep growing past hour
  two.

### A10. Dredge (Black Salt Games, 2023)

- **10 min:** a storm wrecks you near a lighthouse. A mayor lends you a boat to pay off
  ([TechRaptor review](https://Techraptor.net/gaming/reviews/dredge-review)). You fish by day. Night brings fog and
  strangeness. The Marrows is the tutorial area and the hub
  ([Dredge wiki](https://dredge.wiki.gg/wiki/Regions)).
- **Goal:** a strange collector asks you to dredge relics. Each relic needs gear you don't have yet, which is a
  quest-giver who is also a mystery *(memory)*. "As you work… a mystery will start to reveal itself"
  ([Way Too Many Games](https://waytoomany.games/2023/04/14/review-dredge/)).
- **Build why:** you upgrade your boat, not a base:
  - hull: more cargo grid;
  - engines: reach farther regions within daylight;
  - lights: night safety;
  - rods: new fish types.

  Research parts dredged from wrecks feed the tree. The team aimed to "avoid adding in any unnecessary grind"
  ([80.lv](https://origin.80.lv/articles/black-salt-games-on-the-development-of-dredge-the-pale-reach-dlc)).
- **Gates:** distance plus daylight. A slow engine means a far region is a night trip. The Pale Reach is meant for
  players with "better engines" ([God is a Geek](https://godisageek.com/reviews/dredge-the-pale-reach-review/)).
  Every gate is soft and readable: you *can* go, but it costs.
- **Economy:** a spatial cargo grid. Packing is a small, satisfying puzzle, and it turns the inventory chore into a
  mechanic.
- **What failed, then got fixed:** players asked for map markers. Update 1.1 added up to 50
  ([Black Salt Games](https://blacksaltgames.com/dredge-update-v-1-1-0-map-markers-more)).
- **Hook:** a day is short. You end at the dock with money, an upgrade in reach and a question.
- **Lesson:** upgrades that change *range per day* make the map grow without new UI. Daylight as a soft clock is
  calm if you can always get home.

### A11. Factorio (Wube): new findings beyond FFF #241

- **FFF #261:** the old tutorial had about 22 game-stopping dialogs. Kovarex: "if the first 15 minutes… feels
  shitty, there is big chance that the player will not play any further". Testing self-discovery, the team found
  that players who miss something basic "get stuck with no clear direction". Some things are not fun to discover
  alone, so "nothing is for free" ([FFF #261](https://factorio.com/blog/post/fff-261)).
- **FFF #342:** when they revived the old tutorial, they set **channel rules** ([FFF #342](https://factorio.com/blog/post/fff-342)):
  - Each channel has one purpose. Flavour text carries nothing necessary.
  - Players who skip every speech bubble must still be able to finish.
  - Never ask for attention on a concept before it is needed.
- **Lesson for us:** our one-element rule needs a matching channel rule. 07's face, world light and the rare text
  line each have one job, and none carries a reason the others don't.

### A12. Cozy building: Lightyear Frontier, Dyson Sphere Program

**Lightyear Frontier (2024, Early Access).**
- **10 min:** a rough landing, then repairing your farming mech and finding its tools
  ([GamesRadar](https://gamesradar.com/lightyear-frontier-review)).
- **Tone:** no threat. Reviewers compare it to "a cozy Subnautica"
  ([God is a Geek](https://godisageek.com/2024/03/lightyear-frontier-early-access-review-stress-free-building/)).
- **Complaints:** unclear guidance, no map markers, and a start that can feel slow
  ([PureXbox round-up](https://purexbox.com/news/2024/03/heres-what-the-reviews-are-saying-about-lightyear-frontier-on-xbox-game-pass)).
- **Lesson:** when there is no threat, structure has to come from goals and lures, or calm turns into aimless.

**Dyson Sphere Program** *(memory)*. You land a mech on a planet. Research matrices made in your own factory unlock
the tech tree. The title object, a sphere around the star, is the far goal from the menu screen. The first hour is
hand-crafting into the first automation.

**Lesson:** a far goal named by the game itself, plus a tech tree you fill by building, holds players for hundreds
of hours. The cost is a heavy list UI.

### A13. Abiotic Factor (2024), brief

There is no real tutorial. Guides say it "expects you to poke at things, read environmental cues"
([GameBrief](https://www.gamebrief.net/blog/abiotic-factor-beginner-guide-tips-2026)). Players praise cozy
decorating of a base inside a hostile facility
([Gaming Trend](https://gamingtrend.com/reviews/abiotic-factor-review-push-beyond-the-unknown/)). It has combat.

**Lesson:** a strong place and a dark mystery can carry a no-tutorial start for players who like that genre. Ours is
calmer and broader, so we should not count on the same tolerance.

---

## Part B. Steam-review complaints about openings in this genre, and the fixes that worked

Sources: VaporLens Steam review summaries
([Valheim](https://vaporlens.app/app/892970/valheim) and others in the search), the reviews cited above, and
patch notes. These are aggregated themes, not statistics.

| Complaint | Where it shows | Fix that worked | Source |
|---|---|---|---|
| **"I don't know what to do"** | NMS NEXT; Lightyear Frontier; Factorio self-discovery tests | Pinnable goals the player chooses; a short optional guidance mission; a world signal with a location | NMS Waypoint; FFF #261; Subnautica beacons |
| **"Too slow at the start"** | Planet Crafter (sparse early content); Lightyear Frontier; Dredge (slow early fishing in some reviews) | Put the first visible world change early; give a far goal at once; keep the first need short | Wikipedia (Planet Crafter); Dredge's lighthouse and mayor in minute 1 |
| **"Grindy"** | NMS 2016; Slime Rancher fetch quests; survival games in general | Fewer, bigger rewards; automation as a reward; remove "unnecessary grind" | Spinoff; Make Games SA; 80.lv (Dredge) |
| **"Inventory clutter"** | Valheim hauling; NMS slots; many survival games | Bigger stacks; inventories that are always in range; a spatial grid that makes packing fun (Dredge); few item types early | NMS Waypoint notes; VaporLens; Dredge |
| **"Too many interruptions"** | Factorio's old tutorial (about 22 dialogs) | One channel per job; the player can finish while skipping all bubbles | FFF #261, #342 |
| **"Empty world"** | NMS 2016; Slime Rancher's "empty nooks"; BZ land | Hand-placed sites with a purpose; every nook has a note, a fragment or a view | Spinoff; Make Games SA; UW blog |
| **"No map markers"** | Dredge at launch; Lightyear Frontier | Player-placed markers (Dredge 1.1); beacons from signals (Subnautica) | Black Salt Games |
| **"Story felt tacked on"** | Below Zero rewrite; Slime Rancher text | Fragments placed where the player already goes; story that changes what you can do or where you go | UW blog; Make Games SA |

The fixes that worked share one pattern: **cut friction and let the player hold the goal**. They did not add more
tutorial text.

---

## Part C. Distillation for Lunarion

### C1. The genre's proven opening skeleton

Across Subnautica, Planet Crafter, Valheim, Satisfactory, Raft, Dredge and Astroneer, the first hour follows the same
eight steps:

| Step | What happens | Typical time | Best example |
|---|---|---|---|
| **S1 · Strand** | You arrive damaged, alone, with a broken vessel or home in view | 0–1 min | Subnautica's pod; Dredge's storm; Planet Crafter's pod |
| **S2 · Far sight** | A large landmark or goal is visible before anything is asked of you | 0–2 min | the Aurora; Valheim's stones; Dredge's lighthouse |
| **S3 · First need** | One survival or comfort meter starts to matter | 1–5 min | oxygen, thirst, cold, Rested |
| **S4 · First craft** | A two-ingredient craft that answers the need, using resources within about 30 m | 3–10 min | water, an oxygen tether, an axe, the HUB |
| **S5 · First home** | A small structure becomes the safe point and the spawn | 10–25 min | Subnautica's first base; the HUB; Valheim's hut |
| **S6 · First signal** | A device or person gives a direction and a reason beyond survival | 15–40 min | the radio pods; Raft's Receiver; Dredge's collector; Artemis |
| **S7 · First key** | A tool or vehicle lifts a gate the player has already seen | 40–120 min | Seaglide/Seamoth; the antler pickaxe; an engine upgrade |
| **S8 · Release** | Several lures at once, a long goal in view, and home pulling you back | 60–120 min | Subnautica's open ocean; Valheim's Black Forest |

**Where opening v3 matches the skeleton:**
- **S1 and S2:** 07 wakes in the eclipse, and the call's point of light is in view. Strong. Matching the far goal
  this early is better than most games in the genre.
- **S3:** reserve power is the need. It matches.
- **S4 and S5:** the charging pad is the first craft and the first home at once. That is efficient.
- **S6:** the call is present from minute one, and the mast answers it. This matches Raft's Receiver almost
  exactly.
- **S8:** B13 has reach, three lures and a reason to come home. It matches.

**Where opening v3 misses the skeleton:**
1. **S6 comes before any proof that the signal is a someone.** In Subnautica, Raft and NMS the signal arrives as
   words, a voice or a location. Ours is a tone from a dot in the sky. The genre never asks a player to care about a
   pure abstraction at minute one. (Outer Wilds does, but it is not survival-craft; see C7 risk 1.)
2. **S7 is late and thin on 07.** The first *key that lifts a seen gate* is the range ring at B13 (around minute
   45). Before that, every reward is base-side, except the igniter on the cutter. The genre's first key is usually a
   mobility gain on the player's body or vehicle: a fin, an engine, a pickaxe. The owner's spine is "07's growth"
   (Progression first).
3. **Four builds in a row before release.** Subnautica, Valheim and Raft give freedom between builds. v3 is a
   chain of four scan-salvage-build links before the player chooses anything. The genre's S5 → S8 has free play in
   between.
4. **No second channel of discovery.** Every unlock in the first 45 minutes is a scripted fragment. The genre always
   adds a "found by curiosity" channel early: Satisfactory's MAM, Subnautica's optional wrecks, Valheim's new
   materials.

### C2. Progression architecture: gates, keys and lures

**The three parts.**
- **Gates** are physical limits the player can see and test:
  - depth (Subnautica);
  - range per day (Dredge);
  - biome difficulty (Valheim);
  - oxygen line (Astroneer);
  - power (Planet Crafter).
- **Keys** are tools or upgrades that lift one gate each:
  - O2 tanks, depth modules (Subnautica);
  - engines (Dredge);
  - the antler pickaxe (Valheim).
- **Lures** are things seen past the gate:
  - wrecks in the deep (Subnautica);
  - a far island's light (Dredge);
  - the trophy hooks (Valheim).

**Rules that the best games follow:**
1. **Each gate has a readout before the key exists.** Subnautica shows depth, O2 and a crush-depth warning. Dredge
   shows the time of day against the distance. The player learns the gate's *size* first. Then the key's value is
   obvious.
2. **One gate type, scaled.** Subnautica is almost all depth. Dredge is almost all distance plus daylight. A single
   main gate keeps the mental model simple, and secondary gates (heat, radiation) come later.
3. **Soft gates in cozy games.** Dredge and Planet Crafter let you *try* to pass, at a cost, rather than hit a wall.
   You can go; you just can't stay, or get back easily.
4. **Keys come from where the gate is.** Subnautica's deeper-vehicle fragments lie at the edge of what you can
   reach. You find the key by pressing against the gate.
5. **Lures sit just past the gate, and their light or shape reads from inside it.** The genre does this every time.

**For Lunarion.** Our natural main gate is **range: energy out and back**, with **darkness** as the secondary gate.
Night already exists, and craters and shadowed ground are lunar facts. Proposed layout of the first two hours:

| Time | Gate shown | Key | Lure past it |
|---|---|---|---|
| 0–10 min | reserve power: speed and lamp | the charging pad | the call in the sky |
| 10–45 min | the pad needs light; the night | solar, then the battery bank | the call growing clearer; the tower ruin |
| 45–60 min | the range ring on 07's face (first shown) | none yet; the answer gives direction | relay.1 inside the ring; the Peak outside it; Kestrel-3 |
| 60–90 min | **darkness** in shadowed ground (a crater floor the headlamp can't light) | the Kessler–Arai headlamp, then the cell pack | Kestrel-3's interior; relay.2 |
| 90–120 min | the range ring vs. the Peak | elder 06's cell pack, fitted at the pad | the Peak's Great Dish (the 0.6 end) |

**How to make gates visible before keys:**
- The **range ring** is our depth gauge. Show it from B5, faint, right after the first charge. Then the player
  watches it grow with each build, not only from B13.
- **Darkness gates** need a readable edge. The headlamp cone visibly fails to reach the crater floor, and 07's face
  shows a dim lamp glyph at that spot only.
- Put **one unreachable lure in view from the pad** at all times: the Peak, with a faint light or glint. That is our
  Aurora.

### C3. Discovery: scan-to-learn and blueprint reveal

**How Subnautica does it** *(memory; exact counts unverified)*:
- **Early tools need two to three scans**, usually found close together in the starter biomes. This applies to the
  Seaglide, the mobile vehicle bay and the laser cutter.
- **Big tech needs more, spread across biomes.** The Cyclops needs three scans for each of its three parts
  ([Twinfinite](https://twinfinite.net/guides/subnautica-all-cyclops-fragment-locations/)).
- **Databoxes give a whole blueprint in one find**, often behind a gate inside a wreck.
- **A known fragment gives salvage**, so a scan is never a dud. This matches our owner ruling: known blueprint
  gives raw metal.
- **The scan feels like a small ritual.** A beam, a progress ring, a PDA line, and the blueprint's name. The moment
  is short (about 2–3 s) and has a satisfying sound.
- **Fragments are part of the scenery.** They look like broken pieces of the thing they teach. You learn the Seamoth
  by seeing broken Seamoths.
- **The scanner room** (mid-game) can reveal fragment locations. It removes the chore of searching for players who
  want to finish.

**Other channels in the genre:**
- Satisfactory's MAM: research what you find, with a choice of two alternates per Hard Drive
  ([MAM wiki](https://satisfactory.wiki.gg/wiki/MAM)).
- Grounded's analyzer: bring an item to a station.
- Valheim: pick up a new material and its recipes appear.
- Planet Crafter: chips in containers, plus unlocks at thresholds.

**Avoiding "scan everything" chores:**
1. **Separate the two kinds of scan.** A *blueprint fragment* scan is the progression scan. A *databank* scan
   (life, phenomena, makers) is the curiosity scan. Make the curiosity scan cheap, almost passive: a short dwell
   while looking. It should never be needed to progress.
2. **Low counts early, clustered.** Use two to three fragments within a single site for the first three
   blueprints. That is what v3 does: two, three, two, three. Keep it.
3. **Spread only after the release.** The first blueprint that needs two *sites* should come after B13, as the first
   free-choice hunt (C8).
4. **Always show progress on the thing.** 07's CRT draws the wireframe more fully with each fragment. "2/3" lives in
   the drawing, not in a list.
5. **Never waste a scan.** A known blueprint gives salvage (owner rule). A curiosity scan gives a databank entry and,
   sometimes, maker understanding (`maker-understanding.md`).
6. **Use a gate inside the site.** Put the best find behind a cutter, a jammed hatch or a dark interior, like
   Subnautica's sealed-door databoxes. The site teaches "come back with X".
7. **Add a finder later, not now.** A scan-locator device belongs to hour three or later, as a reward for players
   who want to finish.

### C4. Base building motivation: making the first base matter

**What makes a first base matter in the genre:**
- **It answers a need the player feels.** Shelter for Rested (Valheim). Air (Astroneer). The goal terminal
  (Satisfactory). Storage (Subnautica). If the base is optional, as in Subnautica, it needs a strong *want*
  instead: a view, storage, a farm.
- **It changes visibly as you progress.** Planet Crafter's base looks out on a sky that changes. Slime Rancher's
  corrals fill. Valheim's hut grows into a hall.
- **It pulls you home.** Rested and spawn (Valheim). Plorts piling up (Slime Rancher). The next milestone cost
  (Satisfactory). Charging (Astroneer). Players come back for a mechanical reason, and they enjoy it for an
  emotional one.
- **It doubles as the goal board.** Valheim's trophy hooks. Satisfactory's HUB terminal.

**Where v3 is strong.** The pad *is* the need (power), and each build lifts a felt limit. The base will be the
answer to every early problem.

**What v3 is missing:**
1. **A reason to *return* after B13** beyond charging. The decode ticker is good, and needs a partner that is
   mechanical. Suggestion: the battery bank fills while 07 is away, and a full bank lets 07 leave with **extra cells
   for a longer trip**. Home then extends range, which is our Rested.
2. **A goal board at home.** Valheim's empty trophy hooks, in our fiction: an **empty socket rack on the service
   gantry or pad**, with outlines of the parts 07 has seen but not yet earned. A wordless list that lives in the
   world. This fits the existing "Equipment 07" screen, with fixed sockets docked at the bay.
3. **Visible life at home.** The mast's lamp at night, the panels turning to the sun, the bank's level lamp. Owner
   rule: every state gets a visible mechanical action.

### C5. Quest-givers without quest lists

**How the genre does it:**
- **Subnautica:** a radio you repair, then calls from people with a place and a problem. They escalate in distance
  and in stakes. A beacon with a distance appears only for what you've heard.
- **NMS:** Artemis, a distant voice with a plea *(memory)*. Signal boosters and charts are lures you spend.
- **Valheim:** Hugin speaks on first contact only. The stones are a goal board. Vegvisirs mark one altar each.
- **Raft:** a Receiver you build gives dots on a radar and nothing else. The islands tell their story through notes.
- **Dredge:** NPCs with small requests, and a collector with a creepy, long one.
- **Slime Rancher:** notes in the world, mail over days, and ranchers' requests.
- **Satisfactory:** ADA's dry corporate voice, with hints as re-readable emails.

**A tone note.** Subnautica's Alterra PDA and Satisfactory's ADA are both **corporate voices with dry humour**. The
humour makes the loneliness bearable without a companion. Lunarion has no voice, so 07's face and the
1980s-industrial labels (`CHARGER KIT · ILC-R`, Kenji's spares rack) do the same job. Keep them.

**Rules for a low-text, cozy tone:**
1. **One voice, and it is the world.** The call, lamps, labels and 07's own gestures carry reasons. Text lines
   only restate a hint (FFF #342: one channel, one job).
2. **A signal must have a location, a source and a change.** Subnautica and Raft never send a signal without a place.
   Our call needs a **bearing** on 07's face and a **source that changes**: broken, then steady, then answered.
3. **Give later signals a person.** The genre's best pull is a someone: Keen, Artemis, the collector. After B12,
   relay logs and maker notes can be people (Kenji, the crew) in short, plain lines. They are found, never pushed.
4. **Put the goal board in the world.** Trophy hooks or a socket rack, not a menu.
5. **Hint on first contact only** (Hugin). Each tip is shown once, at the object, then is kept in the databank.
6. **Let the player pin their goal** (NMS Waypoint). If a reminder is needed, it is something the player chose,
   such as tagging a lure on the range ring. It is never assigned.

### C6. Retention: the 1-hour, 1-session and day-2 hooks

| Hook | What the genre does | Lunarion now (v3) | Recommendation |
|---|---|---|---|
| **1 hour** | First mobility key and first release: the Seaglide or Seamoth; Valheim's first boss; Dredge's first engine | the answer (B12) and release (B13) at about 45 min | Good. Add one visible upgrade **on 07** before 45 min (see C8). |
| **1 session** (2–3 h) | One big visible change and one open question: Planet Crafter's first rain; Subnautica's first deep base; Valheim's Bronze Age | the 0.6 end at the Peak dish (the carrier locks) | Good beat. Make sure 60–150 min has 3–5 sites, not one corridor. |
| **Session end** | "One more": a milestone cost shown (Satisfactory); a short day ending at the dock (Dredge); timers ticking at home (Slime Rancher) | the decode ticker resolves on docking | Add a mechanical pull (the bank fills for a long trip) and a scheduled awe event. |
| **Day 2** | A gate the player now has a plan for, plus something that grew while away (Stardew, Animal Crossing), plus an unanswered question | the Peak is outside the ring; Kestrel-3 is unexplained | Make the day-2 plan concrete: the socket rack shows the cell pack's outline. The player knows it's at relay.2. |

**Scheduled awe.** Subnautica's Sunbeam countdown is a timed appointment *(memory)*. In a cozy key, our awe hooks
(first nightfall, an Earth eclipse, a meteor night, the network pulse; see the "Awe hooks" memory) can be
**appointments** the player sees coming on 07's face. Example: `EARTHRISE · 2 h` near the end of a session. This is
never a fail timer.

### C7. Risks in the v3 proposal, with mitigations from the genre

**Risk 1: the abstract signal hook.** A point of light and "three short, one long" may read as ambience, not as
"calling me". Every survival-craft game in this survey makes its signal concrete: a place, a voice, or a person.
- **Mitigations:**
  - **Give it a bearing from B0.** 07's head tracks the light, and the tone grows louder when 07 faces it. Turning
    toward it should feel like tuning a radio. This is the Outer Wilds signalscope, done with 07's body.
  - **Make 07 *want* it.** 07's antenna lamp stutters the reply (in v3) *and* 07 makes a small reach-up gesture.
    Then the player reads desire, not data.
  - **Prove it is someone early.** After the first charge (B5), the steadier call carries **one decoded
    character**, not the full ID. A letter means a sender. The rest arrives over time (the decode ticker, moved
    earlier).
  - **Playtest gate.** v3's signal is right: by 5 minutes, a player says "it's trying to answer something". If they
    don't, switch to the alternative in C8.

**Risk 2: the reserve-power start is boring.** A capped drive of 165 m in the dark is the riskiest stretch, and it
is where Planet Crafter and Lightyear Frontier get "slow start" reviews.
- **Mitigations:**
  - **Cap top speed only, never acceleration or turning.** Keep the handling lively (core feel gate).
  - **Keep it short.** First charge by about 6–8 minutes. Subnautica's first craft is under 5 minutes.
  - **Put one non-loot discovery on the route:** a set of old rover tracks, or a dead elder's silhouette (a databank
    curiosity scan, cheap). It gives the drive a second thing to notice.
  - **Make the eclipse do work.** The diamond ring must coincide with the first charge (as v3 has it). That turns
    the slow stretch into a set-up for the awe payoff.

**Risk 3: building as chores.** Four links in a row of scan, salvage, build looks like Factorio's "gather N because
the list says so", even with good reasons. By link three the verb is predictable.
- **Mitigations:**
  - **Vary the verb per link.** For example:
    - pad: scan and open;
    - solar: fix the cutter, then cut;
    - bank: tow the habitat's power module to the pad (a towing verb, if the rover design supports it);
    - mast: raise it segment by segment.

    Each build should teach one new thing, as Portal does.
  - **Shorten the middle.** Consider merging solar and bank into one link, or move the bank to just after the
    release as the first *chosen* build (C8, alternative pacing).
  - **Show the link between cause and effect.** When the lander lamp dies and the pad goes dark, a **visible cable**
    from the lander to the pad must explain it. Otherwise "my charger died" reads as arbitrary.
  - **Building must be quick and spectacular**: unfold, clunk, lock. Subnautica's habitat builder takes seconds.

**Risk 4: an empty map after the tutorial.** This is the owner's core complaint about the current build, and it
matches the genre's top reasons for leaving: NMS 2016, Slime Rancher's empty nooks, the BZ surface.
- **Mitigations:**
  - **Density target inside the first range ring:** five to seven *purposeful* sites (my recommendation, not from a
    source). Each has one of: a fragment, a databank entry, a maker note, a view, or a gate. No loot sprinkles (owner
    rule).
  - **At least two free choices at release**, as v3 has. Add a **third, near and small** one: a five-minute errand
    such as a fallen sign or an elder's dead lamp, so the player gets a quick win before the big trips.
  - **A goal board at home** (C4): the socket rack holds the open goals without a list.
  - **A second discovery channel** (C3): curiosity scans feed maker understanding, which feeds 07's blueprints. Every
    nook can matter.

**Risk 5 (extra): growth is mostly in the base, not on 07.** The owner's spine is 07's growth (Progression first,
ruling 11).
- **Mitigation:** make at least two early rewards visible on 07's body:
  - the igniter on the cutter (B7) as a visible part, not a menu item;
  - one more by about 30 minutes, for example 07's antenna extending a segment when the receiver locks at B5.

### C8. Recommended direction for Lunarion's first 60–90 minutes

#### Recommendation: "The call, made concrete" (keep v3's spine and fix the four misses)

Keep v3's hook A and its limit chain. Change four things:
1. The call gets a bearing and a sender early.
2. 07 grows visibly by 30 minutes.
3. The chain gets one free choice in the middle.
4. Hour two gets a planned set of sites.

Constraints honoured throughout:
- calm, and no death in the opening;
- one guidance element at a time;
- plain short text;
- no floating loot;
- scan fragments;
- base building;
- visible upgrades on 07.

| # | Time | Beat | Gate / key / lure | Guidance element (one) |
|---|---|---|---|---|
| R1 | 0–2 | **Wake.** POST one shot. The lens opens on dark Earth and the point of light. 07's head tracks it, and the tone swells when it faces the light. The antenna stutters the reply. | lure: the call | the sound, plus 07's head |
| R2 | 2–6 | **Drive to the lander lamp** on reserve (top-speed cap only). Pass the old tracks; an optional curiosity scan of the tracks adds a databank entry. | gate: reserve | the warm lamp |
| R3 | 6–10 | **Pad:** scan 2 fragments; the wireframe completes on the CRT. Open the labelled crate. Build. Dock. **The diamond ring** coincides with the cells filling. Reserve lifts. **07's antenna extends one segment** and the call locks to a bearing. **One character decodes.** The **range ring appears faint** for 3 s. | key: the pad; growth on 07 | the first-time prompt chip |
| R4 | 10–20 | **The lamp dies.** A visible cable runs from the lander to the dark pad. The fallen array glints. Scan 3. The cutter sputters. **Fit the igniter from the spares rack: a visible part on 07.** Cut panels and build solar. `PowerOn`, and the music returns. | gate: power source; key: solar; growth on 07 | the glint |
| R5 | 20–30 | **First free choice (new).** With power and daylight, a short loop opens. Two **small, close sites** are visible from the pad, each about 3–5 minutes: (a) an elder robot's dead lamp, a curiosity scan with a maker note; (b) the habitat ruin, where the power module's status lamp blinks. Either order. Neither is required before the evening. | lures: 2 near | none (world only) |
| R6 | 30–40 | **Dusk.** The charge slows on 07's face. The module lamp is the hint. Scan 2 and **tow or carry the module to the pad** (a new verb), then build the bank. **First nightfall, and the lights stay on.** | gate: night; key: the bank | the blinking module lamp |
| R7 | 40–50 | **The failed reply.** 07 tries to answer; `TX · NO RANGE`. The tower ruin is visible against the stars. Scan 3, cut, raise the mast segment by segment. No chips. | gate: voice; key: the mast | none (skill test) |
| R8 | 50–55 | **The answer (B12).** Transmit; the pulse runs out; relay.1 blinks back. `ACK · RELAY.1 · ROUTE · PEAK DISH · OFFLINE`. **Two more characters decode.** | payoff | the CRT line |
| R9 | 55–60 | **Release (B13), staggered.** The full range ring: relay.1 inside, the Peak outside. From the rise: relay.1, the Peak's dish, Kestrel-3. The socket rack at the pad shows outlines: headlamp, cell pack. | gates shown; lures × 3; goal board | the range ring (once) |
| R10 | 60–75 | **Free hour, part 1.** Most players go to relay.1 or Kestrel-3. **Kestrel-3** is the first **two-site fragment hunt**: the headlamp, with 2 fragments in the wreck and 1 at a nearby debris field. Its best find lies in a dark interior that teaches the darkness gate. **relay.1** is a repair that extends the network: the ring grows a little, and relay.2 is revealed on the CRT. | key: the headlamp (on 07); gate: darkness | world only |
| R11 | 75–90 | **Free hour, part 2.** Fit the headlamp at the pad: 07 visibly changes, and night driving opens. Darkness-gated sites (a shadowed crater floor) become readable. relay.2 holds elder 06 and the cell pack, which is the key to the Peak. A good **session-end point**: dock, the ticker decodes a little more, the bank fills, and `EARTHRISE · 2 h` shows on the face. | session hook | 07's face |

**Why this is the right call:**
- It keeps everything v3 earned: the far goal at minute one, felt needs, and the answer as the payoff.
- It fixes the genre misses:
  - **S6 concrete:** a bearing and a decoded character.
  - **S7 on 07:** antenna, igniter, headlamp.
  - **Free play mid-chain:** R5.
  - **A dense, gated hour two:** R10–R11.
- Each build still has one new verb: scan, cut, tow, raise.
- Guidance steps down from a chip (R3) to a glint (R4), to the world (R5), to nothing (R7). This matches Portal's
  "final test removes hints" (player-guidance A4).

**What it needs:**
- the bearing and tone-swell on 07's head (audio + rover);
- the antenna segment (art);
- a tow verb, if the rover design allows one (Director call; otherwise carry the module's core);
- the R5 sites and the R10–R11 sites (world);
- the socket rack (UI + art);
- the earlier decode ticker (story call).

#### Alternative: "The broken radio" (Subnautica and Raft pattern)

There is no sky call at minute one:
- 07 wakes in the eclipse beside the lander. The lander's **receiver** hangs in its cradle, broken, with a cracked
  status lamp.
- The first 20 minutes are survival-craft classic: reserve, the pad, the dawn, solar.
- Then 07 plugs its own antenna into the repaired receiver. The call **arrives as a burst**: a short, partial
  maker log in plain text ("…LUMEN, RESPOND…"), plus a bearing.
- From there it follows the same chain (bank, mast, answer, release).

**For:**
- It is concrete. The signal comes from a broken object 07 repairs (Subnautica's radio), and it has a sender from
  its first second.
- It removes risk 1 entirely.
- The first 20 minutes have one goal, power, which is easier to read.

**Against:**
- It loses the minute-one far goal (P2), our strongest hook against the genre.
- It risks a "survival cliché" first 20 minutes, which is where Planet Crafter's "slow start" lives.
- It gives up the bookend with the Arks' light.

**Choose this only if** the R1 playtest signal fails: by minute 5, players can't say something is calling 07.

---

## Sources

**Developer posts, interviews and patch notes:**
- [Unknown Worlds: Below Zero story changes and save games](https://unknownworlds.com/news/subnautica-below-zero-story-changes-and-save-games)
- [Unknown Worlds: How we approach story (Jill Murray, 2020)](https://unknownworlds.com/en/news/how-we-approach-story)
- [Inverse: Below Zero interview (David Kalina)](https://www.inverse.com/gaming/subnautica-below-zero-interview-twofer)
- [GDC 2019: The Design of Subnautica](https://gdconf.com/news/dive-deep-design-subnautica-gdc-2019)
- [Factorio FFF #261: new player interaction](https://factorio.com/blog/post/fff-261)
- [Factorio FFF #342: the new old tutorial](https://factorio.com/blog/post/fff-342)
- [Factorio FFF #257: NPE/campaign update](https://factorio.com/blog/post/fff-257)
- [NMS Waypoint 4.0 patch notes (DSOGaming)](https://www.dsogaming.com/patches/no-mans-sky-waypoint-update-4-0-released-full-patch-notes-revealed/)
- [NMS Atlas Rises (Gematsu)](https://gematsu.com/2017/08/no-mans-sky-atlas-rises-update-now-available)
- [Raft Q&A, Redbeet (Game Developer)](https://www.gamedeveloper.com/design/q-a-how-steam-hit-i-raft-i-stands-out-in-the-crowded-survival-genre)
- [Dredge Pale Reach interview (80.lv)](https://origin.80.lv/articles/black-salt-games-on-the-development-of-dredge-the-pale-reach-dlc)
- [Dredge update 1.1.0, map markers (Black Salt Games)](https://blacksaltgames.com/dredge-update-v-1-1-0-map-markers-more)
- [Astroneer interview, Joe Tirado (ButWhyTho)](https://butwhytho.net/2019/09/03/interview-astroneer-exploration-and-community-with-joe-tirado/)
- [Valheim devs on BotW inspiration (PCGamesN)](https://www.pcgamesn.com/valheim/ps1-graphics)
- [The Planet Crafter: creating a living world (Galaxus)](https://www.galaxus.fr/fr/page/creer-un-monde-vivant-dans-the-planet-crafter-32696)

**Reviews, analyses and aggregated sentiment:**
- [OpenCritic: Subnautica Below Zero reviews](https://opencritic.com/game/11360/view/reviews)
- [TechRaptor: Below Zero scraps story, hires new writer](https://techraptor.net/gaming/news/subnautica-below-zero-scrapping-original-story-hires-new-writer)
- [GameStar: Below Zero preview](https://www.gamestar.de/artikel/subnautica-below-zero-preview,3358632.html)
- [KeenGamer: will BZ's new story be better](https://www.keengamer.com/articles/features/opinion-pieces/subnautica-below-zero-will-the-new-story-be-better-than-the-old-one/)
- [Steam forum: BZ protagonist discussion](https://steamcommunity.com/app/848450/discussions/0/1751274167972463506)
- [PCGamesN: NMS NEXT review](https://www.pcgamesn.com/no-mans-sky/no-mans-sky-next-pc-review)
- [Press Start: NMS NEXT coverage](https://press-start.com.au/?p=62834)
- [The Spinoff: NMS death and resurrection](https://thespinoff.co.nz/featured/20-12-2016/back-from-the-abyss-the-story-of-no-mans-skys-death-and-resurrection/)
- [Make Games SA: Game Design Club, Slime Rancher](https://makegamessa.com/discussion/5705/game-design-club-1-slime-rancher)
- [TechRaptor: Dredge review](https://Techraptor.net/gaming/reviews/dredge-review)
- [Way Too Many Games: Dredge review](https://waytoomany.games/2023/04/14/review-dredge/)
- [God is a Geek: Dredge Pale Reach review](https://godisageek.com/reviews/dredge-the-pale-reach-review/)
- [Shacknews: Raft review](https://www.shacknews.com/article/131150/raft-review-steam)
- [GamesRadar: Lightyear Frontier review](https://gamesradar.com/lightyear-frontier-review)
- [God is a Geek: Lightyear Frontier EA review](https://godisageek.com/2024/03/lightyear-frontier-early-access-review-stress-free-building/)
- [PureXbox: Lightyear Frontier review round-up](https://purexbox.com/news/2024/03/heres-what-the-reviews-are-saying-about-lightyear-frontier-on-xbox-game-pass)
- [Pieuvre: The Planet Crafter review](https://www.pieuvre.ca/?p=60372)
- [Gaming Trend: Abiotic Factor review](https://gamingtrend.com/reviews/abiotic-factor-review-push-beyond-the-unknown/)
- [GameBrief: Abiotic Factor beginner guide](https://www.gamebrief.net/blog/abiotic-factor-beginner-guide-tips-2026)
- [ThumbCulture: Valheim preview](https://www.thumbculture.co.uk/valheim-early-access-build-your-saga)
- [VaporLens: Valheim Steam review summary](https://vaporlens.app/app/892970/valheim)

**Wikis and guides (facts about mechanics):**
- [Wikipedia: The Planet Crafter](https://en.wikipedia.org/wiki/The_Planet_Crafter)
- [Satisfactory wiki: Tutorial](https://satisfactory.wiki.gg/wiki/Tutorial), [Milestone](https://satisfactory.wiki.gg/wiki/Milestone), [MAM](https://satisfactory.wiki.gg/wiki/MAM)
- [Astroneer wiki: Tether](https://astroneer.wiki.gg/wiki/Tether)
- [Dredge wiki: Regions](https://dredge.wiki.gg/wiki/Regions)
- [Inven Global: Valheim beginner's guide](https://www.invenglobal.com/articles/13389/valheim-guide-beginners-tips-tricks-basics)
- [GGRecon: summoning Eikthyr](https://www.ggrecon.com/guides/how-to-summon-defeat-the-first-boss-eikthyr-in-valheim)
- [Twinfinite: Cyclops fragment locations](https://twinfinite.net/guides/subnautica-all-cyclops-fragment-locations/)
- [Slime Rancher wiki: The Ranch](https://slimerancher.fandom.com/wiki/The_Ranch)

**Not verified in a source this session** (from memory, recheck before quoting):
- Subnautica: fragment counts for the early tools; titanium from known fragments; the Aurora timer; radio-call order
  and Sunbeam details; databox placement.
- NMS: the Artemis framing as the story hook.
- Valheim: Rested and comfort mechanics; the material pickup unlock rule.
- Astroneer: the bytes and catalog flow; the 1.0 cores goal.
- Raft: the Receiver and antennas.
- Grounded: the analyzer and BURG.L.
- Slime Rancher: Gordos, expansions and mail.
- Dredge: the collector and the upgrade categories.
- Dyson Sphere Program: everything stated.
- The Planet Crafter: specific early materials.

Fandom wiki pages for Subnautica, NMS and Valheim refused automated access during research. Those facts rely on
memory or secondary guides.
