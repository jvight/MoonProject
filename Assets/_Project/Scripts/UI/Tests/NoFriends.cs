using System;
using MoonProject.Gameplay;

namespace MoonProject.UI.Tests
{
    /// <summary>A friend roster with nobody in it, for UI pieces that only check ids against it.</summary>
    internal sealed class NoFriends : IFriendStatuses
    {
        public int Count => 0;

        public FriendDefinition Definition(int index)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "There are no friends.");
        }

        public FriendStatus Status(int index)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, "There are no friends.");
        }
    }
}
