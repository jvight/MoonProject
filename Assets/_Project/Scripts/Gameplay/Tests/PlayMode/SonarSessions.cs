using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>Scripted sessions for the sonar, on the real components behind fakes.</summary>
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
        public IEnumerator Ping_RollsARing_AndSitesAnswerNearestFirst()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            SonarSystem sonar = _fixture.Gameplay.Sonar;
            SonarTuning tuning = _fixture.SonarTuning;
            Assert.IsTrue(sonar.IsReady);

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

            yield return Tap();
            Assert.AreEqual(1, _fixture.Events.SonarPinged.Count, "space pings at once");
            float pingTime = _fixture.Events.SonarPinged[0].Time;
            Assert.AreEqual(tuning.Range, _fixture.Events.SonarPinged[0].Value.Range);
            Assert.IsFalse(sonar.IsReady, "then the calm cooldown runs");

            _fixture.Rover.Aim(new Vector3(0f, 26f, -40f), new Vector3(0f, 0f, 45f));
            yield return new WaitForSeconds(0.6f);
            Assert.IsTrue(sonar.Rings[0].Active);
            Assert.Greater(sonar.Rings[0].Radius, 10f, "the ring rolls outward");
            Assert.Greater(sonar.Rings[0].Intensity, 0.1f);
            _fixture.Capture("02-sonar-ring");

            yield return new WaitForSeconds(tuning.RingDuration + tuning.AnswerLag);
            List<EventRecorder.Timed<SiteAnswered>> answers = _fixture.Events.SiteAnswered;
            Assert.AreEqual(distances.Count, answers.Count, "every site in range answers once");
            Assert.IsEmpty(_fixture.Events.RelicAnswered, "a relic in a site's heart lets its site answer for it");
            for (int i = 0; i < answers.Count; i++)
            {
                Assert.AreEqual(distances[i], answers[i].Value.Distance, 0.01f, "nearest first");
                StringAssert.StartsWith("site.", answers[i].Value.SiteId, "each answers with its own id (tone)");
                Assert.IsTrue(answers[i].Value.HoldsRelic, "every site still holds its relic");
                float expected = pingTime + SonarSchedule.RingArrival(distances[i], tuning.RingDuration,
                    tuning.Range) + tuning.AnswerLag;
                Assert.AreEqual(expected, answers[i].Time, 0.1f, "answers when the ring has touched it");
            }

            Assert.IsTrue(_fixture.Rover.GazeLog.Contains(nameof(SonarSystem) + ":" + GazePriorities.Interest),
                "07 turns toward the nearest answer");
            int relicCount = _fixture.Gameplay.Relics.Relics.Count;
            int firstSite = relicCount + _fixture.Gameplay.Friends.Count;
            int standing = 0;
            for (int i = firstSite; i < sonar.Markers.Length; i++)
            {
                if (sonar.Markers[i].PillarIntensity > 0.1f)
                {
                    standing++;
                }
            }

            Assert.AreEqual(answers.Count, standing, "each answer leaves a light pillar on the horizon");
            Friend tilly = _fixture.Gameplay.Friends.Find("tilly");
            bool tillyInRange = SurfaceRules.HorizontalDistance(tilly.Site.Position, Vector3.zero) <= tuning.Range;
            Assert.AreEqual(tillyInRange ? 1 : 0, _fixture.Events.FriendAnswered.Count,
                "a broken friend answers with its own chirp, not as a site");
            if (tillyInRange)
            {
                Assert.AreEqual("tilly", _fixture.Events.FriendAnswered[0].Value.FriendId);
                Assert.Greater(sonar.Markers[relicCount].PillarIntensity, 0.1f, "and leaves a warm pillar");
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

            _fixture.Capture("03-sonar-pillars");
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

            yield return Tap();
            yield return new WaitForSeconds(_fixture.SonarTuning.RingDuration + _fixture.SonarTuning.AnswerLag);
            foreach (EventRecorder.Timed<SiteAnswered> answer in _fixture.Events.SiteAnswered)
            {
                Assert.AreNotEqual(depot.Id, answer.Value.SiteId, "the emptied depot stays silent");
            }

            Assert.AreEqual(1, _fixture.Events.RelicAnswered.Count);
            Assert.AreEqual(walkman.Definition.Id, _fixture.Events.RelicAnswered[0].Value.RelicId);
            Assert.IsTrue(walkman.Discovered);
        }

        [UnityTest]
        public IEnumerator EarlyPress_IsRemembered_AndFiresWhenTheCooldownEnds()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            SonarTuning tuning = _fixture.SonarTuning;

            yield return Tap();
            float first = _fixture.Events.SonarPinged[0].Time;
            yield return new WaitForSeconds(0.5f);
            yield return Tap();
            yield return new WaitForSeconds(tuning.Cooldown);
            Assert.AreEqual(1, _fixture.Events.SonarPinged.Count, "a press far too early is ignored, not queued");
            Assert.IsTrue(_fixture.Gameplay.Sonar.IsReady);

            yield return Tap();
            Assert.AreEqual(2, _fixture.Events.SonarPinged.Count);
            float second = _fixture.Events.SonarPinged[1].Time;
            Assert.GreaterOrEqual(second - first, tuning.Cooldown);

            yield return new WaitForSeconds(tuning.Cooldown - tuning.InputBuffer * 0.5f);
            yield return Tap();
            Assert.AreEqual(2, _fixture.Events.SonarPinged.Count, "not yet: the cooldown is still running");
            yield return new WaitForSeconds(tuning.InputBuffer);
            Assert.AreEqual(3, _fixture.Events.SonarPinged.Count, "a press just before the cooldown ends is kept");
            Assert.AreEqual(second + tuning.Cooldown, _fixture.Events.SonarPinged[2].Time, 0.05f,
                "and fires as soon as the sonar is ready");
        }

        private IEnumerator Tap()
        {
            Press(_keyboard.spaceKey, queueEventOnly: true);
            yield return null;
            Release(_keyboard.spaceKey);
        }
    }
}
