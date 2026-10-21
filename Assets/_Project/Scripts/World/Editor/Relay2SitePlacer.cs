using UnityEngine;
using MoonProject.Core;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// Stands relay.2's hut under [World]/Relay2Site (opening v3 5.2b), at the relay.2 anchor facing the canyon mouth:
    /// a small 1980s relay hut (RelayHut, solid on the Prop layer) with its fixed door (Door), the mast stub on its
    /// roof (MastStub) and the stub's dark relay lamp (MastStub/RelayLamp); and the spot on the approach where 07
    /// stops to scan 06's battery bay (ScanApproach). 06 itself (Art's Elder06 in [World]/BasinProps) slumps against
    /// the hut's side. Stand-ins (Unity's primitive shapes in Art's R1 materials) until Art models the hut; Gameplay
    /// finds the nodes by path.
    /// </summary>
    internal static class Relay2SitePlacer
    {
        public const string SiteName = "Relay2Site";

        private const string HutName = "RelayHut";
        private const string DoorName = "Door";
        private const string MastName = "MastStub";
        private const string LampName = "RelayLamp";
        private const string ApproachName = "ScanApproach";

        // The stand-in hut, metres: a painted steel box with an overhanging roof, a band of the network's orange and a
        // short mast with its lamp at the top.
        private static readonly Vector3 BodySize = new Vector3(2.2f, 2.1f, 1.8f);
        private const float RoofOverhang = 0.12f;
        private const float RoofThickness = 0.1f;
        private const float StripeHeight = 1.5f;
        private const float StripeThickness = 0.14f;
        private static readonly Vector3 DoorSize = new Vector3(0.8f, 1.75f, 0.05f);
        private const float DoorSide = -0.35f;
        private const float MastRadius = 0.07f;
        private const float MastHeight = 2.4f;
        private const float MastSetBack = 0.45f;
        private static readonly Vector3 LampSize = new Vector3(0.22f, 0.18f, 0.22f);

        public static void Stand(SceneBuildContext context, WorldSettings settings, Transform root, MoonSurface surface,
            WorldAnchor hutAnchor, WorldAnchor elderAnchor)
        {
            Relay2SiteSettings site = settings.Landmarks.Relay2Site;
            Vector3 toMouth = Vector3.ProjectOnPlane(hutAnchor.Forward, Vector3.up).normalized;
            Transform holder = context.CreateChild(SiteName, root).transform;
            holder.SetPositionAndRotation(hutAnchor.Position, Quaternion.LookRotation(toMouth, Vector3.up));

            var paint = context.LoadAsset<Material>(WorldPaths.R1Material("R1_CreamEnamel"));
            var steel = context.LoadAsset<Material>(WorldPaths.R1Material("R1_GreyMetal"));
            var stripe = context.LoadAsset<Material>(WorldPaths.R1Material("R1_OrangeStripe"));
            // The relay is dead: its lamp is dark glass until a later slice lights it.
            var lamp = context.LoadAsset<Material>(WorldPaths.R1Material("R1_DarkGlass"));
            var dark = context.LoadAsset<Material>(WorldPaths.R1Material("R1_Rubber"));

            Transform hut = context.CreateChild(HutName, holder).transform;
            hut.SetPositionAndRotation(Ground(surface, hutAnchor.Position),
                holder.rotation * Quaternion.Euler(0f, site.HutYaw, 0f));
            Shape(context, "Body", hut, PrimitiveType.Cube, new Vector3(0f, BodySize.y * 0.5f, 0f), BodySize, paint);
            Shape(context, "Roof", hut, PrimitiveType.Cube,
                new Vector3(0f, BodySize.y + RoofThickness * 0.5f, 0f),
                new Vector3(BodySize.x + RoofOverhang * 2f, RoofThickness, BodySize.z + RoofOverhang * 2f), steel);
            Shape(context, "Stripe", hut, PrimitiveType.Cube, new Vector3(0f, StripeHeight, 0f),
                new Vector3(BodySize.x + 0.02f, StripeThickness, BodySize.z + 0.02f), stripe);
            Transform door = context.CreateChild(DoorName, hut).transform;
            door.localPosition = new Vector3(DoorSide, DoorSize.y * 0.5f, (BodySize.z + DoorSize.z) * 0.5f);
            door.localRotation = Quaternion.identity;
            Shape(context, "Panel", door, PrimitiveType.Cube, Vector3.zero, DoorSize, dark);

            Transform mast = context.CreateChild(MastName, hut).transform;
            mast.localPosition = new Vector3(0f, BodySize.y + RoofThickness, -MastSetBack);
            mast.localRotation = Quaternion.identity;
            // Unity's cylinder is two units tall: half the height on Y gives the stub's own height.
            Shape(context, "Pole", mast, PrimitiveType.Cylinder, new Vector3(0f, MastHeight * 0.5f, 0f),
                new Vector3(MastRadius * 2f, MastHeight * 0.5f, MastRadius * 2f), steel);
            Transform relayLamp = context.CreateChild(LampName, mast).transform;
            relayLamp.localPosition = new Vector3(0f, MastHeight + LampSize.y * 0.5f, 0f);
            relayLamp.localRotation = Quaternion.identity;
            Shape(context, "Lens", relayLamp, PrimitiveType.Cube, Vector3.zero, LampSize, lamp);

            var body = hut.gameObject.AddComponent<BoxCollider>();
            body.center = new Vector3(0f, BodySize.y * 0.5f, 0f);
            body.size = BodySize;
            hut.gameObject.layer = Layers.Prop;

            Vector3 approach = elderAnchor.Position + toMouth * site.ScanApproachDistance;
            Transform scan = context.CreateChild(ApproachName, holder).transform;
            scan.SetPositionAndRotation(Ground(surface, approach), Quaternion.LookRotation(-toMouth, Vector3.up));
        }

        private static void Shape(SceneBuildContext context, string name, Transform parent, PrimitiveType type,
            Vector3 local, Vector3 scale, Material material)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = local;
            shape.transform.localScale = scale;
            shape.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(shape.GetComponent<Collider>());
        }

        private static Vector3 Ground(MoonSurface surface, Vector3 at)
        {
            return new Vector3(at.x, surface.SampleHeight(at.x, at.z), at.z);
        }
    }
}
