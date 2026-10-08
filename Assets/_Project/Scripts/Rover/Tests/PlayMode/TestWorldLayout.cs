using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// <see cref="IWorldLayout"/> stand-in: Earth straight ahead and high; the base at the origin unless a test moves
    /// it (to bring 07 "home").
    /// </summary>
    public sealed class TestWorldLayout : IWorldLayout
    {
        public Vector3 BasePosition { get; set; } = Vector3.zero;

        public Vector3 PeakPosition => new Vector3(0f, 120f, 280f);

        public Vector3 EarthDirection => new Vector3(0f, 0.6f, 0.8f);
    }
}
