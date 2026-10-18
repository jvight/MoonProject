using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Pushes the lander's <see cref="BlastZone"/> into the global shader properties R1Terrain reads to draw the swept
    /// halo, its blotchy edge and its few faint rays (globals, like <see cref="SkyShaderGlobals"/>, so WorldSettings
    /// stays the one source).
    /// </summary>
    public static class BlastZoneShaderGlobals
    {
        private static readonly int ZoneId = Shader.PropertyToID("_MoonBlastZone");
        private static readonly int HaloId = Shader.PropertyToID("_MoonBlastHalo");
        private static readonly int RaysId = Shader.PropertyToID("_MoonBlastRays");
        private static readonly int RayShapeId = Shader.PropertyToID("_MoonBlastRayShape");
        private static readonly int GroundId = Shader.PropertyToID("_MoonBlastGround");

        public static void Apply(BlastZone zone)
        {
            if (zone == null)
            {
                throw new ArgumentNullException(nameof(zone));
            }

            BlastZoneSettings settings = zone.Settings;
            Shader.SetGlobalVector(ZoneId, new Vector4(zone.Center.x, zone.Center.y, settings.Radius,
                settings.EdgeWander));
            Shader.SetGlobalVector(HaloId, new Vector4(settings.HaloLift, settings.HaloFalloff, settings.BlotchSize,
                settings.BlotchContrast));
            Shader.SetGlobalVector(RaysId, new Vector4(settings.RayLift, settings.RaySectors, settings.RayChance,
                settings.LongRayChance));
            Shader.SetGlobalVector(RayShapeId, new Vector4(settings.RayLengthMin, settings.RayLengthMax,
                settings.LongRayLength, settings.RayWidthMin));
            Shader.SetGlobalVector(GroundId, new Vector4(settings.RayWidthMax, settings.LevelGround, 0f, 0f));
        }
    }
}
