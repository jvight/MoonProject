using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary>
    /// <see cref="IFriendRoster"/> stand-in with Tilly and Bell, both dormant until a test wakes one
    /// (<see cref="TestFriend.Activity"/>).
    /// </summary>
    public sealed class TestFriendRoster : IFriendRoster
    {
        private readonly TestFriend[] _friends =
        {
            new TestFriend(RoverKitPieces.TillyId),
            new TestFriend(RoverKitPieces.BellId),
        };

        public int Count => _friends.Length;

        public TestFriend Tilly => _friends[0];

        public TestFriend Bell => _friends[1];

        public IFriendState Get(int index)
        {
            return _friends[index];
        }

        /// <summary>A friend whose activity the test sets.</summary>
        public sealed class TestFriend : IFriendState
        {
            public TestFriend(string id)
            {
                Id = id;
            }

            public string Id { get; }

            public Vector3 Position => Vector3.zero;

            public FriendActivity Activity { get; set; } = FriendActivity.Dormant;

            public float RotorSpeed => 0f;

            public float RepairProgress => Activity == FriendActivity.Dormant ? 0f : 1f;
        }
    }
}
