using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Pushes the lander's <see cref="BlastZone"/> into the global shader properties R1Terrain reads to draw the swept
    /// halo and its rays (globals, like <see cref="SkyShaderGlobals"/>, so WorldSettings stays the one source).
    /// </summary>
    public static class BlastZoneShaderGlobals
    {
        private static readonly int ZoneId = Shader.PropertyToID("_MoonBlastZone");
        private static readonly int LookId = Shader.PropertyToID("_MoonBlastLook");

        public static void Apply(BlastZone zone)
        {
            if (zone == null)
            {
                throw new ArgumentNullException(nameof(zone));
            }

            BlastZoneSettings settings = zone.Settings;
            Shader.SetGlobalVector(ZoneId, new Vector4(zone.Center.x, zone.Center.y, settings.Radius,
                settings.EdgeBand));
            Shader.SetGlobalVector(LookId, new Vector4(settings.AlbedoLift, settings.RayLift, settings.RayCount,
                settings.RayLength));
        }
    }
}
