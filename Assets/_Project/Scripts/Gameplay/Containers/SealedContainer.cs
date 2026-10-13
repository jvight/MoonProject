using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One of Art's sealed containers (docs/design/0.5.5-owner-feedback.md §2): a crate, pod, case, box or cabinet
    /// whose find waits inside. Its seal sits on the lid, or on the clamp that holds the find (<c>Seal</c>, glowing),
    /// and is cut at its <c>CutPoint</c>; the lid then swings open about its hinge (local X; a cabinet's door about
    /// local Y) and the find rises off its <c>ItemSocket</c> to the open mouth, the height of the lid's hinge (a
    /// cabinet's stands inside its door already). It is presented there, in the container, never on the ground. Its
    /// owner says when it may be cut (a part not needed yet keeps its box shut). Plain state;
    /// <see cref="SealedContainers"/> drives it.
    /// </summary>
    public sealed class SealedContainer
    {
        public const string LidPath = "Lid";
        public const string CutPointName = "CutPoint";
        public const string SealName = "Seal";
        public const string ItemSocketPath = "ItemSocket";

        private readonly Func<bool> _available;
        private readonly Vector3 _presentLocal;

        private SealedContainer(string id, ContainerKind kind, Transform root, Transform lid, Transform cutPoint,
            Renderer seal, Transform itemSocket, Func<bool> available)
        {
            LidAxis = kind == ContainerKind.Cabinet ? Vector3.up : Vector3.right;
            _presentLocal = root.InverseTransformPoint(kind == ContainerKind.Cabinet ? itemSocket.position
                : MouthAbove(itemSocket.position, lid));
            Id = id;
            Kind = kind;
            Root = root;
            Lid = lid;
            CutPoint = cutPoint;
            Seal = new EmissionGlow(seal);
            ItemSocket = itemSocket;
            LidRest = lid.localRotation;
            _available = available;
        }

        /// <summary>Stable id ("container.depot.coil"). Saved: never rename.</summary>
        public string Id { get; }

        public ContainerKind Kind { get; }

        public Transform Root { get; }

        public Transform Lid { get; }

        public Transform CutPoint { get; }

        public EmissionGlow Seal { get; }

        /// <summary>Where the find rests inside, and slides out from once the lid is open.</summary>
        public Transform ItemSocket { get; }

        internal Quaternion LidRest { get; }

        /// <summary>
        /// The lid's hinge axis, local to the lid: X for a lifting lid, Y for a cabinet's swinging door.
        /// </summary>
        public Vector3 LidAxis { get; }

        /// <summary>
        /// Where the find is presented once the lid stands open: at the open mouth, in the container.
        /// </summary>
        public Vector3 PresentPoint => Root.TransformPoint(_presentLocal);

        /// <summary>How far the seal is cut, 0..1 (kept on release).</summary>
        public float Progress { get; internal set; }

        /// <summary>True once the seal is cut through (the lid swings from <see cref="OpenedAt"/>).</summary>
        public bool Opened { get; internal set; }

        /// <summary>When the seal broke (negative infinity for a container a save loaded open).</summary>
        public float OpenedAt { get; internal set; } = float.NegativeInfinity;

        /// <summary>True once the lid has swung fully open and the find may slide out.</summary>
        public bool StandsOpen { get; internal set; }

        /// <summary>True while its owner lets it be cut.</summary>
        public bool Available => _available();

        /// <summary>
        /// Reads Art's container contract under <paramref name="root"/>; null and the broken part in
        /// <paramref name="problem"/> when a node is missing.
        /// </summary>
        /// <param name="lidPath">The lid, or the clamp that holds the find (<see cref="LidPath"/> for most): its
        /// CutPoint and Seal are its children.</param>
        /// <param name="itemSocketPath">Where the find rests inside (<see cref="ItemSocketPath"/> for most).</param>
        /// <param name="available">True while the container may be cut (its find is there to be had).</param>
        public static SealedContainer Read(string id, ContainerKind kind, Transform root, string lidPath,
            string itemSocketPath, Func<bool> available, out string problem)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            string cutPath = lidPath + "/" + CutPointName;
            string sealPath = lidPath + "/" + SealName;
            Transform lid = root.Find(lidPath);
            Transform cut = root.Find(cutPath);
            Transform seal = root.Find(sealPath);
            Transform socket = root.Find(itemSocketPath);
            Renderer glow = seal != null ? seal.GetComponent<Renderer>() : null;
            problem = lid == null ? lidPath
                : cut == null ? cutPath
                : glow == null ? sealPath + " (with a renderer)"
                : socket == null ? itemSocketPath
                : null;
            if (problem != null)
            {
                problem = $"'{root.name}' has no '{problem}' node (Art's container contract, 0.5.5 §2)";
                return null;
            }

            return new SealedContainer(id, kind, root, lid, cut, glow, socket,
                available ?? throw new ArgumentNullException(nameof(available)));
        }

        /// <summary>
        /// The socket raised to the lid's hinge height, in the frame the lid hangs in: the open mouth.
        /// </summary>
        private static Vector3 MouthAbove(Vector3 socket, Transform lid)
        {
            Transform frame = lid.parent;
            Vector3 local = frame.InverseTransformPoint(socket);
            local.y = lid.localPosition.y;
            return frame.TransformPoint(local);
        }
    }
}
