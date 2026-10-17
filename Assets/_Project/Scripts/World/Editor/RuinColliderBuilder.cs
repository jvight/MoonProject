using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// Builds compound <see cref="BoxCollider"/>s on <see cref="Layers.Prop"/> from the meshes of a multi-part ruin
    /// prefab (ENV-03) so walls, hulls and equipment are solid while courtyards, gaps between disconnected pieces and
    /// building portals (such as Kenji's Workshop doorway and interior) stay open. Edit time: the scene build fits
    /// them once and saves them with the scene.
    /// </summary>
    public static class RuinColliderBuilder
    {
        public const string CollidersChildName = "Colliders";

        private const float VertexWeldCell = 0.025f;
        private const float MaxSliceHeight = 0.55f;
        private const float MaxColumnSpan = 1.85f;
        private const float MinSolidFloorY = 0.06f;
        private const float HollowFloorSkipY = 0.14f;
        private const float MinBoxThickness = 0.16f;

        /// <summary>
        /// Replaces any raw mesh colliders under <paramref name="ruinInstance"/> with fitted compound
        /// <see cref="BoxCollider"/>s on <see cref="Layers.Prop"/>.
        /// </summary>
        public static int Fit(GameObject ruinInstance)
        {
            if (ruinInstance == null)
            {
                throw new ArgumentNullException(nameof(ruinInstance));
            }

            foreach (Collider existing in ruinInstance.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(existing);
            }

            Transform existingContainer = ruinInstance.transform.Find(CollidersChildName);
            if (existingContainer != null)
            {
                UnityEngine.Object.DestroyImmediate(existingContainer.gameObject);
            }

            var container = new GameObject(CollidersChildName)
            {
                layer = Layers.Prop
            };
            Vector3 right = Vector3.ProjectOnPlane(ruinInstance.transform.right, Vector3.up);
            if (right.sqrMagnitude < 1e-4f)
            {
                right = Vector3.right;
            }
            else
            {
                right.Normalize();
            }

            Vector3 forward = Vector3.Cross(right, Vector3.up).normalized;
            container.transform.SetParent(ruinInstance.transform, false);
            container.transform.position = ruinInstance.transform.position;
            container.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);
            ruinInstance.layer = Layers.Prop;

            bool isHabitat = ruinInstance.name.StartsWith("HabitatModules", StringComparison.Ordinal);
            int created = 0;
            MeshFilter[] filters = ruinInstance.GetComponentsInChildren<MeshFilter>(true);
            for (int f = 0; f < filters.Length; f++)
            {
                MeshFilter filter = filters[f];
                if (filter == null || filter.sharedMesh == null)
                {
                    continue;
                }

                filter.gameObject.layer = Layers.Prop;
                created += BuildForMeshFilter(container.transform, filter, isHabitat);
            }

            if (isHabitat)
            {
                created += AddHabitatNorthWallGuide(container.transform);
            }

            return created;
        }

        private static int BuildForMeshFilter(Transform colliderRoot, MeshFilter filter, bool isHabitat)
        {
            Mesh mesh = filter.sharedMesh;
            Vector3[] rawVerts = mesh.vertices;
            int[] triangles = mesh.triangles;
            if (rawVerts == null || rawVerts.Length == 0)
            {
                return 0;
            }

            var localVerts = new Vector3[rawVerts.Length];
            Transform meshTransform = filter.transform;
            for (int i = 0; i < rawVerts.Length; i++)
            {
                Vector3 world = meshTransform.TransformPoint(rawVerts[i]);
                Vector3 local = colliderRoot.InverseTransformPoint(world);
                if (isHabitat)
                {
                    local.z = Mathf.Clamp(local.z, -1.56f, 1.56f);
                }

                localVerts[i] = local;
            }

            Bounds fullBounds = ComputeBounds(localVerts);
            if (IsHollowRoomShell(localVerts, fullBounds))
            {
                return BuildHollowShellColliders(colliderRoot, localVerts, fullBounds);
            }

            List<List<Vector3>> islands = SplitConnectedIslands(localVerts, triangles);
            int created = 0;
            for (int i = 0; i < islands.Count; i++)
            {
                List<Vector3> island = islands[i];
                Bounds islandBounds = ComputeBounds(island);
                if (islandBounds.max.y <= MinSolidFloorY)
                {
                    continue;
                }

                created += BuildSolidIslandColliders(colliderRoot, island, islandBounds);
            }

            return created;
        }

        /// <summary>
        /// Detects hollow walk-in room shells (such as KenjiWorkshop's QuonsetShell) where the outer bounding box is
        /// large enough for 07 to enter and the interior core volume is empty of mesh vertices.
        /// </summary>
        private static bool IsHollowRoomShell(IReadOnlyList<Vector3> verts, Bounds b)
        {
            if (b.size.x < 2.8f || b.size.z < 2.8f || b.size.y < 1.8f)
            {
                return false;
            }

            float innerHalfX = b.size.x * 0.28f;
            float innerHalfZ = b.size.z * 0.28f;
            float yLow = b.min.y + b.size.y * 0.16f;
            float yHigh = b.min.y + b.size.y * 0.68f;

            int insideCore = 0;
            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i];
                if (v.y >= yLow && v.y <= yHigh
                    && Mathf.Abs(v.x - b.center.x) <= innerHalfX
                    && Mathf.Abs(v.z - b.center.z) <= innerHalfZ)
                {
                    insideCore++;
                }
            }

            return insideCore <= Mathf.Max(2, verts.Count / 60);
        }

        /// <summary>
        /// Builds perimeter wall, jamb and overhead arch colliders for a hollow room shell while keeping its doorway
        /// portal and interior courtyard open.
        /// </summary>
        private static int BuildHollowShellColliders(Transform colliderRoot, IReadOnlyList<Vector3> verts, Bounds b)
        {
            int created = 0;
            float wallMarginX = Mathf.Max(0.42f, b.size.x * 0.14f);
            float wallMarginZ = Mathf.Max(0.42f, b.size.z * 0.11f);
            float leftMaxX = b.min.x + wallMarginX;
            float rightMinX = b.max.x - wallMarginX;
            float frontMaxZ = b.min.z + wallMarginZ;
            float backMinZ = b.max.z - wallMarginZ;
            float archBaseY = b.min.y + b.size.y * 0.72f;

            var leftWall = new List<Vector3>();
            var rightWall = new List<Vector3>();
            var frontWall = new List<Vector3>();
            var backWall = new List<Vector3>();
            var roofArch = new List<Vector3>();

            for (int i = 0; i < verts.Count; i++)
            {
                Vector3 v = verts[i];
                if (v.y <= HollowFloorSkipY)
                {
                    continue;
                }

                if (v.y >= archBaseY)
                {
                    roofArch.Add(v);
                    continue;
                }

                if (v.x <= leftMaxX)
                {
                    leftWall.Add(v);
                }
                else if (v.x >= rightMinX)
                {
                    rightWall.Add(v);
                }

                if (v.z <= frontMaxZ)
                {
                    frontWall.Add(v);
                }
                else if (v.z >= backMinZ)
                {
                    backWall.Add(v);
                }
            }

            created += SliceHeightBands(colliderRoot, leftWall);
            created += SliceHeightBands(colliderRoot, rightWall);
            created += BuildEndWallWithPortalOpening(colliderRoot, frontWall, b);
            created += BuildEndWallWithPortalOpening(colliderRoot, backWall, b);
            created += SliceHeightBands(colliderRoot, roofArch);
            return created;
        }

        /// <summary>
        /// Splits an end wall along X around any portal opening: if vertices are absent across the middle span at
        /// rover height, only the left and right jambs are built so the portal remains open; otherwise the full wall
        /// is built across X columns.
        /// </summary>
        private static int BuildEndWallWithPortalOpening(Transform colliderRoot, List<Vector3> wallVerts, Bounds shellBounds)
        {
            if (wallVerts.Count == 0)
            {
                return 0;
            }

            float portalCheckHalfX = shellBounds.size.x * 0.24f;
            float portalCheckMaxY = shellBounds.min.y + shellBounds.size.y * 0.62f;
            int middleLowCount = 0;
            for (int i = 0; i < wallVerts.Count; i++)
            {
                Vector3 v = wallVerts[i];
                if (v.y > HollowFloorSkipY + 0.08f && v.y < portalCheckMaxY
                    && Mathf.Abs(v.x - shellBounds.center.x) < portalCheckHalfX)
                {
                    middleLowCount++;
                }
            }

            if (middleLowCount == 0)
            {
                var leftJamb = new List<Vector3>();
                var rightJamb = new List<Vector3>();
                for (int i = 0; i < wallVerts.Count; i++)
                {
                    Vector3 v = wallVerts[i];
                    if (v.x < shellBounds.center.x - portalCheckHalfX)
                    {
                        leftJamb.Add(v);
                    }
                    else if (v.x > shellBounds.center.x + portalCheckHalfX)
                    {
                        rightJamb.Add(v);
                    }
                }

                return SliceHeightBands(colliderRoot, leftJamb) + SliceHeightBands(colliderRoot, rightJamb);
            }

            return SliceHeightBands(colliderRoot, wallVerts);
        }

        private static int BuildSolidIslandColliders(Transform colliderRoot, List<Vector3> verts, Bounds islandBounds)
        {
            if (verts.Count == 0)
            {
                return 0;
            }

            int xColumns = Mathf.Max(1, Mathf.CeilToInt(islandBounds.size.x / MaxColumnSpan));
            if (xColumns == 1)
            {
                return SliceHeightBands(colliderRoot, verts);
            }

            int created = 0;
            float colStep = islandBounds.size.x / xColumns;
            for (int c = 0; c < xColumns; c++)
            {
                float xMin = islandBounds.min.x + c * colStep;
                float xMax = islandBounds.min.x + (c + 1) * colStep;
                var columnVerts = new List<Vector3>();
                for (int i = 0; i < verts.Count; i++)
                {
                    Vector3 v = verts[i];
                    if (v.x >= xMin - 0.04f && v.x <= xMax + 0.04f)
                    {
                        columnVerts.Add(new Vector3(Mathf.Clamp(v.x, xMin, xMax), v.y, v.z));
                    }
                }

                created += SliceHeightBands(colliderRoot, columnVerts);
            }

            return created;
        }

        private static int SliceHeightBands(Transform colliderRoot, List<Vector3> verts)
        {
            if (verts == null || verts.Count == 0)
            {
                return 0;
            }

            Bounds b = ComputeBounds(verts);
            float minY = b.min.y < 0.85f ? 0f : b.min.y;
            float maxY = b.max.y;
            if (maxY <= MinSolidFloorY)
            {
                return 0;
            }

            float spanY = Mathf.Max(0.18f, maxY - minY);
            int slices = Mathf.Max(1, Mathf.CeilToInt(spanY / MaxSliceHeight));
            float sliceStep = spanY / slices;
            int created = 0;

            for (int s = 0; s < slices; s++)
            {
                float y0 = minY + s * sliceStep;
                float y1 = minY + (s + 1) * sliceStep;
                Vector3 min = new Vector3(b.min.x, y0, b.min.z);
                Vector3 max = new Vector3(b.max.x, y1, b.max.z);

                Vector3 size = Vector3.Max(max - min, new Vector3(MinBoxThickness, MinBoxThickness, MinBoxThickness));
                size.y = Mathf.Min(size.y, MaxSliceHeight);
                Vector3 center = (min + max) * 0.5f;
                var box = colliderRoot.gameObject.AddComponent<BoxCollider>();
                box.center = center;
                box.size = size;
                created++;
            }

            return created;
        }

        /// <summary>
        /// Adds a continuous flush BoxCollider along the north outer hull of HabitatModules oriented with its forward
        /// axis parallel to the wall (-X) and its outward normal (+Z) facing the open plateau.
        /// </summary>
        private static int AddHabitatNorthWallGuide(Transform colliderRoot)
        {
            var wallNode = new GameObject("NorthHullWall")
            {
                layer = Layers.Prop
            };
            wallNode.transform.SetParent(colliderRoot, false);
            wallNode.transform.localPosition = new Vector3(-2.50f, 2.35f, 1.42f);
            wallNode.transform.localRotation = Quaternion.LookRotation(Vector3.left, Vector3.up);
            var box = wallNode.AddComponent<BoxCollider>();
            box.center = Vector3.zero;
            box.size = new Vector3(0.36f, 4.65f, 5.04f);

            // Height-sliced East continuation along the north hull (X in [0.02, 5.50], outer face flush at Z = 1.60)
            for (int s = 0; s < 4; s++)
            {
                var eastSlice = colliderRoot.gameObject.AddComponent<BoxCollider>();
                eastSlice.center = new Vector3(2.76f, 0.30f + s * 0.50f, 1.42f);
                eastSlice.size = new Vector3(5.48f, 0.50f, 0.36f);
            }

            return 5;
        }

        private static List<List<Vector3>> SplitConnectedIslands(Vector3[] verts, int[] triangles)
        {
            int vCount = verts.Length;
            if (triangles == null || triangles.Length < 3 || vCount == 0)
            {
                return new List<List<Vector3>> { new List<Vector3>(verts) };
            }

            int[] parent = new int[vCount];
            for (int i = 0; i < vCount; i++)
            {
                parent[i] = i;
            }

            var weldedLookup = new Dictionary<Vector3Int, int>(vCount);
            float invCell = 1f / VertexWeldCell;
            for (int i = 0; i < vCount; i++)
            {
                Vector3 v = verts[i];
                var cell = new Vector3Int(
                    Mathf.RoundToInt(v.x * invCell),
                    Mathf.RoundToInt(v.y * invCell),
                    Mathf.RoundToInt(v.z * invCell));
                if (weldedLookup.TryGetValue(cell, out int existing))
                {
                    Union(parent, i, existing);
                }
                else
                {
                    weldedLookup.Add(cell, i);
                }
            }

            for (int t = 0; t + 2 < triangles.Length; t += 3)
            {
                int a = triangles[t];
                int b = triangles[t + 1];
                int c = triangles[t + 2];
                Union(parent, a, b);
                Union(parent, b, c);
            }

            var groups = new Dictionary<int, List<Vector3>>();
            for (int i = 0; i < vCount; i++)
            {
                int root = Find(parent, i);
                if (!groups.TryGetValue(root, out List<Vector3> list))
                {
                    list = new List<Vector3>();
                    groups.Add(root, list);
                }

                list.Add(verts[i]);
            }

            return new List<List<Vector3>>(groups.Values);
        }

        private static int Find(int[] parent, int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }

            return x;
        }

        private static void Union(int[] parent, int a, int b)
        {
            int ra = Find(parent, a);
            int rb = Find(parent, b);
            if (ra != rb)
            {
                parent[rb] = ra;
            }
        }

        private static Bounds ComputeBounds(IReadOnlyList<Vector3> verts)
        {
            if (verts == null || verts.Count == 0)
            {
                return default;
            }

            Vector3 min = verts[0];
            Vector3 max = verts[0];
            for (int i = 1; i < verts.Count; i++)
            {
                min = Vector3.Min(min, verts[i]);
                max = Vector3.Max(max, verts[i]);
            }

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }
    }
}
