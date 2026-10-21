using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// 07's winch cable in real physics (opening v3 R6, ruling 4: a physical cable, no beam): a load on skids hooked
    /// behind 07 follows it 100 m over rolling ground with basin slopes (about 19° at the steepest, where the swell and
    /// the ripple meet) along a winding line, at the tow's speed cap, without catching on the ground: never let go,
    /// never lifted free, never flipped, never sunk or flung, and still at the cable's length behind at the end. A load
    /// dragged into a prop's corner stays hooked while the winch pays out slack, and steering wide frees it on the same
    /// hook.
    /// </summary>
    public sealed class TowSessions
    {
        private const float TowMetres = 100f;

        /// <summary>07's top speed (m/s, RoverTuning) times the tow's share of it (BatteryTuning).</summary>
        private const float TopSpeed = 8f;

        private const float WinchHeight = 0.6f;
        private const float GroundStep = 2f;
        private const float GroundWidth = 40f;
        private const float GroundLength = 150f;

        // The rolling ground: one long swell along the drive and a cross ripple, together about 19° at their steepest.
        private const float SwellHeight = 2.4f;
        private const float SwellLength = 9f;
        private const float RippleHeight = 0.6f;
        private const float RippleLength = 6f;

        // The drive winds from side to side as it goes, as a player steers round rocks.
        private const float WindAmplitude = 4f;
        private const float WindLength = 30f;

        private static readonly Vector3 LoadSize = new Vector3(1.6f, 0.9f, 1.2f);
        private const float LoadMass = 300f;

        // The prop-corner session: a post the load is dragged into, 07 crawling at a strained tow.
        private static readonly Vector3 PropSize = new Vector3(0.6f, 2f, 0.6f);
        private const float PropX = 0.8f;
        private const float LoadX = 2f;
        private const float DragSpeed = 1.2f;
        private const float CatchSeconds = 5f;
        private const float WideSeconds = 4f;
        private const float FreeSeconds = 8f;

        /// <summary>Metres from the winch the load's hitch never comes within once the rope has been taut (07's tail).
        /// </summary>
        private const float NeverCloser = 1.5f;

        /// <summary>Metres above the ground a load may ride (its bumps) before it counts as flung.</summary>
        private const float FlungHeight = 1.5f;

        private readonly List<GameObject> _made = new List<GameObject>();
        private readonly List<TowReleased> _released = new List<TowReleased>();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject made in _made)
            {
                Object.Destroy(made);
            }

            _made.Clear();
            _released.Clear();
        }

        [UnityTest]
        public IEnumerator ALoad_Follows07_100mOverBasinSlopes_WithoutCatching()
        {
            var settings = new TowSettings();
            var battery = ScriptableObject.CreateInstance<BatteryTuning>();
            float speed = TopSpeed * battery.TowCap;
            Object.Destroy(battery);
            var events = new EventBus();
            events.Subscribe<TowReleased>(_released.Add);
            Ground();

            var rover = new MovingRover();
            Transform winch = Make("Winch").transform;
            Vector3 start = OnGround(0f, 6f);
            winch.position = start + Vector3.up * WinchHeight;
            var tow = new TowLine(events, settings, winch, rover, new Vector3(0f, 0f, -1000f));
            TowableBody load = Load(settings, OnGround(0f, 6f - 2f));
            tow.Add(load);
            yield return new WaitForFixedUpdate();
            tow.Step(true);
            Assert.IsTrue(tow.IsTowing, "the winch hooks the load behind it");

            float travelled = 0f;
            float along = 6f;
            float highest = 0f;
            float lowestUp = 1f;
            float closest = float.PositiveInfinity;
            bool taut = false;
            Vector3 startLoad = load.Body.position;
            while (travelled < TowMetres)
            {
                yield return new WaitForFixedUpdate();
                // 07 holds the tow's speed along its winding line: the line's sideways swing is part of that speed.
                float step = speed * Time.fixedDeltaTime;
                travelled += step;
                along += step / Mathf.Sqrt(1f + WindSlope(along) * WindSlope(along));
                Vector3 previous = winch.position;
                Vector3 next = OnGround(Wind(along), along) + Vector3.up * WinchHeight;
                winch.position = next;
                rover.Move(next, (next - previous) / Time.fixedDeltaTime);
                tow.FixedStep(Time.fixedDeltaTime);
                Assert.IsTrue(tow.IsTowing, $"the cable still holds the load after {travelled:F0} m");

                Vector3 at = load.Body.position;
                float distance = Vector3.Distance(winch.position, load.Hitch);
                taut |= distance >= tow.Length;
                closest = taut ? Mathf.Min(closest, distance) : closest;

                highest = Mathf.Max(highest, at.y - Height(at.x, at.z));
                lowestUp = Mathf.Min(lowestUp, Vector3.Dot(load.Body.transform.up, Vector3.up));
                Assert.Greater(at.y, Height(at.x, at.z) - LoadSize.y, $"the load never sinks into the ground ({at})");
            }

            Vector3 end = load.Body.position;
            Assert.IsEmpty(_released, "the cable never let the load go");
            Assert.AreEqual(0, tow.Lifts, "the load never caught on the ground, so the winch never lifted it free");
            Assert.Less(highest, FlungHeight, "the load rides the ground, never flung off it");
            Assert.Greater(lowestUp, 0.5f, "the load never rolled over");
            Assert.Greater(closest, NeverCloser, "down the slopes the skids hold it back: it never runs up on 07");
            Assert.Greater(end.z - startLoad.z, along - 6f - settings.CableLength - settings.SnagStretch,
                "the load came the whole way along the line");
            Assert.LessOrEqual(Vector3.Distance(winch.position, load.Hitch), tow.Length + settings.SnagStretch,
                "and trails at the cable's length, not stretched");
        }

        [UnityTest]
        public IEnumerator ALoadCaughtOnAPropsCorner_StaysHooked_AndSteeringWideFreesIt_WithNoReHook()
        {
            var settings = new TowSettings();
            var events = new EventBus();
            events.Subscribe<TowReleased>(_released.Add);
            GameObject floor = Make("Floor");
            floor.AddComponent<BoxCollider>().size = new Vector3(60f, 1f, 60f);
            floor.transform.position = Vector3.down * 0.5f;
            GameObject prop = Make("PropCorner");
            prop.AddComponent<BoxCollider>().size = PropSize;
            prop.transform.position = new Vector3(PropX, PropSize.y * 0.5f, 0f);

            var rover = new MovingRover();
            Transform winch = Make("Winch").transform;
            var tow = new TowLine(events, settings, winch, rover, new Vector3(0f, 0f, -1000f));
            TowableBody load = Load(settings, new Vector3(LoadX, 0f, -2.5f));
            tow.Add(load);
            winch.position = load.Hitch + Vector3.up * WinchHeight;
            yield return new WaitForFixedUpdate();
            tow.Step(true);
            Assert.IsTrue(tow.IsTowing, "hooked behind the prop");
            winch.position = new Vector3(0f, WinchHeight, 1.5f);

            // 07 pulls straight ahead: the load is dragged into the prop's corner and catches there.
            yield return DriveWinch(tow, rover, winch, Vector3.forward, CatchSeconds);
            Assert.IsTrue(tow.IsTowing, "caught, the cable never drops the load");
            Assert.Greater(tow.Length, settings.CableLength, "the winch paid out slack");
            Assert.Less(load.Body.position.z, PropSize.z * 0.5f + 0.2f, "the load is held behind the corner");

            // 07 steers wide of the prop and drives on: the load comes free on the same hook.
            yield return DriveWinch(tow, rover, winch, Vector3.right, WideSeconds);
            yield return DriveWinch(tow, rover, winch, Vector3.forward, FreeSeconds);
            Assert.IsTrue(tow.IsTowing, "still on the same hook");
            Assert.IsEmpty(_released, "never let go, never hooked again");
            Assert.Greater(load.Body.position.z, PropSize.z * 0.5f + 1f, "past the corner, following 07");
        }

        /// <summary>Moves the winch at the tow's crawl along <paramref name="direction"/> for a while.</summary>
        private static IEnumerator DriveWinch(TowLine tow, MovingRover rover, Transform winch, Vector3 direction,
            float seconds)
        {
            for (float t = 0f; t < seconds; t += Time.fixedDeltaTime)
            {
                yield return new WaitForFixedUpdate();
                Vector3 velocity = direction * DragSpeed;
                winch.position += velocity * Time.fixedDeltaTime;
                rover.Move(winch.position, velocity);
                tow.FixedStep(Time.fixedDeltaTime);
            }
        }

        /// <summary>A rolling ground mesh with a mesh collider, the drive running along +z.</summary>
        private void Ground()
        {
            int across = Mathf.RoundToInt(GroundWidth / GroundStep) + 1;
            int along = Mathf.RoundToInt(GroundLength / GroundStep) + 1;
            var vertices = new Vector3[across * along];
            for (int j = 0; j < along; j++)
            {
                for (int i = 0; i < across; i++)
                {
                    float x = -GroundWidth * 0.5f + i * GroundStep;
                    float z = -10f + j * GroundStep;
                    vertices[j * across + i] = new Vector3(x, Height(x, z), z);
                }
            }

            var triangles = new int[(across - 1) * (along - 1) * 6];
            int t = 0;
            for (int j = 0; j < along - 1; j++)
            {
                for (int i = 0; i < across - 1; i++)
                {
                    int a = j * across + i;
                    triangles[t++] = a;
                    triangles[t++] = a + across;
                    triangles[t++] = a + 1;
                    triangles[t++] = a + 1;
                    triangles[t++] = a + across;
                    triangles[t++] = a + across + 1;
                }
            }

            var mesh = new Mesh { vertices = vertices, triangles = triangles };
            mesh.RecalculateNormals();
            GameObject ground = Make("RollingGround");
            ground.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        private TowableBody Load(TowSettings settings, Vector3 at)
        {
            GameObject load = Make("PowerModule");
            load.transform.position = at + Vector3.up * (LoadSize.y * 0.5f + 0.05f);
            load.AddComponent<BoxCollider>().size = LoadSize;
            var body = load.AddComponent<Rigidbody>();
            Transform hitch = Make("TowHitch").transform;
            hitch.SetParent(load.transform, false);
            hitch.localPosition = new Vector3(0f, -LoadSize.y * 0.1f, LoadSize.z * 0.5f);
            return new TowableBody("module.test", body, hitch, LoadMass, settings);
        }

        private static Vector3 OnGround(float x, float z)
        {
            return new Vector3(x, Height(x, z), z);
        }

        private static float Height(float x, float z)
        {
            return SwellHeight * Mathf.Sin(z / SwellLength) + RippleHeight * Mathf.Sin(x / RippleLength + z / SwellLength);
        }

        private static float Wind(float z)
        {
            return WindAmplitude * Mathf.Sin(z * 2f * Mathf.PI / WindLength);
        }

        /// <summary>How far the winding line swings sideways per metre along it.</summary>
        private static float WindSlope(float z)
        {
            float k = 2f * Mathf.PI / WindLength;
            return WindAmplitude * k * Mathf.Cos(z * k);
        }

        private GameObject Make(string name)
        {
            var made = new GameObject(name);
            _made.Add(made);
            return made;
        }

        private sealed class MovingRover : IRoverState
        {
            public Vector3 Position { get; private set; }
            public Quaternion Rotation => Quaternion.identity;
            public Vector3 Velocity { get; private set; }
            public float Speed => Velocity.magnitude;
            public float NormalizedSpeed => 0f;
            public Vector2 DriveInput => Vector2.zero;
            public bool IsGrounded => true;
            public float AirTime => 0f;
            public Vector3 GroundNormal => Vector3.up;

            public void Move(Vector3 position, Vector3 velocity)
            {
                Position = position;
                Velocity = velocity;
            }
        }
    }
}
