using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// Where a relic rides on 07's Cargo Cradle (docs/features/M3-11), registered in the <see cref="GameContext"/> by
    /// the Rover domain. Gameplay carries a relic home in it instead of towing once the cradle is fitted.
    /// </summary>
    public interface IRoverCargoSeat
    {
        /// <summary>True while the Cargo Cradle kit is on 07 (the ability is owned).</summary>
        bool IsFitted { get; }

        /// <summary>World position of the rack's RelicSeat (on the basket floor). Meaningful only while fitted.</summary>
        Vector3 Position { get; }

        /// <summary>World rotation of the RelicSeat, +Y up. Meaningful only while fitted.</summary>
        Quaternion Rotation { get; }
    }
}
