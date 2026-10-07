using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// Bell: her repair timeline, her life at home (dancing, walking, dozing, waking, greeting, swaying, watching,
    /// crackling, the dial's needle) and her rig.
    /// </summary>
    public sealed class BellTests
    {
        private const float Frame = 0.02f;

        private static readonly Vector3 Corner = new Vector3(-10f, 0f, 0f);

        private readonly List<Object> _created = new List<Object>();
        private BellTuning _bell;
        private FriendTuning _friends;

        [SetUp]
        public void SetUp()
        {
            _bell = Track(ScriptableObject.CreateInstance<BellTuning>());
            _friends = Track(ScriptableObject.CreateInstance<FriendTuning>());
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void Repair_StitchesThenSlidesTheTapeThenWarmsSweepsAndStands_InOrder()
        {
            var repair = new BellRepairSequence(4f, 1.4f, 1.1f, 1.6f, 2.2f);
            Assert.IsTrue(repair.Stitching(3f));
            Assert.IsTrue(repair.Beaming(3f));
            Assert.IsFalse(repair.Stitching(4.5f));
            Assert.IsTrue(repair.Beaming(4.5f), "the beam carries the tape in");
            Assert.AreEqual(0f, repair.Tape(4f), 1e-5f);
            Assert.AreEqual(1f, repair.Tape(repair.TapeIn), 1e-5f);
            Assert.IsFalse(repair.Beaming(repair.TapeIn), "then 07 is free");
            Assert.AreEqual(0f, repair.DialLamp(repair.TapeIn - 0.01f), "dark until the tape is in");
            Assert.AreEqual(1f, repair.DialLamp(repair.TapeIn + 1.1f), "then it holds its glow");
            bool flickered = false;
            for (float t = repair.TapeIn + 0.1f; t < repair.TapeIn + 1.1f; t += 0.01f)
            {
                flickered |= repair.DialLamp(t) == 0f;
            }

            Assert.IsTrue(flickered, "an old valve catching");
            Assert.Greater(repair.SweepStart, repair.TapeIn, "the needle sweeps once the dial is warming");
            Assert.AreEqual(0f, repair.Needle(repair.SweepStart), 1e-5f);
            Assert.AreEqual(1f, repair.Needle(repair.SweepStart + 0.8f), 1e-3f, "across the band");
            Assert.AreEqual(0f, repair.Needle(repair.StandStart), 1e-5f, "and back to Lumen After Dark");
            Assert.AreEqual(0f, repair.Stand(repair.StandStart), 1e-5f);
            Assert.AreEqual(1f, repair.Stand(repair.Duration), 1e-5f, "up on her four legs");
            Assert.IsTrue(repair.Done(repair.Duration));
            Assert.IsFalse(repair.Done(repair.Duration - 0.01f));
        }

        [Test]
        public void Life_DancesThenWalksHome_AndGreetsAtOnceOnHerFirstHomecomingIf07IsThere()
        {
            var bell = new BellLife(_bell, _friends, 1);
            bell.Begin(0f, 0f);
            Assert.AreEqual(BellLife.Mode.Dancing, bell.Current);
            float maxLeg = 0f;
            float t = 0f;
            for (; t < _bell.DanceDuration * 0.9f; t += Frame)
            {
                BellBeat beat = bell.Step(Senses(t, new Vector3(-200f, 0f, 60f), atHome: false));
                maxLeg = Mathf.Max(maxLeg, Mathf.Abs(beat.Pose.Leg(BellPose.FrontLeft)) +
                                           Mathf.Abs(beat.Pose.Leg(BellPose.FrontRight)));
                Assert.AreEqual(_bell.DanceMotor, beat.Motor);
            }

            Assert.Greater(maxLeg, _bell.DanceStep * 0.5f, "a little two-step");
            for (; t < _bell.DanceDuration + 0.1f; t += Frame)
            {
                bell.Step(Senses(t, new Vector3(-200f, 0f, 60f), atHome: false));
            }

            Assert.AreEqual(BellLife.Mode.Walking, bell.Current, "then off home on her own");
            float swing = 0f;
            for (int i = 0; i < 100; i++, t += Frame)
            {
                BellSenses walking = Senses(t, new Vector3(-200f, 0f, 60f), atHome: false);
                walking.Walked = _friends.WalkSpeed * Frame;
                BellBeat beat = bell.Step(walking);
                swing = Mathf.Max(swing, beat.Pose.Leg(BellPose.FrontLeft));
                Assert.AreEqual(beat.Pose.Leg(BellPose.FrontLeft), beat.Pose.Leg(BellPose.RearRight), 1e-4f,
                    "diagonal pairs step together");
            }

            Assert.Greater(swing, _bell.WaddleSwing * 0.5f, "a waddle");
            BellSenses arrived = Senses(t, Corner + new Vector3(0f, 0f, 6f), atHome: true);
            bell.Step(arrived);
            Assert.IsTrue(bell.IsHome);
            Assert.AreEqual(BellLife.Mode.Listening, bell.Current);
            arrived.Now += Frame;
            Assert.IsTrue(bell.Step(arrived).Greeted, "07 is home already: her first homecoming greeting");
            arrived.Now += Frame;
            arrived.Welcomed = true;
            Assert.IsFalse(bell.Step(arrived).Greeted, "once");
        }

        [Test]
        public void Life_DozesWhile07IsAway_ThenWakesDialNeedleStretch_InOrder_AndGreets()
        {
            var bell = new BellLife(_bell, _friends, 2);
            bell.Settle(0f, RadioChannel.TapeDeck);
            float t = Frame;
            BellSenses senses = Senses(t, Corner + new Vector3(0f, 0f, 200f), atHome: true);
            senses.Welcomed = true;
            senses.Channel = RadioChannel.TapeDeck;
            BellBeat beat = bell.Step(senses);
            Assert.AreEqual(BellLife.Mode.Dozing, bell.Current, "07 is away: she dozes");
            Assert.AreEqual(FriendActivity.Napping, beat.Activity);
            for (; t < 20f; t += Frame)
            {
                senses.Now = t;
                beat = bell.Step(senses);
            }

            Assert.AreEqual(_bell.EmberGlow, beat.Pose.DialLamp, 0.01f, "a low ember, never dark");
            Assert.Greater(beat.Pose.DialLamp, 0f);
            Assert.AreEqual(0f, beat.Pose.Needle, 0.5f, "the needle rests");
            Assert.Less(beat.Pose.Bob, 0f, "she settles a little");

            senses.Rover = Corner + new Vector3(0f, 0f, 10f);
            senses.RoverSpeed = 3f;
            senses.Now = t;
            Assert.IsFalse(bell.Step(senses).Greeted, "not before she is awake");
            Assert.AreEqual(BellLife.Mode.Waking, bell.Current, "07 is home: she wakes");
            float start = t;
            float dialMid = -1f;
            float needleAtDialMid = 0f;
            float needleMid = -1f;
            float stretch = 0f;
            bool greeted = false;
            while (t < start + _bell.WakeDuration + 1f)
            {
                t += Frame;
                senses.Now = t;
                senses.RoverSpeed = 0f;
                beat = bell.Step(senses);
                greeted |= beat.Greeted;
                float into = t - start;
                if (dialMid < 0f && into >= _bell.WakeDial * 0.5f)
                {
                    dialMid = beat.Pose.DialLamp;
                    needleAtDialMid = beat.Pose.Needle;
                }

                if (needleMid < 0f && into >= _bell.WakeDial + _bell.WakeNeedle * 0.5f)
                {
                    needleMid = beat.Pose.Needle;
                }

                if (into > _bell.WakeDial + _bell.WakeNeedle)
                {
                    stretch = Mathf.Max(stretch, -beat.Pose.Leg(BellPose.FrontLeft));
                }
            }

            Assert.That(dialMid, Is.InRange(_bell.EmberGlow + 0.05f, _bell.DialGlow - 0.05f), "the dial warms first");
            Assert.Less(needleAtDialMid, 1f, "while the needle still rests");
            Assert.That(needleMid, Is.InRange(5f, _bell.Detent(RadioChannel.TapeDeck) - 5f), "then the needle lifts");
            Assert.Greater(stretch, _bell.StretchLeg * 0.5f, "then a little stretch");
            Assert.IsTrue(greeted, "awake: she greets 07 home");
            Assert.AreEqual(BellLife.Mode.Listening, bell.Current);
            Assert.AreEqual(_bell.Detent(RadioChannel.TapeDeck), bell.Needle, 0.5f, "on the Tape Deck's detent");
        }

        [Test]
        public void Life_DozesAfter07SitsStill_AndWakesWhenItMoves()
        {
            var bell = new BellLife(_bell, _friends, 3);
            bell.Settle(0f, RadioChannel.LumenAfterDark);
            BellSenses senses = Senses(0f, Corner + new Vector3(0f, 0f, 5f), atHome: true);
            senses.Welcomed = true;
            float t = 0f;
            for (; t < _bell.DozeAfter - 1f; t += 0.5f)
            {
                senses.Now = t;
                bell.Step(senses);
            }

            Assert.AreEqual(BellLife.Mode.Listening, bell.Current, "not yet");
            senses.Now = _bell.DozeAfter + 0.5f;
            bell.Step(senses);
            Assert.AreEqual(BellLife.Mode.Dozing, bell.Current, "07 has sat still a long while");
            senses.Now += 0.5f;
            senses.RoverSpeed = 1f;
            Assert.IsFalse(bell.Step(senses).Greeted, "07 never left: no greeting");
            Assert.AreEqual(BellLife.Mode.Waking, bell.Current, "any movement wakes her");
        }

        [Test]
        public void Life_SwaysAndTapsToMusic_AndKeepsStillInQuietHours()
        {
            var bell = new BellLife(_bell, _friends, 4);
            bell.Settle(0f, RadioChannel.LumenAfterDark);
            BellSenses senses = Senses(0f, Corner + new Vector3(0f, 0f, 5f), atHome: true);
            senses.Welcomed = true;
            senses.RoverSpeed = 1f;
            float roll = 0f;
            int taps = 0;
            for (float t = 0f; t < 30f; t += Frame)
            {
                senses.Now = t;
                BellBeat beat = bell.Step(senses);
                roll = Mathf.Max(roll, Mathf.Abs(beat.Pose.Roll));
                taps += beat.FootTapped ? 1 : 0;
            }

            Assert.Greater(roll, _bell.SwayRoll * 0.8f, "she sways to the music");
            Assert.That(taps, Is.InRange(2, 6), "and taps a foot now and then");

            senses.Channel = RadioChannel.QuietHours;
            float quietRoll = 0f;
            int quietTaps = 0;
            for (float t = 30f; t < 60f; t += Frame)
            {
                senses.Now = t;
                BellBeat beat = bell.Step(senses);
                if (t > 33f)
                {
                    quietRoll = Mathf.Max(quietRoll, Mathf.Abs(beat.Pose.Roll));
                }

                quietTaps += beat.FootTapped ? 1 : 0;
            }

            Assert.Less(quietRoll, 0.05f, "Quiet Hours: no swaying");
            Assert.AreEqual(0, quietTaps);
            Assert.AreEqual(_bell.Detent(RadioChannel.QuietHours), bell.Needle, 0.5f, "the needle on its detent");
        }

        [Test]
        public void Life_TurnsHerDialToWatch07Park_WithinHerLimit()
        {
            var bell = new BellLife(_bell, _friends, 5);
            bell.Settle(0f, RadioChannel.LumenAfterDark);
            BellSenses senses = Senses(0f, Corner + new Vector3(6f, 0f, 0f), atHome: true);
            senses.Welcomed = true;
            senses.RoverSpeed = 1f;
            BellBeat beat = default;
            for (float t = 0f; t < 5f; t += Frame)
            {
                senses.Now = t;
                beat = bell.Step(senses);
            }

            Assert.AreEqual(_bell.WatchMaxTurn, beat.Pose.Turn, 0.5f, "07 to her right, past her limit");
            senses.Rover = Corner + new Vector3(2f, 0f, 6f);
            for (float t = 5f; t < 10f; t += Frame)
            {
                senses.Now = t;
                beat = bell.Step(senses);
            }

            Assert.AreEqual(SurfaceRules.Bearing(new Vector3(2f, 0f, 6f)), beat.Pose.Turn, 0.5f, "turned right at 07");
            senses.Rover = Corner + new Vector3(0f, 0f, 25f);
            for (float t = 10f; t < 15f; t += Frame)
            {
                senses.Now = t;
                beat = bell.Step(senses);
            }

            Assert.AreEqual(0f, beat.Pose.Turn, 0.5f, "07 out of sight: back to her corner's facing");
        }

        [Test]
        public void Life_CracklesAtANewRelic_OnlyAtHome_AndThatWakesHer()
        {
            var bell = new BellLife(_bell, _friends, 6);
            bell.Begin(0f, 0f);
            Assert.IsFalse(bell.Crackle(0.1f), "not while dancing at her site");
            bell.Settle(1f, RadioChannel.LumenAfterDark);
            BellSenses senses = Senses(1f, Corner + new Vector3(0f, 0f, 200f), atHome: true);
            senses.Welcomed = true;
            bell.Step(senses);
            Assert.AreEqual(BellLife.Mode.Dozing, bell.Current);
            Assert.IsTrue(bell.Crackle(1.5f));
            Assert.AreEqual(BellLife.Mode.Waking, bell.Current, "a new relic wakes her");
            float jiggle = 0f;
            for (float t = 1.5f; t < 1.5f + _bell.CrackleDuration; t += Frame)
            {
                senses.Now = t;
                jiggle = Mathf.Max(jiggle, Mathf.Abs(bell.Step(senses).Pose.Needle - bell.Needle));
            }

            Assert.Greater(jiggle, _bell.CrackleNeedle * 0.3f, "her needle jiggles happily");
        }

        [Test]
        public void Dial_EasesItsNeedleToEachDetent_WithAClick()
        {
            var bell = new BellLife(_bell, _friends, 7);
            bell.Settle(0f, RadioChannel.LumenAfterDark);
            BellSenses senses = Senses(0f, Corner + new Vector3(0f, 0f, 5f), atHome: true);
            senses.Welcomed = true;
            senses.RoverSpeed = 1f;
            Assert.AreEqual(0f, _bell.Detent(RadioChannel.LumenAfterDark));
            Assert.AreEqual(70f, _bell.Detent(RadioChannel.TapeDeck));
            Assert.AreEqual(140f, _bell.Detent(RadioChannel.QuietHours));
            senses.Channel = RadioChannel.TapeDeck;
            bell.DialClicked(0f);
            float previous = 0f;
            float t = Frame;
            senses.Now = t;
            BellBeat first = bell.Step(senses);
            Assert.Less(first.Pose.Needle, 70f * 0.5f, "eased, not snapped");
            for (; t < 3f; t += Frame)
            {
                senses.Now = t;
                previous = bell.Step(senses).Pose.Needle;
            }

            Assert.AreEqual(70f, previous, 0.5f, "settled on the Tape Deck's detent");
        }

        [Test]
        public void Rig_FindsNestedNodes_BlendsFromAnotherPose_AndPosesOnTop()
        {
            GameObject standing = Model("Bell", false);
            GameObject lying = Model("Bell_Broken", true);
            var rest = new BellRig(standing, 4);
            var broken = new BellRig(lying, 4);
            Assert.AreEqual("TapeSlot", rest.TapeSlot.name);
            Transform body = Find(standing.transform, BellRig.BodyNode);
            Transform needle = Find(standing.transform, BellRig.NeedleNode);
            Transform legFr = Find(standing.transform, "Leg_FR");

            rest.CapturePoseFrom(broken);
            rest.Apply(0f, new BellPose());
            Assert.Less(Quaternion.Angle(Find(lying.transform, BellRig.BodyNode).localRotation, body.localRotation),
                0.01f, "starts from the broken pose");
            rest.Apply(1f, new BellPose());
            Assert.Less(Quaternion.Angle(Quaternion.identity, body.localRotation), 0.01f, "ends standing at rest");

            var pose = new BellPose { Needle = 70f, Bob = 0.05f };
            pose.SetLeg(BellPose.FrontRight, -20f, 30f);
            rest.Apply(1f, pose);
            Assert.AreEqual(70f, needle.localEulerAngles.z, 1e-3f);
            Assert.AreEqual(0.8f, body.localPosition.y, 1e-4f, "the body bobs");
            Assert.AreEqual(0.79f, legFr.localPosition.y, 1e-4f, "with its hips");
            Assert.AreEqual(340f, legFr.localEulerAngles.x, 1e-3f);

            Object.DestroyImmediate(Find(standing.transform, BellRig.TapeSlotNode).gameObject);
            Assert.Throws<InvalidOperationException>(() => new BellRig(standing, 4));
        }

        private static BellSenses Senses(float now, Vector3 rover, bool atHome)
        {
            return new BellSenses
            {
                Now = now,
                DeltaTime = Frame,
                Position = Corner,
                Home = Corner,
                HomeYaw = 0f,
                AtHome = atHome,
                Rover = rover,
                RoverSpeed = 0f,
                Channel = RadioChannel.LumenAfterDark,
            };
        }

        private GameObject Model(string name, bool broken)
        {
            var root = Track(new GameObject(name));
            Transform body = Node(BellRig.BodyNode, root.transform, new Vector3(0f, 0.75f, 0f));
            Node(BellRig.LidNode, body, new Vector3(0f, 0.68f, -0.25f));
            Transform dial = Node("DialFace", body, new Vector3(0f, 0.43f, 0.26f));
            Node(BellRig.NeedleNode, dial, Vector3.zero);
            Lamp(BellRig.DialLampNode, dial);
            Node(BellRig.SpeakerNode, body, new Vector3(0.2f, 0.21f, 0.28f));
            Node(BellRig.TapeSlotNode, body, new Vector3(-0.16f, 0.21f, 0.27f));
            for (int i = 0; i < 4; i++)
            {
                Lamp(BellRig.PartLampPrefix + i, body);
            }

            for (int leg = 0; leg < BellRig.Corners.Length; leg++)
            {
                Transform hip = Node(BellRig.LegPrefix + BellRig.Corners[leg], root.transform,
                    new Vector3(leg % 2 == 0 ? -0.33f : 0.33f, 0.74f, leg < 2 ? 0.15f : -0.15f));
                Node(BellRig.ShinPrefix + BellRig.Corners[leg], hip, new Vector3(0f, -0.35f, 0f));
            }

            if (broken)
            {
                body.localRotation = Quaternion.Euler(-34f, 10f, 0f);
            }

            return root;
        }

        private static Transform Node(string name, Transform parent, Vector3 position)
        {
            var node = new GameObject(name).transform;
            node.SetParent(parent, false);
            node.localPosition = position;
            return node;
        }

        private static void Lamp(string name, Transform parent)
        {
            Node(name, parent, Vector3.zero).gameObject.AddComponent<MeshRenderer>();
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
