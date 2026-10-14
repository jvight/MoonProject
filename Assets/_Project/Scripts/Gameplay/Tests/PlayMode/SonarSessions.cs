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
    /// Scripted sessions for 07's listening (first-hour v2 §3), on the real components behind fakes: no button, it
    /// listens once rested from tower level 1 on, raises no pillar and no ring, blinks each answering place's own lamp
    /// and then stops, turns 07's head toward the nearest answer and releases, and holds a headlamp-lit blink longer.
    /// </summary>
    public sealed class SonarSessions : InputTestFixture
    {
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
        public IEnumerator Listen_BlinksAnsweringPlacesNearestFirst_TurnsAndReleasesGaze_AndRaisesNoPillarOrRing()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            SonarSystem sonar = _fixture.Gameplay.Sonar;
            SonarTuning tuning = _fixture.SonarTuning;
            Press(_keyboard.spaceKey, queueEventOnly: true);
            yield return new WaitForSeconds(tuning.RestSeconds + 0.5f);
            Release(_keyboard.spaceKey);
            Assert.IsEmpty(_fixture.Events.SonarPinged, "before the tower wakes 07 never listens, and space is no ping");

            for (int i = 0; i < sonar.transform.childCount; i++)
            {
                string childName = sonar.transform.GetChild(i).name;
                StringAssert.DoesNotContain("Pillar", childName, "no sky pillar");
                StringAssert.DoesNotContain("Ring", childName, "no ground ring");
            }

            var distances = new List<float>();
            foreach (SalvageSite site in _fixture.Gameplay.Salvage.Sites)
            {
                float distance = SurfaceRules.HorizontalDistance(site.Position, Vector3.zero);
                if (distance <= tuning.Range)
                {
                    distances.Add(distance);
                }
            }

            distances.Sort();
            Assert.GreaterOrEqual(distances.Count, 2, "the depot and the near sites hear the first ping");

            yield return Listen();
            Assert.AreEqual(1, _fixture.Events.SonarPinged.Count, "07 has rested: at tower level 1 it listens");
            float pingTime = _fixture.Events.SonarPinged[0].Time;
            Assert.AreEqual(tuning.Range, _fixture.Events.SonarPinged[0].Value.Range);

            _fixture.Rover.Aim(new Vector3(0f, 26f, -40f), new Vector3(0f, 0f, 45f));
            yield return Waits.Until(() => _fixture.Events.SiteAnswered.Count > 0,
                tuning.SweepDuration + tuning.AnswerLag);
            Assert.IsTrue(_fixture.Rover.TryGetGaze(sonar, out Vector3 gazePoint, out int gazePriority),
                "07 turns toward the nearest answer when it arrives");
            Assert.AreEqual(GazePriorities.Interest, gazePriority);
            Assert.Less(Vector3.Distance(_fixture.FindSite("depot").AnswerPosition, gazePoint), 0.01f,
                "looks at the nearest answering lamp");

            float remaining = Mathf.Max(0f, pingTime + tuning.SweepDuration + tuning.AnswerLag - Time.time);
            yield return new WaitForSeconds(remaining);
            List<EventRecorder.Timed<SiteAnswered>> answers = _fixture.Events.SiteAnswered;
            Assert.AreEqual(1, _fixture.Events.SonarPinged.Count, "sitting still never listens again");
            Assert.AreEqual(distances.Count, answers.Count, "every site in range answers once");
            Assert.IsEmpty(_fixture.Events.RelicAnswered, "a relic in a site's heart lets its site answer for it");
            for (int i = 0; i < answers.Count; i++)
            {
                Assert.AreEqual(distances[i], answers[i].Value.Distance, 0.01f, "nearest first");
                StringAssert.StartsWith("site.", answers[i].Value.SiteId, "each answers with its own id (tone)");
                Assert.IsTrue(answers[i].Value.HoldsRelic, "every site still holds its relic");
                float expected = pingTime + SonarSchedule.Arrival(distances[i], tuning.SweepDuration,
                    tuning.Range) + tuning.AnswerLag;
                Assert.AreEqual(expected, answers[i].Time, 0.1f, "answers when the listen reaches it");
            }

            Assert.IsTrue(_fixture.Rover.GazeLog.Contains(nameof(SonarSystem) + ":" + GazePriorities.Interest),
                "07 turned toward the nearest answer");
            Assert.IsFalse(_fixture.Rover.TryGetGaze(sonar, out _, out _),
                "and released its gaze cleanly back to driving look");

            int relicCount = _fixture.Gameplay.Relics.Relics.Count;
            int firstSite = relicCount + _fixture.Gameplay.Friends.Count;
            int blinking = 0;
            for (int i = firstSite; i < sonar.Blinks.Length; i++)
            {
                if (sonar.Blinks[i].Intensity > 0.1f)
                {
                    blinking++;
                }
            }

            Assert.AreEqual(answers.Count, blinking, "each answering site blinks its own lamp");
            Assert.Greater(_fixture.FindSite("depot").AnswerBlinkIntensity, 0.1f,
                "the wreck's own AnswerLamp mesh glows while blinking");

            Friend tilly = _fixture.Gameplay.Friends.Find("tilly");
            bool tillyInRange = SurfaceRules.HorizontalDistance(tilly.Site.Position, Vector3.zero) <= tuning.Range;
            Assert.AreEqual(tillyInRange ? 1 : 0, _fixture.Events.FriendAnswered.Count,
                "a broken friend answers with its own chirp, not as a site");
            if (tillyInRange)
            {
                Assert.AreEqual("tilly", _fixture.Events.FriendAnswered[0].Value.FriendId);
                Assert.Greater(sonar.Blinks[relicCount].Intensity, 0.1f, "and blinks its own lamp");
                Assert.IsTrue(tilly.Progress.Discovered);
            }

            foreach (SalvageSite site in _fixture.Gameplay.Salvage.Sites)
            {
                bool answered = SurfaceRules.HorizontalDistance(site.Position, Vector3.zero) <= tuning.Range;
                Assert.AreEqual(answered, site.Discovered, site.Id);
                foreach (Relic relic in site.Relics)
                {
                    Assert.AreEqual(answered, relic.Discovered, $"{relic.Definition.Id} is found with its site");
                }
            }

            _fixture.Capture("03-sonar-blinks");

            yield return new WaitForSeconds(tuning.BlinkDuration + 0.2f);
            for (int i = 0; i < sonar.Blinks.Length; i++)
            {
                Assert.IsFalse(sonar.Blinks[i].Blinking(Time.time), $"blink {i} stopped after its slow blinks");
                Assert.AreEqual(0f, sonar.Blinks[i].Intensity, 1e-4f, $"blink {i} faded to zero");
            }

            Assert.AreEqual(0f, _fixture.FindSite("depot").AnswerBlinkIntensity, 1e-4f,
                "the wreck's AnswerLamp mesh also faded back to off");
        }

        [UnityTest]
        public IEnumerator EmptiedSite_FallsSilent_WhileItsLooseRelicAnswersOnItsOwn()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            SalvageSite depot = _fixture.FindSite("depot");
            var taken = new int[depot.Pieces.Count];
            for (int i = 0; i < taken.Length; i++)
            {
                taken[i] = depot.Pieces[i].Number;
            }

            _fixture.Gameplay.Salvage.Restore(new SalvageSaveData
            {
                sites = new[] { new SalvageSiteSaveData { id = depot.Id, taken = taken } },
            });
            Assert.IsTrue(depot.IsPickedClean);
            Assert.IsTrue(depot.AnswersSonar, "picked clean, but its relic still waits in the heart");

            Relic walkman = depot.Relics[0];
            walkman.Restore(RelicState.Loose, 1f, false, depot.Heart + new Vector3(3f, 0.5f, -3f), Quaternion.identity,
                -1);
            Assert.IsFalse(depot.AnswersSonar, "nothing left to find there");
            Assert.IsTrue(walkman.AnswersSonar, "the loose relic answers itself");

            yield return Listen();
            yield return new WaitForSeconds(_fixture.SonarTuning.SweepDuration + _fixture.SonarTuning.AnswerLag);
            foreach (EventRecorder.Timed<SiteAnswered> answer in _fixture.Events.SiteAnswered)
            {
                Assert.AreNotEqual(depot.Id, answer.Value.SiteId, "the emptied depot stays silent");
            }

            Assert.AreEqual(1, _fixture.Events.RelicAnswered.Count);
            Assert.AreEqual(walkman.Definition.Id, _fixture.Events.RelicAnswered[0].Value.RelicId);
            Assert.IsTrue(walkman.Discovered);
        }

        [UnityTest]
        public IEnumerator WarmHeadlamp_HoldsUpTheBlinkAhead_AWhileLonger()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            _fixture.Rover.Grant(RoverAbility.WarmHeadlamp);
            SonarSystem sonar = _fixture.Gameplay.Sonar;
            SonarTuning tuning = _fixture.SonarTuning;
            int firstSite = _fixture.Gameplay.Relics.Relics.Count + _fixture.Gameplay.Friends.Count;
            AnswerBlink ahead = sonar.Blinks[firstSite + _fixture.FindSite("depot").Index];
            AnswerBlink behind = sonar.Blinks[firstSite + _fixture.FindSite("garage").Index];

            yield return Listen();
            yield return new WaitForSeconds(tuning.BlinkDuration + tuning.SweepDuration + tuning.AnswerLag);
            Assert.IsFalse(behind.Blinking(Time.time), "the blink behind 07 fades out on time");
            Assert.IsTrue(ahead.Blinking(Time.time), "the one 07 heads for, in the headlamp, keeps blinking longer");
            yield return new WaitForSeconds(tuning.HeadlampMaxLinger);
            Assert.IsFalse(ahead.Blinking(Time.time), "but only a little longer");
        }

        [UnityTest]
        public IEnumerator FirstListen_SaysItsLineOnce_AndOnlyANewPlaceListensAgain()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            SonarTuning tuning = _fixture.SonarTuning;
            yield return Listen();
            Assert.AreEqual(0, FirstListenLines(), "at home the first listen says nothing (text audit 0.5.5 §3.4)");

            var near = new Vector3(0f, 0f, tuning.ListenSpacing * 0.5f);
            yield return Drive(Vector3.zero, near);
            yield return new WaitForSeconds(tuning.RestSeconds + 1f);
            Assert.AreEqual(1, _fixture.Events.SonarPinged.Count, "not far enough from the last listen");

            var far = new Vector3(0f, 0f, tuning.ListenSpacing * 1.2f);
            yield return Drive(near, far);
            yield return new WaitForSeconds(tuning.RestSeconds * 0.5f);
            Assert.AreEqual(1, _fixture.Events.SonarPinged.Count, "a new place waits for the rest");
            yield return Waits.Until(() => _fixture.Events.SonarPinged.Count > 1, tuning.RestSeconds + 1f);
            Assert.AreEqual(2, _fixture.Events.SonarPinged.Count, "a new place listens once");
            Assert.AreEqual(far.z, _fixture.Events.SonarPinged[1].Value.Origin.z, 0.01f, "from where 07 rests");
            Assert.AreEqual(0, FirstListenLines(), "nor near home, with no wreck answering");
        }

        /// <summary>Brings the tower to level 1 (if it is not yet) and waits until 07, resting, listens.</summary>
        private IEnumerator Listen()
        {
            int before = _fixture.Events.SonarPinged.Count;
            _fixture.PowerTo(1);
            yield return Waits.Until(() => _fixture.Events.SonarPinged.Count > before,
                _fixture.SonarTuning.RestSeconds + 1f);
            Assert.Greater(_fixture.Events.SonarPinged.Count, before, "07 listens once it has rested at level 1");
        }

        /// <summary>Drives 07 from <paramref name="from"/> to <paramref name="to"/> at a walking pace, then stops.</summary>
        private IEnumerator Drive(Vector3 from, Vector3 to)
        {
            const float speed = 6f;
            float seconds = Vector3.Distance(from, to) / speed;
            float began = Time.time;
            while (Time.time - began < seconds)
            {
                _fixture.Rover.MoveTo(Vector3.Lerp(from, to, (Time.time - began) / seconds), 0f);
                yield return null;
            }

            _fixture.Rover.MoveTo(to, 0f);
            yield return null;
        }

        private int FirstListenLines()
        {
            int lines = 0;
            foreach (EventRecorder.Timed<TickerLine> line in _fixture.Events.TickerLine)
            {
                lines += line.Value.Key == ProgressionKeys.FirstListen ? 1 : 0;
            }

            return lines;
        }
    }
}
