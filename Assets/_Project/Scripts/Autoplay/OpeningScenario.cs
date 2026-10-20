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

        public OpeningScenario(AutoplayBot bot, string untilBeat, bool linesExpected)
        {
            _bot = bot;
            _untilBeat = untilBeat;
            _linesExpected = linesExpected;
        }

        public IEnumerator Run()
        {
            _base = new OpeningBase(_bot) { LinesExpected = _linesExpected };
            yield return Boot();
            _base.Listen();
            foreach (Func<IEnumerator> beat in new Func<IEnumerator>[]
                     {
                         Drive, Scan, CollectParts, BuildThePad, ChargeOnThePad, TheRuins, BuildTheArray,
                         TheHabitat, Nightfall, TheTower,
                     })
            {
                if (_bot.Failed || Reached(_lastBeat))
                {
                    break;
                }

                yield return beat();
            }

            _base.Stop();
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
            yield return _base.DriveToTheRuinsGate("the Lumen ruins");
            yield return _base.ScanTheSet("ruin.FallenSolarArray");
            yield return _base.ReadTheAirlockPanel();
            yield return _base.ExpectStep(OpeningSteps.FitIgniter, OpeningKeys.CutOptics,
                "B7: the array's crates, the cutter still FAULT");
            yield return _base.TryTheFaultedCutter("FallenSolarArray");
            yield return _base.ExpectStep(OpeningSteps.FitIgniter, OpeningKeys.TakeIgniter,
                "B7: the cutter sputtered, the rack is pointed at");
            yield return _base.TakeFromTheOpenCrate(SparesRack);
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
            yield return _base.Build(StructureIds.SolarArray, _base.Pad() + Vector3.right * OpeningBase.Spacing);
            yield return _base.UntilReached(OpeningMilestone.PowerOn, "the PowerOn milestone");
            _bot.EndBeat("the array stands and the base has power");
        }

        private IEnumerator TheHabitat()
        {
            Begin("B9", "The habitat: scan the modules, cut the cable reel and power module, build the bank");
            yield return _base.ExpectStep(OpeningSteps.BuildBatteryBank, OpeningKeys.BuildBatteryBank,
                "B9: night is coming");
            yield return _base.ScanTheSet("ruin.HabitatModules");
            yield return _base.CutTheCrates("HabitatModules");
            yield return _base.DriveToTheRuinsGate("out of the Lumen ruins");
            yield return _base.Build(StructureIds.BatteryBank, _base.Pad() + Vector3.left * OpeningBase.BankSpacing);
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

            _bot.EndBeat("the battery bank stands and the base is night-ready");
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
            yield return _base.ExpectStep(OpeningSteps.BuildMast, OpeningKeys.BuildMast, "B11");
            yield return _base.DriveToTheRuinsGate("out of the Lumen ruins");
            yield return _base.Build(StructureIds.AntennaMast, _base.Pad() + Vector3.back * OpeningBase.Spacing);
            yield return _base.UntilReached(OpeningMilestone.SignalWest, "the SignalWest milestone");
            _bot.EndBeat("the mast stands and the west signal answered");
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
