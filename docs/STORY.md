# Lofi Lunar — Story Bible

> Owner: Director. English is the source text (localized via keys, see ARCHITECTURE "Localization").
> Tone: warm, melancholic, hopeful. Told only through objects, short texts, the radio and the base — never cutscene
> exposition, never voice. Every line should be something a person would actually write on a sticky note.

## Premise
**Lumen Station** was a four-person radio-relay and research outpost in a quiet crater on the Moon's near side. Its
crew loved it — they named the rover "07" because it was the seventh thing they fixed in their first week, and it
never stopped following them around. When a **solar storm** forced an evacuation, they left in a hurry, scattering
their things across the basin. Their last instruction to 07 was the one it still follows: *"Keep the radio on. We'll
be back after the storm."* The storm passed. The program was cancelled. Nobody came. 07 kept the radio on.

The game begins decades later, when 07 — slower now, eye half-closed — wakes up one more time.

## The crew (and what they left behind)
Each crew member has: a set of **6 relics** (museum shelf set), a **companion machine** (a friend to repair), **3 logs**
(short text cards found near their things), and a **window** in the lander that lights up when their set is complete.

### Ro Amadi — radio operator, station DJ
Ran the night show "Lumen After Dark" for an audience of three people and a rover. Talks to machines like old friends.
- Companion: **Bell**, the radio cabinet on legs she built to spin records when she slept.
- Slice relics: `cassette_player` (her mixtape "for the long drive"), `golden_record` (she wanted to send our own).
- Later relics: a cracked headset, a hand-lettered setlist, a stage light gel (amber), a postcard from Lagos.
- She is the one who answers at the end.

### Theo Lindqvist — botanist
Grew basil under LED strips and named every seedling. Dry humour, terrible puns, endlessly patient.
- Companion: **Moss**, the slow garden bot who "talks" to plants by humming.
- Slice relics: `teapot` (mint grown in lunar soil, "the first tea on the Moon that tasted like anything"),
  `garden_gnome` (smuggled aboard as a joke, guarded the seedlings, "Chief Security Officer").
- Later relics: a seed tin, a watering can with a dent, a pressed leaf in a notebook, knitted fingerless gloves.

### Ines Kalani — geologist, mission lead
Brave, precise, secretly sentimental. Walked the crater rim every morning "to check the Moon was still there".
- Companion: **Tilly**, the tiny survey drone that followed her like a puppy.
- Slice relic: `astronaut_boot` (her field boot, tread still packed with the dust of her favourite ridge).
- Later relics: a rock hammer with a taped handle, a field journal, a sample jar with a single blue pebble,
  a photo of her daughter, a compass that points nowhere useful on the Moon.

### Kenji Morrow — engineer
Fixed everything, including 07 (seven times). Explained bugs to a rubber duck; the duck usually solved them.
- Companion: **Atlas**, the big gentle hauler crane.
- Slice relic: `rubber_duck` ("Senior Debugging Consultant").
- Later relics: a multitool, a coffee mug with a chip, a wrench signed by the crew, a paper crane, a spare 07 wheel.

## Slice relic texts (source for `relic.<id>.name` / `relic.<id>.memory`)
| id | Name | Memory |
|---|---|---|
| astronaut_boot | Ines's Field Boot | Ines walked the rim every morning "to check the Moon was still there". The tread is still packed with dust from her favourite ridge. |
| cassette_player | Ro's Mixtape | A walkman with a tape still inside, labelled in Ro's handwriting: "for the long drive". Side B just says "for when we come home". |
| garden_gnome | Chief Security Officer | Theo smuggled him aboard as a joke and put him in charge of the seedlings. Nothing was ever stolen on his watch. |
| golden_record | The Lumen Record | Ro's homemade golden record: greetings from four people, one rover and a very proud basil plant, ready for anyone listening. |
| rubber_duck | Senior Debugging Consultant | Kenji explained every broken part to this duck before fixing it. The duck never said a word, and it was usually right. |
| teapot | The First Real Tea | Theo brewed mint grown in lunar soil in this dented pot. "The first tea on the Moon that tasted like anything," Ro wrote on the lid. |

## Logs (12, three per crew member) — the arc each set tells
- **Ro**: the first night show → the storm warning read on air → "I left the radio on for you, 07."
- **Theo**: naming the seedlings → packing in a hurry, leaving Moss to water them → "If anything grows, tell me."
- **Ines**: the morning rim walk → choosing to evacuate → "We were coming back. I need you to know we tried."
- **Kenji**: fixing 07 the first time → the spare wheel he hid for "next time" → "Don't let anyone retire you."

## Cassettes (8 tapes; liner notes are Ro's, one or two sentences)
| id | Title | Ro's note |
|---|---|---|
| after_dark_1 | Lumen After Dark, Vol. 1 | First night show. Audience: three crew, one rover, one basil plant. The basil had requests. |
| dust_and_honey | Dust & Honey | Recorded with the mic taped to the airlock. If you hear a thump, that's Kenji. |
| slow_orbit | Slow Orbit | For the nights Earth looks close enough to walk to. It isn't. I checked. |
The other five arrive with the Rim Terraces, the Shadowed Crater and The Peak (M4).

## Bell's lines (ticker, M3-05)
- Home for the first time: "Bell got home before you. She says the porch light was on."
- A signal picked: "Bell's picking something up… bearing {0}."
- A signal found: "Bell says: told you so." / "Bell says: that one's been waiting for you." / "Bell says: Ro would
  have liked that."

## The radio's fragments (ticker text, unlocked as the signal grows)
Short lines that drift across the radio as 07 restores the tower — old broadcasts, then letters addressed to the
station, years apart. They reveal that the crew tried to return, the program was cancelled, and Ro kept writing
anyway. Examples (source text):
- "…weather tonight: clear skies over the Sea of Tranquility, as always…"
- "Lumen Station, this is Houston relay. Program review postponed. Stand by."
- "07, it's Ro. They say nobody's listening up there. I'm sending this anyway."
- "Theo says the basil on my windowsill isn't as good as yours was. Don't tell him I agree."
- "It's been twenty years. I still play the long-drive tape on Sundays."

## The ending
With every friend restored and the great dish on The Peak repaired, 07 broadcasts the last song on Ro's mixtape —
the one labelled "for when we come home". The camera holds on Earth. After a long, quiet moment, a single light on
Earth's night side blinks: three short, one long — the station's old call sign. A final ticker line:
"I heard you, 07. I'm still here. — Ro." Credits roll over the radio. Then the game lets you keep driving.

## Writing rules
- One to two sentences per relic memory; a concrete sensory detail beats any abstract feeling.
- No exposition dumps: if a line explains the plot, cut it.
- Humour lives next to sadness (the duck, the gnome) — the crew were happy here.
- Never let a line make the player feel guilty for being slow; time is gentle in this world.
