using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Where the cut's feel kit draws each frame, read from whoever owns the cut (SalvageField): 07's emitter, the cut
    /// point and its normal, and the piece with its cut line. Beats say *when*; these say *where*.
    /// </summary>
    public interface IFeelAnchors
    {
        /// <summary>07's beam emitter, the same point the cutting beam leaves from.</summary>
        Vector3 Emitter { get; }

        /// <summary>Where the beam meets the piece (world).</summary>
        Vector3 CutPoint { get; }

        /// <summary>The cut face's outward normal (world).</summary>
        Vector3 CutNormal { get; }

        /// <summary>The piece under the beam, or null.</summary>
        Transform CutPiece { get; }

        /// <summary>The piece's cut line, two to five points in the piece's space.</summary>
        IReadOnlyList<Vector3> CutLine { get; }

        /// <summary>The normal of the face the cut line lies on, in the piece's space.</summary>
        Vector3 CutFaceNormal { get; }
    }
}
