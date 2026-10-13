using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// 07 meets its elder "06" (0.5.5 §1.7, real wiring): the first time it comes near the micro.06 site it looks
    /// once, holding it, with one soft Curious chirp and no text; the meeting is saved, and a saved meeting is never
    /// replayed.
    /// </summary>
    public sealed class RoverElderTests
    {
        private const float Settle = 0.5f;
        private const float Near = 4f;
        private const float Far = 60f;

        private LunarTestPhysics _physics;
        private TestWorld _world;
        private TestRover _rover;
        private readonly List<RoverChirpRequested> _chirps = new List<RoverChirpRequested>();

        [SetUp]
        public void SetUp()
        {
            _physics = new LunarTestPhysics();
            _world = new TestWorld();
            _chirps.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            _rover?.Dispose();
            _rover = null;
            _world.Dispose();
            _physics.Dispose();
        }

        private static IEnumerator Wait(float seconds)
        {
            float until = Time.time + seconds;
            while (Time.time < until)
            {
                yield return null;
            }
        }

        /// <summary>Spawns 07 with 06 <paramref name="elderDistance"/> m to its right and the given saved section.
        /// </summary>
        private void Spawn(float elderDistance, string seededSave)
        {
            Vector3 at = TestWorld.Point(0f, -150f);
            _rover = TestRover.Spawn(_world, at, 0f, prepare: rover =>
            {
                rover.Anchors.PlaceElder(at + Vector3.right * elderDistance);
                if (seededSave != null)
                {
                    rover.Save.Seed("rover.character", seededSave);
                }
            });
            _rover.Context.Events.Subscribe<RoverChirpRequested>(chirp =>
            {
                if (chirp.Mood == ChirpMood.Curious)
                {
                    _chirps.Add(chirp);
                }
            });
        }

        [UnityTest]
        public IEnumerator TheFirstTimeNear06_07LooksOnce_WithASoftCuriousChirp_AndRemembers()
        {
            Spawn(Near, null);
            Vector3 elder = _rover.Anchors.Get(0).Position;
            yield return Wait(Settle);
            Assert.AreEqual(1, _chirps.Count, "One Curious chirp as it notices 06.");
            Assert.AreEqual(ChirpMood.Curious, _chirps[0].Mood);
            Assert.Less(_chirps[0].Intensity, 0.5f, "Soft and low.");
            Assert.IsTrue(_rover.Controller.Gaze.TryGetTop(out Vector3 gaze), "07 looks...");
            Assert.Less(Vector3.Distance(new Vector3(gaze.x, 0f, gaze.z), new Vector3(elder.x, 0f, elder.z)), 0.01f,
                "...at 06.");
            StringAssert.Contains("\"metElder06\":true", _rover.Save.Capture("rover.character"), "Remembered.");

            yield return Wait(_rover.CharacterTuning.ElderGlance.HoldSeconds + 1f);
            Assert.AreEqual(1, _chirps.Count, "Only once, however long it stays near.");
        }

        [UnityTest]
        public IEnumerator Far06_IsNotNoticed()
        {
            Spawn(Far, null);
            yield return Wait(Settle + 1f);
            Assert.IsEmpty(_chirps);
            StringAssert.Contains("\"metElder06\":false", _rover.Save.Capture("rover.character"));
        }

        [UnityTest]
        public IEnumerator ASavedMeeting_IsNeverReplayed()
        {
            Spawn(Near, "{\"metElder06\":true}");
            yield return Wait(Settle + 1f);
            Assert.IsEmpty(_chirps, "A game that has met 06 does not meet it again.");
        }
    }
}
