using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One instance of Bell's Art prefab (Bell or Bell_Broken; same node names, docs/ARCHITECTURE.md "Contract: friend
    /// Bell"): finds her nodes anywhere in the hierarchy, lights her dial lamp and part lamps through the emission
    /// contract, blends every node from another rig's pose to her own rest pose (lying broken to standing), and poses
    /// her with a <see cref="BellPose"/> on top: Body (with the hips) bobbing, rolling and pitching, the Lid, the
    /// Needle, the Speaker cone, each Leg's swing and Shin's bend. Makes her solid to 07 with a soft kinematic box.
    /// Throws when a node is missing.
    /// </summary>
    public sealed class BellRig
    {
        public const string BodyNode = "Body";
        public const string LidNode = "Lid";
        public const string NeedleNode = "Needle";
        public const string DialLampNode = "DialLamp";
        public const string SpeakerNode = "Speaker";
        public const string TapeSlotNode = "TapeSlot";
        public const string PartLampPrefix = "PartLamp_";
        public const string LegPrefix = "Leg_";
        public const string ShinPrefix = "Shin_";

        /// <summary>Leg corners in <see cref="BellPose"/> order.</summary>
        public static readonly string[] Corners = { "FL", "FR", "RL", "RR" };

        private readonly Transform[] _nodes;
        private readonly Vector3[] _restPositions;
        private readonly Quaternion[] _restRotations;
        private readonly Vector3[] _fromPositions;
        private readonly Quaternion[] _fromRotations;
        private readonly Vector3[] _posePositions;
        private readonly Quaternion[] _poseRotations;
        private readonly bool[] _animated;
        private readonly Transform _body;
        private readonly Transform _lid;
        private readonly Transform _needle;
        private readonly Transform _speaker;
        private readonly Transform[] _legs = new Transform[BellPose.Legs];
        private readonly Transform[] _shins = new Transform[BellPose.Legs];
        private readonly int _bodyIndex;
        private readonly int _lidIndex;
        private readonly int _needleIndex;
        private readonly int _speakerIndex;
        private readonly int[] _legIndex = new int[BellPose.Legs];
        private readonly int[] _shinIndex = new int[BellPose.Legs];
        private readonly EmissionGlow _dialLamp;
        private readonly EmissionGlow[] _lamps;

        /// <param name="root">The prefab instance.</param>
        /// <param name="partLamps">How many PartLamp_i nodes she needs (her parts, then her items).</param>
        public BellRig(GameObject root, int partLamps)
        {
            Root = root != null ? root.transform : throw new ArgumentNullException(nameof(root));
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

            _bodyIndex = IndexOf(BodyNode);
            _lidIndex = IndexOf(LidNode);
            _needleIndex = IndexOf(NeedleNode);
            _speakerIndex = IndexOf(SpeakerNode);
            _body = _nodes[_bodyIndex];
            _lid = _nodes[_lidIndex];
            _needle = _nodes[_needleIndex];
            _speaker = _nodes[_speakerIndex];
            TapeSlot = _nodes[IndexOf(TapeSlotNode)];
            for (int leg = 0; leg < BellPose.Legs; leg++)
            {
                _legIndex[leg] = IndexOf(LegPrefix + Corners[leg]);
                _shinIndex[leg] = IndexOf(ShinPrefix + Corners[leg]);
                _legs[leg] = _nodes[_legIndex[leg]];
                _shins[leg] = _nodes[_shinIndex[leg]];
            }

            _animated = new bool[count];
            _animated[_bodyIndex] = true;
            _animated[_lidIndex] = true;
            _animated[_needleIndex] = true;
            _animated[_speakerIndex] = true;
            for (int leg = 0; leg < BellPose.Legs; leg++)
            {
                _animated[_legIndex[leg]] = true;
                _animated[_shinIndex[leg]] = true;
            }

            _dialLamp = new EmissionGlow(Renderer(_nodes[IndexOf(DialLampNode)]));
            _lamps = new EmissionGlow[partLamps];
            for (int i = 0; i < partLamps; i++)
            {
                _lamps[i] = new EmissionGlow(Renderer(_nodes[IndexOf(PartLampPrefix + i)]));
            }
        }

        public Transform Root { get; }

        /// <summary>The cassette door on her front (+Z out): where 07's beam slides the tape in.</summary>
        public Transform TapeSlot { get; }

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

        /// <summary>
        /// Makes her solid to 07 (Prop layer, like the base's props): a kinematic body that follows her root and a box
        /// around her meshes as posed now, <paramref name="padding"/> metres roomier on every side but the ground, so
        /// 07 stops softly just short of her (its frictionless sphere slides along). Throws without a mesh to fit.
        /// </summary>
        public BoxCollider MakeSolid(float padding)
        {
            bool found = false;
            var bounds = new Bounds();
            foreach (MeshFilter filter in Root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                Bounds mesh = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var local = new Vector3((corner & 1) == 0 ? mesh.min.x : mesh.max.x,
                        (corner & 2) == 0 ? mesh.min.y : mesh.max.y, (corner & 4) == 0 ? mesh.min.z : mesh.max.z);
                    Vector3 point = Root.InverseTransformPoint(filter.transform.TransformPoint(local));
                    if (found)
                    {
                        bounds.Encapsulate(point);
                    }
                    else
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        found = true;
                    }
                }
            }

            if (!found)
            {
                throw new InvalidOperationException($"{nameof(BellRig)}: '{Root.name}' has no mesh to make solid.");
            }

            padding = Mathf.Max(0f, padding);
            GameObject host = Root.gameObject;
            host.layer = Layers.Prop;
            var body = host.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            var box = host.AddComponent<BoxCollider>();
            box.center = bounds.center + Vector3.up * (padding * 0.5f);
            box.size = bounds.size + new Vector3(padding * 2f, padding, padding * 2f);
            return box;
        }

        public void SetDialLamp(float intensity)
        {
            _dialLamp.Apply(intensity);
        }

        public void SetLamp(int index, float intensity)
        {
            _lamps[index].Apply(intensity);
        }

        /// <summary>Remembers <paramref name="other"/>'s current node poses as the start of a blend.</summary>
        public void CapturePoseFrom(BellRig other)
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
        /// Poses every node between the captured pose (0) and its own rest pose (1), then <paramref name="pose"/> on
        /// top.
        /// </summary>
        public void Apply(float blend, BellPose pose)
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                _posePositions[i] = Vector3.LerpUnclamped(_fromPositions[i], _restPositions[i], blend);
                _poseRotations[i] = Quaternion.Slerp(_fromRotations[i], _restRotations[i], blend);
            }

            Vector3 lift = Vector3.up * pose.Bob;
            _body.localPosition = _posePositions[_bodyIndex] + lift;
            _body.localRotation = _poseRotations[_bodyIndex] * Quaternion.Euler(pose.Pitch, 0f, pose.Roll);
            _lid.localRotation = _poseRotations[_lidIndex] * Quaternion.Euler(pose.Lid, 0f, 0f);
            _needle.localRotation = _poseRotations[_needleIndex] * Quaternion.Euler(0f, 0f, pose.Needle);
            _speaker.localPosition = _posePositions[_speakerIndex] + Vector3.forward * pose.Speaker;
            for (int leg = 0; leg < BellPose.Legs; leg++)
            {
                int hip = _legIndex[leg];
                int knee = _shinIndex[leg];
                _legs[leg].localPosition = _posePositions[hip] + lift;
                _legs[leg].localRotation = _poseRotations[hip] * Quaternion.Euler(pose.Leg(leg), 0f, 0f);
                _shins[leg].localRotation = _poseRotations[knee] * Quaternion.Euler(pose.Shin(leg), 0f, 0f);
            }

            for (int i = 0; i < _nodes.Length; i++)
            {
                if (!_animated[i])
                {
                    _nodes[i].localPosition = _posePositions[i];
                    _nodes[i].localRotation = _poseRotations[i];
                }
            }
        }

        private Transform Find(string name)
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

        private int IndexOf(string name)
        {
            for (int i = 0; i < _nodes.Length; i++)
            {
                if (_nodes[i].name == name)
                {
                    return i;
                }
            }

            throw new InvalidOperationException($"Bell prefab {Root.name} has no '{name}' node " +
                                                "(Bell rig contract, docs/ARCHITECTURE.md).");
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

        private static Renderer Renderer(Transform node)
        {
            if (!node.TryGetComponent(out Renderer renderer))
            {
                throw new InvalidOperationException($"Bell rig node '{node.name}' has no renderer to light.");
            }

            return renderer;
        }
    }
}
