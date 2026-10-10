using System;
using System.Collections.Generic;

namespace MoonProject.Rover
{
    /// <summary>
    /// The blueprint tubes riding home on 07's back (docs/features/M3-16 P7): a tube shows from the moment 07 scans one
    /// in a site's heart until the last carried one reaches Kenji's plotter, popping on and off with an eased scale. A
    /// loaded game puts the tubes still riding back on 07 at once (<see cref="Restore"/>). The tower's story parts
    /// (M3-16 P1) ride the same way, one shown at a time (<see cref="Part"/>, eased by <see cref="PartScale"/>).
    /// </summary>
    public sealed class TubeCarry
    {
        private readonly HashSet<string> _carried = new HashSet<string>(StringComparer.Ordinal);
        private readonly float _halfLife;

        /// <summary>Tubes put back by a load: the save tells how many, not which, so any delivery takes one off.</summary>
        private int _restored;

        /// <summary>The story parts riding on 07, in the order they were picked up.</summary>
        private readonly List<string> _parts = new List<string>();

        private string _lastPart;

        /// <param name="halfLife">Half-life (s) of the tube's scale easing on and off.</param>
        public TubeCarry(float halfLife)
        {
            _halfLife = halfLife;
        }

        /// <summary>Whether any tube is on 07's back.</summary>
        public bool IsCarrying => _carried.Count + _restored > 0;

        /// <summary>The tube's scale share, 0 (gone) to 1 (on 07's back), eased.</summary>
        public float Scale { get; private set; }

        /// <summary>
        /// Art's model name of the story part shown on 07's back: the first one carried, or the last one while it eases
        /// off after delivery; null before any.
        /// </summary>
        public string Part => _parts.Count > 0 ? _parts[0] : _lastPart;

        /// <summary>The story part's scale share, 0 (gone) to 1 (on 07's back), eased.</summary>
        public float PartScale { get; private set; }

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

        /// <summary>07 picked up story part <paramref name="partId"/>; a restored one is on its back at once.</summary>
        public void PartFound(string partId, bool restored)
        {
            string id = partId ?? string.Empty;
            if (!_parts.Contains(id))
            {
                _parts.Add(id);
            }

            _lastPart = _parts[0];
            if (restored)
            {
                PartScale = 1f;
            }
        }

        /// <summary>Story part <paramref name="partId"/> left 07's back for the tower's port.</summary>
        public void PartDelivered(string partId)
        {
            _parts.Remove(partId ?? string.Empty);
        }

        public void Step(float deltaTime)
        {
            Scale = Smoothing.Damp(Scale, IsCarrying ? 1f : 0f, _halfLife, deltaTime);
            PartScale = Smoothing.Damp(PartScale, _parts.Count > 0 ? 1f : 0f, _halfLife, deltaTime);
        }
    }
}
