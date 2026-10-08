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
    /// Scripted sessions for home carrying across the basin, the museum deposit, the radio tower shop, Kenji's
    /// workbench, the hint query and save/load.
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
        public IEnumerator Tower_BuysALevelOnItsPad_AndGrowsTheNextStage()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            RadioTower tower = _fixture.Gameplay.Tower;
            IUpgradeShop shop = _fixture.Bootstrap.Context.Get<IUpgradeShop>();
            Assert.AreEqual(0, tower.ShownLevel);
            Assert.AreEqual(0, tower.ActiveStage);
            Assert.Less(tower.BeaconLevel, 0.01f, "the old mast stands dark");

            _fixture.GiveMaterials(0, 2, 3);
            Assert.AreEqual(PurchaseResult.NotAtStation, shop.Purchase(Tower), "bought on the pad, not anywhere");
            _fixture.Rover.Place(tower.PadCentre, 0f);
            yield return null;
            yield return null;
            Assert.IsTrue(shop.IsAtStation);
            Assert.AreSame(_fixture.RadioTowerUpgrade, shop.StationUpgrade);
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Upgrade, out InteractionHint hint));
            Assert.IsTrue(hint.Ready, "affordable");
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(tower.PadLevel, 0.8f * _fixture.TowerTuning.PadOccupied, "the pad glows under 07");

            int before = _fixture.Events.Order.Count;
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase(Tower));
            CollectionAssert.AreEqual(new[]
            {
                nameof(MaterialsChanged), nameof(UpgradePurchased), nameof(SignalRadiusChanged),
            }, _fixture.Events.Order.GetRange(before, 3));
            Assert.AreEqual(1, _fixture.Events.UpgradePurchased[0].Value.Level);
            Assert.AreEqual(110f, _fixture.Events.SignalRadiusChanged[0].Value.Radius);
            yield return new WaitForSeconds(2f);
            Assert.AreEqual(1, tower.ShownLevel);
            Assert.AreEqual(0, tower.ActiveStage, "level 1 wakes the old mast");
            Assert.Greater(tower.BeaconLevel, 0.5f, "its beacon glows again");

            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase(Tower));
            yield return new WaitForSeconds(_fixture.TowerTuning.FlareDuration * 0.5f);
            Assert.AreEqual(0, tower.ActiveStage, "the old stage stays while the beacon flares");
            yield return new WaitForSeconds(_fixture.TowerTuning.FlareDuration * 0.5f + 0.3f);
            Assert.AreEqual(1, tower.ActiveStage, "then the next stage grows in");
            Vector3 pad = tower.PadCentre;
            _fixture.Rover.Aim(pad + new Vector3(9f, 6f, -9f), pad + Vector3.up * 3f);
            _fixture.Capture("10-tower-upgrade");
            yield return new WaitForSeconds(2.5f);
            Assert.AreEqual(1, tower.ActiveStage);
            Assert.AreEqual(170f, _fixture.Events.SignalRadiusChanged[1].Value.Radius);
            Assert.AreEqual(0, _fixture.Gameplay.Materials.Total, "exactly the two recipes were spent");
            _fixture.Capture("11-tower-level-2");
        }

        [UnityTest]
        public IEnumerator Workshop_SellsItsKitInOrderOnItsOwnPad_GrantsIt_AndGrantsItAgainOnLoad()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            Workshop workshop = _fixture.Gameplay.Workshop;
            RadioTower tower = _fixture.Gameplay.Tower;
            IUpgradeShop shop = _fixture.Bootstrap.Context.Get<IUpgradeShop>();
            PadLook look = _fixture.WorkshopTuning.PadLook;
            WorkshopTuning tuning = _fixture.WorkshopTuning;
            Assert.AreSame(_fixture.HoverJumpUpgrade, workshop.Definition, "the bench's first offer");
            Assert.AreEqual(tuning.LampIdle, workshop.LampLevel, 1e-3f, "Kenji's lamp is left on");
            Assert.AreEqual(0, workshop.SparkCount, "no sparks before a purchase");
            Assert.IsFalse(_fixture.Rover.Has(RoverAbility.HoverJump));
            Assert.Greater(SurfaceRules.HorizontalDistance(workshop.PadCentre, tower.PadCentre),
                look.Radius + _fixture.TowerTuning.PadRadius, "the two pads never overlap");

            _fixture.GiveMaterials(5, 2, 1);
            Assert.AreEqual(PurchaseResult.NotAtStation, shop.Purchase(HoverJump), "bought at the bench, not anywhere");
            _fixture.Rover.Place(tower.PadCentre, 0f);
            yield return null;
            yield return null;
            Assert.AreSame(_fixture.RadioTowerUpgrade, shop.StationUpgrade);
            Assert.AreEqual(PurchaseResult.NotAtStation, shop.Purchase(HoverJump), "not at the tower either");

            _fixture.Rover.Place(workshop.PadCentre, 0f);
            yield return null;
            yield return null;
            Assert.IsTrue(workshop.Occupied);
            Assert.AreSame(_fixture.HoverJumpUpgrade, shop.StationUpgrade);
            Assert.AreEqual(UpgradeStationKind.Workshop, shop.StationUpgrade.Station);
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Upgrade, out InteractionHint hint));
            Assert.IsTrue(hint.Ready, "affordable");
            Assert.AreEqual(workshop.PadCentre, hint.Position);
            yield return new WaitForSeconds(1.5f);
            Assert.Greater(workshop.PadLevel, 0.8f * look.Occupied, "the pad glows under 07");
            Assert.Greater(workshop.LampLevel, 0.9f * tuning.LampOccupied, "the lamp leans in");

            int before = _fixture.Events.Order.Count;
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase(HoverJump));
            int balance = _fixture.Gameplay.Materials.Total;
            Assert.AreEqual(1, balance, "Hover-Jump takes 4 metal, 2 wiring and 1 optics: one metal is left");
            CollectionAssert.AreEqual(new[] { nameof(MaterialsChanged), nameof(UpgradePurchased) },
                _fixture.Events.Order.GetRange(before, _fixture.Events.Order.Count - before));
            Assert.AreEqual(HoverJump, _fixture.Events.UpgradePurchased[0].Value.UpgradeId);
            Assert.AreEqual(1, _fixture.Events.UpgradePurchased[0].Value.Level);
            Assert.IsTrue(_fixture.Rover.Has(RoverAbility.HoverJump), "07 can leap now");
            Assert.Greater(workshop.PadLevel, look.Occupied, "the purchase flares the pad");
            Assert.AreEqual(tuning.LampFlare, workshop.LampLevel, 1e-3f, "and the lamp");
            Assert.AreSame(_fixture.CargoCradleUpgrade, workshop.Definition, "then the bench offers the Cargo Cradle");
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Upgrade, out hint));
            Assert.IsFalse(hint.Ready, "4 metal it does not have yet");
            Assert.AreEqual(PurchaseResult.Maxed, shop.Purchase(HoverJump));
            Vector3 pad = workshop.PadCentre;
            _fixture.Rover.Aim(pad + new Vector3(-7f, 5f, -7f), pad + Vector3.up);
            yield return new WaitForSeconds(tuning.SparkInterval * 1.5f);
            Assert.Greater(workshop.SparkCount, tuning.SparkCount, "sparks fly from between the vice jaws");
            _fixture.Capture("18-workshop-hover-jump");

            _fixture.GiveMaterials(5, 2, 4);
            UpgradeDefinition[] kit =
            {
                _fixture.CargoCradleUpgrade, _fixture.WarmHeadlampUpgrade, _fixture.BoostCoilsUpgrade,
            };
            RoverAbility[] abilities = { RoverAbility.CargoCradle, RoverAbility.WarmHeadlamp, RoverAbility.BoostCoils };
            for (int i = 0; i < kit.Length; i++)
            {
                Assert.AreSame(kit[i], workshop.Definition, "the bench offers its kit in order");
                Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase(kit[i].Id));
                Assert.IsTrue(_fixture.Rover.Has(abilities[i]), $"{kit[i].Id} grants its ability");
            }

            Assert.AreEqual(0, _fixture.Gameplay.Materials.Total, "the kit takes exactly its recipes");
            balance = 0;
            Assert.IsFalse(_fixture.Gameplay.Hints.TryGet(InteractionKind.Upgrade, out _), "nothing left to buy");
            yield return new WaitForSeconds(tuning.SparkInterval * tuning.SparkBursts + tuning.SparkLifetime.y + 0.5f);
            Assert.AreEqual(0, workshop.SparkCount, "the sparks die out (an idle bench costs nothing)");
            Assert.Less(workshop.PadLevel, look.Occupied * 0.5f, "an empty bench's pad rests dim");
            Assert.AreEqual(tuning.LampOccupied, workshop.LampLevel, 0.05f, "the lamp settles back");

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
            Assert.AreEqual(balance, _fixture.Gameplay.Materials.Total, "the checkpoint kept the change");
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
