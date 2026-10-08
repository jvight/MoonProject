using System;
using System.Collections;
using System.Globalization;
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
    /// Scripted sessions for Bell (docs/features/M3-05) with a stand-in Bell (her rig contract's node names) in the
    /// flat world's canyon anchors: gathering, the repair beat, the unseen walk home, the homecoming, her signals, the
    /// dial, the tape rack and her crackle, her soft solid body; and all of it kept through a save and a reboot.
    /// </summary>
    public sealed class BellSessions : InputTestFixture
    {
        private const string BellId = "bell";
        private const string AfterDark = "after_dark_1";
        private const string DustAndHoney = "dust_and_honey";

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
        public IEnumerator Bell_IsGatheredAndRepaired_WalksHomeUnseen_GreetsAndPointsTheWay()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            FriendField friends = _fixture.Gameplay.Friends;
            Friend bell = friends.Find(BellId);
            var body = (RadioCabinetBody)bell.Body;
            IRadioProgram radio = _fixture.Bootstrap.Context.Get<IRadioProgram>();
            Assert.AreSame(bell, friends.DialFriend);
            WorldAnchor terminus = _fixture.World.Anchor(WorldAnchorIds.CanyonTerminus);
            Assert.Less(Vector3.Distance(bell.Site.Position, terminus.Position + terminus.Forward * 4.85f), 1e-3f,
                "against the terminus wall");
            Assert.AreEqual(-terminus.Forward, bell.Site.Facing, "facing back toward 07 arriving");
            StringAssert.StartsWith("Bell_Broken", SolidBody(friends).name, "lying broken, she is solid to 07");
            for (int part = 0; part < 3; part++)
            {
                WorldAnchor alcove = _fixture.World.Anchor(WorldAnchorIds.CanyonAlcovePrefix + part);
                Assert.Less(SurfaceRules.HorizontalDistance(bell.PartRest[part], alcove.Position), 1e-3f,
                    $"part {part} waits in alcove {part}");
            }

            Vector3 site = bell.Site.Position;
            _fixture.Rover.Place(Flat(site + bell.Site.Facing * 9f), Yaw(bell.Site.Facing * -1f));
            yield return null;
            _fixture.Rover.Aim(site + bell.Site.Facing * 3.5f + Vector3.up * 1.6f, site + Vector3.up * 0.5f);
            _fixture.Capture("22-bell-broken");

            for (int part = 0; part < 3; part++)
            {
                _fixture.Rover.Place(Flat(bell.PartRest[part]), 0f);
                yield return new WaitForSeconds(_fixture.FriendTuning.PartFlightDuration + 1f);
            }

            Assert.AreEqual(3, bell.Progress.Collected);
            Assert.IsFalse(bell.Progress.CanRepair, "her tape is still missing");
            FriendStatus status = friends.Status(bell.Index);
            Assert.AreEqual(0, status.ItemsCollected);
            Assert.AreEqual(1, status.ItemsTotal);

            CassetteSite tape = _fixture.Gameplay.Cassettes.Site(IndexOf(AfterDark));
            _fixture.Rover.Place(Flat(tape.Position + tape.Facing * 1.5f), 0f);
            yield return new WaitForSeconds(_fixture.CassetteTuning.FlightDuration + 1f);
            Assert.AreEqual(AfterDark, _fixture.Events.CassetteCollected[0].Value.CassetteId);
            Assert.AreEqual(1, bell.Progress.ItemsCollected, "the tape is her fourth part");
            Assert.IsTrue(bell.Progress.CanRepair);
            Assert.IsFalse(radio.DialUnlocked);

            _fixture.Rover.Place(Flat(site + bell.Site.Facing * 3f), Yaw(-bell.Site.Facing));
            yield return null;
            yield return null;
            Assert.AreEqual(InteractionKind.Repair, _fixture.Gameplay.Hints.Primary.Kind);
            Press(_keyboard.eKey);
            yield return new WaitForSeconds(_fixture.FriendTuning.RepairHold + 0.2f);
            Release(_keyboard.eKey);
            Assert.AreEqual(BellId, _fixture.Events.FriendRepairStarted[0].Value.FriendId);
            Assert.AreEqual(1, _fixture.Rover.HoldStillCount, "07 holds still under its beam");
            float began = _fixture.Events.FriendRepairStarted[0].Time;
            BellRepairSequence sequence = BellRepairSequence.For(_fixture.Bell, _fixture.BellTuning);
            yield return new WaitForSeconds(began + (sequence.StitchEnd + sequence.TapeIn) * 0.5f - Time.time);
            Transform tapeModel = FindDeep(friends.transform, "Tape_" + AfterDark);
            Assert.IsTrue(tapeModel.gameObject.activeInHierarchy, "07's beam carries the tape to her slot");
            Assert.AreEqual(1, _fixture.Rover.HoldStillCount);
            _fixture.Rover.Aim(site + bell.Site.Facing * 4f + Vector3.up * 1.8f, site + Vector3.up * 0.6f);
            _fixture.Capture("23-bell-tape");

            yield return new WaitForSeconds(began + sequence.SweepStart + 0.2f - Time.time);
            Assert.AreEqual(0, _fixture.Rover.HoldStillCount, "the tape is in: 07 is free");
            Assert.IsFalse(tapeModel.gameObject.activeInHierarchy);
            Assert.AreEqual(BellCue.TapeSlotted, _fixture.Events.BellCued[0].Value.Cue);
            Assert.AreEqual(BellCue.NeedleSwept, _fixture.Events.BellCued[1].Value.Cue);
            yield return new WaitForSeconds(began + sequence.Duration + 0.3f - Time.time);
            Assert.AreEqual(BellId, _fixture.Events.FriendRepaired[0].Value.FriendId, "up on her legs: the jingle");
            Assert.AreEqual(FriendState.Awake, bell.Progress.State);
            Assert.IsTrue(radio.DialUnlocked, "her gift: the dial");
            _fixture.Capture("24-bell-standing");

            yield return new WaitForSeconds(_fixture.BellTuning.DanceDuration + 1.5f);
            Assert.AreEqual(BellLife.Mode.Walking, body.Life.Current, "after her two-step she sets off home");
            Assert.Greater(SurfaceRules.HorizontalDistance(bell.Position, site), 0.5f, "waddling away");
            Assert.IsFalse(bell.IsHome, "watched, she walks");

            var away = new Vector3(150f, 0f, -150f);
            _fixture.Rover.Place(away, 90f);
            _fixture.Rover.Aim(away + new Vector3(-6f, 3f, 0f), away + new Vector3(30f, 0f, 0f));
            yield return new WaitForSeconds(_fixture.FriendTuning.UnseenCheckInterval + 0.1f);
            Assert.IsTrue(bell.IsHome, "out of sight and far from 07: she is home");
            Assert.Less(Vector3.Distance(bell.Position, _fixture.BellCorner.position), 1e-3f, "in her corner");
            BoxCollider standing = SolidBody(friends);
            StringAssert.StartsWith("Bell(", standing.name, "standing, her own box goes with her");
            Assert.Less(SurfaceRules.HorizontalDistance(standing.bounds.center, _fixture.BellCorner.position), 0.5f,
                "07 cannot drive through her at home");
            Assert.AreEqual(0, _fixture.Events.FriendGreeted.Count, "nobody home to greet yet");
            yield return new WaitForSeconds(0.5f);
            Assert.AreEqual(FriendActivity.Napping, bell.Activity, "07 is away: she dozes");

            Vector3 parked = body.DialFront;
            float drive = 8f;
            float start = Time.time;
            while (Time.time - start < drive)
            {
                float t = Ease.InOutSine((Time.time - start) / drive);
                _fixture.Rover.MoveTo(Vector3.Lerp(away, Flat(parked), t), Yaw(Flat(parked) - away));
                yield return null;
            }

            _fixture.Rover.Place(Flat(parked), Yaw(Flat(_fixture.BellCorner.position) - Flat(parked)));
            yield return Until(() => _fixture.Events.FriendGreeted.Count > 0, _fixture.BellTuning.WakeDuration + 4f);
            Assert.AreEqual(BellId, _fixture.Events.FriendGreeted[0].Value.FriendId, "she wakes and greets 07");
            Assert.IsTrue(HasTicker("ticker.bell.home"), "Bell got home before you");
            Assert.IsTrue(bell.Progress.Welcomed);

            yield return Until(() => _fixture.Events.BellSignalPicked.Count > 0, 2f);
            BellSignalPicked picked = _fixture.Events.BellSignalPicked[0].Value;
            Assert.AreEqual(BellSignalTarget.Cassette, picked.Target, "her first signal: the reachable tape");
            CassetteSite basin = _fixture.Gameplay.Cassettes.Site(IndexOf(DustAndHoney));
            Assert.Less(SurfaceRules.HorizontalDistance(picked.Position, basin.Position), 1e-3f,
                "not Slow Orbit up on the ledge: 07 cannot leap yet");
            string bearing = BellSignals.BearingFrom(Vector3.zero, basin.Position)
                .ToString(CultureInfo.InvariantCulture);
            Assert.IsTrue(HasTicker("ticker.bell.signal", bearing), "Bell's picking something up... bearing " +
                                                                     bearing);
            yield return new WaitForSeconds(_fixture.BellTuning.PillarRise);
            Assert.Greater(_fixture.Gameplay.Signals.Pillar.Intensity, _fixture.BellTuning.PillarGlow * 0.5f,
                "an amber pillar stands over it");
            _fixture.Rover.Aim(_fixture.BellCorner.position + _fixture.BellCorner.forward * 5f + Vector3.up * 2f,
                _fixture.BellCorner.position + Vector3.up * 0.8f);
            _fixture.Capture("25-bell-home");
            _fixture.Rover.Aim(new Vector3(0f, 6f, 0f), basin.Position + Vector3.up * 10f);
            _fixture.Capture("26-bell-signal");

            yield return null;
            Assert.AreEqual(InteractionKind.Tune, _fixture.Gameplay.Hints.Primary.Kind, "parked at her dial");
            int changes = _fixture.Events.RadioProgramChanged.Count;
            yield return Click();
            Assert.AreEqual(RadioChannel.TapeDeck, radio.Channel);
            Assert.AreEqual(AfterDark, radio.SelectedTape);
            Assert.AreEqual(changes + 1, _fixture.Events.RadioProgramChanged.Count);
            Assert.AreEqual(BellCue.DialTurned, LastCue());
            yield return new WaitForSeconds(_fixture.BellTuning.NeedleEase * 8f);
            Assert.AreEqual(_fixture.BellTuning.Detent(RadioChannel.TapeDeck), body.Life.Needle, 1f,
                "the needle eased to the Tape Deck");
            yield return Click();
            Assert.AreEqual(RadioChannel.QuietHours, radio.Channel);
            yield return Click();
            Assert.AreEqual(RadioChannel.LumenAfterDark, radio.Channel, "and round again");

            _fixture.Rover.Place(Flat(basin.Position + basin.Facing * 2f), 0f);
            yield return new WaitForSeconds(_fixture.CassetteTuning.FlightDuration + 1f);
            Assert.AreEqual(BellSignalTarget.Cassette, _fixture.Events.BellSignalFound[0].Value.Target);
            Assert.IsTrue(HasTicker(BellSignals.FoundLine0), "Bell says: told you so");
            Assert.AreEqual(2, _fixture.Events.BellSignalPicked.Count, "and she picks the next");
            Assert.AreEqual(BellSignalTarget.Relic, _fixture.Events.BellSignalPicked[1].Value.Target,
                "Ro's log is already open: a relic that has not answered");

            CassetteShelf shelf = _fixture.Gameplay.Shelf;
            Assert.AreEqual(2, shelf.Shown, "both tapes stand on her rack");
            Assert.Less(Vector3.Distance(shelf.TapeIn(0).position,
                _fixture.ShelfSlots[0].position + _fixture.ShelfSlots[0].up * _fixture.BellTuning.ShelfLift), 1e-3f);
            Assert.AreEqual("ShelfTape_" + DustAndHoney, shelf.TapeIn(1).name, "in the order they came");

            int cues = _fixture.Events.BellCued.Count;
            _fixture.Bootstrap.Context.Events.Publish(new RelicDeposited("rubber_duck", Vector3.zero, 1));
            Assert.AreEqual(cues + 1, _fixture.Events.BellCued.Count);
            Assert.AreEqual(BellCue.Crackled, LastCue(), "a new relic: her happy crackle");
        }

        [UnityTest]
        public IEnumerator Bell_StaysHome_WithHerDialSignalAndRack_AfterAReboot()
        {
            string slot = BootstrapHarness.NewTestSlot();
            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            _fixture.Gameplay.Radio.AddTape(AfterDark);
            _fixture.Gameplay.Friends.Restore(new FriendsSaveData
            {
                friends = new[] { new FriendSaveData { id = BellId, state = (int)FriendState.Awake, welcomed = true } },
            });
            yield return null;
            yield return null;
            Friend bell = _fixture.Gameplay.Friends.Find(BellId);
            Assert.IsTrue(bell.IsHome);
            Assert.AreEqual(1, _fixture.Events.BellSignalPicked.Count, "home and welcomed: she listens");
            Assert.IsTrue(_fixture.Gameplay.Radio.TurnDial());
            string target = _fixture.Gameplay.Signals.Signals.Capture().targetId;
            Assert.IsTrue(_fixture.Bootstrap.Context.Get<ISaveService>().SaveNow());
            _fixture.Dispose(true);
            yield return null;

            _fixture = GameplayFixture.Boot(_controls, slot);
            yield return null;
            yield return null;
            bell = _fixture.Gameplay.Friends.Find(BellId);
            IRadioProgram radio = _fixture.Bootstrap.Context.Get<IRadioProgram>();
            Assert.IsTrue(bell.IsHome, "she is in her corner from the first frame");
            Assert.Less(Vector3.Distance(bell.Position, _fixture.BellCorner.position), 1e-3f);
            Assert.IsTrue(radio.DialUnlocked);
            Assert.AreEqual(RadioChannel.TapeDeck, radio.Channel, "the dial stays where it was turned");
            Assert.AreEqual(AfterDark, radio.SelectedTape);
            Assert.AreEqual(0, _fixture.Events.FriendGreeted.Count, "she welcomed 07 long ago");
            Assert.AreEqual(1, _fixture.Events.BellSignalPicked.Count);
            Assert.AreEqual(target, _fixture.Gameplay.Signals.Signals.Capture().targetId, "the same signal");
            Assert.AreEqual(1, _fixture.Gameplay.Shelf.Shown);
            Assert.AreEqual(Vector3.one, _fixture.Gameplay.Shelf.TapeIn(0).localScale, "already in place");
        }

        [UnityTest]
        public IEnumerator Bell_DialIsTappedBy07sBeam_ThenSheTurnsIt()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            FriendField friends = _fixture.Gameplay.Friends;
            friends.Restore(new FriendsSaveData
            {
                friends = new[] { new FriendSaveData { id = BellId, state = (int)FriendState.Awake, welcomed = true } },
            });
            yield return null;
            Friend bell = friends.Find(BellId);
            var body = (RadioCabinetBody)bell.Body;
            IRadioProgram radio = _fixture.Bootstrap.Context.Get<IRadioProgram>();
            Vector3 parked = Flat(body.DialFront);
            _fixture.Rover.Place(parked, Yaw(Flat(_fixture.BellCorner.position) - parked));
            yield return null;
            yield return null;
            Assert.IsTrue(friends.CanTune, "parked in front of her dial");
            RadioChannel before = radio.Channel;
            int cues = _fixture.Events.BellCued.Count;
            int changes = _fixture.Events.RadioProgramChanged.Count;

            Press(_keyboard.eKey);
            yield return null;
            yield return null;
            Release(_keyboard.eKey);
            Assert.IsTrue(friends.TappingDial, "07's beam reaches for her dial");
            Assert.AreEqual(cues, _fixture.Events.BellCued.Count, "no click yet");
            Assert.AreEqual(before, radio.Channel, "she has not turned it yet");
            Assert.AreEqual(changes, _fixture.Events.RadioProgramChanged.Count);
            yield return new WaitForSeconds(_fixture.BellTuning.DialTapTime * 0.5f);
            Assert.Greater(friends.DialBeamLevel, 0.5f, "the beam is out to her dial");
            Press(_keyboard.eKey);
            yield return null;
            Release(_keyboard.eKey);
            _fixture.Rover.Aim(_fixture.BellCorner.position + _fixture.BellCorner.forward * 2.5f +
                               _fixture.BellCorner.right * 1.5f + Vector3.up * 1.6f,
                _fixture.BellCorner.position + Vector3.up * 1.1f);
            _fixture.Capture("26a-bell-dial-tap");

            yield return new WaitForSeconds(_fixture.BellTuning.DialTapTime);
            Assert.IsFalse(friends.TappingDial);
            Assert.AreNotEqual(before, radio.Channel, "then she turns her dial");
            Assert.AreEqual(changes + 1, _fixture.Events.RadioProgramChanged.Count, "one detent: a press mid-tap " +
                                                                                    "never doubles it");
            Assert.AreEqual(cues + 1, _fixture.Events.BellCued.Count);
            Assert.AreEqual(BellCue.DialTurned, LastCue(), "her detented click");
            yield return new WaitForSeconds(0.5f);
            Assert.Less(friends.DialBeamLevel, 0.05f, "the beam lets go");
        }

        /// <summary>A press of Interact at her dial: 07's beam taps it, and she turns it after the tap.</summary>
        private IEnumerator Click()
        {
            Press(_keyboard.eKey);
            yield return null;
            yield return null;
            Release(_keyboard.eKey);
            Assert.IsTrue(_fixture.Gameplay.Friends.TappingDial, "07's beam taps her dial first");
            yield return new WaitForSeconds(_fixture.BellTuning.DialTapTime);
            yield return null;
        }

        private BellCue LastCue()
        {
            return _fixture.Events.BellCued[_fixture.Events.BellCued.Count - 1].Value.Cue;
        }

        private bool HasTicker(string key, string argument = null)
        {
            foreach (EventRecorder.Timed<TickerLine> line in _fixture.Events.TickerLine)
            {
                if (line.Value.Key == key && (argument == null || line.Value.Argument == argument))
                {
                    return true;
                }
            }

            return false;
        }

        private int IndexOf(string cassette)
        {
            CassetteField field = _fixture.Gameplay.Cassettes;
            for (int i = 0; i < field.Count; i++)
            {
                if (field.Definition(i).Id == cassette)
                {
                    return i;
                }
            }

            Assert.Fail($"No cassette '{cassette}'.");
            return -1;
        }

        private static IEnumerator Until(Func<bool> condition, float seconds)
        {
            float deadline = Time.time + seconds;
            while (!condition() && Time.time < deadline)
            {
                yield return null;
            }
        }

        /// <summary>Bell's one live collider (the rig she shows), checked solid to 07 and kinematic.</summary>
        private static BoxCollider SolidBody(FriendField friends)
        {
            BoxCollider[] boxes = friends.transform.Find("Friend_" + BellId).GetComponentsInChildren<BoxCollider>();
            Assert.AreEqual(1, boxes.Length, "one soft box: on the rig she shows");
            Assert.AreEqual(Layers.Prop, boxes[0].gameObject.layer, "solid to 07 like the base's props");
            Assert.IsNotNull(boxes[0].attachedRigidbody);
            Assert.IsTrue(boxes[0].attachedRigidbody.isKinematic, "07 cannot shove her");
            return boxes[0];
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            Assert.Fail($"No '{name}' under {root.name}.");
            return null;
        }

        private static Vector3 Flat(Vector3 point)
        {
            return new Vector3(point.x, 0f, point.z);
        }

        private static float Yaw(Vector3 direction)
        {
            return Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }
    }
}
