using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// Stands relay.2's hut under [World]/Relay2Site (opening v3 5.2b), at the relay.2 anchor facing the canyon mouth,
    /// in Art's RelayHut prefab: a small 1980s relay hut (RelayHut, solid on the Prop layer) with its fixed door
    /// (Door), the mast stub on its roof (MastStub) and the stub's relay lamp (MastStub/RelayLamp); and the spot on the
    /// approach where 07 stops to scan 06's battery bay (ScanApproach). 06 itself (Art's Elder06 in [World]/BasinProps)
    /// slumps against the hut's side; Gameplay finds the nodes by path. relay.2's power line (PowerLine) runs from the
    /// hut across the ground and along the canyon slot's floor, round its turn and on into the dark.
    /// </summary>
    public static class Relay2SitePlacer
    {
        public const string SiteName = "Relay2Site";

        private const string HutName = "RelayHut";
        private const string DoorName = "Door";
        private const string MastName = "MastStub";
        private const string LampName = "RelayLamp";
        private const string ApproachName = "ScanApproach";
        private const string LineName = "PowerLine";

        public static void Stand(SceneBuildContext context, WorldSettings settings, Transform root, MoonSurface surface,
            WorldAnchor hutAnchor, WorldAnchor elderAnchor)
        {
            Relay2SiteSettings site = settings.Landmarks.Relay2Site;
            Vector3 toMouth = Vector3.ProjectOnPlane(hutAnchor.Forward, Vector3.up).normalized;
            Transform holder = context.CreateChild(SiteName, root).transform;
            holder.SetPositionAndRotation(hutAnchor.Position, Quaternion.LookRotation(toMouth, Vector3.up));

            var prefab = context.LoadAsset<GameObject>(WorldPaths.RelayHutPrefab);
            GameObject hut = context.InstantiatePrefab(prefab, holder, Vector3.zero, Quaternion.identity);
            hut.transform.SetPositionAndRotation(Ground(surface, hutAnchor.Position),
                holder.rotation * Quaternion.Euler(0f, site.HutYaw, 0f) * prefab.transform.localRotation);
            hut.name = HutName;
            Require(hut.transform, DoorName);
            Require(hut.transform, MastName);
            Require(hut.transform, MastName + "/" + LampName);
            Solid(hut, Layers.Prop);

            Vector3 approach = elderAnchor.Position + toMouth * site.ScanApproachDistance;
            Transform scan = context.CreateChild(ApproachName, holder).transform;
            scan.SetPositionAndRotation(Ground(surface, approach), Quaternion.LookRotation(-toMouth, Vector3.up));
            GroundCableLayer.LayTrail(context, holder, surface, LineName,
                PowerLine(surface.Canyon, site, hutAnchor, settings.Relays.HutRadius), site.LineWander, site.LineBends);
        }

        /// <summary>
        /// relay.2's power line: from the hut's edge facing the slot, to the slot's mouth, then along its floor a little
        /// off the centre line, round the turn and <see cref="Relay2SiteSettings.LineDepth"/> on past it.
        /// </summary>
        private static Vector2[] PowerLine(Canyon canyon, Relay2SiteSettings site, WorldAnchor hut, float hutRadius)
        {
            CanyonPath slot = canyon.SlotPath;
            var hutSpot = new Vector2(hut.Position.x, hut.Position.z);
            Vector2 mouth = slot.PointAt(0f) + slot.RightAt(0f) * site.LineSide;
            var points = new List<Vector2>
            {
                hutSpot + (mouth - hutSpot).normalized * hutRadius,
            };
            float end = Mathf.Min(canyon.SlotTurnEndArc + site.LineDepth, slot.EndArc);
            for (float arc = 0f; arc < end; arc += site.LineStep)
            {
                points.Add(slot.PointAt(arc) + slot.RightAt(arc) * site.LineSide);
            }

            points.Add(slot.PointAt(end) + slot.RightAt(end) * site.LineSide);
            return points.ToArray();
        }

        /// <summary>One box collider round the whole hut on <paramref name="layer"/>: 07 bumps it.</summary>
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

            if (!any)
            {
                throw new InvalidOperationException($"{prop.name} has no meshes to measure.");
            }

            var collider = prop.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;
            prop.layer = layer;
        }

        private static Transform Require(Transform root, string path)
        {
            Transform node = root.Find(path);
            if (node == null)
            {
                throw new InvalidOperationException($"{root.name} has no '{path}' node (Art's R1 contract).");
            }

            return node;
        }

        private static Vector3 Ground(MoonSurface surface, Vector3 at)
        {
            return new Vector3(at.x, surface.SampleHeight(at.x, at.z), at.z);
        }
    }
}
