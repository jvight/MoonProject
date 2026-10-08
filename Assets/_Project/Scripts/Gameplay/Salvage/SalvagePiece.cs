using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One piece of a salvage site (Art's "Salvage_&lt;n&gt;_&lt;Material&gt;[_Drag]" node): what it yields, how long
    /// it takes to cut, how far the cut has come (kept when the beam lets go, VISION ruling 4) and where it is in its
    /// life.
    /// <see cref="SalvageField"/> drives it; the piece only holds its state and its scene objects.
    /// </summary>
    public sealed class SalvagePiece
    {
        internal SalvagePiece(SalvageSite site, int number, SalvageMaterial material, bool drag, int units,
            float cutSeconds, Transform body, Vector3 localCentre, Transform cutPoint, Collider wreckCollider,
            GlowRenderer halo, Transform bundle)
        {
            Site = site ?? throw new ArgumentNullException(nameof(site));
            Number = number;
            Material = material;
            IsDrag = drag;
            Units = units;
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

        /// <summary>Must be tethered clear of its wreck before it can be cut.</summary>
        public bool IsDrag { get; }

        /// <summary>Material units it folds into.</summary>
        public int Units { get; }

        /// <summary>Seconds of beam it takes from untouched to free.</summary>
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

        /// <summary>A drag piece's tetherable body (null for the others).</summary>
        public SalvageDrag Drag { get; internal set; }

        /// <summary>Its solid collider while it is part of the wreck (null for a drag piece).</summary>
        internal Collider WreckCollider { get; }

        internal GlowRenderer Halo { get; }

        /// <summary>The bundle it folds into on its way to 07 (hidden until then).</summary>
        internal Transform Bundle { get; }

        internal Vector3 BodyScale { get; }

        internal Vector3 BundleScale { get; }

        /// <summary>How strongly the tether's aim highlights a drag piece.</summary>
        internal float TetherHighlight { get; set; }

        /// <summary>Current halo brightness (eased).</summary>
        internal float HaloLevel { get; set; }

        /// <summary>Break-off and flight bookkeeping (see <see cref="SalvageField"/>).</summary>
        internal Vector3 MotionStart { get; set; }

        internal Quaternion MotionStartRotation { get; set; }

        internal Vector3 BreakNormal { get; set; }

        internal float MotionTime { get; set; }

        internal float MotionDuration { get; set; }

        /// <summary>
        /// The beam can cut it now: on its wreck (not a drag piece), or a drag piece pulled clear and off the tether.
        /// </summary>
        public bool IsCuttable => State == SalvagePieceState.Loose
            ? !Drag.IsTethered
            : State == SalvagePieceState.Attached && !IsDrag;

        /// <summary>Where the beam meets it: the cut point on the wreck, the body's centre once loose.</summary>
        public Vector3 CutPosition => State == SalvagePieceState.Loose ? Drag.Position : CutPoint.position;

        /// <summary>The way sparks fly from the cut.</summary>
        internal Vector3 CutNormal => State == SalvagePieceState.Loose ? Vector3.up : CutPoint.forward;
    }
}
