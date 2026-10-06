using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>The world's fixed landmarks, derived from the surface and sky settings (registered as
    /// IWorldLayout).</summary>
    public sealed class WorldLayout : IWorldLayout
    {
        public WorldLayout(MoonSurface surface, SkySettings sky)
        {
            if (surface == null)
            {
                throw new ArgumentNullException(nameof(surface));
            }

            if (sky == null)
            {
                throw new ArgumentNullException(nameof(sky));
            }

            BasePosition = new Vector3(0f, surface.SampleHeight(0f, 0f), 0f);
            PeakPosition = surface.PeakSummit;
            EarthDirection = sky.EarthDirection;
        }

        public Vector3 BasePosition { get; }

        public Vector3 PeakPosition { get; }

        public Vector3 EarthDirection { get; }
    }
}
