using System;
using UnityEngine;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// Which broken friend the parts readout belongs to (pure logic, EditMode-tested): the nearest friend that is
    /// still waiting for its parts or its repair, once 07 is within the show distance; it stays chosen until 07 drives
    /// beyond the (larger) hide distance or the friend wakes, so the readout never flickers at the edge.
    /// </summary>
    internal sealed class FriendFocus
    {
        public const int None = -1;

        private readonly FriendUiSettings _settings;

        public FriendFocus(FriendUiSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>Index of the friend in focus, or <see cref="None"/>.</summary>
        public int Current { get; private set; } = None;

        /// <summary>True for a friend the readout speaks about: broken, waiting for parts or for its repair.</summary>
        public static bool IsBroken(FriendState state)
        {
            return state == FriendState.Dormant || state == FriendState.PartsGathering;
        }

        public int Step(IFriendStatuses friends, Vector3 rover)
        {
            if (Current != None && Current < friends.Count)
            {
                FriendStatus current = friends.Status(Current);
                if (IsBroken(current.State) && Distance(current.Position, rover) <= _settings.HideDistance)
                {
                    return Current;
                }
            }

            Current = None;
            float best = _settings.ShowDistance;
            for (int i = 0; i < friends.Count; i++)
            {
                FriendStatus status = friends.Status(i);
                float distance = Distance(status.Position, rover);
                if (IsBroken(status.State) && distance <= best)
                {
                    best = distance;
                    Current = i;
                }
            }

            return Current;
        }

        private static float Distance(Vector3 a, Vector3 b)
        {
            float x = a.x - b.x;
            float z = a.z - b.z;
            return Mathf.Sqrt(x * x + z * z);
        }
    }
}
