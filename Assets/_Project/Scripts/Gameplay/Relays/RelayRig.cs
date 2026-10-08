using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One instance of a relay mast's Art prefab (RelayMast or RelayMast_Broken; same node names, docs/ARCHITECTURE.md
    /// "Contract: relay masts"): its lamp lit through the emission contract (linear), its part socket and beam point,
    /// a blend from the broken instance's leaning pose up to its own (rocking past upright on the way), and a solid
    /// footing so 07 drives around it. Throws when a node is missing.
    /// </summary>
    public sealed class RelayRig
    {
        public const string BaseNode = "Base";
        public const string MastNode = "Mast";
        public const string DishNode = "Dish";
        public const string LampNode = "Lamp";
        public const string PartSocketNode = "PartSocket";
        public const string BeamPointNode = "BeamPoint";

        private const string Contract = "Relay mast";

        private readonly RigPose _pose;
        private readonly EmissionGlow _lamp;

        /// <param name="root">The prefab instance (root at the pad centre on the ground, +Z toward home).</param>
        public RelayRig(GameObject root)
        {
            Root = root != null ? root.transform : throw new ArgumentNullException(nameof(root));
            _pose = new RigPose(Root, Contract);
            Base = _pose.Node(_pose.IndexOf(BaseNode));
            // Checked only: the straightening blends it by name, but a prefab without it breaks the contract.
            _pose.IndexOf(DishNode);
            _lamp = new EmissionGlow(_pose.RendererOf(LampNode));
            Lamp = _pose.Node(_pose.IndexOf(LampNode));
            PartSocket = _pose.Node(_pose.IndexOf(PartSocketNode));
            BeamPoint = _pose.Node(_pose.IndexOf(BeamPointNode));
        }

        public Transform Root { get; }

        /// <summary>The footing plinth and junction box (static).</summary>
        public Transform Base { get; }

        /// <summary>The lamp housing at the top.</summary>
        public Transform Lamp { get; }

        /// <summary>On the junction box front, +Z out: the relay part slots in here.</summary>
        public Transform PartSocket { get; }

        /// <summary>On the junction box: 07's repair beam aims here.</summary>
        public Transform BeamPoint { get; }

        public float LampLevel => _lamp.Intensity;

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

        public void SetLamp(float intensity)
        {
            _lamp.Apply(intensity);
        }

        /// <summary>Remembers <paramref name="other"/>'s pose (the broken lean) as the start of the blend.</summary>
        public void CapturePoseFrom(RelayRig other)
        {
            if (other == null)
            {
                throw new ArgumentNullException(nameof(other));
            }

            _pose.CaptureFrom(other._pose);
        }

        /// <summary>Poses every node between the captured lean (0) and upright (1); past 1 it rocks beyond.</summary>
        public void Straighten(float upright)
        {
            _pose.Evaluate(upright);
            _pose.Apply(null);
        }

        /// <summary>
        /// Makes the mast solid to 07 (Prop layer, like the base's props): mesh colliders on its footing (with the
        /// junction box, guy wires and stakes) and on the mast itself, so 07 drives around exactly what it sees. The
        /// mast only moves while it straightens. Throws when either node has no mesh.
        /// </summary>
        public void MakeSolid()
        {
            Solid(Base);
            Solid(_pose.Node(_pose.IndexOf(MastNode)));
        }

        private void Solid(Transform node)
        {
            if (!node.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
            {
                throw new InvalidOperationException($"{Contract} node '{node.name}' of '{Root.name}' has no mesh to " +
                                                    "make solid.");
            }

            node.gameObject.layer = Layers.Prop;
            node.gameObject.AddComponent<MeshCollider>().sharedMesh = filter.sharedMesh;
        }
    }
}
