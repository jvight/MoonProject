using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// A salvage site's crew supply crate as it stands (docs/features/M3-25 T4): Art's Site_&lt;id&gt;/CrewCrate with its
    /// Lid, the lid's CutPoint (+Z out of the seal) and Seal glow, and what it holds. <see cref="CrewCrates"/> opens it.
    /// </summary>
    public sealed class CrewCrate
    {
        internal CrewCrate(SalvageSite site, SupplyCrate contents, Transform root, Transform lid, Transform cutPoint,
            Renderer seal)
        {
            Site = site ?? throw new ArgumentNullException(nameof(site));
            Contents = contents;
            Root = root != null ? root : throw new ArgumentNullException(nameof(root));
            Lid = lid != null ? lid : throw new ArgumentNullException(nameof(lid));
            CutPoint = cutPoint != null ? cutPoint : throw new ArgumentNullException(nameof(cutPoint));
            Seal = new EmissionGlow(seal != null ? seal : throw new ArgumentNullException(nameof(seal)));
            LidRest = lid.localPosition;
            LidRestRotation = lid.localRotation;
        }

        public SalvageSite Site { get; }

        /// <summary>Whose hand packed it and the stack inside.</summary>
        public SupplyCrate Contents { get; }

        public Transform Root { get; }

        public Transform Lid { get; }

        /// <summary>Where the beam cuts the seal (+Z out of it).</summary>
        public Transform CutPoint { get; }

        public EmissionGlow Seal { get; }

        internal Vector3 LidRest { get; }

        internal Quaternion LidRestRotation { get; }

        /// <summary>How far the one cut has come, 0..1.</summary>
        public float Progress { get; internal set; }

        /// <summary>True once its seal is cut and its stack taken: it stays an empty frame.</summary>
        public bool Opened { get; internal set; }

        /// <summary>When its lid began to lift (s), for the lid's ease.</summary>
        internal float OpenedAt { get; set; } = float.NegativeInfinity;
    }
}
