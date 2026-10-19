using UnityEngine;
using MoonProject.Core.Events;

namespace MoonProject.Core
{
    /// <summary>
    /// The call from the sky (docs/design/opening-v3-purpose.md §2), registered in the <see cref="GameContext"/> by
    /// Gameplay: where its point of light is, how clearly 07 hears it and how much of its sender has decoded. Rover
    /// turns 07's head toward <see cref="Direction"/>; Audio swells the carrier as 07 faces it. Changes are announced
    /// with <see cref="CallStateChanged"/> and <see cref="CallDecoded"/>.
    /// </summary>
    public interface ICallSource
    {
        /// <summary>Unit vector from the world toward the call's point of light, beside dark Earth.</summary>
        Vector3 Direction { get; }

        /// <summary>How clearly 07 hears the call now.</summary>
        CallState State { get; }

        /// <summary>Characters of the sender's identity decoded so far (0 to <see cref="Total"/>).</summary>
        int Decoded { get; }

        /// <summary>Characters in the sender's identity once it reads in full.</summary>
        int Total { get; }
    }
}
