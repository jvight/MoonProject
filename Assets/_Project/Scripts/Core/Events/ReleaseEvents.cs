using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>The stages of the release after the answer (docs/design/opening-v3-purpose.md R9), in their order.</summary>
    public enum ReleaseStage
    {
        /// <summary>The answer has not finished: no release yet.</summary>
        Waiting = 0,

        /// <summary>The full range ring shows, once.</summary>
        Ring = 1,

        /// <summary>The view from the rise: 07 looks at relay.1, the Peak's dish and Kestrel-3 in turn.</summary>
        View = 2,

        /// <summary>The socket rack waits for 07's next dock to show its first outlines (slice 4.3's hook).</summary>
        RackPending = 3,

        /// <summary>The rack has been shown: the release is over and the free hour begins.</summary>
        Done = 4,
    }

    /// <summary>The release moved to <see cref="Stage"/>; <see cref="Restored"/> when found already reached at load.</summary>
    public readonly struct ReleaseStageChanged
    {
        public ReleaseStageChanged(ReleaseStage stage, bool restored)
        {
            Stage = stage;
            Restored = restored;
        }

        public ReleaseStage Stage { get; }

        public bool Restored { get; }
    }

    /// <summary>
    /// The view from the rise opens: relay.1's lamp, the Peak's dish and Kestrel-3 are the three lures 07 looks at, each
    /// for <see cref="SecondsEach"/>, in that order.
    /// </summary>
    public readonly struct ReleaseViewOpened
    {
        public ReleaseViewOpened(Vector3 relay, Vector3 dish, Vector3 kestrel, float secondsEach)
        {
            Relay = relay;
            Dish = dish;
            Kestrel = kestrel;
            SecondsEach = secondsEach;
        }

        public Vector3 Relay { get; }

        public Vector3 Dish { get; }

        public Vector3 Kestrel { get; }

        public float SecondsEach { get; }
    }

    /// <summary>
    /// 07 docked after the release's view: the socket rack at the pad shows its first outlines (slice 4.3 subscribes).
    /// </summary>
    public readonly struct SocketRackRevealed
    {
        public SocketRackRevealed(Vector3 position)
        {
            Position = position;
        }

        public Vector3 Position { get; }
    }
}
