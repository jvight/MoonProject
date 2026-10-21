using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// The socket rack at 07's pad (docs/design/opening-v3-plan.md 4.3), registered in the <see cref="GameContext"/>
    /// by Gameplay: whether it stands yet and what each bay shows (an outline for a part 07 has seen, filled once the
    /// part is fitted). Read-only and allocation-free. Changes are announced with
    /// <see cref="Events.RackChanged"/>.
    /// </summary>
    public interface IRackStatus
    {
        /// <summary>The rack stands at the pad (the release revealed it, <see cref="Events.SocketRackRevealed"/>).</summary>
        bool Shown { get; }

        /// <summary>
        /// The pad's dock where the release revealed the rack (07's place on the pad), kept by the save: the rack
        /// stands beside it. Meaningful only while <see cref="Shown"/>.
        /// </summary>
        Vector3 Position { get; }

        /// <summary>07's heading on that dock: the frame the rack stands in. Meaningful only while Shown.</summary>
        Quaternion Rotation { get; }

        /// <summary>Bays on the rack, left to right.</summary>
        int Count { get; }

        /// <summary>The bay's id, which is also the string key of its words ("rack.headlamp").</summary>
        string IdOf(int bay);

        RackBayState StateOf(int bay);
    }
}
