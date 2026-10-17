using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>
    /// A dormant friend answered a sonar ping with its broken chirp (published when the answer is heard).
    /// </summary>
    public readonly struct FriendAnswered
    {
        public FriendAnswered(string friendId, Vector3 position)
        {
            FriendId = friendId;
            Position = position;
        }

        public string FriendId { get; }

        public Vector3 Position { get; }
    }

    /// <summary>A friend is repaired and awake (published once it is up in the air, looking at 07).</summary>
    public readonly struct FriendRepaired
    {
        public FriendRepaired(string friendId)
        {
            FriendId = friendId;
        }

        public string FriendId { get; }
    }

    /// <summary>A friend greeted 07 coming home.</summary>
    public readonly struct FriendGreeted
    {
        public FriendGreeted(string friendId)
        {
            FriendId = friendId;
        }

        public string FriendId { get; }
    }

    /// <summary>
    /// A friend's ability found something for 07 (Tilly's spotter: a soft ping over an undiscovered site, relic or
    /// part).
    /// </summary>
    public readonly struct FriendSpotted
    {
        public FriendSpotted(string friendId, Vector3 position)
        {
            FriendId = friendId;
            Position = position;
        }

        public string FriendId { get; }

        public Vector3 Position { get; }
    }
}
