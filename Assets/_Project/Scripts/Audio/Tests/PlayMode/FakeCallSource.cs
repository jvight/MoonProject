using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>A call from the sky the audio tests control: its bearing, state and decoded name.</summary>
    public sealed class FakeCallSource : ICallSource
    {
        public Vector3 Direction { get; set; } = Vector3.forward;

        public CallState State { get; set; } = CallState.Broken;

        public int Decoded { get; set; }

        public int Total { get; set; } = 5;
    }
}
