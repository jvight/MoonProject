using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Read-only salvage state for the UI's hold ring and the audio's cutting beam (docs/features/M3-13), registered
    /// in the GameContext. Poll it each frame; it never allocates. The piece it describes is the one under the beam,
    /// else the one a hold of Excavate would cut.
    /// </summary>
    public interface ISalvageStatus
    {
        /// <summary>A piece is aimed at or being cut (the other values describe it only while this is true).</summary>
        bool HasTarget { get; }

        /// <summary>Where the beam meets the piece in the world (its cut point; its centre once loose).</summary>
        Vector3 CutPoint { get; }

        /// <summary>The beam is cutting right now (07 holds Excavate and has eased to a stop).</summary>
        bool IsCutting { get; }

        /// <summary>How far the piece's cut has come, 0..1 (kept when the hold lets go).</summary>
        float Progress { get; }

        /// <summary>What the piece folds into.</summary>
        SalvageMaterial Material { get; }

        /// <summary>The emitter is charging up before ignition (docs/design/feel-pass.md §3.1).</summary>
        bool IsBeamCharging { get; }

        /// <summary>How far the charge-up has come, 0..1 (0 when not charging): the hold ring's first segment.</summary>
        float BeamChargeProgress { get; }

        /// <summary>Where the seam is hot: as far along the piece's cut line as its cut has come (world).</summary>
        Vector3 HotPoint { get; }

        /// <summary>The cut face's normal there (world, unit length; up for a loose piece).</summary>
        Vector3 CutNormal { get; }

        /// <summary>Points in the piece's cut line (2 to 5; 0 with no target).</summary>
        int CutLineCount { get; }

        /// <summary>The cut line's point <paramref name="index"/> in the world (0 .. <see cref="CutLineCount"/> - 1).
        /// </summary>
        Vector3 CutLinePoint(int index);
    }
}
