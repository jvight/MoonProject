# Loudness table: 0.5.2 feel cues

Generated from sfx_manifest.json. Level = category target + 20 log10(volume) at full bus; every beat layers
at most three of these at once (feel-pass R10): e.g. the crack = cut_crack + the beam fall-away + the rumble (felt, not heard).

| Beat | Cue | Category | Target LUFS | Volume | Effective (dB) |
|---|---|---|---|---|---|
| The cut: charge run, per lamp | `charge_lamp` | oneshot_3d | -18 | 0.55 | -23.2 |
| The cut: ignition per tier | `cut_ignite_tier` | oneshot_3d | -18 | 0.65 | -21.7 |
| The cut: sizzle (loop, x progress) | `cut_sizzle` | loop_3d | -22 | 0.50 | -28.0 |
| The cut: crack | `cut_crack` | oneshot_3d | -18 | 0.70 | -21.1 |
| Fold | `strap_tuk` | tick_3d | -26 | 0.70 | -29.1 |
| Dock: walls rise | `dock_walls_rise` | oneshot_3d | -18 | 0.60 | -22.4 |
| Dock: clamp | `dock_clamp` | oneshot_3d | -18 | 0.65 | -21.7 |
| Dock: full chord | `dock_full` | oneshot_3d | -18 | 0.50 | -24.0 |
| Dock: walls settle | `dock_walls_settle` | tick_3d | -26 | 0.55 | -31.2 |
| Dock: walls drop | `dock_walls_drop` | tick_3d | -26 | 0.50 | -32.0 |
| Base: structure powers on | `structure_power_on` | oneshot_3d | -18 | 0.60 | -22.4 |
| Base: a lamp at nightfall (x per lamp) | `base_lamp_on` | oneshot_3d | -18 | 0.50 | -24.0 |
| Drums: boost (loop) | `boost_whine` | loop_3d | -22 | 0.45 | -28.9 |
| Drums: release | `boost_release` | tick_3d | -26 | 0.60 | -30.4 |
| New step line | `page_tick` | tick_2d | -28 | 0.50 | -34.0 |
| Game saved | `save_tick` | tick_2d | -28 | 0.40 | -36.0 |
| Stock count tick (unwired) | `count_tick` | tick_2d | -28 | 0.45 | -34.9 |
| Radio: carrier (loop) | `radio_carrier` | radio_2d | -24 | 1.00 | -24.0 |
| 07 chirp: affirm | `chirp_affirm` | oneshot_3d | -18 | 0.55 | -23.2 |
| 07 chirp: happy | `chirp_happy` | oneshot_3d | -18 | 0.55 | -23.2 |
| 07 chirp: proud | `chirp_proud` | oneshot_3d | -18 | 0.55 | -23.2 |
| 07 chirp: curious | `chirp_curious` | oneshot_3d | -18 | 0.55 | -23.2 |
| 07 chirp: worried | `chirp_worried` | oneshot_3d | -18 | 0.55 | -23.2 |
| 07 chirp: tired | `chirp_tired` | oneshot_3d | -18 | 0.55 | -23.2 |
| 07 chirp: relieved | `chirp_relieved` | oneshot_3d | -18 | 0.55 | -23.2 |
| 07 chirp: greeting | `chirp_greeting` | oneshot_3d | -18 | 0.55 | -23.2 |
