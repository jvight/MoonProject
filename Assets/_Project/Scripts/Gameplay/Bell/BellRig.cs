using System;
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
        public const string DialFaceNode = "DialFace";
        public const string NeedleNode = "Needle";
        public const string DialLampNode = "DialLamp";
        public const string SpeakerNode = "Speaker";
        public const string TapeSlotNode = "TapeSlot";
        public const string PartLampPrefix = "PartLamp_";
        public const string LegPrefix = "Leg_";
        public const string ShinPrefix = "Shin_";

        private const string Contract = "Bell rig";

        /// <summary>Leg corners in <see cref="BellPose"/> order.</summary>
        public static readonly string[] Corners = { "FL", "FR", "RL", "RR" };

        private readonly RigPose _pose;
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
            _pose = new RigPose(Root, Contract);
            _bodyIndex = _pose.IndexOf(BodyNode);
            _lidIndex = _pose.IndexOf(LidNode);
            _needleIndex = _pose.IndexOf(NeedleNode);
            _speakerIndex = _pose.IndexOf(SpeakerNode);
            _body = _pose.Node(_bodyIndex);
            _lid = _pose.Node(_lidIndex);
            _needle = _pose.Node(_needleIndex);
            _speaker = _pose.Node(_speakerIndex);
            TapeSlot = _pose.Node(_pose.IndexOf(TapeSlotNode));
            DialFace = _pose.Node(_pose.IndexOf(DialFaceNode));
            for (int leg = 0; leg < BellPose.Legs; leg++)
            {
                _legIndex[leg] = _pose.IndexOf(LegPrefix + Corners[leg]);
                _shinIndex[leg] = _pose.IndexOf(ShinPrefix + Corners[leg]);
                _legs[leg] = _pose.Node(_legIndex[leg]);
                _shins[leg] = _pose.Node(_shinIndex[leg]);
            }

            _animated = new bool[_pose.Count];
            _animated[_bodyIndex] = true;
            _animated[_lidIndex] = true;
            _animated[_needleIndex] = true;
            _animated[_speakerIndex] = true;
            for (int leg = 0; leg < BellPose.Legs; leg++)
            {
                _animated[_legIndex[leg]] = true;
                _animated[_shinIndex[leg]] = true;
            }

            _dialLamp = new EmissionGlow(_pose.RendererOf(DialLampNode));
            _lamps = new EmissionGlow[partLamps];
            for (int i = 0; i < partLamps; i++)
            {
                _lamps[i] = new EmissionGlow(_pose.RendererOf(PartLampPrefix + i));
            }
        }

        public Transform Root { get; }

        /// <summary>The cassette door on her front (+Z out): where 07's beam slides the tape in.</summary>
        public Transform TapeSlot { get; }

        /// <summary>Her dial on her front (the needle's window): where 07's beam taps to tune her.</summary>
        public Transform DialFace { get; }

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
            if (!MeshBounds.TryLocal(Root, Root, out Bounds bounds))
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

            _pose.CaptureFrom(other._pose);
        }

        /// <summary>
        /// Poses every node between the captured pose (0) and its own rest pose (1), then <paramref name="pose"/> on
        /// top.
        /// </summary>
        public void Apply(float blend, BellPose pose)
        {
            _pose.Evaluate(blend);
            Vector3 lift = Vector3.up * pose.Bob;
            _body.localPosition = _pose.Position(_bodyIndex) + lift;
            _body.localRotation = _pose.Rotation(_bodyIndex) * Quaternion.Euler(pose.Pitch, 0f, pose.Roll);
            _lid.localRotation = _pose.Rotation(_lidIndex) * Quaternion.Euler(pose.Lid, 0f, 0f);
            _needle.localRotation = _pose.Rotation(_needleIndex) * Quaternion.Euler(0f, 0f, pose.Needle);
            _speaker.localPosition = _pose.Position(_speakerIndex) + Vector3.forward * pose.Speaker;
            for (int leg = 0; leg < BellPose.Legs; leg++)
            {
                int hip = _legIndex[leg];
                int knee = _shinIndex[leg];
                _legs[leg].localPosition = _pose.Position(hip) + lift;
                _legs[leg].localRotation = _pose.Rotation(hip) * Quaternion.Euler(pose.Leg(leg), 0f, 0f);
                _shins[leg].localRotation = _pose.Rotation(knee) * Quaternion.Euler(pose.Shin(leg), 0f, 0f);
            }

            _pose.Apply(_animated);
        }
    }
}
