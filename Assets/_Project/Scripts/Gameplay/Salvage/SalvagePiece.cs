using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One piece of a salvage site (Art's "Salvage_&lt;n&gt;_&lt;Material&gt;[_Drag]" node; with no tether in the
    /// opening a drag piece cuts like any other): what it yields, how long it takes to cut, how far the cut has come
    /// (kept when the beam lets go, VISION ruling 4) and where it is in its life. <see cref="SalvageField"/> drives it;
    /// the piece only holds its state and its scene objects.
    /// </summary>
    public sealed class SalvagePiece
    {
        internal SalvagePiece(SalvageSite site, int number, SalvageMaterial material, PieceSize size, int units,
            int segments, float cutSeconds, Transform body, Vector3 localCentre, Transform cutPoint, Vector3[] cutLine,
            Collider wreckCollider, GlowRenderer halo, Transform bundle)
        {
            CutLine = cutLine ?? throw new ArgumentNullException(nameof(cutLine));
            Site = site ?? throw new ArgumentNullException(nameof(site));
            Number = number;
            Material = material;
            Size = size;
            Units = units;
            Segments = Math.Max(1, segments);
            CutSeconds = cutSeconds;
            Body = body != null ? body : throw new ArgumentNullException(nameof(body));
            LocalCentre = localCentre;
            CutPoint = cutPoint != null ? cutPoint : throw new ArgumentNullException(nameof(cutPoint));
            WreckCollider = wreckCollider;
            Halo = halo ?? throw new ArgumentNullException(nameof(halo));
            Bundle = bundle != null ? bundle : throw new ArgumentNullException(nameof(bundle));
            BodyScale = body.localScale;
            BundleScale = bundle.localScale;
        }

        public SalvageSite Site { get; }

        /// <summary>The &lt;n&gt; of its node name: stable, saves use it.</summary>
        public int Number { get; }

        public SalvageMaterial Material { get; }

        /// <summary>How big it is (docs/features/M3-26 §4): its segments, cut time and yield.</summary>
        public PieceSize Size { get; }

        /// <summary>Material units it folds into.</summary>
        public int Units { get; }

        /// <summary>Segments the beam cuts in turn, sliding from one to the next.</summary>
        public int Segments { get; }

        /// <summary>Seconds of lit beam it takes from untouched to free at tier 0 (the cutter's speed divides it).</summary>
        public float CutSeconds { get; }

        public SalvagePieceState State { get; internal set; }

        /// <summary>How far the cut has come, 0..1; it never goes back down.</summary>
        public float Progress { get; internal set; }

        /// <summary>The piece's own node (its pivot is where it hangs on the wreck).</summary>
        public Transform Body { get; }

        /// <summary>The centre of its mesh bounds in its own space.</summary>
        internal Vector3 LocalCentre { get; }

        /// <summary>Its "CutPoint" node: on the cut face, +Z out of it.</summary>
        internal Transform CutPoint { get; }

        /// <summary>Its solid collider while it is part of the wreck.</summary>
        internal Collider WreckCollider { get; }

        internal GlowRenderer Halo { get; }

        /// <summary>The bundle it folds into on its way to 07 (hidden until then).</summary>
        internal Transform Bundle { get; }

        internal Vector3 BodyScale { get; }

        internal Vector3 BundleScale { get; }

        /// <summary>Current halo brightness (eased).</summary>
        internal float HaloLevel { get; set; }

        /// <summary>Break-off and flight bookkeeping (see <see cref="SalvageField"/>).</summary>
        internal Vector3 MotionStart { get; set; }

        internal Quaternion MotionStartRotation { get; set; }

        internal Vector3 BreakNormal { get; set; }

        internal float MotionTime { get; set; }

        internal float MotionDuration { get; set; }

        /// <summary>The beam can cut it now: it is on its wreck.</summary>
        public bool IsCuttable => State == SalvagePieceState.Attached;

        /// <summary>Where the beam meets it: the cut point on the wreck.</summary>
        public Vector3 CutPosition => CutPoint.position;

        /// <summary>The way sparks fly from the cut.</summary>
        internal Vector3 CutNormal => CutPoint.forward;

        /// <summary>The seam's points in the CutPoint's local space (docs/design/feel-pass.md §3.2).</summary>
        internal Vector3[] CutLine { get; }

        /// <summary>Where the seam is hot: as far along the cut line as the cut has come, in the world.</summary>
        internal Vector3 HotPoint => CutPoint.TransformPoint(SalvageCutLines.PointAt(CutLine, Progress));

        /// <summary>The cut line's point <paramref name="index"/> in the world.</summary>
        internal Vector3 CutLinePoint(int index)
        {
            return CutPoint.TransformPoint(CutLine[index]);
        }
    }
}
