using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World.Map
{
    /// <summary>
    /// The World's <see cref="ISightline"/>: marches the straight line over the analytic surface, so it needs no
    /// colliders and never allocates.
    /// </summary>
    public sealed class Sightline : ISightline
    {
        private readonly ITerrainQuery _terrain;

        public Sightline(ITerrainQuery terrain)
        {
            _terrain = terrain ?? throw new ArgumentNullException(nameof(terrain));
        }

        public bool IsClear(Vector3 eye, Vector3 target, float targetRadius, float step, float clearance)
        {
            if (step <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(step), step, "The sightline step must be positive.");
            }

            float dx = target.x - eye.x;
            float dz = target.z - eye.z;
            float distance = Mathf.Sqrt(dx * dx + dz * dz);
            int samples = Mathf.FloorToInt((distance - targetRadius) / step) + 1;
            for (int i = 1; i < samples; i++)
            {
                float t = i * step / distance;
                float line = eye.y + (target.y - eye.y) * t;
                if (_terrain.SampleHeight(eye.x + dx * t, eye.z + dz * t) > line + clearance)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
