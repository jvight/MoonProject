using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI.PlayModeTests
{
    /// <summary>The call from the sky as the UI tests set it: the UI reads only its sender (the rest comes by event).</summary>
    public sealed class FakeCall : ICallSource
    {
        public Vector3 Direction => Vector3.forward;

        public CallState State { get; set; } = CallState.Broken;

        public string Sender { get; set; } = "ARK IV";

        public int Decoded { get; set; }

        public int Total => 5;
    }
}
