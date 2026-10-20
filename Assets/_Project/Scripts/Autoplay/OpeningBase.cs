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
    /// The steps of the opening after the lander (B4..B11) played as a player does: take the lander's open crate,
    /// build with the build key, the arrows and E, charge on the pad, scan among the ruins, try the faulted cutter
    /// (it sputters), take the spare igniter from the rack by the fallen array and fit it on 07's face screen, cut,
    /// wait out the first sunset. A step names the objective line the game must show before it
    /// starts, and a chip that never shows on a verb's first use is listed as a guidance gap in the beat's warnings.
    /// </summary>
    internal sealed class OpeningBase
    {
        private const float FollowSeconds = 6f;

        /// <summary>Seconds a milestone may take after its build: the bank, for one, has to charge first.</summary>
        private const float MilestoneSeconds = 120f;
        private const float LineSeconds = 15f;
        private const string LowBatteryLanderKey = "battery.low.lander";
        private const string LowBatteryPadKey = "battery.low.pad";

        /// <summary>Seconds a world cue may take to be lit and on screen.</summary>
        private const float CueSeconds = 20f;

        /// <summary>A cue counts as on screen this far in from each edge of the view (a share of the viewport).</summary>
        private const float CueMargin = 0.05f;

        /// <summary>Seconds of one sweep of the view while the bot looks for a lit cue.</summary>
        private const float CueSweepSeconds = 1.5f;
        private const float DriveSeconds = 150f;
        private const float BaseSpacing = 7f;

        /// <summary>The bank stands close to the pad, well inside the cable reach, as a sensible player places it.</summary>
        private const float BankPadSpacing = 4.5f;
        private const float RuinsGateOffset = 10f;
        private const float ScanStandOff = 3.5f;
        private const float PanelStandOff = 2.5f;
        private const float ScanLineUp = 3f;
        private const float LineUpArrive = 3f;
        private const float ScanArrive = 1.5f;
        private const float SealStandOff = 1.6f;
        private const float SealLineUp = 3f;

        // The open ground 07 comes in from, this far beyond a seal's line-up (CrateApproachSessions keeps it clear).
        private const float SealOpenGround = 5f;
        private const float SealArrive = 1.5f;
        private const float SealNose = 1.2f;

        // Closer than this (metres, flat) a small crate's seal lies under 07's nose, out of the chest cutter's sight.
        private const float SealTooClose = 1f;
        private const float SealCutSeconds = 30f;
        private const float SealCandidateSeconds = 3f;
        private const int SealCandidateAttempts = 3;
        private const float SealNudgeMetres = 1f;
        private const float ScanLookSeconds = 12f;
        private const float ScanAllowanceSeconds = 8f;
        private const float ChipSeconds = 3f;
        private const float ApproachThrottleRadius = 0.5f;
        private const float SunsetSeconds = 300f;
        private const float ChargeSeconds = 60f;

        /// <summary>The bot heads back to the pad to charge when 07's cells fall to this share.</summary>
        private const float TopUpBelow = 0.3f;

        /// <summary>...and stays until they are back to this share.</summary>
        private const float TopUpTo = 0.85f;

        private const float TopUpSeconds = 300f;
        private const float ClearHeight = 0.9f;
        private const float ClearRadius = 1.1f;
        private const int ApproachSides = 8;
        private const int MaxMenuSteps = 12;
        private const float PlaceDriftMetres = 2f;
        private const int PlaceBearings = 6;
        private const float PlaceBearingStepDegrees = 60f;
        private const float PlaceStandArrive = 0.5f;

        /// <summary>The weld cancels the frame the structure stands beyond the weld range, so the bot keeps this inside it.</summary>
        private const float PlaceWeldMargin = 1f;
        private const int SettledFrames = 8;
        private const int CutterDevice = 1;

        private readonly AutoplayBot _bot;
        private readonly List<OpeningMilestoneReached> _milestones = new List<OpeningMilestoneReached>();
        private readonly HashSet<string> _verbsUsed = new HashSet<string>();
        private IDisposable _reached;

        public OpeningBase(AutoplayBot bot)
        {
            _bot = bot;
        }

        private GameplaySystem G => _bot.World.Gameplay;

        public void Listen()
        {
            _reached?.Dispose();
            _reached = _bot.World.Context.Events.Subscribe<OpeningMilestoneReached>(_milestones.Add);
        }

        public void Stop()
        {
            _reached?.Dispose();
            _reached = null;
        }

        /// <summary>
        /// The objective lines are part of the opening and checked on screen. Off (the lines have become stall
        /// reminders), a line showing at all fails the run, so the bot follows the game's steps and the world's cues.
        /// </summary>
        public bool LinesExpected { get; set; }

        /// <summary>Where the last world cue the bot waited for stood.</summary>
        public Vector3 CuePoint { get; private set; }

        /// <summary>
        /// The opening must stand at step <paramref name="index"/> (<see cref="OpeningSteps"/>), read from the flow's
        /// own position, not from any text. While the lines are expected, the step's line (<paramref name="key"/>) must
        /// also be the one the game is on and, with <paramref name="onScreen"/>, read on screen, as
        /// <see cref="ExpectLine"/> checks (the opening beats before the lander never waited on the screen).
        /// </summary>
        public IEnumerator ExpectStep(int index, string key, string why, float seconds = FollowSeconds,
            bool onScreen = true)
        {
            yield return _bot.Until(() => G.Opening.Flow.CurrentIndex == index, seconds,
                "the opening to stand at step " + index + " (" + why + ")",
                () => "It stands at step " + G.Opening.Flow.CurrentIndex + ".");
            if (!_bot.Failed && LinesExpected && onScreen && key.Length > 0)
            {
                yield return ExpectLine(key, why);
            }
        }

        /// <summary>
        /// A world cue the bot may head for: it waits until the cue is lit and inside the view, as a player must see
        /// it first, and notes where it stood in <see cref="CuePoint"/>. A cue that never lights or never shows fails.
        /// </summary>
        public IEnumerator ExpectWorld(WorldCue cue, string why, float seconds = CueSeconds)
        {
            yield return _bot.Until(() => cue.Lit() && WorldCue.OnScreen(_bot.World.Context.Get<IViewCamera>().Camera,
                    cue.Point(), CueMargin), seconds,
                "the world cue '" + cue.Name + "' to be lit and on screen (" + why + ")",
                () => "Lit " + cue.Lit() + ", at " + cue.Point() + ", in the view at " +
                      _bot.World.Context.Get<IViewCamera>().Camera.WorldToViewportPoint(cue.Point()) + ".");
            CuePoint = cue.Point();
        }

        /// <summary>
        /// The player's look for a cue: the bot turns the view round until the cue is lit and on screen, within
        /// <paramref name="seconds"/>; a cue that never shows is listed as a warning (the glint depends on where the
        /// view stands against the Sun, so a miss is a note for the world's slice, not a failed run).
        /// </summary>
        public IEnumerator SeeWorldCue(WorldCue cue, string why, float seconds = CueSeconds)
        {
            if (cue == null)
            {
                _bot.Warn("the scene has no cue for " + why + ".");
                yield break;
            }

            float deadline = Time.unscaledTime + seconds;
            while (Time.unscaledTime < deadline)
            {
                if (cue.Lit() && WorldCue.OnScreen(_bot.World.Context.Get<IViewCamera>().Camera, cue.Point(), CueMargin))
                {
                    CuePoint = cue.Point();
                    yield break;
                }

                yield return cue.Lit() ? _bot.LookAround(CueSweepSeconds) : null;
            }

            _bot.Warn("world cue '" + cue.Name + "' was not lit and on screen within " +
                      seconds.ToString("0", CultureInfo.InvariantCulture) + " s (" + why + "): lit " + cue.Lit() + ".");
        }

        /// <summary>The objective line the game must be on, and reading on screen once 07 stands still.</summary>
        public IEnumerator ExpectLine(string key, string why)
        {
            yield return _bot.Until(() => G.Opening.Flow.Current == key, FollowSeconds,
                "the objective line to be '" + key + "' (" + why + ")",
                () => "It is '" + G.Opening.Flow.Current + "'.");
            if (_bot.Failed || key.Length == 0)
            {
                yield break;
            }

            ILocalization words = _bot.World.Context.Get<ILocalization>();
            string expected = words.Get(key);
            string lowLander = words.Get(LowBatteryLanderKey);
            string lowPad = words.Get(LowBatteryPadKey);

            // The low-battery hint takes the objective line's own slot for a while, by the slot's priority.
            yield return _bot.Until(() =>
                {
                    string line = _bot.World.Ui.ObjectiveLine();
                    return line == expected || line == lowLander || line == lowPad;
                }, LineSeconds,
                "the objective line to read '" + expected + "' on screen (" + why + ")",
                () => "On screen: line '" + _bot.World.Ui.ObjectiveLine() + "', chip '" + _bot.World.Ui.Chip() + "'.");
        }

        /// <summary>
        /// An open crate (B4: the lander's charger kit; B7: the spares rack by the fallen array) gives everything it
        /// holds on a held E once 07 is within its reach.
        /// </summary>
        public IEnumerator TakeFromTheOpenCrate(string cacheId)
        {
            if (Taken(cacheId))
            {
                yield break;
            }

            SealedContainer crate = G.Containers.Find(cacheId);
            if (!G.Hints.TryGet(InteractionKind.PickUp, out _))
            {
                // Out of the crate's reach: drive up to it, as a player would.
                Vector3 side = crate.SealSide(_bot.World.Rover.Position);
                Vector3 front = Flat(crate.CutPoint.position) + side * SealStandOff;
                yield return _bot.DriveTo(front + side * SealLineUp, LineUpArrive,
                    "a line-up for " + cacheId, DriveSeconds);
                yield return _bot.DriveTo(front, 1f, cacheId, DriveSeconds);
            }

            yield return _bot.Until(() => G.Hints.TryGet(InteractionKind.PickUp, out _), FollowSeconds,
                cacheId + " to offer what it holds (Pick up)");
            yield return _bot.Hold(Key.E, () => Taken(cacheId), FollowSeconds, "E to take what " + cacheId + " holds");
        }

        /// <summary>Open crate <paramref name="cacheId"/> has given everything it held to 07's hold.</summary>
        private bool Taken(string cacheId)
        {
            SupplyCaches caches = G.Opening.Caches;
            for (int i = 0; i < caches.Count; i++)
            {
                if (caches.Get(i).Id == cacheId)
                {
                    return caches.Left(i).IsFree && !caches.PartLeft(i);
                }
            }

            _bot.Fail("the opening has no supply crate '" + cacheId + "'.");
            return true;
        }

        /// <summary>B5, B8, B9, B11: B opens the menu, arrows pick, E takes up, 07's look finds a spot, E places and welds.</summary>
        public IEnumerator Build(string structureId, Vector3 at)
        {
            BuildField build = G.Building;
            StructureDefinition definition = build.Catalog.Find(structureId);
            yield return _bot.Until(() => G.KnownStructures.IsKnown(structureId), FollowSeconds,
                structureId + "'s blueprint to be learned");
            float ahead = build.Tuning.GhostAheadMetres;
            yield return _bot.DriveTo(Flat(at) + Vector3.back * ahead, PlaceStandArrive, "the build spot's stand",
                DriveSeconds);
            yield return _bot.TurnToward(at, "the build spot");
            if (_bot.Failed)
            {
                yield break;
            }

            yield return FirstUse("build: B");
            yield return _bot.Tap(Key.B);
            yield return _bot.Until(() => build.State == BuildModeState.Menu, FollowSeconds, "B to open the build menu");
            yield return _bot.Until(() => _bot.World.Ui.BuildStripShown(), FollowSeconds, "the build strip to show");
            for (int step = 0; !_bot.Failed && step < MaxMenuSteps &&
                                build.Controller.SelectedStructure?.Id != structureId; step++)
            {
                yield return _bot.Tap(Key.RightArrow);
            }

            if (build.Controller.SelectedStructure?.Id != structureId)
            {
                _bot.Fail("the arrows never reached " + structureId + " in the build menu.");
                yield break;
            }

            yield return _bot.Tap(Key.E);
            yield return _bot.Until(() => build.State == BuildModeState.Placing, FollowSeconds,
                "E to take " + structureId + " up to place");
            yield return PutTheGhostOn(build, at, structureId);
            if (_bot.Failed)
            {
                yield break;
            }

            string aimed = "candidate " + build.Controller.CandidatePose.Position + " is " +
                           DriveSteering.FlatDistance(build.Controller.CandidatePose.Position, _bot.World.Rover.Position)
                               .ToString("0.0", CultureInfo.InvariantCulture) + " m from 07 (weld range " +
                           G.Building.Tuning.WeldRange.ToString("0.0", CultureInfo.InvariantCulture) + " m)";
            int before = G.PlacedBase.Count;
            yield return _bot.Tap(Key.E);
            yield return _bot.Until(() => G.PlacedBase.Count > before, FollowSeconds, "E to place " + structureId,
                () => "State " + build.State + ", reason " + build.Controller.CandidateReason + "; " + aimed + ".");
            if (!_bot.Failed)
            {
                // The tap may already have welded a little and closed build mode; holding E at the frame welds on either way.
                PlacedStructure frame = LastPlaced();
                Vector3 weldPoint = frame.Pose.Position;
                yield return _bot.Hold(Key.E, () =>
                    {
                        _bot.KeepLookingAt(weldPoint);
                        return G.PlacedBase.TryGetStructure(frame.InstanceId, out PlacedStructure built) && built.IsBuilt;
                    }, definition.BuildSeconds + FollowSeconds, "a held E to weld " + structureId);
            }

            yield return _bot.Until(() => build.State == BuildModeState.Closed, FollowSeconds,
                "a finished weld to close build mode");
            Vector3 placed = PlacedPosition(structureId);
            if (!_bot.Failed && DriveSteering.FlatDistance(placed, at) > PlaceDriftMetres)
            {
                _bot.Warn(structureId + " stands " + DriveSteering.FlatDistance(placed, at).ToString("0.0",
                    CultureInfo.InvariantCulture) + " m from the spot asked for.");
            }
        }

        /// <summary>B6: onto the pad, the dock locks 07 in, 07 charges until the lander's reserve runs out.</summary>
        public IEnumerator ChargeOnThePad(string structureId)
        {
            BatteryModel battery = G.Battery.Model;
            Vector3 pad = PlacedPosition(structureId);
            yield return _bot.DriveTo(Flat(pad) + Vector3.back * 6f, 2f, "a line-up for the pad", DriveSeconds);
            yield return _bot.DriveTo(Flat(pad), ApproachThrottleRadius, "the pad", DriveSeconds);
            // After sunrise 07 can reach the pad already full, so FirstCharge fires without the pad charging it and
            // the milestone counts as much as the pad's own charge.
            yield return _bot.Until(() => battery.IsCharging && battery.Charger == ChargerKind.Pad ||
                _milestones.Exists(reached => reached.Milestone == OpeningMilestone.FirstCharge), FollowSeconds,
                "the built pad to charge 07");
            bool arrivedFull = battery.CellsFull;
            yield return UntilReached(OpeningMilestone.FirstCharge, "the FirstCharge milestone");
            if (!arrivedFull)
            {
                // A 07 that reaches the pad with full cells (the Sun topped it up) is not charged, so the dock never
                // locks it in; the reserve still gives out and the opening goes on.
                yield return _bot.Until(() => G.PadLock.Raised, FollowSeconds, "the pad's dock lock to rise round 07");
            }

            yield return _bot.Until(() => !battery.IsCharging, ChargeSeconds, "07 to charge full");
        }

        /// <summary>Every fragment of a blueprint's set, one by one.</summary>
        public IEnumerator ScanTheSet(string setId)
        {
            ScanField scans = G.Scans;
            while (!_bot.Failed)
            {
                ScanFragment fragment = FirstUnscanned(scans, setId);
                if (fragment == null)
                {
                    yield break;
                }

                yield return TopUpIfLow();

                Vector3 away = Flat(_bot.World.Rover.Position - fragment.Point);
                yield return Scan(fragment, away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.back, ScanStandOff);
            }
        }

        private static ScanFragment FirstUnscanned(ScanField scans, string setId)
        {
            for (int i = 0; scans.Find(ScanField.FragmentId(setId, i)) != null; i++)
            {
                ScanFragment fragment = scans.Find(ScanField.FragmentId(setId, i));
                if (!fragment.Scanned)
                {
                    return fragment;
                }
            }

            return null;
        }

        /// <summary>B7: the habitat's airlock panel, read from square on.</summary>
        public IEnumerator ReadTheAirlockPanel()
        {
            ScanFragment panel = G.Scans.Find(StoryScan.IdPrefix + "airlock");
            if (panel == null)
            {
                _bot.Fail("the habitat has no airlock panel to read.");
                yield break;
            }

            Vector3 outward = Flat(panel.Point - Ruin("HabitatModules").position).normalized;
            yield return Scan(panel, outward, PanelStandOff);
        }

        /// <summary>Cuts every sealed crate standing by <paramref name="frame"/> (an anchor or a ruin piece).</summary>
        /// <summary>
        /// Like a player who reads "Battery low. Return to your pad.": when the cells run low and a pad stands, drive
        /// back and charge before going on (the routes round the props are longer than a straight line).
        /// </summary>
        public IEnumerator TopUpIfLow()
        {
            BatteryModel battery = G.Battery.Model;
            Vector3? pad = null;
            foreach (PlacedStructure structure in G.PlacedBase.Structures)
            {
                if (structure.Definition.Id == StructureIds.ChargingPad && structure.IsBuilt)
                {
                    pad = structure.Pose.Position;
                }
            }

            if (_bot.Failed || !pad.HasValue || battery.Charge01 > TopUpBelow)
            {
                yield break;
            }

            _bot.Warn("07's cells ran low (" + (battery.Charge01 * 100f).ToString("0", CultureInfo.InvariantCulture) +
                      "%): the bot went back to the pad to charge.");
            yield return _bot.DriveTo(Flat(pad.Value), ApproachThrottleRadius, "the pad to charge", DriveSeconds);
            yield return _bot.Until(() => battery.Charge01 >= TopUpTo, TopUpSeconds, "07's cells to charge on the pad");
        }

        /// <summary>
        /// B7: the first try at <paramref name="frame"/>'s sealed crates, before the igniter is fitted: a held E at the
        /// first seal and the faulted cutter sputters (CUTTER · NO IGNITER), cutting nothing.
        /// </summary>
        public IEnumerator TryTheFaultedCutter(string frame)
        {
            SupplyCaches caches = G.Opening.Caches;
            for (int i = 0; i < caches.Count; i++)
            {
                SupplyCache cache = caches.Get(i);
                SealedContainer crate = G.Containers.Find(cache.Id);
                if (cache.AnchorId != frame || crate.Opened)
                {
                    continue;
                }

                yield return ApproachTheSeal(cache, crate);
                yield return _bot.Hold(Key.E, () =>
                    {
                        _bot.KeepLookingAt(crate.CutPoint.position);
                        return G.StartKit.Sputtered;
                    }, FollowSeconds, "a held E at the seal of " + cache.Id + " to sputter the faulted cutter",
                    () => "Crate candidate " + (G.Containers.Candidate != null ? G.Containers.Candidate.Id : "none") +
                          ", cutter state " + G.StartKit.StateOf(RoverSystem.Cutter) + ".");
                if (!_bot.Failed && crate.Progress > 0f)
                {
                    _bot.Fail("the faulted cutter cut into " + cache.Id + ".");
                }

                yield break;
            }

            _bot.Fail(frame + " has no sealed crate for the cutter to try.");
        }

        public IEnumerator CutTheCrates(string frame)
        {
            SupplyCaches caches = G.Opening.Caches;
            for (int i = 0; !_bot.Failed && i < caches.Count; i++)
            {
                SupplyCache cache = caches.Get(i);
                SealedContainer crate = G.Containers.Find(cache.Id);
                if (cache.AnchorId != frame || crate.Opened)
                {
                    continue;
                }

                yield return ApproachTheSeal(cache, crate);
                yield return _bot.Hold(Key.E, () =>
                    {
                        _bot.KeepLookingAt(crate.CutPoint.position);
                        return crate.Opened;
                    }, SealCutSeconds, "a held E to cut the seal of " + cache.Id,
                    () => "Crate candidate " + (G.Containers.Candidate != null ? G.Containers.Candidate.Id : "none") +
                          ", cutter ready " + G.Cutter.Ready + ".");
                yield return _bot.Until(() => crate.StandsOpen, G.Cargo.ContainerLidSeconds + FollowSeconds,
                    cache.Id + " to stand open");
            }
        }

        /// <summary>
        /// Out to the open ground before <paramref name="crate"/>'s seal, in to it and facing it, until the game makes
        /// it the cut candidate (the cut chip noted on its first use).
        /// </summary>
        private IEnumerator ApproachTheSeal(SupplyCache cache, SealedContainer crate)
        {
            yield return TopUpIfLow();

            Vector3 side = ClearSide(crate.CutPoint.position, crate.SealSide(_bot.World.Rover.Position),
                SealStandOff, crate.Root);
            Vector3 front = Flat(crate.CutPoint.position) + side * SealStandOff;
            if (Clear(crate.CutPoint.position + side * (SealStandOff + SealLineUp), crate.Root))
            {
                // Out to the open ground before the seal first (CrateApproachSessions keeps it clear): a
                // straight run between crates may cross a ruin.
                yield return _bot.DriveTo(front + side * (SealLineUp + SealOpenGround), LineUpArrive,
                    "the open ground before " + cache.Id, DriveSeconds);
                yield return _bot.DriveTo(front + side * SealLineUp, LineUpArrive, "a line-up for " + cache.Id,
                    DriveSeconds);
            }

            yield return _bot.DriveTo(front, SealArrive, "the seal of " + cache.Id, DriveSeconds);
            yield return _bot.DriveTo(Flat(crate.CutPoint.position), SealNose, "nose to the seal of " + cache.Id,
                DriveSeconds);
            yield return _bot.TurnToward(crate.CutPoint.position, "the seal of " + cache.Id);
            if (DriveSteering.FlatDistance(crate.CutPoint.position, _bot.World.Rover.Position) < SealTooClose)
            {
                // Rolled in over the seal (a small crate goes under 07's nose): the cutter in 07's chest cannot
                // see it there, so back off a little and face it again, as a player would.
                Vector3 seal = crate.CutPoint.position;
                yield return _bot.BackOff(() => DriveSteering.FlatDistance(seal, _bot.World.Rover.Position) >=
                                                SealNose, DriveSeconds, "the seal of " + cache.Id);
                yield return _bot.TurnToward(seal, "the seal of " + cache.Id);
            }

            yield return FirstUse("cut: hold E", crate.CutPoint.position);
            yield return WaitForTheSealCandidate(crate.CutPoint.position, cache.Id);
        }

        /// <summary>
        /// Holding E only cuts while the game's candidate is the seal (a scan target in range can win the brackets), so
        /// the bot waits, gaze on the seal, until the candidate is this crate and nudges 07 to try again if not.
        /// </summary>
        private IEnumerator WaitForTheSealCandidate(Vector3 seal, string cacheId)
        {
            for (int attempt = 0; attempt < SealCandidateAttempts; attempt++)
            {
                float deadline = Time.unscaledTime + SealCandidateSeconds;
                while (Time.unscaledTime < deadline && !IsSealCandidate(cacheId))
                {
                    _bot.KeepLookingAt(seal);
                    yield return null;
                }

                if (IsSealCandidate(cacheId))
                {
                    yield break;
                }

                Vector3 away = Flat(_bot.World.Rover.Position - seal);
                away = away.sqrMagnitude > 1e-4f ? away.normalized : Vector3.back;
                yield return _bot.DriveTo(Flat(seal) + away * SealNudgeMetres, SealArrive, "a nudge off the seal of " + cacheId,
                    DriveSeconds);
                yield return _bot.DriveTo(Flat(seal), SealNose, "nose to the seal of " + cacheId, DriveSeconds);
                yield return _bot.TurnToward(seal, "the seal of " + cacheId);
            }

            _bot.Warn("the game never made " + cacheId + " the cut candidate (chip '" + _bot.World.Ui.Chip() + "'); the bot held E anyway.");
        }

        private bool IsSealCandidate(string cacheId)
        {
            return G.Containers.Candidate != null && G.Containers.Candidate.Id == cacheId;
        }

        public IEnumerator UntilReached(OpeningMilestone milestone, string what, float seconds = MilestoneSeconds)
        {
            yield return _bot.Until(() => _milestones.Exists(reached => reached.Milestone == milestone),
                seconds, what, () => "The objective line is '" + G.Opening.Flow.Current + "'." + UnlinkedReason() + " " + BaseState());
        }

        /// <summary>B10: the night-ready base lets the first sunset run.</summary>
        public IEnumerator TheFirstNightfall()
        {
            var sunlight = _bot.World.Context.Get<ISunlight>();
            yield return _bot.Until(() => sunlight.Phase == SkyPhase.Earthlit, SunsetSeconds, "the first night to fall");
        }

        public IEnumerator DriveToTheRuinsGate(string what)
        {
            yield return TopUpIfLow();
            yield return _bot.DriveTo(RuinsGate(), LineUpArrive, what, DriveSeconds);
        }

        /// <summary>The base and the sky in a line, for a failure message: what stands, what it stores, which phase.</summary>
        private string BaseState()
        {
            var text = new System.Text.StringBuilder("Sky " + _bot.World.Context.Get<ISunlight>().Phase + "; base:");
            foreach (PlacedStructure structure in G.PlacedBase.Structures)
            {
                text.Append(' ').Append(structure.Definition.Id).Append(" built=").Append(structure.IsBuilt)
                    .Append(" stored=").Append(structure.StoredWattHours.ToString("0.0", CultureInfo.InvariantCulture))
                    .Append(" linked=").Append(IsLinkedToPad(structure))
                    .Append(" supply=").Append(structure.SupplyRatio.ToString("0.00", CultureInfo.InvariantCulture))
                    .Append(" at ").Append(structure.Pose.Position).Append(';');
            }

            return text.ToString();
        }

        private bool IsLinkedToPad(PlacedStructure structure)
        {
            foreach (PlacedStructure other in G.PlacedBase.Structures)
            {
                if (other.Definition.Id == StructureIds.ChargingPad)
                {
                    return other.InstanceId == structure.InstanceId ||
                           G.PlacedBase.PowerGrid.AreConnected(structure.InstanceId, other.InstanceId);
                }
            }

            return false;
        }

        /// <summary>Why a built structure may not work: standing but outside the pad's cable reach.</summary>
        private string UnlinkedReason()
        {
            foreach (PlacedStructure structure in G.PlacedBase.Structures)
            {
                if (structure.IsBuilt && !IsLinkedToPad(structure))
                {
                    return " " + structure.Definition.Id + " is built but not linked to the pad (out of cable reach).";
                }
            }

            return string.Empty;
        }

        private PlacedStructure LastPlaced()
        {
            PlacedStructure last = default;
            foreach (PlacedStructure structure in G.PlacedBase.Structures)
            {
                last = structure;
            }

            return last;
        }

        /// <summary>Where the built structure <paramref name="structureId"/> stands (the build spot is only where 07 aimed).</summary>
        private Vector3 PlacedPosition(string structureId)
        {
            foreach (PlacedStructure structure in G.PlacedBase.Structures)
            {
                if (structure.Definition.Id == structureId)
                {
                    return structure.Pose.Position;
                }
            }

            _bot.Fail("no " + structureId + " stands in the base.");
            return Vector3.zero;
        }

        public Vector3 Pad()
        {
            return _bot.World.Anchors.TryGet("place.apron", out WorldAnchor apron) ? apron.Position : Vector3.zero;
        }

        public static float Spacing => BaseSpacing;

        public static float BankSpacing => BankPadSpacing;

        /// <summary>The scan verb's first-use key, shared with B3's own scan loop.</summary>
        public const string ScanVerb = "scan: hold E";

        /// <summary>Records that a verb has been used, so its chip is not expected as a first-use hint again.</summary>
        public void NoteUsed(string verb)
        {
            _verbsUsed.Add(verb);
        }

        /// <summary>Notes a verb's chip on its first use (the chip waits for 07's gaze on the target); a missing chip is a guidance gap, not a stop.</summary>
        private IEnumerator FirstUse(string verb, Vector3? gaze = null)
        {
            if (!_verbsUsed.Add(verb))
            {
                yield break;
            }

            float deadline = Time.unscaledTime + ChipSeconds;
            while (_bot.World.Ui.Chip().Length == 0 && Time.unscaledTime < deadline)
            {
                if (gaze.HasValue)
                {
                    _bot.KeepLookingAt(gaze.Value);
                }

                yield return null;
            }

            if (_bot.World.Ui.Chip().Length == 0)
            {
                _bot.Warn("guidance gap: " + verb + " showed no prompt chip on its first use (line on screen: '" +
                          _bot.World.Ui.ObjectiveLine() + "'; " + _bot.World.Ui.DescribeChip() + ").");
            }
        }

        private IEnumerator Scan(ScanFragment fragment, Vector3 preferred, float standOff)
        {
            ScanField scans = G.Scans;
            Vector3 away = ClearSide(fragment.Point, preferred, standOff, null);
            if (Clear(fragment.Point + away * (standOff + ScanLineUp), null))
            {
                yield return _bot.DriveTo(Flat(fragment.Point + away * (standOff + ScanLineUp)), LineUpArrive,
                    "a line-up for a scan", DriveSeconds);
            }

            yield return _bot.DriveTo(Flat(fragment.Point), standOff, "the spot facing " + fragment.FragmentId,
                DriveSeconds);
            yield return _bot.TurnToward(fragment.Point, fragment.FragmentId);
            UiProbe ui = _bot.World.Ui;
            float deadline = Time.unscaledTime + ScanLookSeconds;
            while (!_bot.Failed && !(_bot.KeepLookingAt(fragment.Point) && OnSetCandidate(scans, fragment)))
            {
                if (Time.unscaledTime > deadline)
                {
                    _bot.Fail("the view never settled on " + fragment.FragmentId + " (candidate " +
                              (scans.Candidate != null ? scans.Candidate.FragmentId : "none") + "). " + _bot.Where());
                    yield break;
                }

                yield return null;
            }

            // The brackets may have left between the check and here (a frame's flicker): scan what was framed.
            fragment = scans.Candidate ?? fragment;
            yield return FirstUse(ScanVerb);
            yield return _bot.Hold(Key.E, () =>
                {
                    _bot.KeepLookingAt(fragment.Point);
                    return fragment.Scanned;
                }, scans.Tuning.Seconds(fragment.Size) + ScanAllowanceSeconds, "a held E to scan " + fragment.FragmentId,
                () => "Chip '" + ui.Chip() + "'.");
        }

        /// <summary>The brackets are on this part or another unscanned part of its set: a player scans what they frame.</summary>
        private static bool OnSetCandidate(ScanField scans, ScanFragment fragment)
        {
            ScanFragment candidate = scans.Candidate;
            if (candidate == null)
            {
                return false;
            }

            int split = fragment.FragmentId.LastIndexOf('.');
            bool story = fragment.FragmentId.StartsWith(StoryScan.IdPrefix, System.StringComparison.Ordinal);
            return candidate == fragment || (!story && !candidate.Scanned && split > 0 &&
                   candidate.FragmentId.StartsWith(fragment.FragmentId.Substring(0, split + 1),
                       System.StringComparison.Ordinal));
        }

        /// <summary>
        /// The placing ghost rides the tuned distance ahead of 07, so 07 stands that far from the spot on one side and
        /// faces it; when the rules refuse the spot or the frame would stand out of weld range, 07 stands again, then tries the other sides.
        /// </summary>
        private IEnumerator PutTheGhostOn(BuildField build, Vector3 at, string structureId)
        {
            float ahead = build.Tuning.GhostAheadMetres;
            float reach = build.Tuning.WeldRange - PlaceWeldMargin;
            for (int attempt = 0; attempt < PlaceBearings * 2; attempt++)
            {
                if (attempt > 0)
                {
                    Vector3 side = Quaternion.Euler(0f, attempt / 2 * PlaceBearingStepDegrees, 0f) * Vector3.back;
                    yield return _bot.DriveTo(Flat(at) + side * ahead, PlaceStandArrive, "another side of the build spot",
                        DriveSeconds);
                    yield return _bot.TurnToward(at, "the build spot");
                    if (_bot.Failed)
                    {
                        yield break;
                    }
                }

                // The ghost follows the camera's aim: a player looks at the ground where the structure should stand.
                var terrain = _bot.World.Context.Get<ITerrainQuery>();
                Vector3 ground = new Vector3(at.x, terrain.SampleHeight(at.x, at.z), at.z);
                yield return _bot.LookAt(ground, "the build spot on the ground");
                for (int frame = 0; frame < SettledFrames; frame++)
                {
                    _bot.KeepLookingAt(ground);
                    yield return null;
                }

                if (build.Controller.CanPlaceNow &&
                    DriveSteering.FlatDistance(build.Controller.CandidatePose.Position, at) <= PlaceDriftMetres &&
                    DriveSteering.FlatDistance(build.Controller.CandidatePose.Position, _bot.World.Rover.Position) <= reach)
                {
                    yield break;
                }
            }

            _bot.Fail(structureId + " found no way to put the ghost on " + at + " (candidate " +
                      build.Controller.CandidatePose.Position + ", last reason " + build.Controller.CandidateReason + ").");
        }

        /// <summary>
        /// B7: Tab opens 07's face screen, the arrows walk the focus to the cutter's socket, Enter fits the spare
        /// igniter where 07 stands (a field swap), Tab closes.
        /// </summary>
        public IEnumerator FitTheIgniter()
        {
            if (_bot.Failed)
            {
                yield break;
            }

            RoverStartKit kit = G.StartKit;
            UiProbe ui = _bot.World.Ui;
            yield return FirstUse("fit the igniter: Tab");
            yield return _bot.Tap(Key.Tab);
            yield return _bot.Until(() => ui.FaceFocus().Length > 0, FollowSeconds,
                "the face screen to open with a focus");
            for (int step = 0; !_bot.Failed && step < MaxMenuSteps && ui.FaceFocusedDevice() != CutterDevice; step++)
            {
                int device = ui.FaceFocusedDevice();
                yield return _bot.Tap(device < 0 ? Key.RightArrow : device < CutterDevice ? Key.DownArrow : Key.UpArrow);
            }

            if (ui.FaceFocusedDevice() != CutterDevice)
            {
                _bot.Fail("the arrows never reached the cutter's socket on the face screen (focus '" + ui.FaceFocus() +
                          "').");
                yield break;
            }

            yield return _bot.Tap(Key.Enter);
            yield return _bot.Until(() => !kit.CarriesIgniter, FollowSeconds, "Enter to fit the spare igniter");
            yield return _bot.Tap(Key.Tab);
            var face = _bot.World.Context.Get<IRoverFaceScreen>();
            yield return _bot.Until(() => !face.IsOpen, FollowSeconds, "Tab to close the face screen");
        }

        private static Vector3 ClearSide(Vector3 point, Vector3 preferred, float standOff, Transform own)
        {
            for (int step = 0; step < ApproachSides; step++)
            {
                float turn = (step + 1) / 2 * (360f / ApproachSides) * (step % 2 == 0 ? 1f : -1f);
                Vector3 side = Quaternion.Euler(0f, turn, 0f) * preferred;
                if (Clear(point + side * standOff, own))
                {
                    return side;
                }
            }

            return preferred;
        }

        private static bool Clear(Vector3 spot, Transform own)
        {
            foreach (Collider hit in Physics.OverlapSphere(spot + Vector3.up * ClearHeight, ClearRadius,
                         Layers.PropMask, QueryTriggerInteraction.Ignore))
            {
                if (own == null || !hit.transform.IsChildOf(own))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Open ground off the ruins' south side: the way in and out among them.</summary>
        private Vector3 RuinsGate()
        {
            return Flat(Ruin("FallenSolarArray").position) + Vector3.back * RuinsGateOffset;
        }

        private Transform Ruin(string id)
        {
            foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Transform ruins = root.transform.Find("Ruins");
                Transform piece = ruins != null ? ruins.Find(id) : null;
                if (piece != null)
                {
                    return piece;
                }
            }

            _bot.Fail("the Main scene has no ruin piece " + id + ".");
            return _bot.World.Gameplay.transform;
        }

        private static Vector3 Flat(Vector3 v)
        {
            return new Vector3(v.x, 0f, v.z);
        }
    }
}
