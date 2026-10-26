using System;
using UnityEngine;

namespace MoonProject.Autoplay
{
    /// <summary>
    /// What a beat cost 07: the cells at its start, their lowest and last values, and the metres driven (the path
    /// length of 07's flat position, frame to frame). Read every frame without allocating.
    /// </summary>
    public sealed class BeatTrip
    {
        private Vector2 _last;
        private bool _hasLast;

        /// <summary>The cells (0..1) when the beat began.</summary>
        public float StartCharge { get; private set; }

        /// <summary>The lowest cells (0..1) seen during the beat.</summary>
        public float MinCharge { get; private set; }

        /// <summary>The cells (0..1) at the latest sample.</summary>
        public float EndCharge { get; private set; }

        /// <summary>Metres 07 travelled over the ground during the beat.</summary>
        public float Metres { get; private set; }

        /// <summary>Starts counting a beat at 07's current cells and place.</summary>
        public void Begin(float charge01, Vector3 position)
        {
            StartCharge = charge01;
            MinCharge = charge01;
            EndCharge = charge01;
            Metres = 0f;
            _last = new Vector2(position.x, position.z);
            _hasLast = true;
        }

        /// <summary>Adds one frame: the cells now and where 07 is.</summary>
        public void Step(float charge01, Vector3 position)
        {
            MinCharge = Math.Min(MinCharge, charge01);
            EndCharge = charge01;
            var flat = new Vector2(position.x, position.z);
            if (_hasLast)
            {
                Metres += Vector2.Distance(_last, flat);
            }

            _last = flat;
            _hasLast = true;
        }
    }
}
