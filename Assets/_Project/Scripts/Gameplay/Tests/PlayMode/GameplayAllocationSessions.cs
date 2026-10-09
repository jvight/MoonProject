using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// The feel checklist's last line: a busy steady state of every gameplay system allocates nothing (Tilly and Bell
    /// awake at home, Bell swaying and pointing her signal, cassettes, caches and her tape rack ticking along, a relay
    /// mast lit with its hop pads breathing, the station's reach polled the way Audio and UI poll it), and neither do
    /// the moments 07 works the base's machines in (the bay's hopper feed, the tower's stitching, resting on the
    /// charging dock, tapping Bell's dial).
    /// </summary>
    public sealed class GameplayAllocationSessions : InputTestFixture
    {
        /// <summary>Seconds a held tether press may take to latch (a slow first frame must not eat it).</summary>
        private const float LatchTimeout = 1f;

        private InputActionAsset _controls;
        private GameplayFixture _fixture;
        private Keyboard _keyboard;
        private Mouse _mouse;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
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
        public IEnumerator BusySteadyState_AllocatesNothing()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            gameplay.Friends.Restore(new FriendsSaveData
            {
                friends = new[]
                {
                    new FriendSaveData { id = "tilly", state = (int)FriendState.Awake },
                    new FriendSaveData { id = "bell", state = (int)FriendState.Awake, welcomed = true },
                },
            });
            gameplay.Relays.Restore(new RelaysSaveData
            {
                masts = new[] { new RelaySaveData { id = "relay.0", part = true, paid = 60, restored = true } },
            });
            _fixture.GiveMaterials(4, 0, 0);
            Assert.AreEqual(PurchaseResult.Purchased, gameplay.Upgrades.Purchase("rover.cargo_cradle"));

            // Stow and latch out of the museum shelf's reach, where a press would set the rack's relic down instead.
            _fixture.Rover.Place(new Vector3(0f, 0f, -8f), 0f);
            Relic gnome = gameplay.Relics.Find("garden_gnome");
            gnome.BeginLift();
            gnome.SetLiftPose(new Vector3(0f, 0.6f, 0f), Quaternion.identity, 1f);
            gnome.Surface(Vector3.zero, Vector3.zero);
            yield return new WaitForSeconds(0.8f);
            Vector3 camera = _fixture.Rover.Camera.transform.position;
            _fixture.Rover.Aim(camera, gnome.transform.position);
            yield return null;
            yield return null;
            Press(_mouse.rightButton, queueEventOnly: true);
            yield return Waits.Until(() => gameplay.Cradle.IsSettled, 5f);
            Release(_mouse.rightButton);
            Assert.IsTrue(gameplay.Cradle.IsSettled, "a relic rides in the cradle");

            Relic duck = gameplay.Relics.Find("rubber_duck");
            duck.BeginLift();
            duck.SetLiftPose(new Vector3(0f, 0.6f, 0f), Quaternion.identity, 1f);
            duck.Surface(Vector3.zero, Vector3.zero);
            yield return new WaitForSeconds(0.8f);

            _fixture.Rover.Aim(camera, duck.transform.position);
            yield return null;
            yield return null;
            Press(_mouse.rightButton, queueEventOnly: true);
            yield return Waits.Until(() => gameplay.Tether.State == TetherAimState.Towing, LatchTimeout);
            Assert.AreEqual(TetherAimState.Towing, gameplay.Tether.State, "the second relic goes on the tether");
            Press(_keyboard.spaceKey, queueEventOnly: true);
            yield return null;
            Release(_keyboard.spaceKey);
            yield return new WaitForSeconds(3f);
            Assert.Greater(_fixture.Events.SiteAnswered.Count, 0, "pillars are standing");
            Assert.AreEqual(1, _fixture.Events.BellSignalPicked.Count, "Bell is home and listening");
            Assert.AreEqual(2, gameplay.Relays.Reach.LitCount, "home and the mound relay are lit");

            var updates = new List<Action>();
            foreach (MonoBehaviour part in new MonoBehaviour[]
                     {
                         gameplay.Salvage, gameplay.Relics, gameplay.Sonar, gameplay.Excavation, gameplay.Tether,
                         gameplay.Home, gameplay.Tower, gameplay.Friends, gameplay.Cassettes, gameplay.Logs,
                         gameplay.Signals, gameplay.Shelf, gameplay.Relays,
                     })
            {
                updates.Add(Method(part, "Update"));
            }

            updates.Add(Method(gameplay.Tether, "FixedUpdate"));
            updates.Add(Method(gameplay.Cradle, "LateUpdate"));
            foreach (Relic relic in gameplay.Relics.Relics)
            {
                updates.Add(Method(relic, "Update"));
            }

            IInteractionHints hints = gameplay.Hints;
            Run(updates, hints, 30);
            long before = GC.GetAllocatedBytesForCurrentThread();
            int silent = Run(updates, hints, 300);
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Release(_mouse.rightButton);
            Assert.AreEqual(0, silent, "a hint (reel) is always available while towing");
            Assert.AreEqual(0L, allocated,
                "bytes allocated by 300 frames of salvage, relics, sonar, excavation, tether, the cradle, home, " +
                "tower, friends, " +
                "cassettes, caches, Bell's signals, her rack, the relays, the reach and hints");
        }

        [UnityTest]
        public IEnumerator StationMoments_FeedStitchDockAndDialTap_AllocateNothing()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            gameplay.Friends.Restore(new FriendsSaveData
            {
                friends = new[] { new FriendSaveData { id = "bell", state = (int)FriendState.Awake, welcomed = true } },
            });
            _fixture.GiveMaterials(9, 6, 6);
            IUpgradeShop shop = _fixture.Bootstrap.Context.Get<IUpgradeShop>();

            Workshop bay = gameplay.Workshop;
            _fixture.Rover.Place(Flat(bay.PadCentre), 0f);
            yield return null;
            yield return null;
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase("rover.hover_jump"));
            FeedLook feed = bay.Tuning.FeedLook;
            yield return new WaitForSeconds(feed.BeamLead + feed.Stagger + feed.Flight * 0.3f);
            Assert.Greater(bay.BundlesInFlight, 0, "the bay's hopper is being fed");
            AssertAllocatesNothing(Method(bay, "Update"), "the bay feeding its hopper");
            yield return Waits.Until(() => !bay.Feeding, 2f);

            RadioTower tower = gameplay.Tower;
            _fixture.Rover.Place(tower.PadCentre, 0f);
            yield return null;
            yield return null;
            Assert.AreEqual(PurchaseResult.Purchased, shop.Purchase("radio_tower"));
            yield return Waits.Until(() => tower.StitchLevel > 0.5f, 4f);
            Assert.Greater(tower.StitchLevel, 0.5f, "07's beam stitches up the tower");
            AssertAllocatesNothing(Method(tower, "Update"), "the tower stitching while its stage grows");
            yield return Waits.Until(() => !tower.Crafting, 4f);

            ChargingDock dock = gameplay.Home.Dock;
            _fixture.Rover.Place(Flat(dock.Position), 0f);
            yield return new WaitForSeconds(gameplay.Home.Tuning.DockDelay + 0.3f);
            Assert.IsTrue(dock.Docked, "07 rests on the dock");
            AssertAllocatesNothing(Method(gameplay.Home, "Update"), "home with 07 charging on the dock");

            var bell = (RadioCabinetBody)gameplay.Friends.Find("bell").Body;
            _fixture.Rover.Place(Flat(bell.DialFront), 0f);
            yield return null;
            yield return null;
            Press(_keyboard.eKey);
            yield return null;
            yield return null;
            Release(_keyboard.eKey);
            Assert.IsTrue(gameplay.Friends.TappingDial, "07's beam taps Bell's dial");
            AssertAllocatesNothing(Method(gameplay.Friends, "Update"), "07's beam tapping Bell's dial");
        }

        /// <summary>Warms <paramref name="update"/> up, then asserts 300 more frames of it allocate nothing.</summary>
        private static void AssertAllocatesNothing(Action update, string what)
        {
            for (int frame = 0; frame < 30; frame++)
            {
                update();
            }

            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int frame = 0; frame < 300; frame++)
            {
                update();
            }

            Assert.AreEqual(0L, GC.GetAllocatedBytesForCurrentThread() - before,
                "bytes allocated by 300 frames of " + what);
        }

        private static Vector3 Flat(Vector3 point)
        {
            return new Vector3(point.x, 0f, point.z);
        }

        /// <summary>
        /// Runs the frames and returns how many had no hint at all or found home out of reach (asserted outside the
        /// measurement).
        /// </summary>
        private int Run(List<Action> updates, IInteractionHints hints, int frames)
        {
            int silent = 0;
            for (int frame = 0; frame < frames; frame++)
            {
                float angle = frame * 0.01f;
                _fixture.Rover.MoveTo(new Vector3(Mathf.Sin(angle) * 3f, 0f, Mathf.Cos(angle) * 3f), angle);
                for (int i = 0; i < updates.Count; i++)
                {
                    updates[i]();
                }

                if (hints.Primary.Kind == InteractionKind.None)
                {
                    silent++;
                }

                IStationReach reach = _fixture.Gameplay.Relays.Reach;
                Vector3 rover = _fixture.Rover.Position;
                if (!reach.IsInReach(rover) || reach.DistanceToNearestNode(rover) < 0f)
                {
                    silent++;
                }
            }

            return silent;
        }

        private static Action Method(MonoBehaviour component, string name)
        {
            MethodInfo method = component.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"{component.GetType().Name}.{name}");
            return (Action)Delegate.CreateDelegate(typeof(Action), component, method);
        }
    }
}
