using System;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// A stand-in Kenji's Rover Bay built in code to the art contract (docs/ARCHITECTURE.md, "Contract: Kenji's Rover
    /// Bay") with art's measures taken from the turntable's top, which lies flush with the test plain: three gantry
    /// arms on the crane rail, each Arm_n/Yaw/Upper/Lower/Tip/SparkSocket with the contract's links (1.30/1.20/0.26 m)
    /// in art's rest pose folded along the rail, and a floor arm whose tip rests just under the turntable's centre.
    /// It is the <see cref="IRoverBay"/> the rover drives, with plain shapes so captures read as a bay: the walls on
    /// three sides and the roof are solid on the Prop layer (as gameplay makes art's bay), the rail and the arms are
    /// not. 07 parks facing into it, its open front behind.
    /// </summary>
    public sealed class TestRoverBay : IRoverBay, IDisposable
    {
        public const float RailHeight = 2.93f;
        public const float UpperLength = 1.3f;
        public const float LowerLength = 1.2f;
        public const float TipLength = 0.26f;

        /// <summary>Links too short to reach the kit sockets (art's first bay: the reach bug).</summary>
        public const float ShortUpperLength = 0.9f;
        public const float ShortLowerLength = 0.8f;

        /// <summary>The folded rest pose (Upper, Lower, Tip pitch, deg), as art's bay.</summary>
        private static readonly Vector3 RestPose = new Vector3(-90f, 170f, -80f);

        /// <summary>Each shoulder's turn folding its arm along the rail at rest (deg), as art's bay.</summary>
        private static readonly float[] FoldYaw = { -90f, -90f, 90f };

        /// <summary>Bay-local shoulders on the rail, as art's bay (+Z is its open front).</summary>
        private static readonly Vector3[] Shoulders =
        {
            new Vector3(-1.3f, RailHeight, -0.35f), new Vector3(0f, RailHeight, -1.65f),
            new Vector3(1.3f, RailHeight, -0.35f),
        };

        private static readonly Vector3 TurntableCentre = new Vector3(0f, 0f, -0.35f);
        private const float TurntableRadius = 1.75f;
        private const float FloorTipRest = -0.015f;
        private const float FloorTipRise = 0.03f;
        private const float HalfWidth = 2.2f;
        private const float FrontZ = 1.9f;
        private const float BackZ = -2.5f;
        private const float EaveHeight = 3.25f;
        private const float Wall = 0.1f;
        private const float Link = 0.08f;

        private readonly GameObject _root;
        private readonly Transform[,] _joints;
        private readonly Quaternion[,] _rest;
        private readonly Quaternion _parked;

        private TestRoverBay(GameObject root, Transform turntable, Transform floorLift, Transform floorTip,
            Transform[,] joints)
        {
            _root = root;
            Turntable = turntable;
            FloorLift = floorLift;
            FloorTip = floorTip;
            _joints = joints;
            _parked = turntable.rotation;
            _rest = new Quaternion[joints.GetLength(0), joints.GetLength(1)];
            for (int arm = 0; arm < joints.GetLength(0); arm++)
            {
                for (int joint = 0; joint < joints.GetLength(1); joint++)
                {
                    _rest[arm, joint] = joints[arm, joint].localRotation;
                }
            }
        }

        public Transform Root => _root.transform;

        public Vector3 TurntablePosition => Turntable.position;

        public Quaternion TurntableRotation => _parked;

        public Transform Turntable { get; }

        public Transform FloorLift { get; }

        public Transform FloorTip { get; }

        public int ArmCount => Shoulders.Length;

        /// <summary>Which way 07 looks out of the bay when it parks (opposite its parked facing).</summary>
        public Vector3 OpenFront => Root.forward;

        public Transform GetArmJoint(int arm, RoverBayJoint joint)
        {
            return _joints[arm, (int)joint];
        }

        /// <summary>
        /// True when <paramref name="world"/> lies within the side walls, in front of the back wall and under the
        /// roof (the open front is no wall: out past it counts).
        /// </summary>
        public bool WithinWalls(Vector3 world)
        {
            Vector3 local = Root.InverseTransformPoint(world);
            return Mathf.Abs(local.x) < HalfWidth && local.z > BackZ && local.y < EaveHeight;
        }

        /// <summary>The largest turn (deg) of any joint of <paramref name="arm"/> away from its rest pose.</summary>
        public float OffRest(int arm)
        {
            float largest = 0f;
            for (int joint = 0; joint < _joints.GetLength(1); joint++)
            {
                largest = Mathf.Max(largest, Quaternion.Angle(_rest[arm, joint], _joints[arm, joint].localRotation));
            }

            return largest;
        }

        /// <summary>The tip's end of <paramref name="arm"/> (where it holds a part), world.</summary>
        public Vector3 TipEnd(int arm)
        {
            return GetArmJoint(arm, RoverBayJoint.Tip).TransformPoint(new Vector3(0f, -TipLength, 0f));
        }

        /// <summary>
        /// Builds the bay with its turntable's centre on <paramref name="centre"/>, 07 parking facing
        /// <paramref name="parkedYaw"/> (deg), and links of the given lengths (the contract's by default).
        /// </summary>
        public static TestRoverBay Build(Vector3 centre, float parkedYaw, Material material,
            float upperLength = UpperLength, float lowerLength = LowerLength)
        {
            var root = new GameObject("TestRoverBay");
            Quaternion rotation = Quaternion.Euler(0f, parkedYaw + 180f, 0f);
            root.transform.SetPositionAndRotation(centre - rotation * TurntableCentre, rotation);
            Transform bay = root.transform;

            Transform turntable = Node("Turntable", bay, TurntableCentre);
            turntable.localRotation = Quaternion.Euler(0f, 180f, 0f);
            Shape(PrimitiveType.Cylinder, turntable, new Vector3(0f, -0.02f, 0f),
                new Vector3(2f * TurntableRadius, 0.02f, 2f * TurntableRadius), material);
            Shape(PrimitiveType.Cube, turntable, new Vector3(0f, 0.005f, 1.2f), new Vector3(0.12f, 0.02f, 0.6f),
                material);

            var joints = new Transform[Shoulders.Length, 5];
            for (int i = 0; i < Shoulders.Length; i++)
            {
                Transform arm = Node("Arm_" + i, bay, Shoulders[i]);
                Vector3 toCentre = TurntableCentre - Shoulders[i];
                arm.localRotation = Quaternion.Euler(0f, Mathf.Atan2(toCentre.x, toCentre.z) * Mathf.Rad2Deg, 0f);
                Transform yaw = Node("Yaw", arm, Vector3.zero);
                yaw.localRotation = Quaternion.Euler(0f, FoldYaw[i], 0f);
                Transform upper = Node("Upper", yaw, Vector3.zero);
                upper.localRotation = Quaternion.Euler(RestPose.x, 0f, 0f);
                Transform lower = Node("Lower", upper, new Vector3(0f, -upperLength, 0f));
                lower.localRotation = Quaternion.Euler(RestPose.y, 0f, 0f);
                Transform tip = Node("Tip", lower, new Vector3(0f, -lowerLength, 0f));
                tip.localRotation = Quaternion.Euler(RestPose.z, 0f, 0f);
                Transform spark = Node("SparkSocket", tip, new Vector3(0f, -TipLength, 0f));
                Bar(upper, upperLength, material);
                Bar(lower, lowerLength, material);
                Bar(tip, TipLength, material);
                joints[i, (int)RoverBayJoint.Upper] = upper;
                joints[i, (int)RoverBayJoint.Lower] = lower;
                joints[i, (int)RoverBayJoint.Tip] = tip;
                joints[i, (int)RoverBayJoint.SparkSocket] = spark;
                joints[i, (int)RoverBayJoint.Yaw] = yaw;
            }

            Transform floorArm = Node("FloorArm", bay, TurntableCentre + Vector3.up * (FloorTipRest - FloorTipRise));
            Transform floorLift = Node("FloorLift", floorArm, Vector3.zero);
            Transform floorTip = Node("FloorTip", floorLift, Vector3.up * FloorTipRise);
            Shape(PrimitiveType.Cube, floorTip, Vector3.zero, new Vector3(0.3f, 0.04f, 0.3f), material);

            BuildShell(bay, material);
            return new TestRoverBay(root, turntable, floorLift, floorTip, joints);
        }

        /// <summary>Back and side walls and the roof (solid), and the crane rail, so captures read as a bay.</summary>
        private static void BuildShell(Transform bay, Material material)
        {
            float depth = FrontZ - BackZ;
            float middle = 0.5f * (FrontZ + BackZ);
            Solid(Shape(PrimitiveType.Cube, bay, new Vector3(0f, 0.5f * EaveHeight, BackZ),
                new Vector3(2f * HalfWidth, EaveHeight, Wall), material));
            Solid(Shape(PrimitiveType.Cube, bay, new Vector3(-HalfWidth, 0.5f * EaveHeight, middle),
                new Vector3(Wall, EaveHeight, depth), material));
            Solid(Shape(PrimitiveType.Cube, bay, new Vector3(HalfWidth, 0.5f * EaveHeight, middle),
                new Vector3(Wall, EaveHeight, depth), material));
            Solid(Shape(PrimitiveType.Cube, bay, new Vector3(0f, EaveHeight, middle),
                new Vector3(2f * HalfWidth, Wall, depth), material));
            Shape(PrimitiveType.Cube, bay, new Vector3(0f, RailHeight + 0.06f, -0.35f),
                new Vector3(2.9f, 0.08f, 0.1f), material);
            Shape(PrimitiveType.Cube, bay, new Vector3(0f, RailHeight + 0.06f, -1.0f),
                new Vector3(0.1f, 0.08f, 1.4f), material);
        }

        private static void Bar(Transform joint, float length, Material material)
        {
            Shape(PrimitiveType.Cube, joint, new Vector3(0f, -0.5f * length, 0f), new Vector3(Link, length, Link),
                material);
        }

        private static Transform Node(string name, Transform parent, Vector3 localPosition)
        {
            var node = new GameObject(name).transform;
            node.SetParent(parent, false);
            node.localPosition = localPosition;
            return node;
        }

        /// <summary>A plain shape without a collider.</summary>
        private static GameObject Shape(PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 scale,
            Material material)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(shape.GetComponent<Collider>());
            shape.GetComponent<MeshRenderer>().sharedMaterial = material;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = localPosition;
            shape.transform.localScale = scale;
            return shape;
        }

        /// <summary>Makes a shape solid scenery on the Prop layer, as gameplay makes art's bay.</summary>
        private static void Solid(GameObject shape)
        {
            shape.layer = Layers.Prop;
            shape.AddComponent<BoxCollider>();
        }

        public void Dispose()
        {
            Object.DestroyImmediate(_root);
        }
    }
}
