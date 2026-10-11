using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// 07's battery gauge (docs/features/M3-20 "How 07 shows it"): a short segmented warm light bar along the lid's
    /// rear-top edge, one segment per cell from left to right, eight built once and shown per capacity, so the bar
    /// grows to the right as packs are fitted. The chase camera behind and above 07 sees the lid but never the flanks
    /// (07's big wheels hide them), and across the rear edge every segment reads apart, countable, just behind the
    /// folded solar wing and the antenna. One bar, never a pair of tail lamps. Each segment (its node) is a dark cell
    /// in a little metal tray holding a raised glass (its own node, on the glow-off material, dark until Rover lights
    /// it), tipped back toward the camera.
    /// </summary>
    internal static class RoverBatteryBarMeshes
    {
        public const int SegmentCount = 8;

        /// <summary>Metres from one segment to the next across the bar; the trays touch into one rail.</summary>
        private const float Pitch = 0.068f;

        /// <summary>Metres the bar sits in from the lid's rear edge, clear of the folded wing's end.</summary>
        private const float RearInset = 0.035f;

        /// <summary>Degrees each segment tips back, so its glass faces the chase camera.</summary>
        private const float TiltBack = 30f;

        private const float TrayDepth = 0.05f;
        private const float TrayHeight = 0.014f;
        private const float GlassWidth = 0.054f;
        private const float GlassDepth = 0.034f;
        private const float GlassHeight = 0.009f;
        private const float Bevel = 0.002f;

        /// <summary>Segment <paramref name="index"/>'s base on the lid's rear edge, in body space (left first).</summary>
        public static Vector3 SegmentPosition(int index)
        {
            return new Vector3((index - (SegmentCount - 1) * 0.5f) * Pitch, RoverMeshes.LidTop,
                RoverMeshes.BodyBack + RearInset);
        }

        /// <summary>Up, tipped back toward the chase camera.</summary>
        public static Quaternion SegmentRotation => Quaternion.Euler(-TiltBack, 0f, 0f);

        /// <summary>Where a segment's glass sits on its tray, in segment space.</summary>
        public static Vector3 GlassCentre => new Vector3(0f, TrayHeight, 0f);

        /// <summary>The tray: a dark cell with a metal rim, one rail section of the bar.</summary>
        public static LowPolyMeshBuilder Tray()
        {
            var b = new LowPolyMeshBuilder(48);
            b.Box(At(0f, TrayHeight * 0.5f, 0f), new Vector3(Pitch, TrayHeight, TrayDepth), PaletteSwatch.Metal,
                Bevel);
            b.Box(At(0f, TrayHeight - 0.001f, 0f), new Vector3(GlassWidth + 0.006f, 0.004f, GlassDepth + 0.006f),
                PaletteSwatch.Charcoal);
            return b;
        }

        /// <summary>The glass: a raised soft block that glows WarmLamp when Rover lights it.</summary>
        public static LowPolyMeshBuilder Glass()
        {
            var b = new LowPolyMeshBuilder(24);
            b.Box(At(0f, GlassHeight * 0.5f, 0f), new Vector3(GlassWidth, GlassHeight, GlassDepth),
                PaletteSwatch.LampGlass, Bevel);
            return b;
        }
    }
}
