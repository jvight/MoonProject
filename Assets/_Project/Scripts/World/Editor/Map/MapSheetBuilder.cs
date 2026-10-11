using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Core.Map;
using MoonProject.Editor.Builders;
using MoonProject.World.Map;

namespace MoonProject.World.Editor.Map
{
    /// <summary>
    /// Writes the crew map's <see cref="MapSheet"/> (docs/features/M3-07) from the shipped world: the same surface,
    /// anchors and crater field <see cref="WorldSystem"/> generates at boot, traced by <see cref="MapSheetTracer"/>
    /// with <see cref="MapSheetTuning"/> (created once, never overwritten). UI reads the sheet; nothing is baked.
    /// </summary>
    public static class MapSheetBuilder
    {
        public const string TuningPath = "Assets/_Project/Data/Tuning/World/MapSheetTuning.asset";
        public const string SheetPath = WorldPaths.GeneratedRoot + "/Map/MapSheet.asset";

        [MoonBuilder("World/Map Sheet", 260)]
        public static void Build()
        {
            var settings = AssetDatabase.LoadAssetAtPath<WorldSettings>(WorldPaths.Settings);
            if (settings == null)
            {
                throw new InvalidOperationException($"No WorldSettings at {WorldPaths.Settings}; run World/Settings.");
            }

            var tuning = AssetDatabase.LoadAssetAtPath<MapSheetTuning>(TuningPath);
            if (tuning == null)
            {
                tuning = GeneratedAssets.CreateOrReplace(ScriptableObject.CreateInstance<MapSheetTuning>(), TuningPath);
            }

            MoonSurface surface = settings.CreateSurface();
            var anchors = new WorldAnchors(surface, settings.Surface.Canyon, settings.Relays);
            SurfaceSettings ground = settings.Surface;
            surface.AttachCraterField(new CraterField(ground.CraterField, settings.Seed,
                ground.FloorRadius - ground.RimWarpAmplitude, ground.PadRadius + ground.PadBlend, anchors,
                surface.Canyon.Bounds));
            var sheet = ScriptableObject.CreateInstance<MapSheet>();
            MapSheetTracer.Trace(sheet, surface, anchors, tuning);
            string problem = sheet.Validate();
            if (problem != null)
            {
                throw new InvalidOperationException($"The traced map sheet is broken: {problem}");
            }

            GeneratedAssets.CreateOrReplace(sheet, SheetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"{nameof(MapSheetBuilder)}: {sheet.LineCount} lines, {sheet.PointCount} points, " +
                      $"{sheet.AnchorCount} anchors.");
        }
    }
}
