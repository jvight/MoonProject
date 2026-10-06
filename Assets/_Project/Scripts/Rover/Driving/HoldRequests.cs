using System;
using System.Collections.Generic;

namespace MoonProject.Rover
{
    /// <summary>
    /// Owners currently asking 07 to stay parked (<c>IRoverRig.SetHoldStill</c>). Held while any owner holds; holding
    /// or releasing twice is harmless. No allocations while within the initial capacity.
    /// </summary>
    public sealed class HoldRequests
    {
        private const int DefaultCapacity = 4;

        private readonly List<object> _owners;

        public HoldRequests(int capacity = DefaultCapacity)
        {
            _owners = new List<object>(capacity);
        }

        public bool IsHeld => _owners.Count > 0;

        public int Count => _owners.Count;

        public void Set(object owner, bool hold)
        {
            if (owner == null)
            {
                throw new ArgumentNullException(nameof(owner));
            }

            int index = IndexOf(owner);
            if (hold && index < 0)
            {
                _owners.Add(owner);
            }
            else if (!hold && index >= 0)
            {
                _owners.RemoveAt(index);
            }
        }

        private int IndexOf(object owner)
        {
            for (int i = 0; i < _owners.Count; i++)
            {
                if (ReferenceEquals(_owners[i], owner))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
