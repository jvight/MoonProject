using System;
using UnityEngine;

namespace MoonProject.Autoplay
{
    /// <summary>
    /// A thing in the world that tells the player where to go (a lit lamp, a glint, a glowing cable): the bot heads
    /// for it only once it is lit and on screen, so a cue that is missing, dark or hidden fails the run instead of
    /// the bot steering by coordinates a player does not have.
    /// </summary>
    public sealed class WorldCue
    {
        public WorldCue(string name, Func<bool> lit, Func<Vector3> point)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Lit = lit ?? throw new ArgumentNullException(nameof(lit));
            Point = point ?? throw new ArgumentNullException(nameof(point));
        }

        public string Name { get; }

        /// <summary>The cue is shining now.</summary>
        public Func<bool> Lit { get; }

        /// <summary>Where the cue is, in the world.</summary>
        public Func<Vector3> Point { get; }

        /// <summary>
        /// True when <paramref name="point"/> is in front of the camera and inside the middle of the screen
        /// (<paramref name="margin"/> in from each edge, as a share of the viewport).
        /// </summary>
        public static bool OnScreen(Camera camera, Vector3 point, float margin)
        {
            Vector3 view = camera.WorldToViewportPoint(point);
            return view.z > 0f && view.x >= margin && view.x <= 1f - margin && view.y >= margin && view.y <= 1f - margin;
        }
    }
}
