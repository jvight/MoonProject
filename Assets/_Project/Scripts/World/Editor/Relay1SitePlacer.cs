using UnityEngine;
using MoonProject.Core;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// Stands relay.1's repair site under [World]/Relay1Site (opening v3 5.1), in the relay.1 anchor's frame, in Art's
    /// props: the open service cabinet (ServiceCabinet, solid on the Prop layer) with a small warm point light at its
    /// Light_Anchor and the feed-unit spare on its shelf at Part_Anchor (FeedUnitSpare_Slot/FeedUnitSpare); the mast
    /// box's feed socket at the mast's own FeedSocket_Anchor (FeedSocket), the fitted unit waiting inactive in its mouth
    /// (FeedUnitFitted); and the contact pad in front of the mast (ContactPad, drivable). Gameplay finds these by path
    /// at its scene build and drives the take, the fit and the charge; <see cref="Relay1SiteLights"/> lights the
    /// cabinet's lamp strip and the socket's outline.
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
        private const string LightsName = "Relay1SiteLights";

        // Art's nodes on its props (R1 relay repair contract).
        private const string LightAnchor = "Light_Anchor";
        private const string PartAnchor = "Part_Anchor";
        private const string InteriorLamp = "InteriorLamp";
        private const string SocketAnchor = "FeedSocket_Anchor";
        private const string SocketOutline = "FeedSocket_Outline";
        private const string PadLamp = "PadLamp";

        public static void Stand(SceneBuildContext context, WorldSettings settings, Transform root, MoonSurface surface,
            WorldAnchor anchor, GameObject mast)
        {
            Relay1SiteSettings site = settings.Landmarks.Relay1Site;
            Vector3 forward = Vector3.ProjectOnPlane(anchor.Forward, Vector3.up).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            Quaternion facing = Quaternion.LookRotation(forward, Vector3.up);
            Transform holder = context.CreateChild(SiteName, root).transform;
            holder.SetPositionAndRotation(anchor.Position, facing);

            Vector2 offset = site.CabinetOffset;
            Vector3 cabinetSpot = Ground(surface, anchor.Position + right * offset.x + forward * offset.y);
            // Art's placements are given with each prop standing at yaw 0: they turn with the yaw it stands at here.
            Quaternion cabinetTurn = facing * Quaternion.Euler(0f, site.CabinetYaw, 0f);
            GameObject cabinet = Prop(context, WorldPaths.ServiceCabinetPrefab, CabinetName, holder, cabinetSpot,
                cabinetTurn);
            Solid(cabinet, Layers.Prop);
            Transform part = Find(cabinet.transform, PartAnchor);
            Transform slot = context.CreateChild(SlotName, cabinet.transform).transform;
            slot.SetPositionAndRotation(part.position + cabinetTurn * site.SpareOnShelf,
                cabinetTurn * Quaternion.Euler(0f, site.SpareYaw, 0f));
            Prop(context, WorldPaths.FeedUnitSparePrefab, SpareName, slot, slot.position, slot.rotation);
            StandLight(context, Find(cabinet.transform, LightAnchor), site);

            Transform mouth = Find(mast.transform, SocketAnchor);
            Transform socket = context.CreateChild(SocketName, holder).transform;
            socket.SetPositionAndRotation(mouth.position, mouth.rotation);
            GameObject fitted = Prop(context, WorldPaths.FeedUnitSparePrefab, FittedName, socket,
                mouth.position + facing * site.FittedInSocket, facing * Quaternion.Euler(0f, site.FittedYaw, 0f));
            fitted.SetActive(false);

            Vector3 padSpot = Ground(surface, anchor.Position + forward * site.PadDistance);
            Vector3 normal = surface.SampleNormal(padSpot.x, padSpot.z);
            GameObject pad = Prop(context, WorldPaths.RelayContactPadPrefab, PadName, holder, padSpot,
                Quaternion.FromToRotation(Vector3.up, normal) * facing);
            Solid(pad, Layers.Ground);

            var lights = context.CreateChild(LightsName, holder).AddComponent<Relay1SiteLights>();
            var serialized = new UnityEditor.SerializedObject(lights);
            serialized.FindProperty("_settings").objectReferenceValue = settings;
            serialized.FindProperty("_interiorLamp").objectReferenceValue = RendererOf(cabinet.transform, InteriorLamp);
            serialized.FindProperty("_socketOutline").objectReferenceValue = RendererOf(mast.transform, SocketOutline);
            serialized.FindProperty("_padLamp").objectReferenceValue = RendererOf(pad.transform, PadLamp);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            context.AddSystem(lights);
        }

        private static GameObject Prop(SceneBuildContext context, string path, string name, Transform parent,
            Vector3 position, Quaternion rotation)
        {
            var prefab = context.LoadAsset<GameObject>(path);
            GameObject prop = context.InstantiatePrefab(prefab, parent, Vector3.zero, Quaternion.identity);
            prop.transform.SetPositionAndRotation(position, rotation * prefab.transform.localRotation);
            prop.name = name;
            return prop;
        }

        /// <summary>One box collider round the whole prop on <paramref name="layer"/>: 07 bumps it or drives on it.</summary>
        private static void Solid(GameObject prop, int layer)
        {
            Transform frame = prop.transform;
            var bounds = new Bounds();
            bool any = false;
            foreach (MeshFilter filter in prop.GetComponentsInChildren<MeshFilter>())
            {
                Bounds mesh = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var local = new Vector3(corner % 2 == 0 ? mesh.min.x : mesh.max.x,
                        corner / 2 % 2 == 0 ? mesh.min.y : mesh.max.y, corner / 4 == 0 ? mesh.min.z : mesh.max.z);
                    Vector3 point = frame.InverseTransformPoint(filter.transform.TransformPoint(local));
                    if (!any)
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        any = true;
                    }

                    bounds.Encapsulate(point);
                }
            }

            var collider = prop.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
            prop.layer = layer;
        }

        private static void StandLight(SceneBuildContext context, Transform at, Relay1SiteSettings site)
        {
            var light = context.CreateChild("LampLight", at).AddComponent<Light>();
            light.type = LightType.Point;
            light.color = site.LampColor;
            light.intensity = site.LampIntensity;
            light.range = site.LampRange;
            light.shadows = LightShadows.None;
        }

        private static Renderer RendererOf(Transform root, string node)
        {
            var renderer = Find(root, node).GetComponent<Renderer>();
            if (renderer == null)
            {
                throw new System.InvalidOperationException($"{root.name}'s '{node}' has no renderer to light.");
            }

            return renderer;
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

        private static Vector3 Ground(MoonSurface surface, Vector3 at)
        {
            return new Vector3(at.x, surface.SampleHeight(at.x, at.z), at.z);
        }
    }
}
