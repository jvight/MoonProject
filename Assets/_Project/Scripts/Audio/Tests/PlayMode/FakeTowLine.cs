using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>07's tow cable the audio tests control: towing or not, where it meets the load and how hard it pulls.
    /// </summary>
    public sealed class FakeTowLine : ITowLine
    {
        public bool IsTowing { get; set; }

        public Vector3 LoadHitch { get; set; }

        public float Length { get; set; } = 6f;

        public float Strain01 { get; set; }

        public bool TryFindLoad(string towId, out Transform root)
        {
            root = null;
            return false;
        }
    }
}
