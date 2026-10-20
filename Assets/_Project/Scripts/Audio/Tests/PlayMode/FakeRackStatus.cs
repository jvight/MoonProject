using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>A socket rack the audio tests place: where it stands; its bays come from the tests' events.</summary>
    public sealed class FakeRackStatus : IRackStatus
    {
        public bool Shown { get; set; } = true;

        public Vector3 Position { get; set; }

        public Quaternion Rotation { get; set; } = Quaternion.identity;

        public int Count { get; set; } = 3;

        public string IdOf(int bay)
        {
            return "rack.test";
        }

        public RackBayState StateOf(int bay)
        {
            return RackBayState.Hidden;
        }
    }
}
