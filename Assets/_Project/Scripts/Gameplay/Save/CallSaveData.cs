using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Save section "gameplay.call" (JsonUtility DTO, docs/design/opening-v3-purpose.md §2): how clearly 07 hears the
    /// call from the sky and how much of its sender has decoded (<see cref="CallProgress"/>).
    /// </summary>
    [Serializable]
    public sealed class CallSaveData
    {
        /// <summary>The call's state (Core's CallState as its number).</summary>
        public int state;

        /// <summary>Characters of the sender's identity decoded so far.</summary>
        public int decoded;

        /// <summary>07 has tried its one failed reply (<see cref="CallReply"/>).</summary>
        public bool replyTried;

        /// <summary>The release's socket rack has been shown (<see cref="ReleaseSequence"/>).</summary>
        public bool rackShown;
    }
}
