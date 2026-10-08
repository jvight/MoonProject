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
    }
}
