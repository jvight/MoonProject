using System;
using UnityEngine;
using MoonProject.Core;
using Object = UnityEngine.Object;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// A bare <see cref="IRoverBay"/> of empty transforms to the art contract (Turntable, Arm_n/Yaw/Upper/Lower/Tip/
    /// SparkSocket in the folded rest pose, FloorArm/FloorLift/FloorTip) at the current bay's shoulders, with the
    /// turntable's centre at the origin and 07 parking facing -Z (the open front is +Z).
    /// </summary>
    public sealed class StandInBay : IRoverBay, IDisposable
    {
        public const float RailHeight = 3.06f;
        public const float FloorTipDepth = 0.4f;

        private static readonly Vector3[] Shoulders =
        {
            new Vector3(-1.3f, RailHeight, 0f), new Vector3(0f, RailHeight, -1.3f), new Vector3(1.3f, RailHeight, 0f),
        };

        private readonly GameObject _root;
        private readonly Transform[,] _joints = new Transform[3, 5];

        public StandInBay(float upper, float lower, float tip)
        {
            _root = new GameObject("StandInBay");
            Turntable = Node("Turntable", _root.transform, Vector3.zero);
            Turntable.localRotation = Quaternion.Euler(0f, 180f, 0f);
            for (int i = 0; i < Shoulders.Length; i++)
            {
                Transform arm = Node("Arm_" + i, _root.transform, Shoulders[i]);
                arm.localRotation = Quaternion.Euler(0f, Mathf.Atan2(-Shoulders[i].x, -Shoulders[i].z) * Mathf.Rad2Deg,
                    0f);
                Transform yaw = Node("Yaw", arm, Vector3.zero);
                Transform upperJoint = Node("Upper", yaw, Vector3.zero);
                upperJoint.localRotation = Quaternion.Euler(80f, 0f, 0f);
                Transform lowerJoint = Node("Lower", upperJoint, new Vector3(0f, -upper, 0f));
                lowerJoint.localRotation = Quaternion.Euler(-160f, 0f, 0f);
                Transform tipJoint = Node("Tip", lowerJoint, new Vector3(0f, -lower, 0f));
                tipJoint.localRotation = Quaternion.Euler(80f, 0f, 0f);
                _joints[i, (int)RoverBayJoint.Yaw] = yaw;
                _joints[i, (int)RoverBayJoint.Upper] = upperJoint;
                _joints[i, (int)RoverBayJoint.Lower] = lowerJoint;
                _joints[i, (int)RoverBayJoint.Tip] = tipJoint;
                _joints[i, (int)RoverBayJoint.SparkSocket] = Node("SparkSocket", tipJoint, new Vector3(0f, -tip, 0f));
            }

            Transform floorArm = Node("FloorArm", _root.transform, Vector3.down * (FloorTipDepth + 0.2f));
            FloorLift = Node("FloorLift", floorArm, Vector3.zero);
            FloorTip = Node("FloorTip", FloorLift, Vector3.up * 0.2f);
        }

        public Vector3 TurntablePosition => Turntable.position;

        public Quaternion TurntableRotation => Turntable.rotation;

        public Transform Turntable { get; }

        public Transform FloorLift { get; }

        public Transform FloorTip { get; }

        public int ArmCount => Shoulders.Length;

        public Transform GetArmJoint(int arm, RoverBayJoint joint)
        {
            return _joints[arm, (int)joint];
        }

        /// <summary>Breaks the contract: arm <paramref name="arm"/> loses <paramref name="joint"/>.</summary>
        public void Remove(int arm, RoverBayJoint joint)
        {
            _joints[arm, (int)joint] = null;
        }

        private static Transform Node(string name, Transform parent, Vector3 localPosition)
        {
            var node = new GameObject(name).transform;
            node.SetParent(parent, false);
            node.localPosition = localPosition;
            return node;
        }

        public void Dispose()
        {
            Object.DestroyImmediate(_root);
        }
    }
}
