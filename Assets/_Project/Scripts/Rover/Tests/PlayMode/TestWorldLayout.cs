using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary><see cref="IWorldLayout"/> stand-in: base at the origin, Earth up and ahead-left.</summary>
    public sealed class TestWorldLayout : IWorldLayout
    {
        public Vector3 BasePosition => Vector3.zero;

        public Vector3 PeakPosition => new Vector3(0f, 120f, 280f);

        public Vector3 EarthDirection => new Vector3(-0.3f, 0.55f, 0.78f).normalized;
    }
}
