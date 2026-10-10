using System;
using System.Collections.Generic;

namespace MoonProject.Rover
{
    /// <summary>
    /// The blueprint tubes riding home on 07's back (docs/features/M3-16 P7): a tube shows from the moment 07 scans one
    /// in a site's heart until the last carried one reaches Kenji's plotter, popping on and off with an eased scale. A
    /// loaded game puts the tubes still riding back on 07 at once (<see cref="Restore"/>).
    /// </summary>
    public sealed class TubeCarry
    {
        private readonly HashSet<string> _carried = new HashSet<string>(StringComparer.Ordinal);
        private readonly float _halfLife;

        /// <summary>Tubes put back by a load: the save tells how many, not which, so any delivery takes one off.</summary>
        private int _restored;

        /// <param name="halfLife">Half-life (s) of the tube's scale easing on and off.</param>
        public TubeCarry(float halfLife)
        {
            _halfLife = halfLife;
        }

        /// <summary>Whether any tube is on 07's back.</summary>
        public bool IsCarrying => _carried.Count + _restored > 0;

        /// <summary>The tube's scale share, 0 (gone) to 1 (on 07's back), eased.</summary>
        public float Scale { get; private set; }

        /// <summary>07 scanned the tube for <paramref name="kitId"/>.</summary>
        public void Found(string kitId)
        {
            _carried.Add(kitId ?? string.Empty);
        }

        /// <summary>The tube for <paramref name="kitId"/> reached the plotter.</summary>
        public void Delivered(string kitId)
        {
            if (!_carried.Remove(kitId ?? string.Empty) && _restored > 0)
            {
                _restored--;
            }
        }

        /// <summary>A loaded game: <paramref name="count"/> tubes ride on 07, shown at once with no ride-in.</summary>
        public void Restore(int count)
        {
            _carried.Clear();
            _restored = Math.Max(0, count);
            Scale = IsCarrying ? 1f : 0f;
        }

        public void Step(float deltaTime)
        {
            Scale = Smoothing.Damp(Scale, IsCarrying ? 1f : 0f, _halfLife, deltaTime);
        }
    }
}
