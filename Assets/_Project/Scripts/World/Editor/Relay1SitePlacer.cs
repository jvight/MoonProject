using UnityEngine;
using UnityEngine.Rendering;
using MoonProject.Core;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// Stands relay.1's repair site under [World]/Relay1Site (opening v3 5.1), in the relay.1 anchor's frame, as
    /// stand-ins Art's props replace in place: the open service cabinet (ServiceCabinet, solid on the Prop layer, its
    /// front open) with its own small warm lamp lighting the inside and the inert feed-unit spare on its shelf
    /// (FeedUnitSpare_Slot/FeedUnitSpare); the mast box's empty feed socket at the mast's own Socket_Power_1
    /// (FeedSocket), with the fitted unit waiting inactive (FeedUnitFitted); and the contact pad in front of the mast
    /// (ContactPad, drivable). Gameplay finds these by path at its scene build and drives the take, the fit and the
    /// charge. The stand-ins are Unity's primitive shapes in Art's R1 materials.
    /// </summary>
    internal static class Relay1SitePlacer
    {
        public const string SiteName = "Relay1Site";

        private const string CabinetName = "ServiceCabinet";
        private const string SlotName = "FeedUnitSpare_Slot";
        private const string SpareName = "FeedUnitSpare";
        private const string SocketName = "FeedSocket";
        private const string FittedName = "FeedUnitFitted";
        private const string PadName = "ContactPad";
        private const string MastSocketNode = "Socket_Power_1";

        // The stand-in cabinet's shape, metres: an upright steel box with its door swung wide.
        private const float CabinetWidth = 0.9f;
        private const float CabinetHeight = 1.3f;
        private const float CabinetDepth = 0.5f;
        private const float Wall = 0.04f;
        private const float ShelfHeight = 0.55f;
        private const float DoorOpenDegrees = 110f;

        // The stand-in feed unit: a painted box with a copper contact block on its plug end, at its back.
        private static readonly Vector3 SpareSize = new Vector3(0.34f, 0.22f, 0.26f);
        private static readonly Vector3 ContactSize = new Vector3(0.18f, 0.08f, 0.04f);

        // The lamp under the cabinet's roof: its housing and lens, and how far below the roof it hangs.
        private static readonly Vector3 LampHousingSize = new Vector3(0.16f, 0.04f, 0.06f);
        private static readonly Vector3 LampLensSize = new Vector3(0.12f, 0.015f, 0.04f);
        private const float LampDrop = 0.08f;
        private const float LampInset = 0.16f;

        // The pad: a steel plate a few centimetres proud of the ground, with a copper contact at its middle.
        private const float PadThickness = 0.05f;
        private const float PadProud = 0.025f;
        private const float PadContactShare = 0.35f;

        public static void Stand(SceneBuildContext context, WorldSettings settings, Transform root, MoonSurface surface,
            WorldAnchor anchor, GameObject mast)
        {
            Relay1SiteSettings site = settings.Landmarks.Relay1Site;
            Vector3 forward = Flat(anchor.Forward);
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Transform holder = context.CreateChild(SiteName, root).transform;
            holder.SetPositionAndRotation(anchor.Position, Quaternion.LookRotation(forward, Vector3.up));

            var steel = context.LoadAsset<Material>(WorldPaths.R1Material("R1_CreamEnamel"));
            var grey = context.LoadAsset<Material>(WorldPaths.R1Material("R1_GreyMetal"));
            var paint = context.LoadAsset<Material>(WorldPaths.R1Material("R1_OrangeStripe"));
            var copper = context.LoadAsset<Material>(WorldPaths.R1Material("R1_Copper"));
            var lens = context.LoadAsset<Material>(WorldPaths.Relay1CabinetLampMaterial);

            Vector2 offset = site.CabinetOffset;
            Vector3 cabinetSpot = Ground(surface, anchor.Position + right * offset.x + forward * offset.y);
            Quaternion cabinetTurn = Quaternion.LookRotation(forward, Vector3.up) *
                Quaternion.Euler(0f, site.CabinetYaw, 0f);
            Transform cabinet = Node(context, CabinetName, holder, cabinetSpot, cabinetTurn);
            StandCabinet(context, cabinet, steel, grey);
            StandLamp(context, cabinet, grey, lens, site);
            Transform slot = Node(context, SlotName, cabinet,
                cabinet.TransformPoint(new Vector3(0f, ShelfHeight + Wall * 0.5f, 0f)), cabinet.rotation);
            StandSpare(context, SpareName, slot, paint, copper);

            Transform mastSocket = Find(mast.transform, MastSocketNode);
            Transform socket = Node(context, SocketName, holder, mastSocket.position, mastSocket.rotation);
            Transform fitted = StandSpare(context, FittedName, socket, paint, copper);
            fitted.gameObject.SetActive(false);

            Vector3 padSpot = Ground(surface, anchor.Position + forward * site.PadDistance);
            Vector3 normal = surface.SampleNormal(padSpot.x, padSpot.z);
            Quaternion padTurn = Quaternion.FromToRotation(Vector3.up, normal) *
                Quaternion.LookRotation(forward, Vector3.up);
            Transform pad = Node(context, PadName, holder, padSpot, padTurn);
            StandPad(context, pad, grey, copper, site.PadRadius);
        }

        private static void StandCabinet(SceneBuildContext context, Transform cabinet, Material steel, Material grey)
        {
            float halfW = CabinetWidth * 0.5f;
            float halfD = CabinetDepth * 0.5f;
            Block(context, "Back", cabinet, new Vector3(0f, CabinetHeight * 0.5f, -halfD + Wall * 0.5f),
                new Vector3(CabinetWidth, CabinetHeight, Wall), grey, true);
            Block(context, "SideLeft", cabinet, new Vector3(-halfW + Wall * 0.5f, CabinetHeight * 0.5f, 0f),
                new Vector3(Wall, CabinetHeight, CabinetDepth), steel, true);
            Block(context, "SideRight", cabinet, new Vector3(halfW - Wall * 0.5f, CabinetHeight * 0.5f, 0f),
                new Vector3(Wall, CabinetHeight, CabinetDepth), steel, true);
            Block(context, "Roof", cabinet, new Vector3(0f, CabinetHeight - Wall * 0.5f, 0f),
                new Vector3(CabinetWidth, Wall, CabinetDepth), steel, true);
            Block(context, "Plinth", cabinet, new Vector3(0f, Wall * 0.5f, 0f),
                new Vector3(CabinetWidth, Wall, CabinetDepth), steel, true);
            Block(context, "Shelf", cabinet, new Vector3(0f, ShelfHeight, 0f),
                new Vector3(CabinetWidth - Wall * 2f, Wall, CabinetDepth - Wall), grey, true);

            // The door hangs open from its left hinge, swung back toward the cabinet's side.
            Transform hinge = Node(context, "DoorHinge", cabinet,
                cabinet.TransformPoint(new Vector3(-halfW, CabinetHeight * 0.5f, halfD)),
                cabinet.rotation * Quaternion.Euler(0f, -DoorOpenDegrees, 0f));
            Block(context, "Door", hinge, new Vector3(halfW, 0f, Wall * 0.5f),
                new Vector3(CabinetWidth, CabinetHeight - Wall, Wall), steel, true);
        }

        private static void StandLamp(SceneBuildContext context, Transform cabinet, Material grey, Material lens,
            Relay1SiteSettings site)
        {
            float y = CabinetHeight - LampDrop;
            Block(context, "LampHousing", cabinet, new Vector3(0f, y, LampInset), LampHousingSize, grey, false);
            Vector3 lensAt = new Vector3(0f, y - (LampHousingSize.y + LampLensSize.y) * 0.5f, LampInset);
            Block(context, "LampLens", cabinet, lensAt, LampLensSize, lens, false);
            Transform glow = Node(context, "LampLight", cabinet, cabinet.TransformPoint(lensAt), cabinet.rotation);
            var light = glow.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = site.LampColor;
            light.intensity = site.LampIntensity;
            light.range = site.LampRange;
            light.shadows = LightShadows.None;
        }

        private static Transform StandSpare(SceneBuildContext context, string name, Transform parent, Material paint,
            Material copper)
        {
            Transform spare = Node(context, name, parent, parent.position, parent.rotation);
            Block(context, "Body", spare, new Vector3(0f, SpareSize.y * 0.5f, 0f), SpareSize, paint, false);
            Block(context, "Contacts", spare,
                new Vector3(0f, SpareSize.y * 0.5f, -(SpareSize.z + ContactSize.z) * 0.5f), ContactSize, copper,
                false);
            return spare;
        }

        private static void StandPad(SceneBuildContext context, Transform pad, Material grey, Material copper,
            float radius)
        {
            float diameter = radius * 2f;
            Disc(context, "Plate", pad, new Vector3(0f, PadProud - PadThickness * 0.5f, 0f), diameter, PadThickness,
                grey);
            float contact = diameter * PadContactShare;
            Disc(context, "Contact", pad, new Vector3(0f, PadProud, 0f), contact, PadThickness * 0.2f, copper);
            var collider = pad.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, PadProud - PadThickness * 0.5f, 0f);
            collider.size = new Vector3(diameter, PadThickness, diameter);
            pad.gameObject.layer = Layers.Ground;
        }

        private static Transform Node(SceneBuildContext context, string name, Transform parent, Vector3 position,
            Quaternion rotation)
        {
            Transform node = context.CreateChild(name, parent).transform;
            node.SetPositionAndRotation(position, rotation);
            return node;
        }

        private static void Block(SceneBuildContext context, string name, Transform parent, Vector3 local,
            Vector3 size, Material material, bool solid)
        {
            Shape(context, name, parent, PrimitiveType.Cube, local, size, material, solid);
        }

        private static void Disc(SceneBuildContext context, string name, Transform parent, Vector3 local,
            float diameter, float thickness, Material material)
        {
            // Unity's cylinder is two units tall: half the thickness on Y gives the plate's own thickness.
            Shape(context, name, parent, PrimitiveType.Cylinder, local,
                new Vector3(diameter, thickness * 0.5f, diameter), material, false);
        }

        private static void Shape(SceneBuildContext context, string name, Transform parent, PrimitiveType type,
            Vector3 local, Vector3 scale, Material material, bool solid)
        {
            GameObject shape = GameObject.CreatePrimitive(type);
            shape.name = name;
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = local;
            shape.transform.localScale = scale;
            var renderer = shape.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.On;
            Collider collider = shape.GetComponent<Collider>();
            if (solid)
            {
                shape.layer = Layers.Prop;
            }
            else
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static Transform Find(Transform root, string name)
        {
            foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
            {
                if (node.name == name)
                {
                    return node;
                }
            }

            throw new System.InvalidOperationException($"{root.name} has no '{name}' node (Art's R1 contract).");
        }

        private static Vector3 Flat(Vector3 direction)
        {
            return Vector3.ProjectOnPlane(direction, Vector3.up).normalized;
        }

        private static Vector3 Ground(MoonSurface surface, Vector3 at)
        {
            return new Vector3(at.x, surface.SampleHeight(at.x, at.z), at.z);
        }
    }
}
