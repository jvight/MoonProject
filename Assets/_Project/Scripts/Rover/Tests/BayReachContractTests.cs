using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Rover.Editor;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// Proves the art contract's reach (docs/ARCHITECTURE.md, "Contract: Kenji's Rover Bay"): with the real built
    /// Rover.prefab parked on the real RoverBay.prefab's turntable, every kit piece's socket (and the point above it
    /// the arm lowers from) lies within reach of a different arm at some turntable turn, and the Hover-Jump coils'
    /// socket sits over the floor arm (category BayReach runs it alone).
    /// </summary>
    [TestFixture]
    [Category("BayReach")]
    public sealed class BayReachContractTests
    {
        private const string BayPrefab = "Assets/_Project/Generated/Art/Base/RoverBay.prefab";

        private GameObject _bay;
        private GameObject _rover;

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_bay);
            Object.DestroyImmediate(_rover);
        }

        [Test]
        public void EveryKitSocket_OnAParked07_IsInReachOfTheBay()
        {
            var bayAsset = AssetDatabase.LoadAssetAtPath<GameObject>(BayPrefab);
            var roverAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RoverAssetPaths.RoverPrefab);
            Assert.IsNotNull(bayAsset, $"{BayPrefab} is not built.");
            Assert.IsNotNull(roverAsset, $"{RoverAssetPaths.RoverPrefab} is not built.");
            _bay = Object.Instantiate(bayAsset);
            _rover = Object.Instantiate(roverAsset);
            var bay = new PrefabBay(_bay.transform);
            _rover.transform.SetPositionAndRotation(bay.TurntablePosition, bay.TurntableRotation);

            var arms = new BayArm[bay.ArmCount];
            for (int i = 0; i < arms.Length; i++)
            {
                arms[i] = BayArm.Read(bay, i, out string problem);
                Assert.IsNotNull(arms[i], $"Art's bay: {problem}");
            }

            var settings = new BayFitSettings();
            Vector3 centre = bay.TurntablePosition;
            Vector3 up = bay.Turntable.up;
            Vector3 front = Vector3.ProjectOnPlane(bay.TurntableRotation * Vector3.back, up).normalized;
            var report = new StringBuilder();
            bool reached = true;
            reached &= Reach(arms, centre, up, front, settings.Hover, RoverKitPiece.LampBar, report, "Kit_LampBar");
            reached &= Reach(arms, centre, up, front, settings.Hover, RoverKitPiece.CapacitorDrums, report,
                "Kit_CapacitorDrum", "Kit_CapacitorDrum");
            reached &= Reach(arms, centre, up, front, settings.Hover, RoverKitPiece.CargoRack, report,
                "Kit_CargoRack");

            Vector3 coils = Find(_rover.transform, "HoverCoils", 0).position;
            bool floor = BayPlanner.TryPlanFloor(bay.FloorLift, bay.FloorTip, centre, up, coils, settings.FloorAlign,
                out float coilTurn, out float rise);
            report.AppendLine($"HoverCoils: floor arm {(floor ? "reaches" : "MISSES")} (turn {coilTurn:0}, rise "
                + $"{rise:0.00} m)");
            Debug.Log("[rover-bay-reach]\n" + report);
            Assert.IsTrue(reached && floor, report.ToString());
        }

        /// <summary>Plans <paramref name="piece"/>, noting each part's best reach to spare (m) in the report.</summary>
        private bool Reach(BayArm[] arms, Vector3 centre, Vector3 up, Vector3 front, float hover,
            RoverKitPiece piece, StringBuilder report, params string[] parts)
        {
            var sockets = new Vector3[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                sockets[i] = Find(_rover.transform, parts[i], i).position;
            }

            var armOf = new int[parts.Length];
            bool planned = BayPlanner.TryPlan(arms, centre, up, front, sockets, parts.Length, hover, armOf,
                out float turn);
            report.Append($"{piece}: {(planned ? $"turn {turn:0}" : "OUT OF REACH")}");
            for (int i = 0; i < sockets.Length; i++)
            {
                report.Append($"; part {i} best spare {BestSlack(arms, centre, up, sockets[i]):0.000} m");
            }

            report.AppendLine();
            return planned;
        }

        /// <summary>The most reach to spare any arm has for <paramref name="socket"/> at any turntable turn.</summary>
        private static float BestSlack(BayArm[] arms, Vector3 centre, Vector3 up, Vector3 socket)
        {
            float best = float.NegativeInfinity;
            float[] turns = { 0f, 90f, 180f, -90f };
            foreach (float turn in turns)
            {
                Vector3 turned = BayPlanner.Turned(socket, centre, up, turn);
                foreach (BayArm arm in arms)
                {
                    best = Mathf.Max(best, BayArmIk.Slack(arm.Geometry, arm.ToShoulder(turned)));
                }
            }

            return best;
        }

        /// <summary>The <paramref name="index"/>-th node called <paramref name="name"/> under the root.</summary>
        private static Transform Find(Transform root, string name, int index)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            int seen = 0;
            foreach (Transform node in all)
            {
                if (node.name == name && seen++ == index)
                {
                    return node;
                }
            }

            Assert.Fail($"{root.name} has no node '{name}' #{index}.");
            return null;
        }

        /// <summary>Art's bay prefab read as the contract names its nodes (a test may look them up by name).</summary>
        private sealed class PrefabBay : IRoverBay
        {
            private readonly Transform[,] _joints;

            public PrefabBay(Transform root)
            {
                Turntable = Find(root, "Turntable", 0);
                Transform floorArm = Find(root, "FloorArm", 0);
                FloorLift = Find(floorArm, "FloorLift", 0);
                FloorTip = Find(FloorLift, "FloorTip", 0);
                int arms = 0;
                while (root.Find("Arm_" + arms) != null)
                {
                    arms++;
                }

                _joints = new Transform[arms, 5];
                for (int i = 0; i < arms; i++)
                {
                    Transform arm = root.Find("Arm_" + i);
                    _joints[i, (int)RoverBayJoint.Yaw] = Find(arm, "Yaw", 0);
                    _joints[i, (int)RoverBayJoint.Upper] = Find(arm, "Upper", 0);
                    _joints[i, (int)RoverBayJoint.Lower] = Find(arm, "Lower", 0);
                    _joints[i, (int)RoverBayJoint.Tip] = Find(arm, "Tip", 0);
                    _joints[i, (int)RoverBayJoint.SparkSocket] = Find(arm, "SparkSocket", 0);
                }
            }

            public Vector3 TurntablePosition => Turntable.position;

            public Quaternion TurntableRotation => Turntable.rotation;

            public Transform Turntable { get; }

            public Transform FloorLift { get; }

            public Transform FloorTip { get; }

            public int ArmCount => _joints.GetLength(0);

            public Transform GetArmJoint(int arm, RoverBayJoint joint)
            {
                return _joints[arm, (int)joint];
            }
        }
    }
}
