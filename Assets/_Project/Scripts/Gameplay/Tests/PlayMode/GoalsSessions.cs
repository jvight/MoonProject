using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Free order after the teaching steps (docs/features/M3-21 §5), on the real components behind the fake rover:
    /// past the bay step a different path than the golden one (the Cargo Cradle fitted first while Hover-Jump's tube
    /// still waits at the lip, the mound mast lit before the tower's last level) still reaches Power 3/3 with nothing
    /// blocked, the recovery rows light as it goes, and the next line names only the targets all the way.
    /// </summary>
    public sealed class GoalsSessions : InputTestFixture
    {
        private const string Tower = "radio_tower";
        private const string CargoCradle = "rover.cargo_cradle";
        private const string HoverJump = "rover.hover_jump";
        private const int SparePart = 2;

        private InputActionAsset _controls;
        private GameplayFixture _fixture;

        public override void Setup()
        {
            base.Setup();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator PastTheTeachingSteps_ADifferentOrder_ReachesPower3_WithTheLineOnTheTargetsOnly()
        {
            _fixture = GameplayFixture.Boot(_controls, tether: false);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            Assert.IsFalse(_fixture.Rover.Has(RoverAbility.Tether), "no tether before Kenji's port, in any order");
            Assert.IsFalse(gameplay.Port.RingFitted);
            gameplay.Port.Restore(new GaragePortSaveData { ringFitted = true, placeSeen = true });
            gameplay.Port.Announce();
            Assert.IsTrue(_fixture.Rover.Has(RoverAbility.Tether), "the port's ring wakes the tether");
            BaseProgression progression = gameplay.Progression;
            var lines = new List<string>();
            using var listening = _fixture.Bootstrap.Context.Events.Subscribe<NextStepChanged>(changed =>
            {
                if (progression.PastTeaching)
                {
                    lines.Add(changed.LineKey);
                }
            });

            progression.Restore(new ProgressionSaveData { woke = true, salvaged = true, chasmSeen = true });
            gameplay.Friends.Restore(new FriendsSaveData
            {
                friends = new[] { new FriendSaveData { id = "tilly", state = (int)FriendState.Awake, welcomed = true } },
            });
            _fixture.Display(_fixture.FindRelic("rubber_duck"), 0);
            _fixture.PowerTo(2);
            _fixture.GiveMaterials(40, 40, 40);
            _fixture.Decode(CargoCradle);
            Workshop bay = gameplay.Workshop;
            yield return Waits.Until(() => bay.Awake, 5f);
            Assert.AreEqual(ProgressionKeys.NextBay, progression.NextStep, "the last teaching step: fit a kit");

            _fixture.Rover.Place(Flat(bay.PadCentre), Yaw(-bay.BayForward));
            yield return null;
            yield return null;
            IUpgradeShop shop = _fixture.Bootstrap.Context.Get<IUpgradeShop>();
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase(CargoCradle),
                "the Cradle first: no kit waits for another");
            yield return null;
            Assert.IsTrue(progression.PastTeaching, "a fitted kit ends the teaching steps");
            Assert.AreEqual(ProgressionKeys.NextPowerPart, progression.NextStep, "Power first");
            Assert.AreEqual(TechNodeState.Built, gameplay.Tech.StateOf(TechNodeId.CargoCradle));
            Assert.AreEqual(TechNodeState.Unknown, gameplay.Tech.StateOf(TechNodeId.HoverJump),
                "Hover-Jump's tube still waits at the lip, and nothing asks for it");
            Assert.IsTrue(gameplay.GroundTubes.IsResting(HoverJump));
            Assert.AreEqual(2, gameplay.Recovery.LitLamps(RecoveryTarget.Power));

            gameplay.Relays.Restore(new RelaysSaveData
            {
                masts = new[] { new RelaySaveData { id = "relay.0", paid = 1, restored = true } },
            });
            yield return null;
            Assert.AreEqual(1, gameplay.Recovery.LitLamps(RecoveryTarget.Link), "the mound mast: Link begins early");
            Assert.AreEqual(ProgressionKeys.NextPowerPart, progression.NextStep, "still Power before Link");

            TowerPartField parts = gameplay.TowerPartField;
            yield return Waits.Until(() => parts.IsResting(SparePart), 4f);
            _fixture.Rover.Place(parts.RestPosition(SparePart), 0f);
            yield return Waits.Until(() => gameplay.TowerParts.StateOf(SparePart) == TowerPartState.Carried, 8f);
            yield return null;
            Assert.AreEqual(ProgressionKeys.NextPowerCarried, progression.NextStep);

            RadioTower tower = gameplay.Tower;
            _fixture.Rover.Place(tower.PadCentre, 0f);
            yield return Waits.Until(() => gameplay.TowerParts.StateOf(SparePart) == TowerPartState.Delivered, 4f);
            yield return null;
            Assert.AreEqual(ProgressionKeys.NextPowerBuild, progression.NextStep);
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase(Tower), "level 3, with no leap and no Bell");
            yield return Waits.Until(() => gameplay.Recovery.IsComplete(RecoveryTarget.Power), 30f);
            yield return null;
            StringAssert.StartsWith(ProgressionKeys.NextLink, progression.NextStep, "then the Link toward the Peak");

            CollectionAssert.IsNotEmpty(lines);
            foreach (string line in lines)
            {
                Assert.IsTrue(ProgressionKeys.SpeaksOfTargets(line), $"'{line}' is not about a recovery target");
            }
        }

        private static Vector3 Flat(Vector3 position)
        {
            return new Vector3(position.x, 0f, position.z);
        }

        private static float Yaw(Vector3 direction)
        {
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }
    }
}
