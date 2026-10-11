using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// 07's battery lamps (docs/features/M3-20 "How 07 shows it"): a strip of small amber lamps along each flank,
    /// on the lid's edge right above the orange stripe, eight sockets from the front corner back, one per cell. The
    /// flanks themselves hide behind 07's big wheels from the chase camera, so the lamps sit on top and face up, a
    /// little out and back, where the camera behind and above 07 sees them. Each lamp is a dark cup with a metal lip
    /// (its node's mesh) holding a domed glass (its own node, on the glow-off material, dark until Rover lights it).
    /// In lamp space +X is the glass's facing; the left flank's lamps turn half round, so one pair of meshes serves
    /// both sides.
    /// </summary>
    internal static class RoverFlankLampMeshes
    {
        public const int LampCount = 8;

        /// <summary>Metres between neighbouring lamps along the stripe.</summary>
        private const float Spacing = 0.062f;

        /// <summary>Along-body position of the front lamp, where the flank's straight edge begins.</summary>
        private const float FrontZ = 0.52f;

        private const float CupRadius = 0.031f;
        private const float CupDepth = 0.012f;

        /// <summary>The metal lip round the cup's mouth, bevelled in toward the glass.</summary>
        private const float LipRadius = 0.027f;
        private const float LipDepth = 0.004f;

        private const float GlassRadius = 0.026f;
        private const float GlassTopRadius = 0.015f;
        private const float GlassDepth = 0.01f;

        /// <summary>Eight sides: round at a lamp's size from the chase camera, and 16 lamps stay inside 07's budget.</summary>
        private const int Sides = 8;

        /// <summary>Degrees each lamp's glass leans out of the vertical and toward the rear, to the chase camera.</summary>
        private const float TiltOut = 25f;
        private const float TiltBack = 20f;

        /// <summary>Metres the lamps sit in from the lid's outer edge, right above the stripe.</summary>
        private const float EdgeInset = 0.05f;

        /// <summary>Points out of the flank (+X) from a prism's local Y.</summary>
        private static readonly Vector3 Outward = new Vector3(0f, 0f, -90f);

        /// <summary>The lamps' line on the right flank's lid edge (the left mirrors x).</summary>
        private const float EdgeX = RoverMeshes.BodyHalfWidth - EdgeInset;

        /// <summary>Lamp <paramref name="index"/>'s base on the lid edge, in body space (front lamp first).</summary>
        public static Vector3 LampPosition(bool right, int index)
        {
            return new Vector3(right ? EdgeX : -EdgeX, RoverMeshes.LidTop, FrontZ - index * Spacing);
        }

        /// <summary>
        /// Up, leaning a little out of its flank and back toward the chase camera; the left flank's lamps turn half
        /// round so they lean out of theirs.
        /// </summary>
        public static Quaternion LampRotation(bool right)
        {
            return Quaternion.Euler(0f, right ? TiltBack : 180f - TiltBack, 90f - TiltOut);
        }

        /// <summary>Where the glass sits in the cup, in lamp space.</summary>
        public static Vector3 GlassCentre => new Vector3(CupDepth, 0f, 0f);

        /// <summary>The cup: a shallow dark can on the paint with a bevelled metal lip.</summary>
        public static LowPolyMeshBuilder Housing()
        {
            var b = new LowPolyMeshBuilder(80);
            b.Prism(At(new Vector3(CupDepth * 0.5f, 0f, 0f), Outward), CupRadius, CupDepth, Sides,
                PaletteSwatch.Charcoal);
            b.Frustum(At(new Vector3(CupDepth + LipDepth * 0.5f, 0f, 0f), Outward), CupRadius, LipRadius, LipDepth,
                Sides, PaletteSwatch.Metal, false);
            return b;
        }

        /// <summary>The glass: a low dome facing out of the flank (it glows WarmLamp when Rover lights it).</summary>
        public static LowPolyMeshBuilder Glass()
        {
            var b = new LowPolyMeshBuilder(40);
            b.Frustum(At(new Vector3(GlassDepth * 0.5f, 0f, 0f), Outward), GlassRadius, GlassTopRadius, GlassDepth,
                Sides, PaletteSwatch.LampGlass);
            return b;
        }
    }
}
