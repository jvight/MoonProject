using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>Scripted sessions for the scrap magnet and the sonar, on the real components behind fakes.</summary>
    public sealed class ScrapAndSonarSessions : InputTestFixture
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
        public IEnumerator ScrapCluster_SpiralsIn_AsARisingArpeggio()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            ScrapField field = _fixture.Gameplay.Scrap;
            Assert.Greater(field.Count, 0);

            int seed = NearestPiece(field, Vector3.zero);
            Vector3 stop = field.RestPosition(seed);
            stop.y = 0f;
            var expected = new List<int>();
            int expectedValue = 0;
            for (int i = 0; i < field.Count; i++)
            {
                if (Vector3.Distance(field.RestPosition(i), stop) <= _fixture.ScrapTuning.MagnetRadius)
                {
                    expected.Add(i);
                    expectedValue += field.ValueOf(i);
                }
            }

            Assert.GreaterOrEqual(expected.Count, 2, "a cluster, not a lone piece");
            int remainingBefore = field.Remaining;
            float arrived = Time.time;
            _fixture.Rover.Place(stop, 0f);
            yield return new WaitForSeconds(0.35f);
            _fixture.Capture("01-scrap-spiralling-in");
            yield return new WaitForSeconds(2.4f);

            List<EventRecorder.Timed<ScrapCollected>> collected = _fixture.Events.ScrapCollected;
            Assert.AreEqual(expected.Count, collected.Count, "every piece in reach is collected, nothing else");
            foreach (int index in expected)
            {
                Assert.IsTrue(field.IsCollected(index));
            }

            float firstDelay = collected[0].Time - arrived;
            Assert.That(firstDelay, Is.InRange(0.4f, 1.6f), "pieces take a soft flight, not a snap");
            for (int i = 0; i < collected.Count; i++)
            {
                Assert.AreEqual(i, collected[i].Value.ComboStep, "each pickup climbs the melody");
                if (i > 0)
                {
                    Assert.GreaterOrEqual(collected[i].Time - collected[i - 1].Time,
                        _fixture.ScrapTuning.MinPickupInterval - 1e-4f, "an arpeggio, never a chord");
                }
            }

            List<EventRecorder.Timed<CurrencyChanged>> currency = _fixture.Events.CurrencyChanged;
            Assert.AreEqual(collected.Count, currency.Count);
            Assert.AreEqual(expectedValue, currency[currency.Count - 1].Value.Total);
            Assert.AreEqual(expectedValue, _fixture.Gameplay.Wallet.Balance);
            Assert.AreEqual(remainingBefore - expected.Count, field.Remaining);
            for (int i = 0; i < _fixture.Events.Order.Count; i += 2)
            {
                Assert.AreEqual(nameof(ScrapCollected), _fixture.Events.Order[i]);
                Assert.AreEqual(nameof(CurrencyChanged), _fixture.Events.Order[i + 1]);
            }
        }

        [UnityTest]
        public IEnumerator ScrapNearby_DrawsAGlance_AndTheMelodyRestartsAfterAPause()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            ScrapField field = _fixture.Gameplay.Scrap;
            int seed = NearestPiece(field, Vector3.zero);
            Vector3 piece = field.RestPosition(seed);
            Vector3 away = piece - new Vector3(0f, piece.y, 0f) + new Vector3(0f, 0f, -7.5f);
            _fixture.Rover.Place(away, 0f);
            yield return null;
            yield return null;
            Assert.IsTrue(_fixture.Rover.TryGetGaze(field, out _, out int priority), "07 glances at glinting scrap");
            Assert.AreEqual(GazePriorities.Glance, priority);

            _fixture.Rover.Place(new Vector3(piece.x, 0f, piece.z), 0f);
            yield return new WaitForSeconds(2.5f);
            int firstChain = _fixture.Events.ScrapCollected.Count;
            Assert.Greater(firstChain, 0);

            int other = NearestPiece(field, new Vector3(piece.x, 0f, piece.z) + new Vector3(40f, 0f, 40f));
            Vector3 next = field.RestPosition(other);
            _fixture.Rover.Place(new Vector3(next.x, 0f, next.z), 0f);
            yield return new WaitForSeconds(1.8f);
            Assert.Greater(_fixture.Events.ScrapCollected.Count, firstChain);
            Assert.AreEqual(0, _fixture.Events.ScrapCollected[firstChain].Value.ComboStep,
                "after a pause of more than the combo window the melody starts again");
        }

        [UnityTest]
        public IEnumerator OpeningView_ScrapGlintsSparkleOnTheHorizon()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            yield return null;
            ScrapField field = _fixture.Gameplay.Scrap;
            GlintTuning tuning = _fixture.GlintTuning;
            int expected = 0;
            Vector3 camera = _fixture.Rover.Camera.transform.position;
            for (int i = 0; i < field.Count; i++)
            {
                float distance = Vector3.Distance(field.RestPosition(i) + Vector3.up * tuning.GlintLift, camera);
                if (distance > tuning.GlintFadeNear && distance < tuning.GlintMaxDistance)
                {
                    expected++;
                }
            }

            Assert.Greater(expected, 20);
            Assert.AreEqual(expected, field.Glints.Drawn, "every resting piece in range glints, in one instanced draw");
            Assert.Greater(field.Glints.Brightest, 0.3f);
            _fixture.Capture("00-opening-scrap-glints");
        }

        [UnityTest]
        public IEnumerator ScrapField_SteadyState_AllocatesNothing()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            ScrapField field = _fixture.Gameplay.Scrap;
            int seed = NearestPiece(field, Vector3.zero);
            Vector3 piece = field.RestPosition(seed);
            _fixture.Rover.Place(new Vector3(piece.x, 0f, piece.z) + new Vector3(0f, 0f, -8f), 0f);
            Action update = UpdateOf(field);
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
                "bytes allocated by 300 frames of scrap idle, glints and glance");
        }

        [UnityTest]
        public IEnumerator Ping_RollsARing_AndRelicsAnswerNearestFirst()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            SonarSystem sonar = _fixture.Gameplay.Sonar;
            SonarTuning tuning = _fixture.SonarTuning;
            Assert.IsTrue(sonar.IsReady);

            var distances = new List<float>();
            foreach (Relic relic in _fixture.Gameplay.Relics.Relics)
            {
                float distance = SurfaceRules.HorizontalDistance(relic.SonarPosition, Vector3.zero);
                if (distance <= tuning.Range)
                {
                    distances.Add(distance);
                }
            }

            distances.Sort();
            Assert.GreaterOrEqual(distances.Count, 2, "the onboarding relics are within reach of the first ping");

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
            List<EventRecorder.Timed<RelicAnswered>> answers = _fixture.Events.RelicAnswered;
            Assert.AreEqual(distances.Count, answers.Count, "every relic in range answers once");
            for (int i = 0; i < answers.Count; i++)
            {
                Assert.AreEqual(distances[i], answers[i].Value.Distance, 0.01f, "nearest first");
                float expected = pingTime + SonarSchedule.RingArrival(distances[i], tuning.RingDuration,
                    tuning.Range) + tuning.AnswerLag;
                Assert.AreEqual(expected, answers[i].Time, 0.1f, "answers when the ring has touched it");
            }

            Assert.IsTrue(_fixture.Rover.GazeLog.Contains(nameof(SonarSystem) + ":" + GazePriorities.Interest),
                "07 turns toward the nearest answer");
            int relicCount = _fixture.Gameplay.Relics.Relics.Count;
            int standing = 0;
            for (int i = 0; i < relicCount; i++)
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
                "a broken friend answers with its own chirp, not as a relic");
            if (tillyInRange)
            {
                Assert.AreEqual("tilly", _fixture.Events.FriendAnswered[0].Value.FriendId);
                Assert.Greater(sonar.Markers[relicCount].PillarIntensity, 0.1f, "and leaves a warm pillar");
                Assert.IsTrue(tilly.Progress.Discovered);
            }
            foreach (Relic relic in _fixture.Gameplay.Relics.Relics)
            {
                bool answered = SurfaceRules.HorizontalDistance(relic.SonarPosition, Vector3.zero) <= tuning.Range;
                Assert.AreEqual(answered, relic.Discovered);
            }

            _fixture.Capture("03-sonar-pillars");
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

        private static Action UpdateOf(MonoBehaviour component)
        {
            MethodInfo method = component.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, $"{component.GetType().Name}.Update");
            return (Action)Delegate.CreateDelegate(typeof(Action), component, method);
        }

        private static int NearestPiece(ScrapField field, Vector3 point)
        {
            int nearest = -1;
            float best = float.MaxValue;
            for (int i = 0; i < field.Count; i++)
            {
                if (field.IsCollected(i))
                {
                    continue;
                }

                float distance = SurfaceRules.HorizontalDistance(field.RestPosition(i), point);
                if (distance < best)
                {
                    best = distance;
                    nearest = i;
                }
            }

            return nearest;
        }
    }
}
