# Player guidance and openings: research for the Lunarion opening redesign

Status: research input, 2026-10-19. For the designer redesigning the first 30 minutes of Lunarion.
Scope: how well-loved games guide, motivate and teach in their first 5-30 minutes, mostly without
text, then the principles, a checklist, and three reframes of our own beats.

How to read the confidence level: facts that come from a developer talk, interview or developer
commentary carry a source link. Other descriptions come from my knowledge of the games and from fan
wikis. Where I could not confirm a detail, the text says "unverified" or "as I recall it".

---

## Part A. Case studies

### A1. Subnautica (Unknown Worlds, 2018)

- **Hook (first 60 s):** a short evacuation sequence, then you wake in a burning lifepod. You put out
  a fire with an extinguisher and climb up the hatch. Outside is open ocean and the wrecked starship
  Aurora, burning on the horizon ([Lifepod 5, Subnautica Wiki](https://subnautica.fandom.com/wiki/Lifepod_5)).
- **First goal and how it is shown:** survival needs (water, food, oxygen) appear as meters, and the
  pod's PDA voice explains them in a few calm lines. The first concrete tasks come from the pod
  itself: the secondary systems and the radio are damaged and are fixed with the Repair Tool. A fixed
  radio starts receiving distress calls from other pods. Each call gives a direction and a reason to
  swim somewhere ([Lifepod 5 wiki](https://subnautica.fandom.com/wiki/Lifepod_5)).
- **Why each action happens:** every early action answers a felt need. You fabricate because you are
  thirsty. You scan fragments because the vehicle you want (Seaglide, Seamoth) only exists as broken
  pieces in the world. You go deeper because the radio tells you someone might be alive down there.
  Game director Charlie Cleveland's GDC 2019 talk lists "adding structure to a sandbox through radio
  signals" and "story beats that guided players without overly directing them" as core topics
  ([GDC news](https://gdconf.com/news/dive-deep-design-subnautica-gdc-2019);
  [Game Developer summary](https://www.gamedeveloper.com/design/video-inside-the-design-of-i-subnautica-i-)).
- **Teaching:** almost nothing is explicitly taught. The fabricator menu shows what you can make, and
  the scanner explains itself when you point it at something.
- **Landmarks and lures:** the Aurora is visible from almost everywhere at the surface, a constant
  "what happened there?" question. Later it explodes (on a timer) and becomes radioactive, so the
  landmark changes state and becomes a gated goal you need a radiation suit for.
- **Gating:** depth is the gate. Oxygen capacity, then vehicle depth limits, decide how far down you
  can go. The player always sees the next depth band before they can survive in it.
- **Early economy:** a few resources (titanium, copper, quartz, creepvine fiber) appear in the first
  biome, and each is needed for something the player already wants.
- **After the opening:** once the radio works and the first vehicle is built, the ocean is open. The
  radio keeps feeding new pings, so there is always a next lead.
- **Lesson for us:** a broken thing next to the player is the first quest. Signals give direction
  without UI. A landmark that changes state keeps pulling.

### A2. The Legend of Zelda: Breath of the Wild (Nintendo, 2017)

- **Hook:** Link wakes in a chamber, takes the Sheikah Slate, and steps out onto a cliff. The camera
  pulls back over all of Hyrule. The "world opens" moment happens at minute two, as a view, before any
  freedom is given.
- **First goal:** a voice and a marker lead to a pedestal. It raises the first Sheikah Tower, which
  fills in the map and points out four shrines glowing in the distance. An old man by a campfire
  offers his paraglider in exchange for the shrines' treasures. The paraglider is a goal you can see
  the need for: the Great Plateau is a cliff-walled island and every edge is a deadly drop.
- **Why each action happens:** shrines give runes (tools) and spirit orbs (the trade for the glider).
  The cold mountain asks for warmth, and the old man's notes point to a cooking recipe that solves it.
  Need first, solution second.
- **Teaching:** each shrine teaches exactly one rune in a small room. Everything else (cooking,
  climbing, fire, weather) is learned by trying it in the open. Wikipedia sums up the critical
  consensus that the plateau "encourages players to experiment"
  ([Great Plateau, Wikipedia](https://en.wikipedia.org/wiki/Great_Plateau)).
- **Landmarks:** in Nintendo's CEDEC 2017 level design talk (via secondary write-ups), the team
  described towers as visible "points", triangle-shaped hills that hide what is behind them, and
  landmarks that read from far away. Heatmaps showed about 80% of playtesters followed the tower
  route when towers were the main guide, which felt like an "invisible guide rope", so they made other
  lures (shrines, smoke, stables) compete for attention
  ([GMTK](https://gmtk.substack.com/p/how-nintendo-solved-zeldas-open-world);
  [Zelda Universe](https://zeldauniverse.net/2017/10/09/a-deep-dive-into-nintendos-development-of-breath-of-the-wild/)).
  Nintendo also said the Great Plateau was the first area built
  ([Nintendo Everything](https://nintendoeverything.com/zelda-breath-of-the-wild-devs-on-starting-out-with-10-people-showing-miyamoto-the-2d-prototype-great-plateau-made-first-more/)).
- **After the tutorial:** you get the glider, the old man reveals who he is and gives the main story
  goal. Then you jump off the plateau into a world you already saw from the cliff. Hyrule Castle,
  wrapped in Ganon's smoke, has been in view since minute two as the final goal.
- **Lesson for us:** show the whole world early, then let the player earn the one tool that lets
  them leave. The final reward of the tutorial is mobility, and the next goal is already visible.

### A3. Outer Wilds (Mobius Digital, 2019)

- **Hook:** you wake by a campfire looking up at a sky with planets in it. Your village is the
  launch site, and your rocket is visible on its pad.
- **First goal:** a villager tells you to get the launch codes from the observatory. It is the only
  given task. Everything after it is self-chosen.
- **Teaching:** the walk to the observatory passes small, optional lessons. A zero-gravity cave
  teaches thrusters. A villager gives you the translator. The observatory/museum explains the ancient
  Nomai, the signalscope, and other strange phenomena through exhibits you read at your own pace.
  (These details are from my knowledge of the game; I could not find a developer source for them.)
- **Developer view:** Alex Beachum said the game has many mechanics that cannot all be taught at the
  start, so teaching has to be spread across a fully non-linear game. Playtesters went to the moon
  early "because it felt safe", so the team put tutorial content there. He also described the
  signalscope as a tool that lets players track sounds across the solar system to find "major
  curiosities" ([Mobius 2016 interview](https://www.mobiusdigitalgames.com/news/archives/09-2016)).
  The GDC 2020 talk "Curiosity-Driven Exploration" covers rewarding curiosity and communicating an
  unusual structure ([GDC Vault](https://gdcvault.com/play/1027008/Independent-Games-Summit-Sparking-Curiosity)).
- **Motivation:** the only reward is knowledge. The ship log records clues and links them as a web of
  open questions. Each question is a goal.
- **Landmarks and lures:** planets in the sky, music you can tune into with the signalscope.
- **After the opening:** you launch, and the whole solar system is open at once.
- **Lesson for us:** curiosity is a full motivation system if every clue opens a question and the
  game keeps a visible list of questions (not tasks).

### A4. Half-Life 2 and Portal (Valve, 2004 / 2007)

- **Half-Life 2 hook:** you step off a train into an oppressive city. Within a minute a guard knocks
  a can over and orders you to pick it up. That one beat teaches the use key and physics objects, and
  it also tells you who is in charge in this world. You can obey or throw the can at him
  ([The Escapist](https://www.escapistmagazine.com/half-life-2-pick-up-the-can/)). Later, a
  character's robot dog plays fetch with the gravity gun to show what it does (as I recall it).
- **Portal teaching:** the developer commentary calls Portal an "extended training exercise". Tools
  are introduced one at a time, then layered. Specific notes from the commentary
  ([Portal developer commentary](https://theportalwiki.com/wiki/Portal_developer_commentary)):
  - Early chambers are built so aimless wandering hits a dead end, which standardises learning.
  - A forced pause with a sound and a particle effect makes players notice the portal gun.
  - When a puzzle introduced too many ideas, two chambers were added before it.
  - Overwhelmed players stop absorbing information, so a combined puzzle was cut back.
  - Players rarely look up, so a ladder or a moving piston pulls the eye.
  - Warm light and shadows point at the important object.
  - The final test removes the hints, so the player proves the skill alone.
- **Method:** Valve's loop was goal, build, playtest, revise. Playtesters found the early puzzles fun
  but said it "didn't feel like a game" until GLaDOS gave it a character and a reason
  ([GMTK on Valve's playtesting](https://gmtk.substack.com/p/valves-secret-weapon)).
- **Lesson for us:** one idea per beat, a deliberate pause to notice the new tool, light as a
  pointer, and a final beat with no help to prove the skill.

### A5. Super Mario Bros. World 1-1 (Nintendo, 1985), brief

Mario starts on the left with open space to the right. The first Goomba walks toward you, and you
learn to jump to survive. Question blocks invite a head bump, and the first mushroom slides away,
hits a pipe and comes back toward you, so most players touch it and learn it is good (this mushroom
detail is from Miyamoto's 2015 Eurogamer video as widely reported). Miyamoto said the level holds
everything a player needs to "gradually and naturally understand what they're doing", and the aim is
that once players understand, the game feels like theirs
([Game Developer](https://gamedeveloper.com/design/how-miyamoto-built-i-super-mario-bros-i-legendary-world-1-1)).
**Lesson:** the layout is the tutorial, and the first danger is also the first lesson.

### A6. Journey (thatgamecompany, 2012)

- **Hook:** the robed figure sits in the sand. You climb the first dune and see the mountain, with a
  beam of light at its peak. That is the goal for the whole game, communicated by one view.
- **Guidance:** no words are used outside the title and credits. Controls are shown as small
  pictures at the start ([Journey, Wikipedia](https://en.wikipedia.org/wiki/Journey_(2012_video_game))).
  Each area is a linear section that keeps the mountain in frame as often as possible.
- **Motivation:** the wish to reach the light, and wonder at each place. Jenova Chen wanted to move
  away from the "defeat/kill/win" mindset. The team used a Japanese-garden approach: remove anything
  that does not serve the feeling. The intended feeling is being small and in awe
  ([Wikipedia](https://en.wikipedia.org/wiki/Journey_(2012_video_game))).
- **Mechanics:** the scarf powers flight. Glowing symbols lengthen it, which is visible growth on the
  character. Singing turns grey cloth red, which opens paths.
- **Lesson for us:** one distant landmark can carry the whole game's purpose. Growth that shows on
  the character's body is more felt than a number.

### A7. Survival-building onboarding: Astroneer, The Planet Crafter, Factorio

**Astroneer (System Era).** You land in a pod next to a shelter. Oxygen comes from the shelter and
from tethers you place outward, so the first building is literally the safety line that lets you
explore. Research samples found in the world are processed for "bytes", which buy blueprints. In
2018 (patch 0.8.0) the studio added a separate, skippable tutorial that covers terrain shaping,
oxygen and power, research and base building
([Astroneer wiki, Patch 0.8.0](https://astroneer.wiki.gg/wiki/Patch_0.8.0)). Lesson: when building
serves a felt need (I need air to go farther), it needs no explanation.

**The Planet Crafter (Miju Games).** The headline number is the Terraformation Index (oxygen, heat,
pressure, life). Blueprints unlock at index thresholds, and the blueprint screen shows which number
each unlock needs ([Wikipedia](https://en.wikipedia.org/wiki/The_Planet_Crafter)). Progress is seen in
the world: the sky changes colour, ice melts, water and plants appear. Lesson: one visible world
measure ties every machine to a reason, and the world itself shows you are making progress.

**Factorio (Wube).** Friday Facts #241 is an honest teardown of their own old tutorial
([FFF #241](https://factorio.com/blog/post/fff-241)). Problems they named: the first level did not
show what the game is; players learned to complete tutorial steps rather than concepts; tasks like
"gather N resources" had no purpose beyond being told; too many messages competed for attention; and
each level reset progress. Their fixes: a gripping start, core systems early, goals that come from the
research tree, each level continuing from the last, and learning by experimenting. A later post
(FFF #342, "The new old tutorial") explains that they dropped a rebuilt intro because it did not
match the lead designer's vision ([FFF #342](https://cf-www.factorio.com/blog/post/fff-342)).
Lesson: "gather N of X because the list says so" is a known failure, named by a studio about its own
game.

### A8. Cozy pacing: Stardew Valley and Animal Crossing: New Horizons

**Stardew Valley.** Day one: the mayor walks you to the farm, there are parsnip seeds in the house,
and the field is full of debris. Parsnips take a few days to grow, which gives the player a reason to
come back tomorrow. Energy ends the day softly. After a few days the run-down Community Center opens,
and its bundles become a visible long-term checklist whose rewards (repairs, a bridge, a greenhouse)
change the town ([strategy guide summary](https://xgamingserver.com/blog/a-beginners-guide-to-your-first-year-in-stardew-valley/)).
Lesson: a cozy opening ends day one with something growing, not with a finished list.

**Animal Crossing: New Horizons.** You arrive on an empty island. Tom Nook asks for a few simple
things (as I recall it, branches and fruit for a welcome campfire), and the first day ends with
naming the island. Many things finish "tomorrow" (a museum tent, a shop), so real time is part of the
pacing. Director Aya Kyogoku said the team chose an empty island because some players of earlier
games struggled to find goals, and building from scratch gives purpose. Producer Hisashi Nogami
described Tom Nook as the character who keeps players motivated
([Nintendo Everything](https://nintendoeverything.com/animal-crossing-new-horizons-devs-on-the-decision-to-start-on-an-island-new-features-multiplayer-more);
[GoNintendo](https://gonintendo.com/stories/339412-animal-crossing-new-horizons-devs-talk-about-the-island-premise)).
Lesson: an empty place is a purpose when the player can see how it could fill up.

### A9. Firewatch and A Short Hike

**Firewatch (Campo Santo, 2016).** A short text prologue gives Henry a reason to run away to a fire
lookout. On the first day, Delilah calls on the radio and sends him to check on fireworks at a lake.
That errand teaches the paper map and compass, and it has a story reason. The radio can be raised at
any time to comment on what you are looking at
([Game Informer](https://gameinformer.com/games/firewatch/b/pc/archive/2014/08/31/campo-santo-drops-clues-about-firewatch-s-mystery-at-pax-panel);
[Prima walkthrough](https://primagames.com/news/firewatch-exploration-guide-and-walkthrough)).
Lesson: a warm voice with a small, human errand makes navigation feel like a story, not a lesson.

**A Short Hike (Adam Robinson-Yu, 2019).** Claire needs to reach the top of Hawk Peak to get a phone
signal ([Wikipedia](https://en.wikipedia.org/wiki/A_Short_Hike)). The peak is visible from the start.
Golden feathers increase how far you can climb and glide, so growth means reach. Everything else
(fishing, side characters, races) is optional and found by wandering. A podcast summary says the
designer talks about guiding players with landmarks that make them curious and natural barriers that
control progress (podcast summary only; not confirmed). Lesson: one tiny, personal goal on a visible
summit is enough for a cozy game, and each upgrade should mean "I can reach further".

### A10. Meaning in delivery and building: Death Stranding, brief

Every delivery connects a settlement to a network, which reveals more map and makes structures
(roads, bridges, others' signs) appear in the world. Kojima framed the game around connecting
people, and the "likes" as closer to thanks than to currency
([GamesBeat](https://gamesbeat.com/hideo-kojima-is-anxious-you-wont-think-death-stranding-is-fun/);
[Gematsu, Tribeca 2019](https://gematsu.com/2019/04/hideo-kojima-discusses-death-stranding-at-tribeca-film-festival-2019)).
Lesson: building and hauling feel meaningful when each act visibly connects or restores something,
and when the reason is a relationship, not a quota.

### A11. Hollow Knight and Metroid, brief

**Metroid (1986).** You start with a path right that is blocked by a low gap. The first upgrade, the
Morph Ball, is a short walk left. The game does not wall off the left, so players discover the path
themselves, and learn the core rule: "blocked now, open later with the right tool"
([Anatomy of Metroid](https://www.quaddicted.com/webarchive/www.anatomyofgames.com_20170315/gamespite-quarterly/the-anatomy-of-metroid-i/index.html);
[Hardcore Gaming 101](https://www.hardcoregaming101.net/metroid/)).

**Hollow Knight (2017).** You get no map at first. The mapmaker Cornifer is found by following a
trail of paper and his humming, and he sells a rough map that you complete yourself (these mechanic
details are from my knowledge of the game). Team Cherry said they wanted players to enjoy "getting
lost and finding your way" ([PC Gamer](https://pcgamer.com/how-to-design-a-great-metroidvania-map)).
Lesson: let the player see gates they cannot pass yet, and make the map itself an earned thing.

---

## Part B. Distilled principles

### B1. Principles of a good opening

1. **Hook with a feeling, not a menu.** The first 60 seconds should create one strong feeling and one
   question. Why: players decide what a game is from its first minute. (Subnautica's burning pod,
   Journey's dune, BotW's cliff view, Factorio FFF #241 on first impressions.)
2. **Show the far goal early.** Put the end, or a big landmark, in view before the player can reach
   it. Why: a visible destination is a goal that needs no text. (Journey's mountain, BotW's castle,
   A Short Hike's peak, Subnautica's Aurora.)
3. **Create the need before giving the tool.** The player should feel the problem first. Why: a tool
   that solves a felt problem is understood instantly and feels earned. (BotW's cliff edges and the
   glider, Subnautica's thirst and the fabricator, Astroneer's oxygen and tethers.)
4. **One new idea per beat.** Why: overloaded players stop learning. (Portal commentary; Mario 1-1.)
5. **Teach with the layout and the object first.** Words come last. Why: what players discover, they
   remember and own. (Mario 1-1, Half-Life 2's can, Metroid's left turn.)
6. **A broken thing near the start is the first quest.** Why: repair is self-explaining and gives a
   clear before/after. (Subnautica's pod and radio.)
7. **Rewards should change what the player can do or see.** Why: new reach or new sight is felt; a
   number in a list is not. (BotW glider, Journey scarf, A Short Hike feathers, Subnautica depth.)
8. **Let the world show progress.** Why: players trust what they see more than what a UI says.
   (Planet Crafter's sky and lakes, Death Stranding's roads, Stardew's repaired town.)
9. **Use one trusted voice, briefly.** Why: a character with a reason to speak turns instructions
   into story. (Delilah, the BotW old man, Tom Nook, the Subnautica PDA.)
10. **Keep questions open.** End each beat with a new question, not just a tick. Why: open questions
    pull harder than closed tasks. (Outer Wilds ship log, Subnautica radio.)
11. **Show gates before keys.** Why: seeing a place you cannot reach yet becomes a goal on its own.
    (Metroid, Subnautica depth, BotW plateau walls.)
12. **End the tutorial with a release, not a stop.** The last beat should open space and point at
    several next things. (BotW's jump off the plateau, Outer Wilds' launch.)
13. **Leave something growing.** Close the first session with something that will be different next
    time. Why: a reason to come back. (Stardew crops, Animal Crossing "tomorrow".)
14. **Playtest for confusion, not for completion.** Watch where people look, stall or skip. Why:
    designers cannot see their own guidance. (Valve's playtest loop, BotW heatmaps.)

### B2. Guidance hierarchy

Use the highest level that works. Drop down only when a level fails in playtests.

1. **World and landmarks.** Shape, light, silhouette, motion, sound sources. The player chooses to
   go, so it feels like their idea. Examples: Journey's mountain, BotW's glowing shrines, Portal's
   warm light on the box.
2. **Diegetic objects and devices.** A radio ping, a gauge on the character, a broken part that
   sparks, a map you hold. It belongs to the world, so it can be loud without breaking mood. Examples:
   Subnautica's radio, Firewatch's compass, the Outer Wilds signalscope.
3. **A character or voice.** A short line from someone with a reason to speak. Use sparingly, and
   give each line a job. Examples: Delilah, Tom Nook, the Subnautica PDA.
4. **Contextual prompt.** A small key glyph near the object, shown once, at the moment it is needed,
   then gone. Example: Journey's picture prompts.
5. **Text objective.** A written task in the UI.

**When text objectives are acceptable:**
- As a **reminder** of a goal that was already set up in the world (player returns after a break).
- For **precise information** the world cannot show well (a recipe's exact needs, a count).
- As an **accessibility option** players can turn on.
- Never as the only reason to do something. If removing the text leaves the player with no reason to
  act, the beat is broken, not the text.

For Lunarion specifically (owner rule): at most **one** guidance element on screen at a time, plain
functional text only when needed, and never repeated.

### B3. Motivation

**Intrinsic** motivation comes from the activity: curiosity, mastery, wonder, care for a place.
**Extrinsic** motivation comes from outside: rewards, quotas, points, being told. Cozy games live on
intrinsic motivation. Extrinsic hooks are fine as scaffolding, but each one should point at an
intrinsic reward (a new place, a new view, a new ability), not at another quota.

**Need before tool.** The pattern, in four steps:
1. **Feel the limit.** The player meets a limit in play (slow, dark, can't reach, can't hear).
2. **See the hint.** The world shows the shape of a solution (a broken charger, a fallen mast, a
   wreck with the part).
3. **Earn the means.** Scan, salvage, build. Short.
4. **Feel the change.** The limit lifts in a way the player can see and hear, and a new limit or
   question appears just beyond it.

**Every task answers "why now?"** Before shipping any beat, write the answer in one line from the
player's point of view: "I am doing this because ___, and I need it now because ___." If the second
blank is empty or says "the text told me", the beat needs a reason in the world.

### B4. Early item economy rules

1. **Introduce a resource at the moment it is needed**, ideally right after the need is felt.
2. **Cap the count.** In the first 15-20 minutes, only as many item types as the first builds use.
   About three to five is a reasonable target (my recommendation, not from a source). Planet Crafter
   and Subnautica start with a small handful.
3. **Name its purpose at pickup.** "Copper coil, for the charging pad." A name without a purpose is
   noise.
4. **No random drops early.** The first containers should give exactly what the current need asks
   for. Randomness can come later, when the player knows the item set.
5. **One source, one look.** Each early resource should come from a place whose look tells you what
   is inside (a cable spool gives wire, a solar panel gives cells).
6. **Spend it soon.** An early item should be used within a few minutes of pickup, so the loop
   closes while the player still remembers why they took it.
7. **Avoid gather-N quotas** without a reason (Factorio FFF #241). If a count is needed, show it on
   the thing being built, not in a list.

### B5. The tutorial handoff: what a great final beat does

- **Gives mobility or reach.** The glider, the rocket, the first vehicle.
- **Opens a view.** The player sees more of the world than before, ideally from a high point.
- **Points at several lures, not one.** Two or three visible options, so the first free choice is
  the player's.
- **Sets one long goal.** A far landmark or mystery that will hold for hours (BotW's castle).
- **Leaves a reason to return home.** Something at the base that grows or needs tending.
- **Steps the guidance down.** The voice speaks less, the UI goes quiet, the world takes over.

### B6. Checklist for any opening beat

Answer yes or no. Any "no" needs a fix or a written reason.

**Purpose**
- [ ] Can a playtester say, in their own words, why they are doing this?
- [ ] Did the player feel the need before getting the tool or task?
- [ ] Does the beat answer "why now?" in the world, not only in text?
- [ ] Would the beat still make sense with all objective text hidden?

**Guidance**
- [ ] Is the goal shown by the world, an object or a device first?
- [ ] Is there at most one guidance element on screen?
- [ ] Is any text plain, short, shown once, and only if a playtest showed it is needed?
- [ ] Is the target visible (light, silhouette, sound) from where the player stands?

**Teaching**
- [ ] Does the beat introduce only one new idea?
- [ ] Is there a moment to notice the new thing (pause, sound, light) before using it?
- [ ] Can the player try it safely and fail softly?

**Reward**
- [ ] Does completing it change what the player can do, see or reach?
- [ ] Is the change visible on 07 or the base, and audible (pentatonic chirp)?
- [ ] Does it open a new question or a next need?

**Economy**
- [ ] Is every item picked up in this beat used in this beat or the next one?
- [ ] Is each item's purpose named at pickup?
- [ ] Is there no random loot?

**Flow**
- [ ] Does the beat lead to the next one without a dead end?
- [ ] Is the pace calm (no timers that rush, no fail spikes)?
- [ ] Would a first-time player know what to do 10 seconds after finishing it?

### B7. Anti-patterns, mapped to our current opening

| Anti-pattern | What it looks like | Seen in our opening | Fix direction |
|---|---|---|---|
| **Task-list guidance** | Text lines say what to do next; the player follows the list, not the world. | "Drive over, build 3 things following text lines." | Put the reason in the world (gauge, signal, broken object); keep text as a quiet reminder only. |
| **Unmotivated building** | Build X because the list says so; the result does not change play. | Charging pad, solar, mast built without knowing why. | Need before tool: let 07 feel low power, deafness or darkness first. |
| **Tool before need** | A tool or blueprint arrives before the problem it solves. | Blueprints and builds come before any felt limit. | Reorder: limit, hint, means, change. |
| **Loot spam** | Containers give random items whose use is unknown. | Shooting small crates gives 2-3 random items. | Fixed contents tied to the current need; name the purpose; no random drops in the first session. Also conflicts with the owner's "no floating loot" and "purpose must read" rules. |
| **Too many nouns early** | Many item names before any is used. | Many unexplained items early. | Cap item types; introduce each when needed. |
| **Dead end after tutorial** | The guided part ends and nothing points onward. | "After building there is nothing to do and no next goal." | Final beat opens a view, gives reach, and shows 2-3 lures plus one far goal. |
| **Gather-N quota** | Collect N of X with no reason. | Risk if the bank/solar costs are just counts. | Show cost on the ghost of the build; tie each item to a visible part. |
| **Hint overload** | Several prompts, voice lines and markers at once. | Risk with text lines plus face lines plus markers. | One guidance element at a time (owner rule). |

---

## Part C. Applying this to Lunarion

The opening already has strong raw material: an eclipse wake, a dead waystation, a CRT face that can
speak without words, scanning, and a calm basin. What is missing is a felt chain of needs. A
suggested chain for the first 30 minutes:

- **Felt limit 1: power.** 07 wakes on reserve during the eclipse. It is slow and dim.
- **Felt limit 2: night/storage.** The pad only works in sun; the dark returns.
- **Felt limit 3: silence.** 07 hears a faint repeating signal it cannot resolve.
- **Release:** the mast resolves the signal into a direction and a far landmark. The basin opens.

Each of the three beats below follows "limit, hint, means, change". All examples stay calm: no
combat, no rushing timers. The eclipse is used as a gentle mood clock, not a countdown.

### C1. Scan the lander crate

**Before (text-driven).** A text line says to drive to the crate and scan it. The scan unlocks a
blueprint. The player does not know why this crate matters or what the blueprint is for.

**After (need-driven, diegetic).**
- **Limit.** 07 wakes in eclipse dark on reserve power. Its CRT face shows a battery glyph that
  flickers low, and its top speed is soft-capped (a gentle, felt slowness, with a low motor tone).
  No text. The player feels "I am weak".
- **Hint.** The only warm light in view is a small amber pilot lamp on the old lander, blinking in
  the dark next to a cracked crate. Beside the crate lies a half-buried, broken 1980s charging pad
  with a clear plug shape. Its silhouette reads as "a place to park and charge". Light is the lure,
  like Portal's warm light on the box.
- **Means.** 07 drives close. The scan is the natural verb: 07's face draws the broken pad's outline
  in amber wireframe as the scan fills, with a rising pentatonic chirp. The scan teaches one thing:
  "scanning broken things gives 07 the knowledge to rebuild them". The crate opens (no shooting) and
  gives exactly the parts the pad needs, each named with its purpose at pickup ("Coil, for the
  charging pad").
- **Change.** 07's face now shows a small pad icon next to the low battery glyph: the problem and the
  answer side by side. The player knows the next step without a text line.
- **Why now?** "I am running out of power, and this is the only thing here that looks like it can
  charge me."

### C2. Build the charging pad

**Before (text-driven).** A text line says to build the charging pad. The player builds it where they
like. Nothing in play changes afterwards, so the build feels like a checkbox.

**After (need-driven, diegetic).**
- **Limit.** Still on reserve, still slow. The pad needs sunlight, and the eclipse is ending.
- **Hint.** The eclipse ends during this beat. The first sliver of sunlight sweeps across the basin
  floor and lands on a flat patch near the ruins. This is the awe beat (first dawn) and the
  placement hint at once: build where the light is. When the player opens the build ghost, it glows
  warmer when placed in sunlight and cooler in shadow, so the rule is learned by looking.
- **Means.** The build is short and physical: legs unfold, panels click and lock (owner rule:
  every state change gets a visible mechanical action and a chirp).
- **Change.** 07 rolls onto the pad. The battery glyph fills with a calm rising arpeggio, the face
  brightens, and the speed cap lifts. The player feels the difference in their hands the moment they
  drive off. 07's face can then show a simple range ring: how far 07 can go and still return. The
  ring just misses the interesting things at the basin edge. That plants the next desire (more
  storage, more range) without a word.
- **Follow-on need (solar array and battery bank).** As the long lunar day turns, the pad's light
  fades or the range ring is too small for the ruins. The battery bank is the answer to "I want to go
  farther"; the solar array is the answer to "the pad is too slow". Both should be introduced only
  when that limit is felt, never as items on a list.
- **Why now?** "The sun just came back, I am almost empty, and the pad I learned needs this light."

### C3. Build the antenna mast

**Before (text-driven).** A text line says to build the antenna mast. It is the third build of the
list. Afterwards there is no next goal, and the opening ends in a dead end.

**After (need-driven, diegetic).**
- **Limit.** Once 07 has power and range, it drives up the first rise of the basin. There, its CRT
  shows a faint, repeating waveform: a slow signal in D major pentatonic, broken by static. 07 tilts
  its head toward it, but the direction wobbles. 07 can hear that something is out there but not
  where. The limit is felt as a question, not a threat.
- **Hint.** In the Lumen ruins, a fallen mast lies across the ground with its dish face-down. Its
  shape says "tall thing that listens". Scanning it gives the mast blueprint, using the same verb
  learned in C1.
- **Means.** Build the mast next to the charging pad. It rises in segments with clunks and a
  settling sway (vacuum logic: rigid, no flags or wind).
- **Change.** The mast extends, and the signal on 07's face sharpens into a clean tone with a clear
  direction. Far out on the crater rim, a small light starts blinking in answer: the first far
  landmark, visible from the base. At the same time, the mast's own top lamp becomes the home
  landmark, visible from across the basin, so the player can always find the way back.
- **Handoff (the release).** From the top of the rise, the player can now see three things: the
  blinking light on the rim (the long goal, beyond current range), a wreck in the middle distance
  (scan fragments, the near goal), and the dark Lumen buildings (story clues in the databank, the
  curious goal). One is clearly out of range, which points straight back at the battery bank
  upgrade. The guidance steps down here: no more text lines, only the world and 07's face.
- **Why now?** "I heard something out there, and I need a way to hear where it is coming from."

### C4. Notes for the redesign

- Replace "shoot small crates for random items" in the opening with fixed, purposeful containers.
  If shooting/cutting remains a verb, it should open a specific stuck hatch or free a specific part
  the player can already see they need.
- Every opening item should appear first as part of something broken in the world (pad, mast, panel
  array), so the player sees what it is for before they own it.
- Keep the eclipse as the emotional frame: dark and weak at the start, light and capable at the
  dawn, curious at the signal. That is a three-act mini story in 20-30 minutes with no text.
- Run the checklist in B6 on every beat, then watch a first-time playtester with the objective text
  hidden. If they stall for more than about 30 seconds (my suggested threshold), the world is not
  saying enough yet.

---

## Sources

Developer talks, interviews and developer commentary:
- [Portal developer commentary (The Portal Wiki)](https://theportalwiki.com/wiki/Portal_developer_commentary)
- [Factorio Friday Facts #241: New player experience](https://factorio.com/blog/post/fff-241)
- [Factorio Friday Facts #342: The new old tutorial](https://cf-www.factorio.com/blog/post/fff-342)
- [GDC 2019: The Design of Subnautica (GDC news)](https://gdconf.com/news/dive-deep-design-subnautica-gdc-2019)
- [Inside the design of Subnautica (Game Developer)](https://www.gamedeveloper.com/design/video-inside-the-design-of-i-subnautica-i-)
- [GDC Vault: Subnautica design talk](https://gdcvault.com/play/1025745/The-Design-of-Subnautica)
- [Mobius Digital news, Sept 2016 (Outer Wilds interview)](https://www.mobiusdigitalgames.com/news/archives/09-2016)
- [GDC Vault: Sparking Curiosity-Driven Exploration in Outer Wilds](https://gdcvault.com/play/1027008/Independent-Games-Summit-Sparking-Curiosity)
- [How Miyamoto built World 1-1 (Game Developer)](https://gamedeveloper.com/design/how-miyamoto-built-i-super-mario-bros-i-legendary-world-1-1)
- [BotW devs on the Great Plateau being made first (Nintendo Everything)](https://nintendoeverything.com/zelda-breath-of-the-wild-devs-on-starting-out-with-10-people-showing-miyamoto-the-2d-prototype-great-plateau-made-first-more/)
- [Animal Crossing: New Horizons island premise (Nintendo Everything)](https://nintendoeverything.com/animal-crossing-new-horizons-devs-on-the-decision-to-start-on-an-island-new-features-multiplayer-more)
- [Animal Crossing: New Horizons devs on the island premise (GoNintendo)](https://gonintendo.com/stories/339412-animal-crossing-new-horizons-devs-talk-about-the-island-premise)
- [Hollow Knight map design, Team Cherry (PC Gamer)](https://pcgamer.com/how-to-design-a-great-metroidvania-map)
- [Kojima on Death Stranding (GamesBeat)](https://gamesbeat.com/hideo-kojima-is-anxious-you-wont-think-death-stranding-is-fun/)
- [Kojima at Tribeca 2019 (Gematsu)](https://gematsu.com/2019/04/hideo-kojima-discusses-death-stranding-at-tribeca-film-festival-2019)
- [Crafting a tiny open world: A Short Hike (PlayStation Blog)](https://blog.playstation.com/2021/08/05/crafting-a-tiny-open-world-a-look-behind-the-scenes-at-the-creation-of-a-short-hike/)

Design analyses and reference:
- [How Nintendo solved Zelda's open world problem (GMTK)](https://gmtk.substack.com/p/how-nintendo-solved-zeldas-open-world)
- [A deep dive into Nintendo's development of BotW (Zelda Universe)](https://zeldauniverse.net/2017/10/09/a-deep-dive-into-nintendos-development-of-breath-of-the-wild/)
- [Valve's secret weapon: playtesting (GMTK)](https://gmtk.substack.com/p/valves-secret-weapon)
- [Half-Life 2 "pick up the can" (The Escapist)](https://www.escapistmagazine.com/half-life-2-pick-up-the-can/)
- [The Anatomy of Metroid](https://www.quaddicted.com/webarchive/www.anatomyofgames.com_20170315/gamespite-quarterly/the-anatomy-of-metroid-i/index.html)
- [Metroid (Hardcore Gaming 101)](https://www.hardcoregaming101.net/metroid/)
- [Great Plateau (Wikipedia)](https://en.wikipedia.org/wiki/Great_Plateau)
- [Journey (Wikipedia)](https://en.wikipedia.org/wiki/Journey_(2012_video_game))
- [The Planet Crafter (Wikipedia)](https://en.wikipedia.org/wiki/The_Planet_Crafter)
- [A Short Hike (Wikipedia)](https://en.wikipedia.org/wiki/A_Short_Hike)
- [Lifepod 5 (Subnautica Wiki)](https://subnautica.fandom.com/wiki/Lifepod_5)
- [Astroneer Patch 0.8.0 (Astroneer wiki)](https://astroneer.wiki.gg/wiki/Patch_0.8.0)
- [Firewatch PAX panel (Game Informer)](https://gameinformer.com/games/firewatch/b/pc/archive/2014/08/31/campo-santo-drops-clues-about-firewatch-s-mystery-at-pax-panel)
- [Firewatch walkthrough (Prima Games)](https://primagames.com/news/firewatch-exploration-guide-and-walkthrough)
- [Stardew Valley first-year guide (xGamingServer)](https://xgamingserver.com/blog/a-beginners-guide-to-your-first-year-in-stardew-valley/)

Not verified against a primary source: the Subnautica Aurora timer and depth details, Outer Wilds
museum/observatory specifics, the Half-Life 2 dog "fetch" beat, the Mario mushroom-bounce detail
(widely reported from Miyamoto's 2015 Eurogamer video, which I did not watch), Animal Crossing's exact
day-one request, and Hollow Knight's Cornifer mechanics. These are standard descriptions of the games
but should be rechecked before being quoted in a pitch.
