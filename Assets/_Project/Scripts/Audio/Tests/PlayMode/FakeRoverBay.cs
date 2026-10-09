using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>
    /// An <see cref="IRoverBay"/> made of plain transforms the test moves: a turntable 20 m east of the base, two
    /// arms (each joint a child of the last) and the floor arm's lift and tip.
    /// </summary>
    public sealed class FakeRoverBay : IRoverBay
    {
        public const int Arms = 2;

        private const int JointCount = 5;

        private readonly Transform[,] _joints = new Transform[Arms, JointCount];

        public FakeRoverBay(Transform parent)
        {
            var root = new GameObject("FakeRoverBay").transform;
            root.SetParent(parent, false);
            root.position = new Vector3(20f, 0f, 0f);
            Turntable = Child(root, "Turntable", Vector3.zero);
            FloorLift = Child(root, "FloorLift", Vector3.down * 0.5f);
            FloorTip = Child(FloorLift, "FloorTip", Vector3.up * 0.3f);
            for (int arm = 0; arm < Arms; arm++)
            {
                Transform parentJoint = Child(root, $"Arm{arm}", new Vector3(arm == 0 ? -2f : 2f, 3f, 0f));
                for (int joint = 0; joint < JointCount; joint++)
                {
                    parentJoint = Child(parentJoint, ((RoverBayJoint)joint).ToString(), Vector3.down * 0.4f);
                    _joints[arm, joint] = parentJoint;
                }
            }
        }

        public Vector3 TurntablePosition => Turntable.position;

        public Quaternion TurntableRotation => Turntable.rotation;

        public Transform Turntable { get; }

        public Transform FloorLift { get; }

        public Transform FloorTip { get; }

        public int ArmCount => Arms;

        public Transform GetArmJoint(int arm, RoverBayJoint joint)
        {
            return _joints[arm, (int)joint];
        }

        private static Transform Child(Transform parent, string name, Vector3 localPosition)
        {
            var child = new GameObject(name).transform;
            child.SetParent(parent, false);
            child.localPosition = localPosition;
            return child;
        }
    }
}
