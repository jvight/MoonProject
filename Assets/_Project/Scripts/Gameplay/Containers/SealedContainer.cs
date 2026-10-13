using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One of Art's sealed containers (docs/design/0.5.5-owner-feedback.md §2): a crate, pod, case, box or cabinet
    /// whose
    /// find waits inside. Its seal sits on the lid (<c>Lid/Seal</c>, glowing) and is cut at <c>Lid/CutPoint</c>; the
    /// lid then swings open about its local X and the find slides out of <c>ItemSocket</c>. Its owner says when it may
    /// be cut (a part not needed yet keeps its box shut). Plain state; <see cref="SealedContainers"/> drives it.
    /// </summary>
    public sealed class SealedContainer
    {
        public const string LidPath = "Lid";
        public const string CutPointPath = "Lid/CutPoint";
        public const string SealPath = "Lid/Seal";
        public const string ItemSocketPath = "ItemSocket";

        private readonly Func<bool> _available;

        private SealedContainer(string id, ContainerKind kind, Transform root, Transform lid, Transform cutPoint,
            Renderer seal, Transform itemSocket, Func<bool> available)
        {
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
        /// <param name="itemSocketPath">Where the find rests inside (<see cref="ItemSocketPath"/> for most).</param>
        /// <param name="available">True while the container may be cut (its find is there to be had).</param>
        public static SealedContainer Read(string id, ContainerKind kind, Transform root, string itemSocketPath,
            Func<bool> available, out string problem)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            Transform lid = root.Find(LidPath);
            Transform cut = root.Find(CutPointPath);
            Transform seal = root.Find(SealPath);
            Transform socket = root.Find(itemSocketPath);
            Renderer glow = seal != null ? seal.GetComponent<Renderer>() : null;
            problem = lid == null ? LidPath
                : cut == null ? CutPointPath
                : glow == null ? SealPath + " (with a renderer)"
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
    }
}
