using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;

namespace MoonProject.Autoplay
{
    /// <summary>
    /// The opening (docs/design/opening-v2.md, beats B0..B3 so far) played the way a player plays it: New Game on the
    /// title, 07 wakes in the eclipse, drive to the lander along the cable, scan the two parts in the open crate by
    /// holding the key its prompt chip shows. Each beat is checked against what the game's own objective line says.
    /// </summary>
    public sealed class OpeningScenario
    {
        private const float BootTimeoutSeconds = 60f;
        private const float TitleAttemptSeconds = 1.5f;
        private const int TitleAttempts = 12;
        private const float WakeTimeoutSeconds = 90f;
        private const float LookAroundSeconds = 4f;
        private const float LineTimeoutSeconds = 30f;
        private const float DriveTimeoutSeconds = 240f;
        private const float LanderStandOff = 12f;
        private const float LanderArriveRadius = 4f;
        private const float LanderCargoRadius = 25f;
        private const float ScanStandOff = 3.5f;
        private const float ScanArriveRadius = 1.5f;
        private const float ScanLookTimeoutSeconds = 12f;

        /// <summary>Seconds without the chip after which the bot shifts 07 a hair, as a player fidgets.</summary>
        private const float NudgeSeconds = 3f;
        private const float TraceSeconds = 1f;
        private const float ScanAllowanceSeconds = 8f;
        private const float FollowSeconds = 6f;

        /// <summary>The lander's open charger kit (B4) and the CUTTER SPARES rack by the fallen array (B7).</summary>
        private const string ChargerKit = "supply.lander.open";
        private const string SparesRack = "supply.array.spares";

        /// <summary>Seconds the base may take to become night-ready: a sunset that comes first leaves the array idle, so the bank never charges.</summary>
        private const float NightReadySeconds = 150f;

        /// <summary>Degrees under last light that still count as last light (the Sun settles onto it).</summary>
        private const float LastLightToleranceDegrees = 0.5f;

        /// <summary>The lander cargo set's fragment ids start with this; a story scan standing nearby is not part of it.</summary>
        private static readonly string CargoPrefix = "ruin." + ScanTuning.LanderCargoId + ".";

        private readonly AutoplayBot _bot;
        private readonly string _untilBeat;
        private bool _awoke;
        private OpeningBase _base;
        private string _lastBeat;

        private readonly bool _linesExpected;

        private readonly NaiveGuard _naive;

        public OpeningScenario(AutoplayBot bot, string untilBeat, bool linesExpected, NaiveGuard naive = null)
        {
            _naive = naive;
            DeclareNextActions();
            _bot = bot;
            _untilBeat = untilBeat;
            _linesExpected = linesExpected;
        }

        public IEnumerator Run()
        {
            _base = new OpeningBase(_bot) { LinesExpected = _linesExpected, Naive = _naive };
            yield return Boot();
            _base.Listen();
            foreach (Func<IEnumerator> beat in new Func<IEnumerator>[]
                     {
                         Drive, Scan, CollectParts, BuildThePad, ChargeOnThePad, TheRuins, BuildTheArray,
                         TheFirstChoice, TheHabitat, Nightfall, TheTower, TheAnswer, TheRelay,
                     })
            {
                if (_bot.Failed || Reached(_lastBeat))
                {
                    break;
                }

                yield return beat();
                if (_naive != null && !_bot.Failed && _lastBeat != null)
                {
                    yield return _naive.Check(_lastBeat);
                }
            }

            _base.Stop();
        }

        /// <summary>
        /// What a first-time player must do after each beat (the first-time check passes only a cue that names it):
        /// the key, the verb words of a chip, the words of a line and the world cues that point at it.
        /// </summary>
        private void DeclareNextActions()
        {
            if (_naive == null)
            {
                return;
            }

            const string drive = "ui.hint.drive";
            _naive.Declare("B0", new NextAction("WASD", new[] { drive }, new[] { "lander" }, new[] { "lander" }));
            _naive.Declare("B2", new NextAction("E", new[] { "hint.scan" }, new[] { "scan", "crate" }, null));
            _naive.Declare("B3", new NextAction("E", new[] { "hint.pickup" }, new[] { "take", "parts", "crate" }, null));
            _naive.Declare("B4", new NextAction("B", new[] { "hint.openbuild" },
                new[] { "pad", "apron", "build", "charging" }, null));
            _naive.Declare("B4b", new NextAction("E", new string[0], new[] { "place", "apron", "marks" }, null));
            _naive.Declare("B5", new NextAction("WASD", new[] { drive }, new[] { "pad", "park" }, null));
            _naive.Declare("B6", new NextAction("WASD", new[] { drive }, new[] { "Lumen", "solar", "ruins", "array" },
                new[] { "glint" }));
            _naive.Declare("B7a", new NextAction("E", new[] { "hint.salvage" }, new[] { "cut", "panel", "junction" },
                null));
            _naive.Declare("B7b", new NextAction("E", new[] { "hint.pickup" }, new[] { "rack", "igniter" },
                new[] { "rack" }));
            _naive.Declare("B7c", new NextAction("Tab", new[] { "hint.fitpart" }, new[] { "igniter", "fit" }, null));
            _naive.Declare("B7", new NextAction("B", new[] { "hint.openbuild" }, new[] { "solar", "array" }, null));
            // R5's free choice is told by the world only: no line is expected, and the dead lamp's glance comes
            // after 30 s with no site visited.
            _naive.Declare("B8", new NextAction("WASD", new string[0], new[] { "seismic", "habitat" },
                new[] { "seismic", "module" }));
            _naive.SetBudget("B8", FreeChoiceBudgetSeconds);
            _naive.Declare("R5", new NextAction("WASD", new[] { drive }, new[] { "night", "module", "habitat", "store" },
                new[] { "lamp" }));
            _naive.Declare("B9a", new NextAction("RMB", new string[0], new[] { "tow", "home", "frame", "hook" }, null));
            _naive.Declare("B9b", new NextAction("B", new[] { "hint.openbuild" }, new[] { "bank", "build", "module" },
                null));
            // The first nightfall plays by itself (B10): nothing is asked of the player after the bank.
            _naive.Skip("B9");
            _naive.Declare("B10", new NextAction("WASD", new[] { drive }, new[] { "tower", "antenna", "transmit" },
                new[] { "tower" }));
            _naive.Declare("B11a", new NextAction("B", new[] { "hint.openbuild" }, new[] { "mast", "antenna", "build" },
                null));
            _naive.Declare("B11", new NextAction("E", new[] { "hint.transmit" }, new[] { "transmit", "answer" },
                new[] { "foot", "TX" }));
            _naive.Declare("B12", new NextAction("WASD", new[] { drive }, new[] { "relay", "west" }, new[] { "relay" }));
            _naive.Skip("R10");
        }

        /// <summary>The first free choice (B8, R5) is told by the world: the dead lamp's glance comes after 30 s.</summary>
        private const float FreeChoiceBudgetSeconds = 35f;

        private IEnumerator Sub(string id)
        {
            if (_naive != null && !_bot.Failed)
            {
                yield return _naive.Check(id);
            }
        }

        private bool Reached(string beat)
        {
            return beat != null && string.Equals(_untilBeat, beat, StringComparison.OrdinalIgnoreCase);
        }

        private void Begin(string id, string title)
        {
            _lastBeat = id;
            _bot.BeginBeat(id, title);
        }

        private IEnumerator Boot()
        {
            Begin("B0", "New game and the eclipse wake");
            yield return _bot.Until(() => _bot.World.Refresh() && _bot.World.Ui != null, BootTimeoutSeconds,
                "the game to boot");
            if (_bot.Failed)
            {
                _bot.EndBeat(string.Empty);
                yield break;
            }

            using (_bot.World.Context.Events.Subscribe<RoverAwoke>(_ => _awoke = true))
            {
                for (int attempt = 0; attempt < TitleAttempts && _bot.World.Flow.State == GameFlowState.Title; attempt++)
                {
                    yield return _bot.Tap(Key.Enter);
                    yield return _bot.Wait(TitleAttemptSeconds);
                }

                yield return _bot.Until(() => _bot.World.Flow.State == GameFlowState.Playing, BootTimeoutSeconds,
                    "New Game to start play from the title");
                yield return _bot.Until(() => _awoke, WakeTimeoutSeconds, "07 to wake in the eclipse");
            }

            if (!_bot.Failed && _naive != null)
            {
                // Control is handed over: what does a player who has woken see to do?
                yield return _naive.Check("B0");
            }

            if (!_bot.Failed)
            {
                yield return _bot.LookAround(LookAroundSeconds);
            }

            _bot.EndBeat("New Game from the title, 07 woke on its own, the view moved for " +
                         LookAroundSeconds.ToString("0", CultureInfo.InvariantCulture) + " s");
        }

        private IEnumerator Drive()
        {
            Begin("B2", "Drive to the lander");
            yield return _bot.Until(() => _bot.World.Refresh(), LineTimeoutSeconds, "the game to be up");
            yield return _base.ExpectStep(OpeningSteps.DriveToLander, OpeningKeys.DriveToLander, "B2", LineTimeoutSeconds,
                onScreen: false);
            if (!_bot.Failed)
            {
                WorldAnchor lander = Lander();
                Vector3 spot = lander.Position + lander.Forward * LanderStandOff;
                yield return _bot.DriveTo(spot, LanderArriveRadius, "the lander's port side", DriveTimeoutSeconds);
                yield return _bot.Until(() => _bot.World.Gameplay.Opening.Port.Reached, FollowSeconds,
                    "the game to count the lander as reached");
            }

            _bot.EndBeat("07 reached the lander");
        }

        private IEnumerator Scan()
        {
            Begin("B3", "Scan the two parts in the open crate by the lander");
            yield return _base.ExpectStep(OpeningSteps.ScanLander, OpeningKeys.ScanLander, "B3", onScreen: false);
            var chips = new List<string>();
            ScanField scans = _bot.World.Gameplay.Scans;
            ScanFragment next;
            while (!_bot.Failed && (next = NextCargoFragment(scans)) != null)
            {
                yield return ScanOne(scans, next, chips);
                yield return _bot.Wait(0.5f);
            }

            if (!_bot.Failed)
            {
                yield return _bot.Until(() => _bot.World.Gameplay.Opening.Flow.CurrentIndex > OpeningSteps.ScanLander,
                    FollowSeconds, "the opening to move on from step " + OpeningSteps.ScanLander);
            }

            int step = _bot.World.Gameplay.Opening.Flow.CurrentIndex;
            if (!_bot.Failed && step != OpeningSteps.CollectParts)
            {
                _bot.Warn("after the scan the opening stands at step " + step + ", not " + OpeningSteps.CollectParts +
                          " (opening-v2 B4).");
            }

            _bot.EndBeat("both fragments scanned (chips seen: " + string.Join("; ", chips) + ")");
        }

        /// <summary>The part the brackets are on: a player scans whichever cargo part is the candidate, in any order.</summary>
        private bool OnCargoCandidate(ScanField scans, ScanFragment fragment)
        {
            ScanFragment candidate = scans.Candidate;
            return candidate != null && (candidate == fragment || (!candidate.Scanned &&
                   DriveSteering.FlatDistance(candidate.Point, Lander().Position) <= LanderCargoRadius));
        }

        private IEnumerator ScanOne(ScanField scans, ScanFragment fragment, List<string> chips)
        {
            Vector3 point = fragment.Point;
            Vector3 away = Flat(_bot.World.Rover.Position - point);
            away = away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.back;
            yield return _bot.DriveTo(point + away * ScanStandOff, ScanArriveRadius, fragment.FragmentId, DriveTimeoutSeconds);
            yield return _bot.TurnToward(point, fragment.FragmentId);
            yield return _bot.LookAt(point, fragment.FragmentId);
            UiProbe ui = _bot.World.Ui;
            float deadline = Time.unscaledTime + ScanLookTimeoutSeconds;
            float nudgeAt = Time.unscaledTime + NudgeSeconds;
            float traceAt = 0f;
            int nudges = 0;
            while (!_bot.Failed && !(_bot.KeepLookingAt(point) && OnCargoCandidate(scans, fragment) &&
                                     ui.ChipGlyph().Length > 0))
            {
                if (Time.unscaledTime > deadline)
                {
                    if (!OnCargoCandidate(scans, fragment) || ui.RawChipGlyph().Length == 0)
                    {
                        _bot.Fail("timed out after " + ScanLookTimeoutSeconds.ToString("0", CultureInfo.InvariantCulture) +
                                  " s waiting for the brackets on " + fragment.FragmentId + " (candidate " +
                                  (scans.Candidate != null ? scans.Candidate.FragmentId : "none") + "; " +
                                  ui.DescribeChip() + "). " + _bot.Where());
                        yield break;
                    }

                    _bot.Warn("the Scan chip never showed on " + fragment.FragmentId + " in " +
                              ScanLookTimeoutSeconds.ToString("0", CultureInfo.InvariantCulture) +
                              " s while 07 stood in range with the objective line up (" + ui.DescribeChip() +
                              "); the bot pressed the key the hidden chip carries.");
                    break;
                }

                if (Time.unscaledTime > traceAt)
                {
                    traceAt = Time.unscaledTime + TraceSeconds;
                    Debug.Log("Autoplay: waiting for the Scan chip on " + fragment.FragmentId + ": " +
                              DriveSteering.FlatDistance(_bot.World.Rover.Position, point).ToString("0.0", CultureInfo.InvariantCulture) +
                              " m away, candidate " + (scans.Candidate != null ? scans.Candidate.FragmentId : "none") +
                              ", objective '" + ui.ObjectiveLine() + "', " + ui.DescribeChip());
                }

                if (Time.unscaledTime > nudgeAt)
                {
                    nudgeAt = Time.unscaledTime + NudgeSeconds;
                    yield return _bot.Tap(nudges++ % 2 == 0 ? Key.W : Key.S);
                }

                yield return null;
            }

            if (_bot.Failed)
            {
                yield break;
            }

            if (scans.Candidate != fragment)
            {
                fragment = scans.Candidate;
                point = fragment.Point;
            }

            _base.NoteUsed(OpeningBase.ScanVerb);
            string glyph = ui.RawChipGlyph();
            chips.Add(ui.Chip().Length > 0 ? ui.Chip() : "(hidden) " + glyph);
            if (!GlyphKeys.TryResolve(glyph, out Key key))
            {
                _bot.Fail("the prompt chip names a key the bot does not know: '" + glyph + "'.");
                yield break;
            }

            yield return _bot.Hold(key, () =>
                {
                    _bot.KeepLookingAt(point);
                    return fragment.Scanned;
                }, scans.Tuning.Seconds(fragment.Size) + ScanAllowanceSeconds,
                "held " + key + " to scan " + fragment.FragmentId);
        }

        private IEnumerator CollectParts()
        {
            Begin("B4", "Collect the lander's open crate");
            yield return _base.TakeFromTheOpenCrate(ChargerKit);
            if (!_bot.Failed)
            {
                yield return _base.ExpectStep(OpeningSteps.BuildPad, OpeningKeys.BuildPad, "B5: the hold covers the pad");
            }

            _bot.EndBeat("07 took the charger kit's parts from the open crate");
        }

        private IEnumerator BuildThePad()
        {
            Begin("B5", "Build the charging pad");
            yield return _base.Build(StructureIds.ChargingPad, _base.Pad());
            _bot.EndBeat("the pad was placed and welded with the build keys");
        }

        private IEnumerator ChargeOnThePad()
        {
            Begin("B6", "Charge on the pad");
            yield return _base.ExpectStep(OpeningSteps.Charge, OpeningKeys.Charge, "B6");
            yield return _base.ChargeOnThePad(StructureIds.ChargingPad);
            _bot.EndBeat("07 charged on the pad until the lander's reserve gave out");
        }

        private IEnumerator TheRuins()
        {
            Begin("B7", "The Lumen ruins: scan the array and the airlock, the cutter sputters, fit the spare igniter " +
                        "from the rack, cut the array's panels and junction box");
            yield return _base.ExpectStep(OpeningSteps.FindSolar, OpeningKeys.FindSolar, "B7: the reserve is spent");
            yield return _base.SeeWorldCue(WorldCues.ArrayGlint(_bot.World), "B7: the fallen array glints in the sunrise");
            yield return _base.DriveToTheRuinsGate("the Lumen ruins");
            yield return _base.ScanTheSet("ruin.FallenSolarArray");
            yield return Sub("B7a");
            yield return _base.ReadTheAirlockPanel();
            yield return _base.ExpectStep(OpeningSteps.FitIgniter, OpeningKeys.CutOptics,
                "B7: the array's crates, the cutter still FAULT");
            yield return _base.TryTheFaultedCutter("FallenSolarArray");
            yield return _base.ExpectStep(OpeningSteps.FitIgniter, OpeningKeys.TakeIgniter,
                "B7: the cutter sputtered, the rack is pointed at");
            yield return Sub("B7b");
            yield return _base.TakeFromTheOpenCrate(SparesRack);
            yield return Sub("B7c");
            yield return _base.ExpectStep(OpeningSteps.FitIgniter, OpeningKeys.FitIgniter,
                "B7: the spare igniter in the hold");
            yield return _base.FitTheIgniter();
            yield return _base.ExpectStep(OpeningSteps.BuildSolar, OpeningKeys.CutOptics, "B7: the cutter works");
            yield return _base.CutTheCrates("FallenSolarArray");
            _bot.EndBeat("the fallen array scanned, the airlock panel read, the cutter sputtered, the spare igniter " +
                         "taken from the rack and fitted, the array's panels and junction box cut");
        }

        private IEnumerator BuildTheArray()
        {
            Begin("B8", "Build the solar array");
            yield return _base.ExpectStep(OpeningSteps.BuildSolar, OpeningKeys.BuildSolar,
                "B8: the array's cost is covered");
            yield return _base.DriveToTheRuinsGate("out of the Lumen ruins");
            // West of the pad: the ruins side stays open for the power module towed home at R6.
            yield return _base.Build(StructureIds.SolarArray, _base.Pad() + Vector3.left * OpeningBase.Spacing);
            yield return _base.UntilReached(OpeningMilestone.PowerOn, "the PowerOn milestone");
            _bot.EndBeat("the array stands and the base has power");
        }

        private IEnumerator TheFirstChoice()
        {
            Begin("R5", "The first free choice: visit the seismic station, dock, and the first dusk begins");
            yield return _base.ExpectStep(OpeningSteps.BuildBatteryBank, OpeningKeys.VisitSite,
                "R5: the afternoon names the near sites");
            yield return _base.VisitTheSeismicStation();
            yield return _base.DockForTheDusk(StructureIds.ChargingPad);
            _bot.EndBeat("07 read the 1971 seismic station, docked, and the first dusk began");
        }

        private IEnumerator TheHabitat()
        {
            Begin("B9", "The habitat: scan the modules, cut the cable reel, tow the power module home, build the bank on it");
            yield return _base.ExpectStep(OpeningSteps.BuildBatteryBank, OpeningKeys.ScanModule,
                "B9: night is coming");
            yield return _base.ScanTheSet("ruin.HabitatModules");
            yield return _base.ExpectStep(OpeningSteps.BuildBatteryBank, OpeningKeys.BuildBatteryBank,
                "B9: the module is the bank's frame");
            yield return _base.CutTheCrates("HabitatModules");
            yield return Sub("B9a");
            yield return _base.TowTheModuleHome();
            yield return Sub("B9b");
            yield return _base.Build(StructureIds.BatteryBank, _base.ModulePosition());
            yield return _base.UntilReached(OpeningMilestone.NightReady, "the NightReady milestone",
                NightReadySeconds);
            if (!_bot.Failed)
            {
                var sunlight = _bot.World.Context.Get<ISunlight>();
                float elevation = SunElevationDegrees(sunlight.SunDirection);
                float lastLight = sunlight.LastLightElevationDegrees;
                if (elevation < lastLight - LastLightToleranceDegrees)
                {
                    _bot.Warn("the base became night-ready with the sun at " +
                              elevation.ToString("0.0", CultureInfo.InvariantCulture) +
                              " degrees, past last light (" + lastLight.ToString("0", CultureInfo.InvariantCulture) +
                              "): the array stood idle through the dusk.");
                }
            }

            _bot.EndBeat("the module towed home, the battery bank stands on it and the base is night-ready");
        }

        private IEnumerator Nightfall()
        {
            Begin("B10", "The first nightfall");
            yield return _base.TheFirstNightfall();
            _bot.EndBeat("the first night fell on a lit base");
        }

        private IEnumerator TheTower()
        {
            Begin("B11", "The radio tower and the antenna mast");
            yield return _base.ExpectStep(OpeningSteps.ScanTower, OpeningKeys.ScanTower, "B11: after nightfall");
            yield return _base.CutTheCrates("RadioTower");
            yield return _base.ScanTheSet("ruin.RadioTower");
            yield return Sub("B11a");
            yield return _base.ExpectStep(OpeningSteps.BuildMast, OpeningKeys.BuildMast, "B11");
            yield return _base.DriveToTheRuinsGate("out of the Lumen ruins");
            yield return _base.Build(StructureIds.AntennaMast, _base.Pad() + Vector3.back * OpeningBase.Spacing);
            yield return _base.UntilReached(OpeningMilestone.SignalWest, "the SignalWest milestone");
            _bot.EndBeat("the mast stands and the west signal answered");
        }

        private IEnumerator TheAnswer()
        {
            Begin("B12", "The answer");
            yield return _base.AnswerTheCall();
            _bot.EndBeat("07 transmitted from the mast and relay.1 answered the call");
        }

        private IEnumerator TheRelay()
        {
            Begin("R10", "relay.1's repair: take the spare from its cabinet, fit it, charge on its pad");
            yield return _base.RepairTheRelay();
            _bot.EndBeat("relay.1 relit: its spare fitted at the feed socket, and its pad charges 07");
        }

        /// <summary>The next unscanned fragment of the lander's cargo, in the order the blueprint numbers them.</summary>
        private ScanFragment NextCargoFragment(ScanField scans)
        {
            Vector3 lander = Lander().Position;
            ScanFragment best = null;
            foreach (ScanFragment fragment in scans.Fragments)
            {
                if (!fragment.Scanned && fragment.FragmentId.StartsWith(CargoPrefix, StringComparison.Ordinal) &&
                    DriveSteering.FlatDistance(fragment.Point, lander) <= LanderCargoRadius &&
                    (best == null || fragment.Index < best.Index))
                {
                    best = fragment;
                }
            }

            return best;
        }

        private static float SunElevationDegrees(Vector3 towardTheSun)
        {
            return Mathf.Asin(Mathf.Clamp(towardTheSun.normalized.y, -1f, 1f)) * Mathf.Rad2Deg;
        }

        private WorldAnchor Lander()
        {
            if (!_bot.World.Anchors.TryGet(WorldAnchorIds.PlaceLander, out WorldAnchor lander))
            {
                _bot.Fail("the World has no anchor " + WorldAnchorIds.PlaceLander + ".");
            }

            return lander;
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }
    }
}
