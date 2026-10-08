using System.Collections;
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
    /// <summary>Scripted sessions for the friends framework with a stand-in Tilly (rig contract node names).</summary>
    public sealed class FriendSessions : InputTestFixture
    {
        private const string TillyId = "tilly";

        private InputActionAsset _controls;
        private GameplayFixture _fixture;
        private Keyboard _keyboard;

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator Tilly_IsFoundGatheredRepaired_AndFollows07Home()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            Friend tilly = _fixture.Gameplay.Friends.Find(TillyId);
            IFriendRoster roster = _fixture.Bootstrap.Context.Get<IFriendRoster>();
            Assert.AreEqual(2, roster.Count, "Tilly and Bell");
            Assert.AreSame(tilly, roster.Get(0));
            Assert.AreEqual(FriendActivity.Dormant, tilly.Activity);
            float fromHome = SurfaceRules.HorizontalDistance(tilly.Site.Position, Vector3.zero);
            Assert.That(fromHome, Is.InRange(60f, 110f), "Tilly lies 60-110 m from home");

            Vector3 site = tilly.Site.Position;
            _fixture.Rover.Place(Toward(site, Vector3.zero, 20f), Yaw(site, Vector3.zero) + 180f);
            yield return null;
            Press(_keyboard.spaceKey, queueEventOnly: true);
            yield return null;
            Release(_keyboard.spaceKey);
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(1, _fixture.Events.FriendAnswered.Count, "a broken chirp answers the ping");
            Assert.AreEqual(TillyId, _fixture.Events.FriendAnswered[0].Value.FriendId);
            Assert.IsTrue(tilly.Progress.Discovered);
            _fixture.Rover.Aim(site + new Vector3(-4f, 2.5f, -4f), site);
            _fixture.Capture("13-tilly-broken");

            for (int part = 0; part < 3; part++)
            {
                Vector3 spot = tilly.PartRest[part];
                _fixture.Rover.Place(new Vector3(spot.x, 0f, spot.z), 0f);
                yield return new WaitForSeconds(_fixture.FriendTuning.PartFlightDuration + 1f);
                Assert.AreEqual(part + 1, _fixture.Events.FriendPartCollected.Count, $"part {part} gathered");
                FriendPartCollected collected = _fixture.Events.FriendPartCollected[part].Value;
                Assert.AreEqual(part + 1, collected.Collected);
                Assert.AreEqual(3, collected.Total);
            }

            Assert.AreEqual(FriendState.PartsGathering, tilly.Progress.State);
            Assert.IsTrue(tilly.Progress.CanRepair);
            IFriendStatuses statuses = _fixture.Bootstrap.Context.Get<IFriendStatuses>();
            Assert.AreEqual(3, statuses.Status(0).Collected);
            Assert.IsTrue(statuses.Status(0).CanRepair);

            _fixture.Rover.Place(Toward(site, Vector3.zero, 3f), Yaw(site, Vector3.zero) + 180f);
            yield return null;
            yield return null;
            Assert.IsTrue(_fixture.Gameplay.Hints.TryGet(InteractionKind.Repair, out InteractionHint hint));
            Assert.AreEqual(InteractionKind.Repair, _fixture.Gameplay.Hints.Primary.Kind);
            Assert.IsTrue(hint.Ready);

            Press(_keyboard.eKey);
            yield return new WaitForSeconds(_fixture.FriendTuning.RepairHold + 0.2f);
            Release(_keyboard.eKey);
            Assert.AreEqual(1, _fixture.Events.FriendRepairStarted.Count, "holding Interact begins the repair");
            Assert.AreEqual(1, _fixture.Rover.HoldStillCount, "07 holds still while its beam stitches");
            Assert.AreEqual(FriendActivity.Repairing, tilly.Activity);
            yield return new WaitForSeconds(_fixture.Tilly.RepairDuration * 0.5f);
            _fixture.Rover.Aim(site + new Vector3(-3f, 2.2f, -5f), site);
            _fixture.Capture("14-tilly-stitching");
            Assert.That(tilly.RepairProgress, Is.InRange(0.05f, 0.95f));

            RepairSequence sequence = RepairSequence.For(_fixture.Tilly, _fixture.FriendTuning);
            yield return new WaitForSeconds(sequence.Duration - _fixture.Tilly.RepairDuration * 0.5f -
                                            _fixture.FriendTuning.RepairHold + 0.5f);
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount, "07 is free once the stitching is done");
            Assert.AreEqual(1, _fixture.Events.FriendRepaired.Count, "she boots up and looks at 07");
            Assert.AreEqual(FriendState.Awake, tilly.Progress.State);
            Assert.AreEqual(1f, tilly.RepairProgress);
            Assert.Greater(tilly.Position.y, site.y + 0.8f, "up in the air");
            _fixture.Capture("15-tilly-awake");

            Vector3 from = _fixture.Rover.Position;
            float began = Time.time;
            float drive = 12f;
            while (Time.time - began < drive)
            {
                float t = (Time.time - began) / drive;
                _fixture.Rover.MoveTo(Vector3.Lerp(from, new Vector3(0f, 0f, 2f), Ease.InOutSine(t)),
                    Yaw(from, Vector3.zero));
                Assert.Less(Vector3.Distance(tilly.Position, _fixture.Rover.Position), 40f, "she keeps up");
                yield return null;
            }

            Assert.AreEqual(1, _fixture.Events.FriendGreeted.Count, "she greets 07 coming home");
            yield return new WaitForSeconds(_fixture.FriendTuning.GreetDuration + 4f);
            Assert.Less(Vector3.Distance(tilly.Position, _fixture.TillyPerch.position), 1f, "home on her perch");
            Assert.That(tilly.Activity, Is.EqualTo(FriendActivity.Home).Or.EqualTo(FriendActivity.Napping));
            _fixture.Rover.Aim(_fixture.TillyPerch.position + new Vector3(4f, 1.5f, 4f), _fixture.TillyPerch.position);
            _fixture.Capture("16-tilly-home");
        }

        [UnityTest]
        public IEnumerator AwakeTilly_SpotsAnUndiscoveredSite_OnTheNextTrip()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            FriendField friends = _fixture.Gameplay.Friends;
            friends.Restore(new FriendsSaveData
            {
                friends = new[] { new FriendSaveData { id = TillyId, state = (int)FriendState.Awake } },
            });
            Friend tilly = friends.Find(TillyId);
            yield return new WaitForSeconds(0.5f);
            Assert.Less(Vector3.Distance(tilly.Position, _fixture.TillyPerch.position), 0.5f, "a load: on her perch");

            Vector3 stop = Toward(_fixture.FindSite("drill").Position, Vector3.zero, 20f);
            float began = Time.time;
            SalvageSite target = null;
            while (target == null && Time.time - began < 60f)
            {
                float t = Mathf.Clamp01((Time.time - began) / 15f);
                _fixture.Rover.MoveTo(Vector3.Lerp(Vector3.zero, stop, Ease.InOutSine(t)), Yaw(Vector3.zero, stop));
                target = SpottedSite();
                yield return null;
            }

            Assert.Greater(_fixture.Events.FriendSpotted.Count, 0, "07 left home: she came along and spotted things");
            Assert.AreEqual(TillyId, _fixture.Events.FriendSpotted[0].Value.FriendId);
            Assert.IsNotNull(target, "an undiscovered salvage site within her reach is spotted");
            Assert.AreEqual(FriendActivity.Spotting, tilly.Activity);
            Assert.Less(SurfaceRules.HorizontalDistance(target.Position, _fixture.Rover.Position),
                _fixture.FriendTuning.SpotRadius + 1f, "within her spotting radius of 07");
            Assert.IsTrue(target.Discovered, "and it shows on 07's sonar without a ping");
            Assert.IsTrue(target.Relics[0].Discovered, "the relic in its heart counts as found too");
            Assert.AreEqual(0, _fixture.Events.SiteAnswered.Count);
            yield return new WaitForSeconds(1f);
            _fixture.Rover.Aim(_fixture.Rover.Position + new Vector3(-6f, 6f, -8f), target.Position);
            _fixture.Capture("17-tilly-spotting");
        }

        [UnityTest]
        public IEnumerator FriendProgress_SurvivesASaveAndAReboot()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            Friend tilly = _fixture.Gameplay.Friends.Find(TillyId);
            for (int part = 0; part < 2; part++)
            {
                Vector3 spot = tilly.PartRest[part];
                _fixture.Rover.Place(new Vector3(spot.x, 0f, spot.z), 0f);
                yield return new WaitForSeconds(_fixture.FriendTuning.PartFlightDuration + 1f);
            }

            Assert.AreEqual(2, tilly.Progress.Collected);
            Assert.IsTrue(_fixture.Bootstrap.Context.Get<MoonProject.Core.Save.ISaveService>().SaveNow());
            _fixture.Dispose(true);
            yield return null;

            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            tilly = _fixture.Gameplay.Friends.Find(TillyId);
            Assert.AreEqual(FriendState.PartsGathering, tilly.Progress.State);
            Assert.IsTrue(tilly.Progress.IsCollected(0));
            Assert.IsTrue(tilly.Progress.IsCollected(1));
            Assert.IsFalse(tilly.Progress.IsCollected(2));
            Assert.IsFalse(tilly.Parts[0].gameObject.activeSelf, "gathered parts stay gathered");
            Assert.IsTrue(tilly.Parts[2].gameObject.activeSelf);
            Assert.AreEqual(0, _fixture.Events.FriendPartCollected.Count, "a load is not a pickup");
        }

        private SalvageSite SpottedSite()
        {
            foreach (EventRecorder.Timed<FriendSpotted> spotted in _fixture.Events.FriendSpotted)
            {
                foreach (SalvageSite site in _fixture.Gameplay.Salvage.Sites)
                {
                    if (SurfaceRules.HorizontalDistance(spotted.Value.Position, site.Position) < 0.1f)
                    {
                        return site;
                    }
                }
            }

            return null;
        }

        private static Vector3 Toward(Vector3 from, Vector3 to, float distance)
        {
            Vector3 direction = to - from;
            direction.y = 0f;
            Vector3 point = from + direction.normalized * distance;
            point.y = 0f;
            return point;
        }

        private static float Yaw(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }
    }
}
