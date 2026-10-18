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
    /// title, 07 wakes in the eclipse, drive to the lander along the cable, scan the two fragments in its cargo bay by
    /// holding the key its prompt chip shows. Each beat is checked against what the game's own objective line says.
    /// </summary>
    public sealed class OpeningScenario
    {
        public const string LastBeat = "B3";

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
        private const float ScanLookTimeoutSeconds = 5f;
        private const float ScanAllowanceSeconds = 8f;
        private const float FollowSeconds = 6f;

        private readonly AutoplayBot _bot;
        private readonly string _untilBeat;
        private bool _awoke;

        public OpeningScenario(AutoplayBot bot, string untilBeat)
        {
            _bot = bot;
            _untilBeat = untilBeat;
        }

        public IEnumerator Run()
        {
            yield return Boot();
            if (_bot.Failed || Reached("B0"))
            {
                yield break;
            }

            yield return Drive();
            if (_bot.Failed || Reached("B2"))
            {
                yield break;
            }

            yield return Scan();
        }

        private bool Reached(string beat)
        {
            return string.Equals(_untilBeat, beat, StringComparison.OrdinalIgnoreCase);
        }

        private IEnumerator Boot()
        {
            _bot.BeginBeat("B0", "New game and the eclipse wake");
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
            _bot.BeginBeat("B2", "Drive to the lander");
            yield return _bot.Until(() => _bot.World.Refresh() && _bot.World.Gameplay.Opening.Flow.Current ==
                OpeningKeys.DriveToLander, LineTimeoutSeconds, "the objective line to say '" + OpeningKeys.DriveToLander + "'");
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
            _bot.BeginBeat("B3", "Scan the lander's cargo");
            yield return _bot.Until(() => _bot.World.Gameplay.Opening.Flow.Current == OpeningKeys.ScanLander,
                FollowSeconds, "the objective line to say '" + OpeningKeys.ScanLander + "'");
            var chips = new List<string>();
            ScanField scans = _bot.World.Gameplay.Scans;
            while (!_bot.Failed && _bot.World.Gameplay.Opening.Flow.Current == OpeningKeys.ScanLander)
            {
                ScanFragment next = NearestCargoFragment(scans);
                if (next == null)
                {
                    _bot.Fail("no unscanned fragment in the lander's cargo, but the objective line still says '" +
                              OpeningKeys.ScanLander + "'.");
                    break;
                }

                yield return ScanOne(scans, next, chips);
                yield return _bot.Wait(0.5f);
            }

            if (!_bot.Failed)
            {
                yield return _bot.Until(() => _bot.World.Gameplay.Opening.Flow.Current == OpeningKeys.CollectParts,
                    FollowSeconds, "the objective line to move on to '" + OpeningKeys.CollectParts + "'");
            }

            _bot.EndBeat("both fragments scanned (chips seen: " + string.Join("; ", chips) + ")");
        }

        private IEnumerator ScanOne(ScanField scans, ScanFragment fragment, List<string> chips)
        {
            Vector3 point = fragment.Point;
            Vector3 away = Flat(_bot.World.Rover.Position - point);
            away = away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.back;
            yield return _bot.DriveTo(point + away * ScanStandOff, ScanArriveRadius, fragment.FragmentId, DriveTimeoutSeconds);
            yield return _bot.TurnToward(point, fragment.FragmentId);
            yield return _bot.LookAt(point, fragment.FragmentId);
            yield return _bot.Until(() => scans.Candidate == fragment, ScanLookTimeoutSeconds,
                "the brackets and the Scan chip on " + fragment.FragmentId);
            if (_bot.Failed)
            {
                yield break;
            }

            UiProbe ui = _bot.World.Ui;
            chips.Add(ui.Chip());
            if (!GlyphKeys.TryResolve(ui.ChipGlyph(), out Key key))
            {
                _bot.Fail("the prompt chip names a key the bot does not know: '" + ui.ChipGlyph() + "'.");
                yield break;
            }

            yield return _bot.Hold(key, () => fragment.Scanned, scans.Tuning.Seconds(fragment.Size) + ScanAllowanceSeconds,
                "held " + key + " to scan " + fragment.FragmentId);
        }

        private ScanFragment NearestCargoFragment(ScanField scans)
        {
            Vector3 lander = Lander().Position;
            Vector3 here = _bot.World.Rover.Position;
            ScanFragment best = null;
            float bestDistance = float.MaxValue;
            foreach (ScanFragment fragment in scans.Fragments)
            {
                if (fragment.Scanned || DriveSteering.FlatDistance(fragment.Point, lander) > LanderCargoRadius)
                {
                    continue;
                }

                float distance = DriveSteering.FlatDistance(fragment.Point, here);
                if (distance < bestDistance)
                {
                    best = fragment;
                    bestDistance = distance;
                }
            }

            return best;
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
