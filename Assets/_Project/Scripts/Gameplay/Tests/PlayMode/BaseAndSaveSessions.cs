using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Save;
using MoonProject.Gameplay.Lift;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Scripted sessions for home carrying across the basin, the keepsake deposit up in the lab, the radio tower shop
    /// and its service port, Kenji's Rover Bay, the charging dock, the hint query and save/load.
    /// </summary>
    public sealed class BaseAndSaveSessions : InputTestFixture
    {
        /// <summary>Seconds a held tether press may take to latch: a slow first frame or the loose duck settling out
        /// of the aim cone for a moment must not eat it (the held button latches as soon as it is back).</summary>
        private const float LatchTimeout = 3f;

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
            Assert.AreEqual(0f, home.WindowLevel, 1e-3f, "asleep, home's windows are dark (M3-16)");
            _fixture.PowerTo(1);
            yield return new WaitForSeconds(tuning.WarmEase * 3f);
            float homeWindows = home.WindowLevel;
            float homeWarmth = home.Warmth;
            Assert.AreEqual(tuning.WindowGlow * _fixture.Gameplay.Upgrades.LightBoost, homeWindows, 0.05f,
                "powered, the windows at their glow (with the tower's boost) while 07 is home");
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
        public IEnumerator Deposit_ATowedRelicRidesUpBeside07_ShowsItsCubby_ThenThePressFloatsItOntoTheShelf()
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

            LiftStation lift = _fixture.Gameplay.Lift;
            Vector3 from = _fixture.Rover.Position;
            Vector3 to = lift.RideSpot - lift.RideHeading * 3f;
            float began = Time.time;
            while (Time.time - began < 5f)
            {
                _fixture.Rover.MoveTo(Vector3.Lerp(from, to, Ease.InOutSine((Time.time - began) / 4.5f)),
                    Yaw(Flat(to - from)));
                yield return null;
            }

            Assert.AreSame(duck, _fixture.Gameplay.Tether.Towed, "towed to the belly lift");
            Assert.IsFalse(_fixture.Gameplay.Hints.TryGet(InteractionKind.Deposit, out _),
                "nothing to place outside: the keepsakes are up in the lab");
            yield return _fixture.RideIntoTheLab();
            Assert.AreEqual(LiftPhase.Inside, lift.Phase, "the lift carried 07 up into the lab");
            Assert.AreSame(duck, lift.Rider, "the towed duck rode up beside 07");
            Assert.AreEqual(RelicState.Riding, duck.State);
            Assert.IsNull(_fixture.Gameplay.Tether.Towed, "the tether let it go aboard");
            Assert.AreEqual(LabStation.Keepsakes, lift.Lab.Facing, "the turntable turned to the shelf that wants it");
            Assert.IsTrue(home.KeepsakesFaced);
            yield return new WaitForSeconds(_fixture.BaseTuning.SlotHintEase * 4f);
            Assert.GreaterOrEqual(home.HintedSlot, 0, "a warm glow shows which cubby it will take");
            Assert.Greater(home.HintLevel, 0.3f);
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Deposit, out InteractionHint hint));
            Assert.AreEqual(InteractionKind.Deposit, _fixture.Gameplay.Hints.Primary.Kind);
            Assert.IsTrue(hint.Ready);
            Assert.AreEqual(home.KeepsakesPosition, hint.Position);
            int slot = home.HintedSlot;
            _fixture.Capture("08-deposit-hint");

            int before = _fixture.Events.Order.Count;
            Release(_mouse.rightButton);
            yield return null;
            Press(_mouse.rightButton, queueEventOnly: true);
            yield return Waits.Until(() => duck.State == RelicState.Depositing, LatchTimeout);
            Release(_mouse.rightButton);
            Assert.AreEqual(RelicState.Depositing, duck.State, "the press lifts it into its outline");
            Assert.IsNull(lift.Rider);
            Assert.IsEmpty(_fixture.Events.TetherAttached.GetRange(1, _fixture.Events.TetherAttached.Count - 1),
                "and reaches for nothing else");
            yield return new WaitForSeconds(_fixture.BaseTuning.DepositDuration + 0.4f);
            Assert.AreEqual(1, _fixture.Events.RelicDeposited.Count, "it floats onto the shelf");
            RelicDeposited deposited = _fixture.Events.RelicDeposited[0].Value;
            Assert.AreEqual(Duck, deposited.RelicId);
            Assert.AreEqual(1, deposited.DisplayedCount);
            Assert.AreEqual("kenji", deposited.CrewSet, "the duck is Kenji's");
            Assert.AreEqual(1, deposited.SetCount, "Kenji's things: 1...");
            Assert.AreEqual(1, deposited.SetSize, "...of 1 in this version");
            Assert.AreEqual(RelicState.Displayed, duck.State);
            Assert.AreEqual(slot, duck.Slot, "it took the slot that was shown");
            Assert.AreEqual(1, home.DisplayedCount);
            Assert.AreEqual(nameof(RelicDeposited), _fixture.Events.Order[before]);
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
            _fixture.DeliverTowerParts();
            RadioTower tower = _fixture.Gameplay.Tower;
            RadioTowerTuning tuning = _fixture.TowerTuning;
            IUpgradeShop shop = _fixture.Bootstrap.Context.Get<IUpgradeShop>();
            Assert.AreEqual(0, tower.ShownLevel);
            Assert.AreEqual(0, tower.ActiveStage);
            Assert.Less(tower.BeaconLevel, 0.01f, "the old mast stands dark");
            Assert.Less(tower.HatchOpenAngle, 0.5f, "its service hatch is shut");
            Vector3 port = tower.HopperMouth;
            float clear = SurfaceRules.HorizontalDistance(tower.PadCentre, port) - tuning.PadRadius;
            Assert.Greater(clear, 0.2f, "the pad stays clear of the service port");
            Assert.IsFalse(tower.PadShown, "nothing to feed it yet: the pilot blink is the tower's only light");

            _fixture.GiveMaterials(1, 3, 1);
            Assert.AreEqual(PurchaseResult.NotAtStation, shop.Purchase(Tower), "bought on the pad, not anywhere");
            _fixture.Rover.Place(tower.PadCentre, Yaw(Flat(port) - Flat(tower.PadCentre)));
            yield return null;
            yield return null;
            Assert.IsTrue(tower.PadShown, "with materials in hand the tower's pad shows");
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
            AssertCues(cues, Tower, StationCue.FeedStarted, StationCue.BundleDropped, StationCue.BundleDropped,
                StationCue.Fed, StationCue.HatchOpened, StationCue.StitchStarted, StationCue.HatchClosed);
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
        public IEnumerator TowerCoil_WaitsSealedInTheDepotsCrate_SlidesOut_RidesOn07_IsFed_ThenTheFirstLevelCanBeBuilt()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            TowerParts parts = gameplay.TowerParts;
            TowerPartField field = gameplay.TowerPartField;
            RadioTower tower = gameplay.Tower;
            IUpgradeShop shop = _fixture.Bootstrap.Context.Get<IUpgradeShop>();
            var found = new List<ComponentFound>();
            var delivered = new List<ComponentDelivered>();
            _fixture.Bootstrap.Context.Events.Subscribe<ComponentFound>(found.Add);
            _fixture.Bootstrap.Context.Events.Subscribe<ComponentDelivered>(delivered.Add);
            var cuts = new List<ContainerSealCut>();
            var opens = new List<ContainerOpened>();
            _fixture.Bootstrap.Context.Events.Subscribe<ContainerSealCut>(cuts.Add);
            _fixture.Bootstrap.Context.Events.Subscribe<ContainerOpened>(opens.Add);
            Assert.AreEqual("Part_TowerCoil", parts.PartAt(0).PartId);
            SealedContainer crate = gameplay.Containers.Find(TowerPartField.ContainerId(parts.PartAt(0)));
            Assert.IsNotNull(crate, "the coil rests in Art's crate (0.5.5 §2)");
            Assert.AreEqual(ContainerKind.Crate, crate.Kind);
            Assert.Less(SurfaceRules.HorizontalDistance(crate.Root.position, _fixture.FindSite("depot").Heart), 1e-3f,
                "the crate stands in the depot's heart");
            Assert.IsTrue(crate.Available, "the heart is open: the crate's seal glows to be cut");
            Assert.IsFalse(field.IsResting(0), "nothing lies loose on the ground: the coil is sealed in");
            Assert.IsFalse(field.IsResting(2), "no mast is restored yet, so no relay spare");

            _fixture.GiveMaterials(30, 30, 30);
            _fixture.Rover.Place(tower.PadCentre, 0f);
            yield return null;
            yield return null;
            Assert.AreEqual(PurchaseResult.NeedsPart, shop.Purchase(Tower), "materials alone do not wake the tower");
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Upgrade, out InteractionHint hint));
            Assert.IsFalse(hint.Ready, "the prompt waits for the coil");

            gameplay.Crates.Restore(new CratesSaveData { opened = new[] { WorldAnchorIds.SitePrefix + "depot" } });
            _fixture.Rover.Place(crate.CutPoint.position + crate.Root.forward * 1.5f, Yaw(-Flat(crate.Root.forward)));
            yield return SealCut.Hold(this, gameplay, crate, "the coil crate");
            Assert.AreEqual(1, cuts.Count, "the seal parts");
            Assert.AreEqual(crate.Id, cuts[0].ContainerId);
            Assert.AreEqual(1, opens.Count, "the lid stands open");
            Assert.AreEqual(ContainerKind.Crate, opens[0].Kind);
            yield return Waits.Until(() => field.IsResting(0), _fixture.ProgressionTuning.PartAppearSeconds + 1f);
            Assert.IsTrue(field.IsResting(0), "the coil slides out onto the ground at the crate's front");
            Assert.Greater(Vector3.Dot(Flat(field.RestPosition(0) - crate.ItemSocket.position), crate.Root.forward),
                0f, "out in front, not inside");
            yield return null;
            Assert.IsNull(gameplay.Containers.Candidate, "an open crate has nothing more to cut");
            _fixture.Rover.Place(field.RestPosition(0), 0f);
            yield return PickupPress.Take(this, _fixture.Gameplay, 2f, "the coil");
            yield return Waits.Until(() => found.Count > 0, 5f);
            Assert.AreEqual("Part_TowerCoil", found[0].PartId);
            Assert.AreEqual(1, found[0].TowerLevel);
            Assert.AreEqual(TowerPartState.Carried, parts.StateOf(0), "it rides home on 07's back");
            Assert.IsFalse(field.IsResting(0));
            Assert.AreEqual(PurchaseResult.NotAtStation, shop.Purchase(Tower));

            _fixture.Rover.Place(tower.PadCentre, 0f);
            yield return Waits.Until(() => delivered.Count > 0, 2f);
            Assert.AreEqual("Part_TowerCoil", delivered[0].PartId);
            Assert.AreEqual(0, field.Feeding, "parked on the pad, 07 hands the coil to the port");
            yield return Waits.Until(() => field.Feeding < 0, _fixture.ProgressionTuning.PartFeedSeconds + 1f);
            Assert.AreEqual(TowerPartState.Delivered, parts.StateOf(0));
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Upgrade, out hint));
            Assert.IsTrue(hint.Ready);
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase(Tower));
            Assert.AreEqual(1, delivered.Count, "the transformer is not on 07");
        }

        [UnityTest]
        public IEnumerator Chasm_TillySpotsBellsLight_07LooksAcrossFromTheLip_ThenTheTransformerShows()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            BaseProgression progression = gameplay.Progression;
            TowerPartField field = gameplay.TowerPartField;
            Friend tilly = FindFriend("tilly");
            Friend bell = FindFriend("bell");
            Assert.IsFalse(field.IsResting(1), "the garage's transformer waits for the chasm (P2)");
            TowerPart transformer = gameplay.TowerParts.PartAt(1);
            SealedContainer box = gameplay.Containers.Find(TowerPartField.ContainerId(transformer));
            Assert.IsFalse(box.Available,
                "its junction box stays dark until then");
            Assert.IsFalse(bell.Progress.Discovered);

            tilly.Progress.Restore(new FriendSaveData { id = "tilly", state = (int)FriendState.Awake, parts = -1,
                items = -1 });
            HomeBase home = gameplay.Home;
            _fixture.Display(_fixture.FindRelic(Duck), 0);
            yield return null;
            Assert.AreEqual(1, home.DisplayedCount, "the first memory is on the shelf");
            Assert.IsTrue(bell.Progress.Discovered, "Tilly spots Bell's light across the chasm");
            Assert.IsFalse(progression.ChasmSeen);

            Assert.IsTrue(_fixture.Bootstrap.Context.Get<IWorldAnchors>()
                .TryGet(WorldAnchorIds.CanyonLip, out WorldAnchor lip));
            _fixture.Rover.Place(lip.Position - lip.Forward * 5f, Yaw(Flat(lip.Forward)));
            yield return null;
            yield return null;
            Assert.IsTrue(progression.ChasmSeen, "07 reached the lip without Hover-Jump");
            Assert.IsTrue(progression.LookingAcross, "it looks across at Bell's light");
            Assert.AreEqual(1, _fixture.Rover.HoldStillCount, "holding still while it looks");
            yield return new WaitForSeconds(_fixture.ProgressionTuning.ChasmLookSeconds + 0.5f);
            Assert.IsFalse(progression.LookingAcross);
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount, "then it is free to drive");
            Assert.AreEqual(ContainerKind.Box, box.Kind);
            Assert.IsTrue(box.Available, "now Tilly spots it: the garage's junction box glows to be cut");
            Assert.IsFalse(field.IsResting(1), "the transformer is still sealed in");

            _fixture.Rover.Place(lip.Position - lip.Forward * 30f, 0f);
            yield return null;
            _fixture.Rover.Place(lip.Position - lip.Forward * 5f, 0f);
            yield return null;
            yield return null;
            Assert.IsFalse(progression.LookingAcross, "the look happens once");
        }

        private Friend FindFriend(string id)
        {
            foreach (Friend friend in _fixture.Gameplay.Friends.Friends)
            {
                if (friend.Id == id)
                {
                    return friend;
                }
            }

            Assert.Fail($"no friend '{id}' in the fixture");
            return null;
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
            _fixture.Decode(HoverJump);
            Assert.AreSame(_fixture.HoverJumpUpgrade, bay.Definition, "the bay's first offer");
            Assert.IsFalse(bay.Awake, "asleep until the tower's second level powers it (M3-16)");
            Assert.AreEqual(0f, bay.LampLevel, 1e-3f, "its work lamps are dark");
            Assert.AreEqual(0f, bay.LightLevel, 1e-3f, "and so is the bay inside");
            Assert.AreEqual(0f, bay.SignLevel, 1e-3f, "the bay's sign is dark until power reaches it");
            _fixture.Rover.Place(Flat(bay.PadCentre), Yaw(-bay.BayForward));
            yield return null;
            yield return null;
            Assert.IsFalse(bay.Occupied, "a sleeping bay has no pad");
            Assert.IsFalse(bay.PadShown, "not even painted lamps");
            Assert.AreEqual(0f, bay.PadLevel, 1e-3f);
            Assert.IsFalse(shop.IsAtStation, "no panel");
            Assert.IsFalse(_fixture.Gameplay.Hints.TryGet(InteractionKind.Upgrade, out _), "no prompt");
            Assert.AreEqual(PurchaseResult.NotAtStation, shop.Purchase(HoverJump), "nothing to buy");

            _fixture.Rover.Place(tower.PadCentre, 0f);
            _fixture.PowerTo(2);
            yield return new WaitForSeconds(tuning.LightEase * 5f);
            Assert.IsTrue(bay.Awake);
            Assert.IsTrue(bay.Powered);
            Assert.IsTrue(bay.PadShown, "awake with a kit to fit, its pad shows");
            Assert.AreEqual(tuning.SignGlow, bay.SignLevel, 1e-3f, "woken by a loaded tower, its sign is simply lit");
            Assert.AreEqual(tuning.LampIdle, bay.LampLevel, 0.05f, "Kenji's work lamps come on");
            Assert.AreEqual(tuning.LightIdle, bay.LightLevel, 0.05f, "a low warm glow lights the bay inside");
            Assert.AreEqual(0f, bay.PitLevel, "the pit under the turntable is dark");
            Assert.IsFalse(bay.Working);
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
            Assert.IsFalse(_fixture.Rover.Has(RoverAbility.HoverJump), "bought, not yet on 07: the bay crafts it");
            Assert.Greater(bay.PadLevel, look.Occupied, "the purchase flares the pad");
            Assert.AreSame(_fixture.HoverJumpUpgrade, bay.Definition, "no other blueprint yet: it rests on Hover-Jump");
            _fixture.Decode(HoverJump, _fixture.CargoCradleUpgrade.Id);
            Assert.AreSame(_fixture.CargoCradleUpgrade, bay.Definition, "the Cargo Cradle's sheet pinned, it offers it");
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Upgrade, out hint));
            Assert.IsFalse(hint.Ready, "4 metal it does not have yet");
            Assert.AreEqual(PurchaseResult.Maxed, shop.Purchase(HoverJump));

            Assert.IsTrue(bay.Feeding, "07's beam reaches for the bay's hopper");
            yield return null;
            Assert.IsTrue(bay.Working, "the bay works");
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
            Assert.Greater(bay.LightLevel, tuning.LightIdle + (tuning.LightWorking - tuning.LightIdle) * 0.4f,
                "its work lights ease up to light the inside and the arms");
            Assert.AreEqual(0, _fixture.Events.PartCrafted.Count, "nothing is crafted before the hopper is fed");
            Assert.AreEqual(0, bay.SparkCount);
            Vector3 front = bay.BayForward;
            _fixture.Rover.Aim(bay.BayPosition + front * 7f + Vector3.Cross(Vector3.up, front) * 3f + Vector3.up * 2.6f,
                bay.BayPosition + Vector3.up);
            _fixture.Capture("18a-bay-hopper-feed");

            yield return Waits.Until(() => _fixture.Events.PartCrafted.Count > 0,
                HopperFeed.DurationFor(3, feedLook) + 0.5f);
            AssertCues(cues, HoverJump, StationCue.FeedStarted, StationCue.BundleDropped, StationCue.BundleDropped,
                StationCue.BundleDropped, StationCue.Fed);
            RoverEquipment equipment = _fixture.Gameplay.Equipment;
            Assert.AreEqual(RoverKitPiece.HoverCoils, _fixture.Events.PartCrafted[0].Value.Part);
            Assert.AreEqual(1, equipment.RowCount, "the coils wait in the parts row (0.5.5 §3.5)");
            Assert.IsFalse(_fixture.Rover.Has(RoverAbility.HoverJump), "a part in the row gives nothing yet");
            var rig = _fixture.Bootstrap.Context.Get<IRoverBay>();
            Assert.AreSame(bay, rig, "the bay itself is registered for the rover to drive its arms");
            Assert.AreEqual(3, rig.ArmCount);
            Assert.AreEqual("Yaw", rig.GetArmJoint(1, RoverBayJoint.Yaw).name);
            Assert.AreEqual("SparkSocket", rig.GetArmJoint(1, RoverBayJoint.SparkSocket).name);
            Assert.AreEqual("FloorTip", rig.FloorTip.name);
            Assert.Less(Vector3.Distance(bay.PadCentre, rig.TurntablePosition), 0.2f, "07 parks on the turntable");
            Assert.IsFalse(bay.Feeding);
            Assert.AreEqual(0, bay.BundlesInFlight, "every bundle went in");
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount, "the bay lets go: the rover's install holds 07");
            Assert.IsFalse(_fixture.Rover.TryGetGaze(bay, out _, out _));
            yield return new WaitForSeconds(tuning.SparkInterval * 1.5f);
            Assert.AreEqual(0, bay.SparkCount, "no sparks while the arms are still folded at the rail");
            Assert.IsTrue(bay.ReadyToFit, "fed and free, with 07 on the turntable");

            // The rover's floor arm lifts the Hover-Jump coils up through the turntable and sets them on.
            rig.FloorLift.localPosition += Vector3.up * 0.25f;
            yield return new WaitForSeconds(tuning.LightEase * 4f);
            Assert.Greater(bay.PitLevel, 0.9f, "the pit and the floor arm's tip light the coils coming up");
            _fixture.Bootstrap.Context.Events.Publish(new PartFitted(RoverSocket.Wing, RoverKitPiece.SolarCell, true,
                false, default, 0));
            Seat();
            Assert.AreEqual(0, bay.SparkCount, "a friend's gift seating is no weld");
            Assert.IsTrue(equipment.TryFit(0), "the player drops the coils on their socket");
            Assert.AreEqual(RoverSocket.Belly, _fixture.Events.PartFitted[1].Value.Socket, "after the gift's");
            Assert.IsTrue(_fixture.Rover.Has(RoverAbility.HoverJump), "fitted: 07 can leap now");
            yield return null;
            Assert.IsTrue(bay.Working, "the bay works while the part goes on");
            Assert.IsFalse(bay.ReadyToFit, "and fits nothing else until it seats");
            Seat();
            Assert.Greater(bay.LampLevel, tuning.LampOccupied * 2f, "the coils seat: the lamps flare at the weld");
            yield return new WaitForSeconds(tuning.SparkInterval * 1.5f);
            Assert.Greater(bay.FloorSparkCount, tuning.SparkCount, "weld sparks fly from the floor arm's tip");
            for (int arm = 0; arm < rig.ArmCount; arm++)
            {
                Assert.AreEqual(0, bay.SparkCountOf(arm), "never from an arm folded at the rail");
            }

            _fixture.Capture("18-bay-hover-jump");
            rig.FloorLift.localPosition -= Vector3.up * 0.25f;
            yield return new WaitForSeconds(tuning.WorkLinger + tuning.LightEase * 5f);
            Assert.IsFalse(bay.Working, "done: the bay rests");
            Assert.Less(bay.PitLevel, 0.02f, "the arm is down: the pit goes dark again");
            Assert.AreEqual(tuning.LightIdle, bay.LightLevel, 0.1f, "its lights ease back down to the low glow");

            _fixture.GiveMaterials(5, 2, 4);
            UpgradeDefinition[] kit =
            {
                _fixture.CargoCradleUpgrade, _fixture.WarmHeadlampUpgrade, _fixture.BoostCoilsUpgrade,
            };
            RoverAbility[] abilities = { RoverAbility.CargoCradle, RoverAbility.WarmHeadlamp, RoverAbility.BoostCoils };
            Assert.AreEqual(PurchaseResult.NotPinned, shop.Purchase(kit[1].Id), "no blueprint, no kit (M3-16)");
            _fixture.Gameplay.Blueprints.Restore(
                new BlueprintsSaveData { found = new[] { kit[0].Id, kit[1].Id, kit[2].Id } });
            for (int i = 0; i < kit.Length; i++)
            {
                Assert.AreSame(kit[i], bay.Definition, "the bay offers its kit in order");
                Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase(kit[i].Id));
                Assert.IsFalse(_fixture.Rover.Has(abilities[i]), $"{kit[i].Id} waits for its part to be fitted");
            }

            Assert.AreEqual(0, _fixture.Gameplay.Materials.Total, "the kit takes exactly its recipes");
            balance = 0;
            Assert.IsFalse(_fixture.Gameplay.Hints.TryGet(InteractionKind.Upgrade, out _), "nothing left to buy");
            yield return Waits.Until(() => _fixture.Events.PartCrafted.Count == 4,
                3f * HopperFeed.DurationFor(3, feedLook) + 1f);
            RoverKitPiece[] parts = { RoverKitPiece.CargoRack, RoverKitPiece.LampBar, RoverKitPiece.CapacitorDrums };
            for (int i = 0; i < kit.Length; i++)
            {
                Assert.AreEqual(parts[i], equipment.RowPart(i),
                    "pieces bought together are fed and crafted one after another, in order");
            }

            yield return Waits.Until(() => bay.ReadyToFit, 2f);
            Assert.IsTrue(bay.ReadyToFit, "the last feed done, the bay is free to fit");
            Assert.IsTrue(equipment.TryFit(0), "the Cargo Cradle's rack first");

            // The right-hand arm swings down to set the Cargo Cradle's rack on 07's back.
            Transform shoulder = rig.GetArmJoint(2, RoverBayJoint.Upper);
            Quaternion folded = shoulder.localRotation;
            shoulder.localRotation = folded * Quaternion.Euler(70f, 0f, 0f);
            Seat();
            yield return new WaitForSeconds(tuning.SparkInterval * 1.5f);
            Assert.Greater(bay.SparkCountOf(2), tuning.SparkCount, "weld sparks fly from the fitting arm's tip");
            Assert.AreEqual(0, bay.SparkCountOf(0) + bay.SparkCountOf(1), "and only from it");
            shoulder.localRotation = folded;
            Assert.AreEqual(2, equipment.RowCount, "two more parts to set on");
            for (int i = 1; i < kit.Length; i++)
            {
                Assert.IsTrue(equipment.TryFit(0), $"{parts[i]} in its turn");
                Seat();
            }

            for (int i = 0; i < kit.Length; i++)
            {
                Assert.IsTrue(_fixture.Rover.Has(abilities[i]), $"{kit[i].Id}'s part brings its ability");
            }

            yield return new WaitForSeconds(tuning.SparkInterval * tuning.SparkBursts + tuning.SparkLifetime.y + 0.5f);
            Assert.AreEqual(0, bay.SparkCount, "the sparks die out (an idle bay costs nothing)");
            Assert.Less(bay.PadLevel, look.Occupied * 0.5f, "a finished bay's pad lamps ease out");
            Assert.AreEqual(tuning.LampOccupied, bay.LampLevel, 0.05f, "the lamps settle back");
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount);
            yield return new WaitForSeconds(tuning.WorkLinger + tuning.LightEase * 5f);
            Assert.IsFalse(bay.Working, "every piece is on");
            Assert.IsFalse(bay.PadShown, "a finished bay shows no pad");

            _fixture.Dispose(true);
            yield return null;
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            Assert.AreEqual(1, _fixture.Gameplay.Upgrades.LevelOf(HoverJump), "the purchase was a checkpoint");
            Assert.IsTrue(_fixture.Rover.Has(RoverAbility.HoverJump), "fitted again on load");
            Assert.IsTrue(_fixture.Gameplay.Equipment.IsFitted(RoverKitPiece.HoverCoils));
            Assert.AreEqual(0, _fixture.Gameplay.Equipment.RowCount);
            Assert.IsTrue(_fixture.Rover.Has(RoverAbility.CargoCradle));
            Assert.IsTrue(_fixture.Rover.Has(RoverAbility.WarmHeadlamp));
            Assert.IsTrue(_fixture.Rover.Has(RoverAbility.BoostCoils));
            Assert.AreEqual(0, _fixture.Events.UpgradePurchased.Count, "a load is never a purchase");
            Assert.AreEqual(0, _fixture.Events.StationCued.Count, "nor a feed");
            Assert.AreEqual(0, _fixture.Events.PartFitted.Count, "nor a fit: the rover reads the equipment");
            Assert.AreEqual(balance, _fixture.Gameplay.Materials.Total, "the checkpoint kept the change");
        }

        [UnityTest]
        public IEnumerator BaySign_LightsWhenPowerReachesTheBay_FlickeringOnTheFirstTime_AndIsLitOnLoad()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            Workshop bay = _fixture.Gameplay.Workshop;
            RadioTower tower = _fixture.Gameplay.Tower;
            WorkshopTuning tuning = _fixture.WorkshopTuning;
            _fixture.DeliverTowerParts();
            _fixture.PowerTo(1);
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(bay.Powered);
            Assert.AreEqual(0f, bay.SignLevel, 1e-3f, "dark: the tower's first level powers home, not the bay");

            _fixture.GiveMaterials(30, 30, 30);
            _fixture.Rover.Place(tower.PadCentre, 0f);
            yield return null;
            yield return null;
            Assert.AreEqual(PurchaseResult.Purchased, _fixture.Bootstrap.Context.Get<IUpgradeShop>().Purchase(Tower));
            yield return null;
            Assert.IsFalse(bay.Powered, "not while 07 is still feeding the tower");
            yield return Waits.Until(() => bay.Powered, 5f);
            Assert.IsTrue(bay.Powered, "the tower's second level brings the bay its power");
            Assert.AreEqual(2, tower.ShownLevel);
            var levels = new List<float>();
            float start = Time.time;
            while (Time.time - start < tuning.SignFlickerDuration)
            {
                levels.Add(bay.SignLevel);
                yield return null;
            }

            Assert.Less(levels[0], tuning.SignGlow * 0.5f, "it comes up from dark");
            int dims = 0;
            for (int i = 1; i < levels.Count; i++)
            {
                dims += levels[i] < levels[i - 1] - 1e-4f ? 1 : 0;
            }

            Assert.Greater(dims, 0, "flickering on the way up");
            yield return null;
            Assert.AreEqual(tuning.SignGlow, bay.SignLevel, 1e-3f, "then holds steady");
            _fixture.Rover.Aim(bay.BayPosition + bay.BayForward * 9f + Vector3.up * 3f,
                bay.BayPosition + Vector3.up * 3f);
            _fixture.Capture("18b-bay-sign-lit");

            _fixture.Dispose(true);
            yield return null;
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            yield return null;
            bay = _fixture.Gameplay.Workshop;
            Assert.IsTrue(bay.Powered, "the base keeps its power across a load");
            Assert.AreEqual(tuning.SignGlow, bay.SignLevel, 1e-3f, "and the sign is simply lit, no flicker");
        }

        [UnityTest]
        public IEnumerator BlueprintTube_RidesHome_TheLabsReaderDecodesIt_ThePlotterDrawsIt_AndALoadKeepsIt()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            BlueprintPlotter plotter = _fixture.Gameplay.Plotter;
            Blueprints board = _fixture.Gameplay.Blueprints;
            string cradle = _fixture.CargoCradleUpgrade.Id;
            Assert.IsFalse(plotter.ShowsSheet(_fixture.HoverJumpUpgrade.Id), "no sheet hangs from the start (M3-21)");
            Assert.IsFalse(plotter.ShowsSheet(cradle));
            Relic duck = null;
            foreach (Relic relic in _fixture.Gameplay.Relics.Relics)
            {
                duck = duck == null && relic.Home != null && relic.Home.Id == "site.garage" ? relic : duck;
            }

            _fixture.Bootstrap.Context.Events.Publish(new RelicSurfaced(duck.transform.position, duck.Definition.Id));
            Assert.IsTrue(board.IsCarrying, "the garage's tube rides home on 07");
            Assert.IsFalse(board.IsPinned(cradle));

            _fixture.PowerTo(2);
            _fixture.Rover.Place(_fixture.Gameplay.Workshop.PadCentre, 0f);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(board.IsCarrying, "the plotter in the awake bay takes no tube: the lab's reader does");
            Assert.IsFalse(plotter.Drawing);

            yield return _fixture.RideIntoTheLab();
            ProgressionTuning tuning = _fixture.ProgressionTuning;
            yield return Waits.Until(() => board.IsPinned(cradle),
                tuning.ReaderFeedSeconds + tuning.ReaderDecodeSeconds + 1f);
            Assert.IsTrue(board.IsPinned(cradle), "the reader decoded it: pinned");
            Assert.AreEqual(1, _fixture.Events.BlueprintDelivered.Count, "the reader took it");
            Assert.AreEqual(cradle, _fixture.Events.BlueprintDelivered[0].Value.KitId);
            yield return Waits.Until(() => plotter.Drawing, tuning.HandOffSeconds + 1f);
            Assert.IsTrue(plotter.Drawing, "the hand-off reached the awake bay: the plotter draws");
            Assert.IsFalse(plotter.ShowsSheet(cradle), "the sheet is still on the bed");
            yield return Waits.Until(() => !plotter.Drawing, _fixture.ProgressionTuning.PlotterDrawSeconds + 2f);
            Assert.IsTrue(plotter.ShowsSheet(cradle), "drawn, it hangs on Kenji's pegboard");

            _fixture.Dispose(true);
            yield return null;
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            yield return null;
            Assert.IsTrue(_fixture.Gameplay.Blueprints.IsPinned(cradle), "pinned across a load");
            Assert.IsTrue(_fixture.Gameplay.Plotter.ShowsSheet(cradle), "and simply shown");
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
            _fixture.DeliverTowerParts();

            Relic duck = _fixture.FindRelic(Duck);
            _fixture.Display(duck, 2);
            Assert.AreEqual(RelicState.Displayed, duck.State);
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
            Assert.AreEqual(RelicState.Displayed, duckAgain.State, "the keepsake shelf keeps its relics");
            Assert.AreEqual(duckSlot, duckAgain.Slot);
            Assert.AreEqual(1, gameplay.Home.DisplayedCount);
            Relic teapotAgain = _fixture.FindRelic(Teapot);
            Assert.AreEqual(RelicState.Surfacing, teapotAgain.State, "a half-lifted relic waits where it was");
            Assert.AreEqual(0.4f, teapotAgain.Progress, 1e-4f);
            Assert.IsTrue(teapotAgain.Discovered);
            Assert.Less(Vector3.Distance(teapotAgain.transform.position, halfway), 0.1f);
            Assert.AreEqual(1, gameplay.Upgrades.LevelOf(Tower));
            Assert.IsFalse(gameplay.TowerParts.NeedsPart(Tower, 2), "the tower's components stay in its port");
            Assert.AreEqual(1, gameplay.Tower.ShownLevel);
            Assert.AreEqual(0, _fixture.Events.UpgradePurchased.Count, "a load is never a purchase");
            yield return new WaitForSeconds(0.5f);
            Assert.Greater(gameplay.Tower.BeaconLevel, 0.5f, "the beacon is lit straight away");
            Vector3 keepsakes = gameplay.Home.KeepsakesPosition;
            _fixture.Rover.Aim(gameplay.Lift.StationPosition(LabStation.Chart), keepsakes);
            _fixture.Capture("12-reloaded-keepsakes");
        }

        /// <summary>Stands in for the rover's install moment seating a part on 07 (its Install Impact beat).</summary>
        private void Seat()
        {
            _fixture.Bootstrap.Context.Events.Publish(new FeelBeat(FeelKind.Install, FeelPhase.Impact,
                _fixture.Rover.Position, Vector3.up, 1f, 0f));
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
