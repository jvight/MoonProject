using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Something the tether can latch onto and tow: a loose relic, or a salvage drag piece still hanging from its
    /// wreck. The tether drives its body; the towable only says when it may be grabbed and when it is done.
    /// </summary>
    internal interface ITowable
    {
        Rigidbody Body { get; }

        Vector3 Position { get; }

        /// <summary>Half of the largest dimension of its bounds (m): line-of-sight checks stop short of it.</summary>
        float Radius { get; }

        /// <summary>The tether may latch onto it now.</summary>
        bool IsTetherable { get; }

        bool IsTethered { get; }

        /// <summary>The tow has done its job (a drag piece is clear of its wreck): the tether lets go softly.</summary>
        bool WantsRelease { get; }

        /// <summary>The tether latched on: the body becomes free to be pulled.</summary>
        void BeginTow();

        /// <summary>The tether let go.</summary>
        void EndTow();

        /// <summary>How strongly the tether's aim highlights it (0 = not aimed at, 1 = the hovered target).</summary>
        void SetAimHighlight(float level);
    }
}
