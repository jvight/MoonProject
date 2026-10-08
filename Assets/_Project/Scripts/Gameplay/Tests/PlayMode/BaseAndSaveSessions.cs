using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Scripted sessions for home carrying across the basin, the museum deposit, the radio tower shop and its service
    /// port, Kenji's Rover Bay, the charging dock, the hint query and save/load.
    /// </summary>
    public sealed class BaseAndSaveSessions : InputTestFixture
    {
        /// <summary>Seconds a held tether press may take to latch (a slow first frame must not eat it).</summary>
        private const float LatchTimeout = 1f;

        private const string Duck = "rubber_duck";
        private const string Teapot = "teapot";
        private const string Tower = "radio_tower";
        private const string HoverJump = "rover.hover_jump";

        private InputActionAsset _controls;
        private GameplayFixture _fixture;
        private Mouse _mouse;

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Home_CarriesAcrossTheBasin_WindowsHold_LampsDim_AndAHaloShowsFromAfar()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            HomeBase home = _fixture.Gameplay.Home;
            BaseTuning tuning = home.Tuning;
            Vector3 lander = home.LanderPosition;
            _fixture.Rover.Place(lander + new Vector3(8f, 0f, 0f), 0f);
            yield return new WaitForSeconds(tuning.WarmEase * 3f);
            float homeWindows = home.WindowLevel;
            float homeWarmth = home.Warmth;
            Assert.AreEqual(tuning.WindowGlow, homeWindows, 0.05f, "the windows at their glow while 07 is home");
            Assert.AreEqual(1f, homeWarmth, 0.05f, "the lamps light the ground");
            RenderTheView();
            Assert.AreEqual(0f, home.Halo.Level, "no halo over the base while 07 is there");

            _fixture.Rover.Place(lander + new Vector3(0f, 0f, -250f), 0f);
            yield return new WaitForSeconds(tuning.WarmEase * 3f);
            Assert.Less(home.Warmth, homeWarmth - 0.3f, "the lamps on the ground dim to a light left on");
            Assert.Greater(home.WindowLevel, homeWindows, "the windows glow a little brighter, calling 07 home");
            RenderTheView();
            Assert.Greater(home.Halo.Level, 0.9f * tuning.HaloGlow, "a soft amber halo marks home from afar");
            Assert.Greater(home.Halo.Radius, tuning.HaloRadius, "grown with the distance");
        }

        [UnityTest]
        public IEnumerator Deposit_TowedRelicShowsItsSlot_ThenFloatsOntoTheShelfWithAGift()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            HomeBase home = _fixture.Gameplay.Home;
            Relic duck = Loose(Duck, new Vector3(12f, 0.6f, 0f));
            _fixture.Rover.Place(new Vector3(6f, 0f, 0f), 90f);
            yield return new WaitForSeconds(0.8f);
            Vector3 camera = _fixture.Rover.Camera.transform.position;
            _fixture.Rover.Aim(camera, duck.transform.position);
            yield return null;
            yield return null;
            Assert.AreSame(duck, _fixture.Gameplay.Tether.Hovered,
                $"duck {duck.State} at {duck.transform.position}, camera {camera}");
            Press(_mouse.rightButton, queueEventOnly: true);
            yield return Waits.Until(() => _fixture.Events.TetherAttached.Count > 0, LatchTimeout);
            Assert.AreEqual(1, _fixture.Events.TetherAttached.Count);

            Vector3 from = _fixture.Rover.Position;
            Vector3 to = new Vector3(-14f, 0f, 6f);
            float began = Time.time;
            while (Time.time - began < 5f)
            {
                _fixture.Rover.MoveTo(Vector3.Lerp(from, to, Ease.InOutSine((Time.time - began) / 4.5f)), -90f);
                yield return null;
            }

            Assert.IsTrue(home.InDepositZone(duck.transform.position), "towed into reach of the shelf");
            Assert.GreaterOrEqual(home.HintedSlot, 0, "a warm glow shows which slot it will take");
            Assert.Greater(home.HintLevel, 0.3f);
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Deposit, out InteractionHint hint));
            Assert.AreEqual(InteractionKind.Deposit, _fixture.Gameplay.Hints.Primary.Kind);
            Assert.IsTrue(hint.Ready);
            int slot = home.HintedSlot;
            AimAtShelf(home);
            _fixture.Capture("08-deposit-hint");

            int before = _fixture.Events.Order.Count;
            Release(_mouse.rightButton);
            yield return new WaitForSeconds(_fixture.BaseTuning.DepositDuration + 0.4f);
            Assert.AreEqual(1, _fixture.Events.RelicDeposited.Count, "it floats onto the shelf");
            RelicDeposited deposited = _fixture.Events.RelicDeposited[0].Value;
            Assert.AreEqual(Duck, deposited.RelicId);
            Assert.AreEqual(1, deposited.DisplayedCount);
            Assert.AreEqual(RelicState.Displayed, duck.State);
            Assert.AreEqual(slot, duck.Slot, "it took the slot that was shown");
            Assert.AreEqual(1, home.DisplayedCount);
            Assert.AreEqual(nameof(TetherReleased), _fixture.Events.Order[before]);
            Assert.IsEmpty(_fixture.Events.MaterialsChanged, "memories are not paid for: materials come from salvage");
            Assert.Less(Vector3.Distance(duck.transform.position, deposited.Position), 0.05f, "settled on its slot");
            Assert.IsTrue(System.IO.File.Exists(_fixture.Bootstrap.Context.Get<ISaveService>().FilePath),
                "a deposit is a save checkpoint");
            _fixture.Capture("09-deposited");
        }

        [UnityTest]
        public IEnumerator Tower_BuysALevelOnItsPad_07FeedsItsPort_StitchesWhileTheNextStageGrows()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            RadioTower tower = _fixture.Gameplay.Tower;
            RadioTowerTuning tuning = _fixture.TowerTuning;
            IUpgradeShop shop = _fixture.Bootstrap.Context.Get<IUpgradeShop>();
            Assert.AreEqual(0, tower.ShownLevel);
            Assert.AreEqual(0, tower.ActiveStage);
            Assert.Less(tower.BeaconLevel, 0.01f, "the old mast stands dark");
            Assert.Less(tower.HatchOpenAngle, 0.5f, "its service hatch is shut");
            Vector3 port = tower.HopperMouth;
            float clear = SurfaceRules.HorizontalDistance(tower.PadCentre, port) - tuning.PadRadius;
            Assert.Greater(clear, 0.2f, "the pad's ring stays clear of the service port");

            _fixture.GiveMaterials(0, 2, 3);
            Assert.AreEqual(PurchaseResult.NotAtStation, shop.Purchase(Tower), "bought on the pad, not anywhere");
            _fixture.Rover.Place(tower.PadCentre, Yaw(Flat(port) - Flat(tower.PadCentre)));
            yield return null;
            yield return null;
            Assert.IsTrue(shop.IsAtStation);
            Assert.AreSame(_fixture.RadioTowerUpgrade, shop.StationUpgrade);
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Upgrade, out InteractionHint hint));
            Assert.IsTrue(hint.Ready, "affordable");
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(tower.PadLevel, 0.8f * tuning.PadOccupied, "the pad glows under 07");

            int before = _fixture.Events.Order.Count;
            int cues = _fixture.Events.StationCued.Count;
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase(Tower));
            CollectionAssert.AreEqual(new[]
            {
                nameof(MaterialsChanged), nameof(UpgradePurchased), nameof(SignalRadiusChanged),
            }, _fixture.Events.Order.GetRange(before, 3));
            Assert.AreEqual(1, _fixture.Events.UpgradePurchased[0].Value.Level);
            Assert.AreEqual(110f, _fixture.Events.SignalRadiusChanged[0].Value.Radius);
            Assert.IsTrue(tower.Crafting && tower.Feeding, "07's beam feeds the service port's hopper");
            Assert.AreEqual(1, _fixture.Rover.HoldStillCount, "07 holds still while it works");
            Assert.IsTrue(_fixture.Rover.TryGetGaze(tower, out Vector3 gaze, out int priority));
            Assert.Less(Vector3.Distance(port, gaze), 1e-3f, "and looks at the hopper");
            Assert.AreEqual(GazePriorities.Focus, priority);
            StationCued feed = _fixture.Events.StationCued[cues].Value;
            Assert.AreEqual(StationCue.FeedStarted, feed.Cue);
            Assert.AreEqual(Tower, feed.UpgradeId);
            Assert.Less(Vector3.Distance(port, feed.Position), 1e-3f);

            FeedLook look = tuning.FeedLook;
            yield return new WaitForSeconds(look.BeamLead + look.Flight * 0.5f);
            Assert.Greater(tower.FeedBeamLevel, 0.5f, "the beam reaches the hopper");
            Assert.Greater(tower.BundlesInFlight, 0, "wiring and optics fly in along it");
            Assert.Less(tower.HatchOpenAngle, 0.5f, "the hatch waits for the materials");
            Assert.AreEqual(0, tower.ShownLevel, "nothing grows before the hopper is fed");
            AimAtPort(tower);
            _fixture.Capture("10a-tower-port-feed");

            yield return Waits.Until(() => Cued(StationCue.Fed, cues), 3f);
            Assert.IsTrue(Cued(StationCue.HatchOpened, cues), "fed: the hatch swings open");
            yield return new WaitForSeconds(tuning.HatchTime + 0.1f);
            Assert.AreEqual(tuning.HatchAngle, tower.HatchOpenAngle, 1f, "wide open");
            Assert.IsTrue(Cued(StationCue.StitchStarted, cues), "and 07's beam starts stitching");
            Assert.AreEqual(1, tower.ShownLevel, "the level shows as the stitching starts");
            yield return new WaitForSeconds(0.3f);
            Assert.Greater(tower.StitchLevel, 0.5f, "the stitching beam is bright");
            Assert.Less(tower.FeedBeamLevel, 0.1f, "the feed beam has let go");
            Assert.Greater(tower.BeaconLevel, 0.5f, "its beacon glows again");
            AimAtPort(tower);
            _fixture.Capture("10b-tower-port-stitch");

            yield return Waits.Until(() => !tower.Crafting, TowerPortMoment.Duration(tuning.HatchTime,
                TowerPortMoment.StitchDuration(tuning.FlareDuration, tuning.GrowDuration)) + 1f);
            Assert.IsFalse(tower.Crafting, "the moment ends");
            AssertCues(cues, Tower, StationCue.FeedStarted, StationCue.Fed, StationCue.HatchOpened,
                StationCue.StitchStarted, StationCue.HatchClosed);
            Assert.Less(tower.HatchOpenAngle, 0.5f, "the hatch is shut again");
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount, "07 is free to drive");
            Assert.IsFalse(_fixture.Rover.TryGetGaze(tower, out _, out _), "and to look around");
            Assert.AreEqual(0, tower.ActiveStage, "level 1 wakes the old mast");
            yield return new WaitForSeconds(0.3f);
            Assert.Less(tower.StitchLevel, 0.05f, "the beam has faded");

            cues = _fixture.Events.StationCued.Count;
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase(Tower));
            yield return Waits.Until(() => Cued(StationCue.StitchStarted, cues), 5f);
            Assert.AreEqual(0, tower.ActiveStage, "the old stage stays while the beacon flares");
            yield return new WaitForSeconds(tuning.FlareDuration + 0.3f);
            Assert.AreEqual(1, tower.ActiveStage, "then the next stage grows in as 07 stitches");
            Assert.Greater(tower.HatchOpenAngle, tuning.HatchAngle - 1f, "the new stage's hatch stands open too");
            Vector3 pad = tower.PadCentre;
            _fixture.Rover.Aim(pad + new Vector3(9f, 6f, -9f), pad + Vector3.up * 3f);
            _fixture.Capture("10-tower-upgrade");
            yield return Waits.Until(() => !tower.Crafting, 5f);
            Assert.AreEqual(1, tower.ActiveStage);
            Assert.AreEqual(170f, _fixture.Events.SignalRadiusChanged[1].Value.Radius);
            Assert.AreEqual(0, _fixture.Gameplay.Materials.Total, "exactly the two recipes were spent");
            yield return new WaitForSeconds(1f);
            _fixture.Capture("11-tower-level-2");
        }

        [UnityTest]
        public IEnumerator Bay_SellsItsKitInOrderOnItsTurntable_07FeedsItsHopper_ThenItFits_AndGrantsItAgainOnLoad()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            Workshop bay = _fixture.Gameplay.Workshop;
            RadioTower tower = _fixture.Gameplay.Tower;
            IUpgradeShop shop = _fixture.Bootstrap.Context.Get<IUpgradeShop>();
            PadLook look = _fixture.WorkshopTuning.PadLook;
            WorkshopTuning tuning = _fixture.WorkshopTuning;
            Assert.AreSame(_fixture.HoverJumpUpgrade, bay.Definition, "the bay's first offer");
            Assert.AreEqual(tuning.LampIdle, bay.LampLevel, 1e-3f, "Kenji's work lamps are left on");
            Assert.AreEqual(tuning.SignGlow, bay.SignLevel, 1e-3f, "the bay's sign glows as a landmark");
            Assert.AreEqual(0, bay.SparkCount, "no sparks before a purchase");
            Assert.IsFalse(_fixture.Rover.Has(RoverAbility.HoverJump));
            Assert.Greater(SurfaceRules.HorizontalDistance(bay.PadCentre, tower.PadCentre),
                look.Radius + _fixture.TowerTuning.PadRadius, "the two pads never overlap");
            Assert.AreEqual(0.15f, bay.PadCentre.y - bay.BayPosition.y, 1e-3f, "the pad lies on the turntable");

            _fixture.GiveMaterials(5, 2, 1);
            Assert.AreEqual(PurchaseResult.NotAtStation, shop.Purchase(HoverJump), "bought in the bay, not anywhere");
            _fixture.Rover.Place(tower.PadCentre, 0f);
            yield return null;
            yield return null;
            Assert.AreSame(_fixture.RadioTowerUpgrade, shop.StationUpgrade);
            Assert.AreEqual(PurchaseResult.NotAtStation, shop.Purchase(HoverJump), "not at the tower either");

            _fixture.Rover.Place(Flat(bay.PadCentre) + bay.BayForward * (look.Radius + 0.5f), 0f);
            yield return null;
            yield return null;
            Assert.IsFalse(bay.Occupied, "just outside the turntable is not in the bay");
            _fixture.Rover.Place(Flat(bay.PadCentre), Yaw(-bay.BayForward));
            yield return null;
            yield return null;
            Assert.IsTrue(bay.Occupied);
            Assert.AreSame(_fixture.HoverJumpUpgrade, shop.StationUpgrade);
            Assert.AreEqual(UpgradeStationKind.Workshop, shop.StationUpgrade.Station);
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Upgrade, out InteractionHint hint));
            Assert.IsTrue(hint.Ready, "affordable");
            Assert.AreEqual(bay.PadCentre, hint.Position);
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(bay.PadLevel, 0.8f * look.Occupied, "the pad glows under 07");
            Assert.Greater(bay.LampLevel, 0.9f * tuning.LampOccupied, "the lamps lean in");

            int before = _fixture.Events.Order.Count;
            int cues = _fixture.Events.StationCued.Count;
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase(HoverJump));
            int balance = _fixture.Gameplay.Materials.Total;
            Assert.AreEqual(1, balance, "Hover-Jump takes 4 metal, 2 wiring and 1 optics: one metal is left");
            CollectionAssert.AreEqual(new[] { nameof(MaterialsChanged), nameof(UpgradePurchased) },
                _fixture.Events.Order.GetRange(before, _fixture.Events.Order.Count - before));
            Assert.AreEqual(HoverJump, _fixture.Events.UpgradePurchased[0].Value.UpgradeId);
            Assert.AreEqual(1, _fixture.Events.UpgradePurchased[0].Value.Level);
            Assert.IsTrue(_fixture.Rover.Has(RoverAbility.HoverJump), "07 can leap now");
            Assert.Greater(bay.PadLevel, look.Occupied, "the purchase flares the pad");
            Assert.AreSame(_fixture.CargoCradleUpgrade, bay.Definition, "then the bay offers the Cargo Cradle");
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Upgrade, out hint));
            Assert.IsFalse(hint.Ready, "4 metal it does not have yet");
            Assert.AreEqual(PurchaseResult.Maxed, shop.Purchase(HoverJump));

            Assert.IsTrue(bay.Feeding, "07's beam reaches for the bay's hopper");
            Assert.AreEqual(1, _fixture.Rover.HoldStillCount, "07 holds still on the turntable");
            Assert.IsTrue(_fixture.Rover.TryGetGaze(bay, out Vector3 gaze, out _));
            Assert.Less(Vector3.Distance(bay.HopperMouth, gaze), 1e-3f, "and looks at the hopper");
            StationCued feed = _fixture.Events.StationCued[cues].Value;
            Assert.AreEqual(StationCue.FeedStarted, feed.Cue);
            Assert.AreEqual(HoverJump, feed.UpgradeId);
            Assert.Less(Vector3.Distance(bay.HopperMouth, feed.Position), 1e-3f);
            FeedLook feedLook = tuning.FeedLook;
            yield return new WaitForSeconds(feedLook.BeamLead + feedLook.Stagger * 1.5f);
            Assert.Greater(bay.FeedBeamLevel, 0.5f, "the beam reaches the hopper");
            Assert.AreEqual(2, bay.BundlesInFlight, "metal and wiring are on their way, optics next");
            Assert.AreEqual(0, _fixture.Events.RoverBayFitting.Count, "nothing is fitted before the hopper is fed");
            Assert.AreEqual(0, bay.SparkCount);
            Vector3 front = bay.BayForward;
            _fixture.Rover.Aim(bay.BayPosition + front * 7f + Vector3.Cross(Vector3.up, front) * 3f + Vector3.up * 2.6f,
                bay.BayPosition + Vector3.up);
            _fixture.Capture("18a-bay-hopper-feed");

            yield return Waits.Until(() => _fixture.Events.RoverBayFitting.Count > 0,
                HopperFeed.DurationFor(3, feedLook) + 0.5f);
            AssertCues(cues, HoverJump, StationCue.FeedStarted, StationCue.Fed);
            RoverBayFitting fitting = _fixture.Events.RoverBayFitting[0].Value;
            Assert.AreEqual(HoverJump, fitting.UpgradeId);
            var rig = _fixture.Bootstrap.Context.Get<IRoverBay>();
            Assert.AreSame(bay, rig, "the bay itself is registered for the rover to drive its arms");
            Assert.AreEqual(3, rig.ArmCount);
            Assert.AreEqual("Yaw", rig.GetArmJoint(1, RoverBayJoint.Yaw).name);
            Assert.AreEqual("SparkSocket", rig.GetArmJoint(1, RoverBayJoint.SparkSocket).name);
            Assert.AreEqual("FloorTip", rig.FloorTip.name);
            Assert.Less(Vector3.Distance(bay.PadCentre, rig.TurntablePosition), 0.2f, "07 parks on the turntable");
            Assert.IsFalse(bay.Feeding);
            Assert.AreEqual(0, bay.BundlesInFlight, "every bundle went in");
            Assert.Greater(bay.LampLevel, tuning.LampOccupied * 2f, "the lamps flare as the bay starts fitting");
            yield return new WaitForSeconds(tuning.SparkInterval * 1.5f);
            Assert.Greater(bay.SparkCount, tuning.SparkCount * 3, "weld sparks fly from every arm's tip");
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount, "07 is free again");
            Assert.IsFalse(_fixture.Rover.TryGetGaze(bay, out _, out _));
            _fixture.Capture("18-bay-hover-jump");

            _fixture.GiveMaterials(5, 2, 4);
            UpgradeDefinition[] kit =
            {
                _fixture.CargoCradleUpgrade, _fixture.WarmHeadlampUpgrade, _fixture.BoostCoilsUpgrade,
            };
            RoverAbility[] abilities = { RoverAbility.CargoCradle, RoverAbility.WarmHeadlamp, RoverAbility.BoostCoils };
            for (int i = 0; i < kit.Length; i++)
            {
                Assert.AreSame(kit[i], bay.Definition, "the bay offers its kit in order");
                Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase(kit[i].Id));
                Assert.IsTrue(_fixture.Rover.Has(abilities[i]), $"{kit[i].Id} grants its ability");
            }

            Assert.AreEqual(0, _fixture.Gameplay.Materials.Total, "the kit takes exactly its recipes");
            balance = 0;
            Assert.IsFalse(_fixture.Gameplay.Hints.TryGet(InteractionKind.Upgrade, out _), "nothing left to buy");
            yield return Waits.Until(() => _fixture.Events.RoverBayFitting.Count == 4,
                3f * HopperFeed.DurationFor(3, feedLook) + 1f);
            for (int i = 0; i < kit.Length; i++)
            {
                Assert.AreEqual(kit[i].Id, _fixture.Events.RoverBayFitting[i + 1].Value.UpgradeId,
                    "pieces bought together are fed and fitted one after another, in order");
            }

            yield return new WaitForSeconds(tuning.SparkInterval * tuning.SparkBursts + tuning.SparkLifetime.y + 0.5f);
            Assert.AreEqual(0, bay.SparkCount, "the sparks die out (an idle bay costs nothing)");
            Assert.Less(bay.PadLevel, look.Occupied * 0.5f, "an empty bay's pad rests dim");
            Assert.AreEqual(tuning.LampOccupied, bay.LampLevel, 0.05f, "the lamps settle back");
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount);

            _fixture.Dispose(true);
            yield return null;
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            Assert.AreEqual(1, _fixture.Gameplay.Upgrades.LevelOf(HoverJump), "the purchase was a checkpoint");
            Assert.IsTrue(_fixture.Rover.Has(RoverAbility.HoverJump), "granted again on load");
            Assert.IsTrue(_fixture.Rover.Has(RoverAbility.CargoCradle));
            Assert.IsTrue(_fixture.Rover.Has(RoverAbility.WarmHeadlamp));
            Assert.IsTrue(_fixture.Rover.Has(RoverAbility.BoostCoils));
            Assert.AreEqual(0, _fixture.Events.UpgradePurchased.Count, "a load is never a purchase");
            Assert.AreEqual(0, _fixture.Events.StationCued.Count, "nor a feed");
            Assert.AreEqual(balance, _fixture.Gameplay.Materials.Total, "the checkpoint kept the change");
        }

        [UnityTest]
        public IEnumerator Dock_07RestsWhenItStopsOnIt_GlowsWhileItCharges_AndLeavesOnAnyDriveInput()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            BaseTuning tuning = _fixture.BaseTuning;
            ChargingDock dock = _fixture.Gameplay.Home.Dock;
            Assert.IsFalse(dock.Docked);
            Assert.AreEqual(tuning.DockIdleGlow, dock.GlowLevel, 1e-3f, "the dock waits with a soft glow");
            Vector3 anchor = Flat(dock.Position);
            Vector3 facing = dock.Rotation * Vector3.forward;
            Assert.Less(Vector3.Angle(facing, Flat(_fixture.Gameplay.Home.LanderPosition) - anchor), 1f,
                "07 rests facing the lander");

            Vector3 approach = anchor - facing * 6f;
            float start = Time.time;
            while (Time.time - start < 1f)
            {
                _fixture.Rover.MoveTo(Vector3.Lerp(approach, anchor + facing * 6f, (Time.time - start) / 1f),
                    Yaw(facing));
                yield return null;
            }

            Assert.AreEqual(0, _fixture.Events.RoverDockChanged.Count, "driving over the dock never docks");

            Vector3 parked = anchor - facing * (tuning.DockRadius * 0.5f);
            _fixture.Rover.Place(parked, Yaw(facing));
            yield return new WaitForSeconds(tuning.DockDelay * 0.5f);
            Assert.IsFalse(dock.Docked, "it waits a moment first");
            yield return new WaitForSeconds(tuning.DockDelay * 0.5f + 0.2f);
            Assert.IsTrue(dock.Docked, "stopped on the dock, no input: 07 rests");
            Assert.AreEqual(1, _fixture.Events.RoverDockChanged.Count);
            RoverDockChanged rest = _fixture.Events.RoverDockChanged[0].Value;
            Assert.IsTrue(rest.Docked);
            Assert.Less(Vector3.Distance(dock.Position, rest.Position), 1e-3f, "settled onto the dock's anchor");
            Assert.Less(Quaternion.Angle(dock.Rotation, rest.Rotation), 0.1f);
            Assert.AreEqual(0, _fixture.Rover.Placements, "the rover settles 07 itself: gameplay never snaps it");
            yield return new WaitForSeconds(tuning.DockGlowEase * 3f);
            Assert.Greater(dock.GlowLevel, tuning.DockChargingGlow * (1f - tuning.DockBreathDepth) * 0.9f,
                "the dock's glow warms while 07 charges");
            _fixture.Rover.Aim(anchor + facing * -5f + Vector3.Cross(Vector3.up, facing) * 3f + Vector3.up * 2.5f,
                anchor + Vector3.up * 0.5f);
            _fixture.Capture("12a-charging-dock");

            _fixture.Rover.DriveInput = new Vector2(0f, 0.3f);
            yield return null;
            Assert.IsFalse(dock.Docked, "any drive input leaves at once");
            Assert.AreEqual(2, _fixture.Events.RoverDockChanged.Count);
            Assert.IsFalse(_fixture.Events.RoverDockChanged[1].Value.Docked);
            _fixture.Rover.DriveInput = new Vector2(0.4f, 0f);
            yield return new WaitForSeconds(tuning.DockDelay + 0.3f);
            Assert.IsFalse(dock.Docked, "steering alone keeps it awake");
            _fixture.Rover.DriveInput = Vector2.zero;
            yield return new WaitForSeconds(tuning.DockDelay + 0.3f);
            Assert.IsTrue(dock.Docked, "let go again and it settles back in");
            Assert.AreEqual(3, _fixture.Events.RoverDockChanged.Count);

            _fixture.Rover.Place(anchor - facing * (tuning.DockRadius + 1f), Yaw(facing));
            yield return null;
            Assert.IsFalse(dock.Docked, "moved off the dock (a recovery, a hop): the rest ends");
            Assert.AreEqual(4, _fixture.Events.RoverDockChanged.Count);
            yield return new WaitForSeconds(tuning.DockGlowEase * 5f);
            Assert.AreEqual(tuning.DockIdleGlow, dock.GlowLevel, 0.05f, "the glow cools back to waiting");
        }

        [UnityTest]
        public IEnumerator SaveAndReload_BringsBackEveryPieceOfProgress()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            _fixture.GiveMaterials(3, 3, 3);

            HomeBase home = gameplay.Home;
            Vector3 towardPad = (Vector3.zero - home.ShelfPosition).normalized;
            Relic duck = Loose(Duck, home.ShelfPosition + towardPad * 3f + Vector3.up * 0.6f);
            yield return new WaitForSeconds(_fixture.BaseTuning.DepositDuration + 0.5f);
            Assert.AreEqual(RelicState.Displayed, duck.State, "a relic resting by the shelf is taken in too");
            int duckSlot = duck.Slot;

            Relic teapot = _fixture.FindRelic(Teapot);
            teapot.MarkDiscovered();
            teapot.BeginLift();
            Vector3 halfway = teapot.Site.Position + Vector3.up * 0.4f;
            teapot.SetLiftPose(halfway, Quaternion.identity, 0.4f);
            teapot.PauseLift();

            _fixture.Rover.Place(gameplay.Tower.PadCentre, 0f);
            yield return null;
            yield return null;
            Assert.AreEqual(PurchaseResult.Purchased, _fixture.Bootstrap.Context.Get<IUpgradeShop>().Purchase(Tower));
            Assert.IsTrue(_fixture.Bootstrap.Context.Get<ISaveService>().SaveNow());
            _fixture.Dispose(true);
            yield return null;

            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            gameplay = _fixture.Gameplay;
            Assert.AreEqual(3, gameplay.Materials.Metal, "materials, less the tower's recipe");
            Assert.AreEqual(2, gameplay.Materials.Wiring);
            Assert.AreEqual(2, gameplay.Materials.Optics);
            Relic duckAgain = _fixture.FindRelic(Duck);
            Assert.AreEqual(RelicState.Displayed, duckAgain.State, "the museum keeps its relics");
            Assert.AreEqual(duckSlot, duckAgain.Slot);
            Assert.AreEqual(1, gameplay.Home.DisplayedCount);
            Relic teapotAgain = _fixture.FindRelic(Teapot);
            Assert.AreEqual(RelicState.Surfacing, teapotAgain.State, "a half-lifted relic waits where it was");
            Assert.AreEqual(0.4f, teapotAgain.Progress, 1e-4f);
            Assert.IsTrue(teapotAgain.Discovered);
            Assert.Less(Vector3.Distance(teapotAgain.transform.position, halfway), 0.1f);
            Assert.AreEqual(1, gameplay.Upgrades.LevelOf(Tower));
            Assert.AreEqual(1, gameplay.Tower.ShownLevel);
            Assert.AreEqual(0, _fixture.Events.UpgradePurchased.Count, "a load is never a purchase");
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(gameplay.Tower.BeaconLevel, 0.5f, "the beacon is lit straight away");
            AimAtShelf(gameplay.Home);
            _fixture.Capture("12-reloaded-museum");
        }

        /// <summary>The review camera out in front of the tower's service port, a little aside.</summary>
        private void AimAtPort(RadioTower tower)
        {
            Vector3 port = tower.HopperMouth;
            Vector3 front = Flat(tower.PadCentre - port).normalized;
            _fixture.Rover.Aim(port + front * 6f + Vector3.Cross(Vector3.up, front) * 3f + Vector3.up * 2.5f,
                port + Vector3.up * 0.5f);
        }

        private bool Cued(StationCue cue, int from)
        {
            for (int i = from; i < _fixture.Events.StationCued.Count; i++)
            {
                if (_fixture.Events.StationCued[i].Value.Cue == cue)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The station cues after the first <paramref name="from"/> are exactly these, all for it.</summary>
        private void AssertCues(int from, string upgradeId, params StationCue[] expected)
        {
            Assert.AreEqual(from + expected.Length, _fixture.Events.StationCued.Count, "each beat once");
            for (int i = 0; i < expected.Length; i++)
            {
                StationCued cued = _fixture.Events.StationCued[from + i].Value;
                Assert.AreEqual(expected[i], cued.Cue, $"beat {i}");
                Assert.AreEqual(upgradeId, cued.UpgradeId, $"beat {i} is for {upgradeId}");
            }
        }

        private static Vector3 Flat(Vector3 point)
        {
            return new Vector3(point.x, 0f, point.z);
        }

        private static float Yaw(Vector3 direction)
        {
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        private void AimAtShelf(HomeBase home)
        {
            Vector3 shelf = home.ShelfPosition;
            Vector3 front = (Vector3.zero - shelf).normalized;
            _fixture.Rover.Aim(shelf + front * 6f + Vector3.up * 2.5f, shelf + Vector3.up);
        }

        /// <summary>Renders 07's view once (the halo places itself for each camera as it renders).</summary>
        private void RenderTheView()
        {
            Object.Destroy(FrameCapture.Render(_fixture.Rover.Camera, 64, 36));
        }

        private Relic Loose(string id, Vector3 position)
        {
            Relic relic = _fixture.FindRelic(id);
            relic.BeginLift();
            relic.SetLiftPose(position, Quaternion.identity, 1f);
            relic.Surface(Vector3.zero, Vector3.zero);
            return relic;
        }
    }
}
