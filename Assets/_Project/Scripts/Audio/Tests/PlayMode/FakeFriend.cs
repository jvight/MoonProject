using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>A friend machine whose state the test sets directly.</summary>
    public sealed class FakeFriend : IFriendState
    {
        public FakeFriend(string id)
        {
            Id = id;
        }

        public string Id { get; }

        public Vector3 Position { get; set; }

        public FriendActivity Activity { get; set; } = FriendActivity.Dormant;

        public float RotorSpeed { get; set; }

        public float RepairProgress { get; set; }
    }
}
