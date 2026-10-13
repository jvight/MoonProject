# North star: what draws a player in, keeps them, finishes them, and what they feel

> Owner: the game design session. This is a living doc: it is updated whenever the owner's play-through or a playtest
> teaches us something.
>
> The owner (2026-10-13, memory `player-experience-first`): the most important skill in game design is *asking*
> what draws players in, what keeps them playing to the end, what feeling the game gives, and why Subnautica worked so
> well, then setting the development goals from those answers.
>
> Every design doc starts from this one (skill `player-experience`). Sources: VISION, story v3 (box/story 1ff92db8),
> `0.6-world-regions.md`, `0.6-living-moon.md`, the 0.5.5 spec, and the owner's reviews of 0.4–0.5.4.

## 1. Why Subnautica works (honest, specific)

### Hook: the first minutes
- **You crash into an ocean whose bottom you can't see.** The first dive shows colour, light shafts, small fish, and
  then *blue fading to black below you*. The unknown is literally underneath, in every direction.
- **The Aurora burns on the horizon,** huge, wrecked and smoking. It is a landmark and a question before the player
  has done anything.
- **Survival is mild but immediate** (oxygen, then water and food), so the first verbs have a reason.

### Pull: the question in your head
- **Depth is the map of progress.** Deeper is stranger, more beautiful and more dangerous. Every "can I go a bit
  lower?" is a question the player asks themselves.
- **The Aurora** is entered later. It is seen from minute one and becomes a goal once you can survive its radiation.
- **Radio signals** give coordinates (lifepods, the Degasi): small, answerable questions that drift you outward.
- **Leviathan roars** far away tell you something huge is out there before you ever see it.
- **The story questions** stack: why was the Aurora shot down? Who were the Precursors? What is the infection?

### Hold: why you come back
- **Scarcity turns into mastery.** Oxygen and depth limits shrink your world, and each upgrade expands the safe zone
  (tanks, Seaglide, Seamoth depth modules, Cyclops, Prawn suit). You *feel* the frontier move.
- **The base is safety and expression:** light, a window onto the dark, your choices.
- **The databank rewards curiosity:** scan anything, and a page opens. Knowledge is a collection.
- **Vehicles are identity:** you remember your first Seamoth.
- **There is no minimap.** You build the map in your head, so places become *yours*.

### Finish: why you play to the end
- **Escape is promised early** (the rocket) and **the plot escalates** (the infection, the Precursor gun, the Sea
  Emperor). The ending answers both the survival goal and the mystery.

### Feeling
- **Awe:** scale, bioluminescence at night, the lost river, the first leviathan.
- **Dread:** the dark below, the roar you can't place, the void at the map's edge.
- **Safety:** the lifepod, the base lights, the hum of your Cyclops.
- **Pride:** "I built this, I went down there."
- **Loneliness** under all of it.
- **How it's made:**
  - **sound:** distant roars, a quiet-to-loud biome bed, near-silence at depth, the PDA's calm voice;
  - **light:** day and night, biolum, your torch in the dark;
  - **pacing:** player-driven, with story arriving by radio and PDA when *you* go somewhere.

### What fails or annoys, even in Subnautica
- Inventory tetris and storage management: lockers, labels, walking to the right one.
- Finding one specific fragment or resource. Players open a wiki.
- Late-game backtracking across a known map, and an empty world once it's done.
- Food and water chores long after they stop being interesting.
- Vehicles that get stuck or destroyed; Cyclops micromanagement.
- Fear that is too much for some players (thalassophobia). Dread is a filter.
- PDA voice lines repeating; signals that send you somewhere with nothing new.

## 2. Lunarion's answers

### For the whole game
| Question | Lunarion's answer |
|---|---|
| **Hook** | **A tiny rover wakes on a dark moon.** Its one eye opens, Earth hangs huge and low, a red light blinks on the Peak, and a faint signal hisses in the static. Around it, a home is dark and asleep. The first five seconds say "aww"; the next minute says "what is all this?" |
| **Pull** | **Lights you can see and haven't reached,** and **a question you can't stop asking:** *who is still transmitting, and why was this place written off?* (story v3). Every region is visible from the last: the swirl and the dish's blink over the rim, Meridian's white strobes, the crater's green glow, the red beacon. Each region holds a piece of the **survey: the proof that the Moon is worth coming back to.** |
| **Hold** | **Everything you fix visibly comes back to life:** 07's body (sockets, cutter, cells, wings), home (lights, shelves, machines), the chart inking, and **the machines of the moon waking downstream of your repairs** (living moon §1a). Every evening at the dock shows the next light. The databank collects what you've understood, in two people's voices. |
| **Finish** | **Carry the proof home.** The protocol shows its four steps from minute one, and the last is "transmit the archive". The ending keeps that promise: the survey reaches Earth, Ro hears it first, and Houston plans a return. *The moon was written off, and you gave people the reason to come back.* |
| **Feeling** | **Lonely wonder turning into quiet pride.** Lonely wonder comes from small 07 under a huge Earth, long shadows, the radio thinning to silence, warm points far away in a cold field. Quiet pride comes from a lit mast you built, machines waking, the music returning, home glowing from far away. |

### Per release
| Release | Hook | Pull | Hold | Finish (of the release) | Feeling |
|---|---|---|---|---|---|
| **0.5** basin + canyon | the waking rover, the dark home | the red beacon; Kestrel-3's foreign livery; 06's log ("who is transmitting?") | the tower and home waking step by step; 07's kit; the cut | Power restored; the chasm crossed; Bell home | tender, curious |
| **0.6** the Long Mare | the swirl and the dish blink seen over the west rim | the mare dish archive (what was said for 20 years?); the flock (whose drones?); the lights at the shore and in the skylight | range growing (Cell Pack III, pylons); the first machine ecology waking; Alloy and Cutter Ribs | the dish lit: 20 years of the crew's channel, then silence (2067); Link begins | vast, lonely, then less alone |
| **0.7** Rim Terraces | Meridian's white strobes, still blinking | a station still working with no one reading: what's in the vault? | treads; waking a whole foreign family by contact; Atlas | Meridian's vault: the reservoir mapped | awe at a stranger's machines; respect |
| **0.8** Crater + tube | the green glow; the skylight's shafts | is the ice really there? What did the shelter keep? | floods, the heater, Moss; the moss alive | the ice core; the shelter backup | hushed wonder |
| **1.0** the Peak | the beacon, always | can 07 climb what stopped 01? | everything 07 has become | the uplink; Earth answers | quiet pride, hope |

### Where today's 0.5.x is weak (blunt)
- **The basin is small and known in an hour.** Every landmark is inside ~300 m, and nothing is unknown after the
  canyon. The owner called it boring. *Fix: regions (0.6) and the mare lure visible from 0.5.5's west rim.*
- **07 is alone with stations, not with life.** Nothing moves except Tilly and Bell, so the drive between sites is
  dead. *Fix: the living moon (0.6), and 0.5.5's derelicts as a bridge.*
- **The pull is a readout, not a question.** The protocol tells you the goal; it doesn't make you wonder. The red
  beacon promises something 0.5 can't deliver. *Fix: story v3's questions (Kestrel's foreign livery, who is
  transmitting) seeded in 0.5.5.*
- **The notes were sentimental, and the text was noisy.** The owner rejected both. *Fix: story v3 (operational
  machines, human-to-human letters); the guidance budget (0.5.5 §4).*
- **Purpose didn't read** (the lift, the "07 can" page, the tower task). *Fixed or fixing in 0.5.5.*
- **The look was flat** (the visual review). *Fixing in M3-18.*
- **Verbs felt weightless** (the cut). *Fixing: the weighted charge-up (0.5.5 §5).*
- **There's no awe beat after the first frame,** except the chasm leap. *Fix: each region's first sight is staged
  (the swirl at the rim, the strobes, the skylight's shafts).*

## 3. What Lunarion does better or differently (and must not lose)
| Subnautica | Lunarion, differently or better |
|---|---|
| dread from creatures and the dark | **calm tension** from range, darkness and the unknown only. No threat, no death, no loss: the crawl home, chargers, Tilly (ruling 15). |
| fauna | **machine life that is logical:** every machine has power, a goal and a reason. Repairing changes the ecology visibly and permanently. |
| day and night as mood | **light as a resource:** the polar Sun charges, earthshine only lights, and dark places need floods and heat. |
| a faceless diver | **07 as a character:** one lens, body language, wordless pentatonic chirps; it grows visibly from tired to cared-for |
| escape | **hope:** you carry the reason for humans to return |
| inventory tetris, lockers, wikis | **no juggling:** auto-unload, open shelves, fragments at the door of their region, one guidance element |
| PDA voice lines | **a databank in two people's voices** (Kenji and Ro), one or two plain sentences, with *your* scan drawing |
| a base as free building | **home as one warm place** that grows by machines on pads |

**Must not lose:**
- the first frame;
- solitude and calm (pillar 6);
- warmth in the cold;
- every touch sings, in D pentatonic;
- visible progression;
- logic (power, goal, reason);
- one gentle guidance element;
- no punishment.

## 4. Development goals (playtest signals)
Each is observable in a play-through, and the owner's play or a capture checks it.

| Release | Signal |
|---|---|
| **0.5.5** | - Within 2 min the player looks up at Earth or drives without a prompt. - Within 10 min they reach the tower unprompted. - The first cut feels satisfying (the owner says so). - They stop at 06 for > 3 s. - No frame shows two guidance elements. - By 30 min they can say what the lab is for. - They name "the red light" or "Kestrel's logo" as something they want to understand. |
| **0.6** | - Within 10 min of 0.6 content the player **drives toward the west rim unprompted**. - They follow the flock or the trencher at least once. - They notice a sun-phase change and comment on it. - They light the mare dish within ~45 min. - Afterwards they say they want to reach the terraces or the skylight. - **The owner doesn't call it boring after an hour.** |
| **0.7** | - From the mare's shore the player heads for Meridian's strobes unprompted. - They wake a Meridian machine by repairing its comms node, then use a Power Bridge. |
| **0.8** | - On entering the crater with floods, the player stops moving for ≥ 5 s. - They pause at the moss. |
| **1.0** | - The main arc takes 6–8 h. - The player transmits and stays after the credits. |
| **Always** | - After a dock evening the player sets out again within 30 s in at least half of evenings (the "one more trip" hook). - A second session starts voluntarily. |

## 4a. Awe beats (owner, 2026-10-13, memory `awe-hooks`)
Recurring wow moments. Awe is half of the target feeling, "lonely wonder"; the other half is the pride that follows.
Every beat below follows the light model L1 (a low polar Sun circling once per 2 h of play; earthshine never
charges; Earth fixed and low) and the ecology rules (living moon §1a).

**Rules for awe beats:**
- They are earned by being somewhere, never forced.
- Any input continues play. A camera move happens only when 07 is still and Gentle camera is off.
- They never come with text.
- At most one staged beat per ~20 minutes, so they stay special.

### The beats
| # | Beat | Trigger | What you see and hear | Logic | Frequency | Release |
|---|---|---|---|---|---|---|
| 1 | **The first nightfall** | the Sun's bearing passes behind the basin's rim (timed for the first day, below) | Long shadows race across the floor. The panels go dark one by one. Then home's windows, the station lamps and the lit masts come on **in sequence, like a village at dusk**. The stars and the Milky Way bloom as the sky darkens. Earth brightens, and its night half shows **city lights**. Machines walk back to their pads and settle with soft chimes. The radio's music shifts to the night set. | When the local Sun sets, Earth seen from the Moon is near half phase, so its night half (city lights) faces us. Through the night it waxes toward full, and earthshine grows (the lavender night). Lamps switch on at dusk because the station's light sensors do. Solar machines seek pads (§1a.2). | the first one is staged; then every dusk, unstaged (no camera) | **0.6** (needs the L1 Sun rig); see "0.5.5" below |
| 2 | **Machine fireflies** | night, anywhere with dormant machines | Tiny slow-blinking **standby LEDs** dot the landscape: a vista looks inhabited | Dormant machines keep a standby light at minimal draw. **Dead derelicts show none**, so the LEDs also tell the player what can be woken. | every night | **0.5.5** (cheap: dormant stations and 0.5.5's derelicts get standby LEDs), more in 0.6 |
| 3 | **Dawn, the world waking** | the Sun's bearing clears the rim | The **terminator sweeps** across the ground with the **dust glow** riding it. Frost in shadowed pockets sparkles and lifts. Panels flare as light hits them, mites pour out onto the panels, the flock lifts off its perches, and the music brightens. | Living moon §2: dust levitates at the terminator, frost sublimates in first light, and solar machines wake when their source returns. | every dawn, unstaged except the first | 0.6 |
| 4 | **An eclipse by Earth** | a scripted alignment: the Sun passes exactly behind Earth's fixed disk | Earth goes black with **a thin red ring**: every sunrise and sunset on Earth at once (real). The landscape dims to a red-violet. Biolum things glow in VISION's reserved eclipse swatches (#3CFFC2 / #FF5FD2). Total silence, then one held radio chord. | It is real, and it happens when Earth eclipses the Sun as seen from the Moon. With both low on the horizon, it needs the Sun's circle to cross behind Earth: rare by construction. | once, as a story beat (proposed: when the survey's last piece is found, before the Peak, 0.8) plus a rare repeat after the ending | 0.8 |
| 5 | **Flash-shower nights** | a "shower night" (about one in 4 nights in open regions) | **No streaks:** the Moon has no air to burn meteors. Instead, many **tiny white impact flashes** spark across the plain and far ridges. High **ejecta arcs** catch the sunlight above the shadow line and glitter, then fall. The next dawn shows **fresh craters with warm impact glass** (living moon §2). | Real impact flashes; ejecta lit by the Sun above the terminator | about one in 4 nights in open regions | 0.6 |
| 6 | **Vista reveals** | the first crossing into a new region (the rille pass's mouth, the terraces' first step, the crater rim, the skylight's edge) | 07 stops at the opening. A slow camera pull-out (6–8 s) shows the whole region: the Long Mare's swirl and the dish's blink under a huge low Earth. Then the region's first sound bed fades in. | Each region's lure is designed to be seen whole from its seam (regions §3) | once per region | 0.6 (the mare), then one per region |
| 7 | **The network pulse** | lighting a mast at night | A **handshake pulse** runs mast to mast: each lit mast's lamp flashes in turn, out to the horizon. Where cables exist (home, the trencher's line), light runs along them. A rising pentatonic run, one note per mast. | The relay chain re-links: each mast confirms the next (radio handshake), and the cable lamps show power flowing | every mast lit at night | **0.5.5** (cheap: the 4 basin masts' lamps blink in sequence on `RelayRestored`), then grows with every region |
| 8 | **The skylight's shaft** | the lunar day's course | In the lava tube, the shaft of sunlight **drifts across the cave floor** as the Sun circles. Once per day it reaches the crystals and the moss, which glow. | The Sun circles (L1), so the shaft moves | once per lunar day in the tube | 0.8 |

### Timing the first day
The first session is about 60–90 min, and the beloved look is the earthlit night. So the first day is paced on
purpose:
- **New Game starts in late, low sunlight:** the Sun about 35 minutes from setting behind the basin's west rim.
  Golden-white light, very long shadows, a black sky with Earth huge and few stars. The opening (wake, Look up) plays
  in that light.
- **Minute ~30–35:** the player usually rides down from their first lab visit (first-hour-v2 §4). **The first
  nightfall** (beat 1) falls right as they come out of the lander: home's lights, which they just powered, come on
  as a village at dusk. It is the payoff of the first half hour.
- **The first night lasts ~55 min** (the iconic lavender look) and holds the Tilly, garage, lift and chasm beats.
- **The first dawn** comes around minute 90.
- **After that,** a normal 2-hour lunar day: the basin floor is sunlit about 45% of the cycle, with ~5-minute dusk
  and dawn transitions.
- **Loaded games keep their saved Sun.**
- Story's opening storyboard (`cutscenes.md`) adapts: the wake in low sunlight, and the stars bloom at nightfall
  instead of at minute one. **Story to confirm.**

### 0.5.5 versus 0.6
- **In 0.5.5 (cheap, no Sun rig):**
  - **machine fireflies** on the dormant stations and the new derelicts (art: one standby LED each; gameplay: on
    while dormant, off when dead or awake);
  - **the network pulse** on the basin masts (world and gameplay: the sequence on `RelayRestored`; audio: the run).
- **A scripted first nightfall in 0.5.5 is possible but not recommended.** It would need a tuned sunlit preset and
  a timed blend (world and art, M) and would change the opening's look just before release. Better as the first
  item of 0.6's L1 build.
- **0.6:** the Sun rig and phases, then the first nightfall (staged), dawns, flash-shower nights and the mare's vista
  reveal.
- **0.8:** the eclipse beat and the skylight's shaft.

### Playtest signals
- At the first nightfall, the player **stops driving to watch** (07 still for ≥ 5 s during the sequence).
- At a vista reveal, the player waits out the pull-out without pressing anything.
- During a dawn, the player looks around (camera movement while still).
- After a mast lights at night, the player looks toward the horizon to follow the pulse.
- The owner names at least one awe beat unprompted after a session.

## 5. The checklist every future spec must pass
1. **Hook, pull, hold, finish and feeling** are answered *in the doc*, before the mechanics (skill
   `player-experience`).
2. **Learn, then improve:** the borrowed mechanic, why it works there, and Lunarion's better version.
3. **Logic:** every element has a power source, a goal and a reason (living moon §1a.7).
4. **Purpose reads without text.**
5. **Visible progression** on 07 or home.
6. **Calm:** tension only from range, dark and the unknown; never threat, punishment or a timer.
7. **One guidance element;** hints only when needed; never repeat the world.
8. **A playtest signal** that says how we'll know it works.
9. **Phased scope:** what ships in which release.
