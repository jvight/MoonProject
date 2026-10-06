using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One instance of a friend's Art prefab and its rig nodes (contract: Body, Eye, Rotor_FL/FR/RL/RR, Antenna,
    /// PartLamp_0.., TetherPoint; the broken and repaired prefabs share the names). Lights the eye and part lamps
    /// through the emission contract, spins the rotors, and blends every node from another rig's pose to its own
    /// (the broken friend rolling upright as it boots). Throws when a node is missing.
    /// </summary>
    public sealed class FriendRig
    {
        public const string BodyNode = "Body";
        public const string EyeNode = "Eye";
        public const string AntennaNode = "Antenna";
        public const string TetherPointNode = "TetherPoint";
        public const string PartLampPrefix = "PartLamp_";

        public static readonly string[] RotorNodes = { "Rotor_FL", "Rotor_FR", "Rotor_RL", "Rotor_RR" };

        private readonly Transform[] _nodes;
        private readonly Vector3[] _restPositions;
        private readonly Quaternion[] _restRotations;
        private readonly Vector3[] _fromPositions;
        private readonly Quaternion[] _fromRotations;
        private readonly Transform[] _rotors;
        private readonly Quaternion[] _rotorRest;
        private readonly EmissionGlow _eye;
        private readonly EmissionGlow[] _lamps;
        private float _rotorAngle;

        /// <param name="root">The prefab instance.</param>
        /// <param name="partLamps">How many PartLamp_i nodes the friend needs (its part count).</param>
        public FriendRig(GameObject root, int partLamps)
        {
            Root = root != null ? root.transform : throw new ArgumentNullException(nameof(root));
            Body = Node(BodyNode);
            TetherPoint = Node(TetherPointNode);
            Antenna = Node(AntennaNode);
            _eye = new EmissionGlow(Renderer(Node(EyeNode)));
            _rotors = new Transform[RotorNodes.Length];
            _rotorRest = new Quaternion[RotorNodes.Length];
            for (int i = 0; i < RotorNodes.Length; i++)
            {
                _rotors[i] = Node(RotorNodes[i]);
                _rotorRest[i] = _rotors[i].localRotation;
            }

            _lamps = new EmissionGlow[partLamps];
            for (int i = 0; i < partLamps; i++)
            {
                _lamps[i] = new EmissionGlow(Renderer(Node(PartLampPrefix + i)));
            }

            _nodes = new Transform[Root.childCount];
            _restPositions = new Vector3[_nodes.Length];
            _restRotations = new Quaternion[_nodes.Length];
            _fromPositions = new Vector3[_nodes.Length];
            _fromRotations = new Quaternion[_nodes.Length];
            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i] = Root.GetChild(i);
                _restPositions[i] = _nodes[i].localPosition;
                _restRotations[i] = _nodes[i].localRotation;
                _fromPositions[i] = _restPositions[i];
                _fromRotations[i] = _restRotations[i];
            }
        }

        public Transform Root { get; }

        public Transform Body { get; }

        public Transform TetherPoint { get; }

        public Transform Antenna { get; }

        public bool Visible
        {
            get => Root.gameObject.activeSelf;
            set
            {
                if (Root.gameObject.activeSelf != value)
                {
                    Root.gameObject.SetActive(value);
                }
            }
        }

        public void SetEye(float intensity)
        {
            _eye.Apply(intensity);
        }

        public void SetLamp(int index, float intensity)
        {
            _lamps[index].Apply(intensity);
        }

        /// <summary>Turns the rotors by <paramref name="degrees"/> about their own up axis.</summary>
        public void SpinRotors(float degrees)
        {
            _rotorAngle = Mathf.Repeat(_rotorAngle + degrees, 360f);
            for (int i = 0; i < _rotors.Length; i++)
            {
                float direction = (i & 1) == 0 ? 1f : -1f;
                _rotors[i].localRotation = _rotorRest[i] * Quaternion.Euler(0f, _rotorAngle * direction, 0f);
            }
        }

        /// <summary>Remembers <paramref name="other"/>'s current node poses as the start of a blend.</summary>
        public void CapturePoseFrom(FriendRig other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            for (int i = 0; i < _nodes.Length; i++)
            {
                Transform match = other.Find(_nodes[i].name);
                _fromPositions[i] = match != null ? match.localPosition : _restPositions[i];
                _fromRotations[i] = match != null ? match.localRotation : _restRotations[i];
            }
        }

        /// <summary>
        /// Poses every node between the captured pose (0) and its own rest pose (1); rotors keep spinning on top.
        /// </summary>
        public void BlendPose(float t)
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                _nodes[i].localPosition = Vector3.LerpUnclamped(_fromPositions[i], _restPositions[i], t);
                _nodes[i].localRotation = Quaternion.Slerp(_fromRotations[i], _restRotations[i], t);
            }

            for (int i = 0; i < _rotors.Length; i++)
            {
                _rotorRest[i] = _rotors[i].localRotation;
            }

            SpinRotors(0f);
        }

        private Transform Find(string name)
        {
            for (int i = 0; i < Root.childCount; i++)
            {
                Transform child = Root.GetChild(i);
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        private Transform Node(string name)
        {
            Transform node = Find(name);
            if (node == null)
            {
                throw new InvalidOperationException($"Friend prefab {Root.name} has no '{name}' node " +
                                                    "(friend rig contract, docs/features/M3-02-friends-tilly.md).");
            }

            return node;
        }

        private static Renderer Renderer(Transform node)
        {
            if (!node.TryGetComponent(out Renderer renderer))
            {
                throw new InvalidOperationException($"Friend rig node '{node.name}' has no renderer to light.");
            }

            return renderer;
        }
    }
}
