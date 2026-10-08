using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Every node of a rigged Art prefab instance (each descendant, depth first) with its rest pose, and a blend from a
    /// pose captured off another instance with the same node names (lying broken to standing, leaning to upright) back
    /// to rest. Finds nodes by name and throws, naming the contract, when one is missing. Allocation-free after
    /// construction.
    /// </summary>
    public sealed class RigPose
    {
        private readonly string _contract;
        private readonly Transform[] _nodes;
        private readonly Vector3[] _restPositions;
        private readonly Quaternion[] _restRotations;
        private readonly Vector3[] _fromPositions;
        private readonly Quaternion[] _fromRotations;
        private readonly Vector3[] _posePositions;
        private readonly Quaternion[] _poseRotations;

        /// <param name="root">The prefab instance's root.</param>
        /// <param name="contract">The rig contract's name, for errors ("Bell rig", "relay mast").</param>
        public RigPose(Transform root, string contract)
        {
            Root = root != null ? root : throw new ArgumentNullException(nameof(root));
            _contract = contract ?? throw new ArgumentNullException(nameof(contract));
            var nodes = new List<Transform>();
            Collect(Root, nodes);
            _nodes = nodes.ToArray();
            int count = _nodes.Length;
            _restPositions = new Vector3[count];
            _restRotations = new Quaternion[count];
            _fromPositions = new Vector3[count];
            _fromRotations = new Quaternion[count];
            _posePositions = new Vector3[count];
            _poseRotations = new Quaternion[count];
            for (int i = 0; i < count; i++)
            {
                _restPositions[i] = _nodes[i].localPosition;
                _restRotations[i] = _nodes[i].localRotation;
                _fromPositions[i] = _restPositions[i];
                _fromRotations[i] = _restRotations[i];
                _posePositions[i] = _restPositions[i];
                _poseRotations[i] = _restRotations[i];
            }
        }

        public Transform Root { get; }

        public int Count => _nodes.Length;

        public Transform Node(int index)
        {
            return _nodes[index];
        }

        /// <summary>The index of node <paramref name="name"/>; throws when the prefab breaks its contract.</summary>
        public int IndexOf(string name)
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i].name == name)
                {
                    return i;
                }
            }

            throw new InvalidOperationException($"{_contract} prefab '{Root.name}' has no '{name}' node ({_contract} " +
                                                "contract, docs/ARCHITECTURE.md).");
        }

        /// <summary>The renderer on node <paramref name="name"/> (a glow to light); throws without one.</summary>
        public Renderer RendererOf(string name)
        {
            Transform node = _nodes[IndexOf(name)];
            if (!node.TryGetComponent(out Renderer renderer))
            {
                throw new InvalidOperationException($"{_contract} node '{name}' of '{Root.name}' has no renderer to " +
                                                    "light.");
            }

            return renderer;
        }

        /// <summary>Node <paramref name="name"/>, or null when there is none.</summary>
        public Transform Find(string name)
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i].name == name)
                {
                    return _nodes[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Remembers <paramref name="other"/>'s current node poses (matched by name) as the start of a blend; a node it
        /// lacks starts at rest.
        /// </summary>
        public void CaptureFrom(RigPose other)
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
        /// Evaluates every node between the captured pose (0) and its rest pose (1); values past 1 overshoot. Read the
        /// result with <see cref="Position"/> and <see cref="Rotation"/>, or write it with <see cref="Apply"/>.
        /// </summary>
        public void Evaluate(float blend)
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                _posePositions[i] = Vector3.LerpUnclamped(_fromPositions[i], _restPositions[i], blend);
                _poseRotations[i] = Quaternion.SlerpUnclamped(_fromRotations[i], _restRotations[i], blend);
            }
        }

        public Vector3 Position(int index)
        {
            return _posePositions[index];
        }

        public Quaternion Rotation(int index)
        {
            return _poseRotations[index];
        }

        /// <summary>Writes the evaluated pose to every node whose <paramref name="skip"/> flag is not set.</summary>
        /// <param name="skip">Per node, true when the caller poses it itself; null poses them all.</param>
        public void Apply(bool[] skip)
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (skip == null || !skip[i])
                {
                    _nodes[i].localPosition = _posePositions[i];
                    _nodes[i].localRotation = _poseRotations[i];
                }
            }
        }

        private static void Collect(Transform parent, List<Transform> nodes)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                nodes.Add(child);
                Collect(child, nodes);
            }
        }
    }
}
