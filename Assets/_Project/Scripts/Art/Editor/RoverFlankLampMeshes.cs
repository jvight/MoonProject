using UnityEngine;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// 07's battery lamps (docs/features/M3-20 "How 07 shows it"): a strip of small amber lamps set into the orange
    /// stripe along each flank, eight sockets from the front corner back, one per cell. Each lamp is a riveted metal
    /// cup on the paint (its node's mesh) holding a domed glass (its own node, on the glow-off material, dark until
    /// Rover lights it). In lamp space +X points out of the flank; the left flank's lamps turn half round, so one
    /// pair of meshes serves both sides.
    /// </summary>
    internal static class RoverFlankLampMeshes
    {
        public const int LampCount = 8;

        /// <summary>Metres between neighbouring lamps along the stripe.</summary>
        private const float Spacing = 0.062f;

        /// <summary>Along-body position of the front lamp, just behind the flat flank's front edge.</summary>
        private const float FrontZ = 0.48f;

        private const float CupRadius = 0.029f;
        private const float CupDepth = 0.012f;

        /// <summary>The metal lip round the cup's mouth, bevelled in toward the glass.</summary>
        private const float LipRadius = 0.024f;
        private const float LipDepth = 0.004f;

        private const float GlassRadius = 0.022f;
        private const float GlassTopRadius = 0.013f;
        private const float GlassDepth = 0.01f;

        /// <summary>Eight sides: round at a lamp's size from the chase camera, and 16 lamps stay inside 07's budget.</summary>
        private const int Sides = 8;

        /// <summary>Points out of the flank (+X) from a prism's local Y.</summary>
        private static readonly Vector3 Outward = new Vector3(0f, 0f, -90f);

        /// <summary>Outer face of the stripe's paint on the right flank (the left mirrors x).</summary>
        private const float StripeFace = RoverMeshes.BodyHalfWidth + RoverMeshes.PaintProud +
                                         RoverMeshes.PaintThickness * 0.5f;

        /// <summary>Lamp <paramref name="index"/>'s centre on the stripe, in body space (front lamp first).</summary>
        public static Vector3 LampPosition(bool right, int index)
        {
            return new Vector3(right ? StripeFace : -StripeFace, RoverMeshes.StripeY, FrontZ - index * Spacing);
        }

        /// <summary>The left flank's lamps turn half round so their +X points out too.</summary>
        public static Quaternion LampRotation(bool right)
        {
            return right ? Quaternion.identity : Quaternion.Euler(0f, 180f, 0f);
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
