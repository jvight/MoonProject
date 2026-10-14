using System;
using System.Collections.Generic;

namespace MoonProject.Rover
{
    /// <summary>
    /// The tower's story parts riding on 07's back (M3-16 P1): one shown at a time, the first one carried, popping on
    /// and off with an eased scale. A part restored by a load is on 07's back at once.
    /// </summary>
    public sealed class StoryPartCarry
    {
        private readonly float _halfLife;

        /// <summary>The story parts riding on 07, in the order they were picked up.</summary>
        private readonly List<string> _parts = new List<string>();

        private string _lastPart;

        /// <param name="halfLife">Half-life (s) of a part's scale easing on and off.</param>
        public StoryPartCarry(float halfLife)
        {
            _halfLife = halfLife;
        }

        /// <summary>
        /// Art's model name of the story part shown on 07's back: the first one carried, or the last one while it eases
        /// off after delivery; null before any.
        /// </summary>
        public string Part => _parts.Count > 0 ? _parts[0] : _lastPart;

        /// <summary>The story part's scale share, 0 (gone) to 1 (on 07's back), eased.</summary>
        public float PartScale { get; private set; }

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
            PartScale = Smoothing.Damp(PartScale, _parts.Count > 0 ? 1f : 0f, _halfLife, deltaTime);
        }
    }
}
