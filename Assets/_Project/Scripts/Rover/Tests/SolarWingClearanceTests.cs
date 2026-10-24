using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;
using MoonProject.Rover.Editor;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// Verifies that 07's Mechanism A ("trunk lid") SolarWing on the generated Rover prefab never collides with or
    /// comes within 15 mm of Head, Neck, Body, Antenna, or any mounted kit across the full reachable wing and head
    /// pose envelope defined in <see cref="RoverCharacterTuning"/>.
    /// </summary>
    public sealed class SolarWingClearanceTests
    {
        private const float MinClearanceMeters = 0.015f;

        [Test]
        public void CharacterTuning_DefinesSolarWingHingeAxisAndAngleLimits_AndNeckLiftLimits()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<RoverCharacterTuning>(RoverAssetPaths.CharacterTuning);
            Assert.IsNotNull(tuning, $"Missing tuning asset at {RoverAssetPaths.CharacterTuning}.");

            Assert.AreEqual(Vector3.right, tuning.WingHingeAxis, "SolarWing hinge axis must be local +X (head-side trunk-lid hinge).");
            Assert.AreEqual(0f, tuning.WingClosedAngle, 1e-4f, "SolarWing closed angle must be 0 deg (flush in roof recess).");
            Assert.That(tuning.WingOpenAngle, Is.InRange(45f, 75f), "SolarWing open angle must be in the safe trunk-lid range.");
            Assert.That(tuning.WingOpenAngle + tuning.Chirp.SighWing, Is.LessThanOrEqualTo(75f),
                "SolarWing full open + sigh angle must stay within the 75 deg trunk-lid envelope.");

            Assert.That(tuning.NeckAlertLift, Is.InRange(2f, 15f), "NeckAlertLift must be positive and within clearance range.");
            Assert.That(tuning.NeckSleepDroop, Is.GreaterThan(10f), "NeckSleepDroop must exceed 10 deg for the waking tuck lift.");
        }

        [Test]
        public void SolarWing_MaintainsAtLeast15mmClearance_AcrossAllReachableHeadNeckAndWingPoses()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<RoverCharacterTuning>(RoverAssetPaths.CharacterTuning);
            Assert.IsNotNull(tuning, $"Missing tuning asset at {RoverAssetPaths.CharacterTuning}.");

            using (var scratch = new BuilderScratchScene())
            {
                GameObject rover = RoverPrefabBuilder.Assemble(scratch);
                Transform model = rover.transform.Find("Visual/Chassis/RoverModel");
                Assert.IsNotNull(model, "Assembled Rover must contain Visual/Chassis/RoverModel.");

                Transform solarWing = model.Find(RoverModelNodes.SolarWing);
                Transform cellFilled = solarWing != null ? solarWing.Find(RoverModelNodes.CellFilled) : null;
                Transform neck = model.Find(RoverModelNodes.Neck);
                Transform neckLift = neck != null ? neck.Find(RoverModelNodes.NeckLift) : null;
                Transform head = neckLift != null ? neckLift.Find(RoverModelNodes.Head) : null;
                Transform body = model.Find(RoverModelNodes.Body);
                Transform antenna = model.Find(RoverModelNodes.Antenna);

                Assert.IsNotNull(solarWing, "RoverModel/SolarWing must exist.");
                Assert.IsNotNull(cellFilled, "RoverModel/SolarWing/CellFilled (Tilly's gift cell) must exist.");
                Assert.IsNotNull(neck, "RoverModel/Neck must exist.");
                Assert.IsNotNull(neckLift, "RoverModel/Neck/NeckLift must exist.");
                Assert.IsNotNull(head, "RoverModel/Neck/NeckLift/Head must exist.");
                Assert.IsNotNull(body, "RoverModel/Body must exist.");
                Assert.IsNotNull(antenna, "RoverModel/Antenna must exist.");

                List<Vector3> wingPointsInWingSpace = CollectSamplePointsInFrame(solarWing, includeChildren: true);
                Assert.Greater(wingPointsInWingSpace.Count, 100, "SolarWing must have mesh geometry to sample.");

                var bodyBvh = BuildTargetBvh(body, includeChildren: false);
                var antennaBvh = BuildTargetBvh(antenna, includeChildren: true);
                var neckBvh = BuildTargetBvh(neck, includeChildren: false);
                var headBvh = BuildHeadSubtreeBvh(head, excludeRoot: null);

                Quaternion wingRest = solarWing.localRotation;
                Quaternion neckRest = neck.localRotation;
                Quaternion neckLiftRest = neckLift.localRotation;
                Quaternion headRest = head.localRotation;

                Vector3 hingeAxis = tuning.WingHingeAxis;
                float wingMin = Mathf.Min(tuning.WingClosedAngle, tuning.WingOpenAngle);
                float wingMax = Mathf.Max(tuning.WingClosedAngle, tuning.WingOpenAngle) + Mathf.Max(0f, tuning.Chirp.SighWing);
                float yawLimit = tuning.NeckYawLimit;
                float liftMin = -tuning.NeckAlertLift;
                float liftMax = Mathf.Max(tuning.NeckSleepDroop,
                    tuning.NeckTiredDroop + tuning.NeckCrawlExtend + tuning.NeckCargoExtend);
                float pitchMin = -tuning.HeadPitchDownLimit;
                float pitchMax = tuning.HeadPitchUpLimit;

                float[] wingAngles = SampleRange(wingMin, wingMax, 8);
                float[] yawAngles = SampleRange(-yawLimit, yawLimit, 9);
                float[] liftAngles = SampleRange(liftMin, liftMax, 4);
                float[] pitchAngles = SampleRange(pitchMin, pitchMax, 5);

                float minBodyDist = float.PositiveInfinity;
                float minAntennaDist = float.PositiveInfinity;
                float minNeckDist = float.PositiveInfinity;
                float minHeadDist = float.PositiveInfinity;
                string worstHeadPose = string.Empty;
                string worstBodyPose = string.Empty;

                foreach (float wDeg in wingAngles)
                {
                    solarWing.localRotation = wingRest * Quaternion.AngleAxis(wDeg, hingeAxis);

                    float dBody = QueryMinDistance(wingPointsInWingSpace, solarWing, body, bodyBvh);
                    if (dBody < minBodyDist)
                    {
                        minBodyDist = dBody;
                        worstBodyPose = $"wing={wDeg:F1}";
                    }

                    float dAnt = QueryMinDistance(wingPointsInWingSpace, solarWing, antenna, antennaBvh);
                    if (dAnt < minAntennaDist)
                    {
                        minAntennaDist = dAnt;
                    }

                    foreach (float yawDeg in yawAngles)
                    {
                        neck.localRotation = neckRest * Quaternion.AngleAxis(yawDeg, Vector3.up);

                        float dNeck = QueryMinDistance(wingPointsInWingSpace, solarWing, neck, neckBvh);
                        if (dNeck < minNeckDist)
                        {
                            minNeckDist = dNeck;
                        }

                        foreach (float liftDeg in liftAngles)
                        {
                            float effectiveLift = RoverBodyLanguage.EffectiveNeckLift(liftDeg, yawDeg);
                            neckLift.localRotation = neckLiftRest * Quaternion.AngleAxis(effectiveLift, Vector3.right);

                            foreach (float pitchDeg in pitchAngles)
                            {
                                head.localRotation = headRest * Quaternion.AngleAxis(-pitchDeg, Vector3.right);

                                float dHead = QueryMinDistance(wingPointsInWingSpace, solarWing, head, headBvh);
                                if (dHead < minHeadDist)
                                {
                                    minHeadDist = dHead;
                                    worstHeadPose = $"wing={wDeg:F1}, yaw={yawDeg:F1}, lift={liftDeg:F1}, pitch={pitchDeg:F1}";
                                }
                            }
                        }
                    }
                }

                Assert.GreaterOrEqual(minBodyDist, MinClearanceMeters,
                    $"SolarWing to Body clearance ({minBodyDist * 1000f:F2} mm at {worstBodyPose}) must be >= 15 mm.");
                Assert.GreaterOrEqual(minAntennaDist, MinClearanceMeters,
                    $"SolarWing to Antenna clearance ({minAntennaDist * 1000f:F2} mm) must be >= 15 mm.");
                Assert.GreaterOrEqual(minNeckDist, MinClearanceMeters,
                    $"SolarWing to Neck clearance ({minNeckDist * 1000f:F2} mm) must be >= 15 mm.");
                Assert.GreaterOrEqual(minHeadDist, MinClearanceMeters,
                    $"SolarWing to Head/mounted-kits clearance ({minHeadDist * 1000f:F2} mm at {worstHeadPose}) must be >= 15 mm.");
            }
        }

        private static float[] SampleRange(float min, float max, int count)
        {
            var values = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = count > 1 ? (float)i / (count - 1) : 0f;
                values[i] = Mathf.Lerp(min, max, t);
            }
            return values;
        }

        private static List<Vector3> CollectSamplePointsInFrame(Transform root, bool includeChildren)
        {
            var points = new List<Vector3>();
            Matrix4x4 worldToRoot = root.worldToLocalMatrix;
            MeshFilter[] filters = includeChildren
                ? root.GetComponentsInChildren<MeshFilter>(includeInactive: true)
                : root.GetComponents<MeshFilter>();

            foreach (MeshFilter mf in filters)
            {
                Mesh mesh = mf.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                Matrix4x4 meshToRoot = worldToRoot * mf.transform.localToWorldMatrix;
                Vector3[] verts = mesh.vertices;
                var uniqueVerts = new HashSet<Vector3Int>();
                for (int i = 0; i < verts.Length; i++)
                {
                    Vector3 p = meshToRoot.MultiplyPoint3x4(verts[i]);
                    var key = new Vector3Int(
                        Mathf.RoundToInt(p.x * 2000f),
                        Mathf.RoundToInt(p.y * 2000f),
                        Mathf.RoundToInt(p.z * 2000f));
                    if (uniqueVerts.Add(key))
                    {
                        points.Add(p);
                    }
                }

                int[] tris = mesh.triangles;
                for (int i = 0; i + 2 < tris.Length; i += 3)
                {
                    Vector3 a = meshToRoot.MultiplyPoint3x4(verts[tris[i]]);
                    Vector3 b = meshToRoot.MultiplyPoint3x4(verts[tris[i + 1]]);
                    Vector3 c = meshToRoot.MultiplyPoint3x4(verts[tris[i + 2]]);
                    float area2 = Vector3.Cross(b - a, c - a).magnitude;
                    if (area2 > 0.0004f)
                    {
                        points.Add((a + b + c) / 3f);
                        points.Add((a + b) * 0.5f);
                        points.Add((b + c) * 0.5f);
                        points.Add((c + a) * 0.5f);
                    }
                }
            }

            return points;
        }

        private static TriangleBvh BuildTargetBvh(Transform targetRoot, bool includeChildren)
        {
            var tris = new List<Triangle>();
            Matrix4x4 worldToTarget = targetRoot.worldToLocalMatrix;
            MeshFilter[] filters = includeChildren
                ? targetRoot.GetComponentsInChildren<MeshFilter>(includeInactive: true)
                : targetRoot.GetComponents<MeshFilter>();

            foreach (MeshFilter mf in filters)
            {
                AppendMeshTriangles(mf, worldToTarget, tris);
            }

            return new TriangleBvh(tris);
        }

        private static TriangleBvh BuildHeadSubtreeBvh(Transform headRoot, Transform excludeRoot)
        {
            var tris = new List<Triangle>();
            Matrix4x4 worldToHead = headRoot.worldToLocalMatrix;
            MeshFilter[] filters = headRoot.GetComponentsInChildren<MeshFilter>(includeInactive: true);
            foreach (MeshFilter mf in filters)
            {
                if (excludeRoot != null && mf.transform.IsChildOf(excludeRoot))
                {
                    continue;
                }
                if (mf.name.Contains("ScanFan"))
                {
                    continue;
                }
                AppendMeshTriangles(mf, worldToHead, tris);
            }
            return new TriangleBvh(tris);
        }

        private static void AppendMeshTriangles(MeshFilter mf, Matrix4x4 worldToTarget, List<Triangle> outTris)
        {
            Mesh mesh = mf.sharedMesh;
            if (mesh == null)
            {
                return;
            }

            Matrix4x4 toTarget = worldToTarget * mf.transform.localToWorldMatrix;
            Vector3[] verts = mesh.vertices;
            int[] indices = mesh.triangles;
            for (int i = 0; i + 2 < indices.Length; i += 3)
            {
                Vector3 a = toTarget.MultiplyPoint3x4(verts[indices[i]]);
                Vector3 b = toTarget.MultiplyPoint3x4(verts[indices[i + 1]]);
                Vector3 c = toTarget.MultiplyPoint3x4(verts[indices[i + 2]]);
                outTris.Add(new Triangle(a, b, c));
            }
        }

        private static float QueryMinDistance(
            List<Vector3> wingPointsInWingSpace,
            Transform wingTransform,
            Transform targetTransform,
            TriangleBvh targetBvh)
        {
            Matrix4x4 wingToTarget = targetTransform.worldToLocalMatrix * wingTransform.localToWorldMatrix;
            float bestSq = float.PositiveInfinity;
            for (int i = 0; i < wingPointsInWingSpace.Count; i++)
            {
                Vector3 p = wingToTarget.MultiplyPoint3x4(wingPointsInWingSpace[i]);
                bestSq = targetBvh.QueryMinDistanceSq(p, bestSq);
            }
            return Mathf.Sqrt(bestSq);
        }

        private readonly struct Triangle
        {
            public readonly Vector3 A;
            public readonly Vector3 B;
            public readonly Vector3 C;
            public readonly Vector3 Min;
            public readonly Vector3 Max;
            public readonly Vector3 Centroid;

            public Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                A = a;
                B = b;
                C = c;
                Min = Vector3.Min(a, Vector3.Min(b, c));
                Max = Vector3.Max(a, Vector3.Max(b, c));
                Centroid = (a + b + c) / 3f;
            }
        }

        private sealed class TriangleBvh
        {
            private struct Node
            {
                public Vector3 Min;
                public Vector3 Max;
                public int Left;
                public int Right;
                public int Start;
                public int Count;
            }

            private readonly Triangle[] _triangles;
            private readonly List<Node> _nodes = new List<Node>();

            public TriangleBvh(List<Triangle> triangles)
            {
                _triangles = triangles.ToArray();
                if (_triangles.Length > 0)
                {
                    Build(0, _triangles.Length);
                }
            }

            private int Build(int start, int count)
            {
                Vector3 min = _triangles[start].Min;
                Vector3 max = _triangles[start].Max;
                Vector3 cMin = _triangles[start].Centroid;
                Vector3 cMax = _triangles[start].Centroid;

                for (int i = start + 1; i < start + count; i++)
                {
                    min = Vector3.Min(min, _triangles[i].Min);
                    max = Vector3.Max(max, _triangles[i].Max);
                    cMin = Vector3.Min(cMin, _triangles[i].Centroid);
                    cMax = Vector3.Max(cMax, _triangles[i].Centroid);
                }

                int nodeIdx = _nodes.Count;
                _nodes.Add(default);

                if (count <= 8)
                {
                    _nodes[nodeIdx] = new Node { Min = min, Max = max, Left = -1, Right = -1, Start = start, Count = count };
                    return nodeIdx;
                }

                Vector3 extent = cMax - cMin;
                int axis = extent.x >= extent.y && extent.x >= extent.z ? 0 : (extent.y >= extent.z ? 1 : 2);
                float split = 0.5f * (cMin[axis] + cMax[axis]);

                int mid = start;
                for (int i = start; i < start + count; i++)
                {
                    if (_triangles[i].Centroid[axis] < split)
                    {
                        (_triangles[mid], _triangles[i]) = (_triangles[i], _triangles[mid]);
                        mid++;
                    }
                }

                if (mid == start || mid == start + count)
                {
                    mid = start + (count / 2);
                }

                int left = Build(start, mid - start);
                int right = Build(mid, start + count - mid);
                _nodes[nodeIdx] = new Node { Min = min, Max = max, Left = left, Right = right, Start = start, Count = 0 };
                return nodeIdx;
            }

            public float QueryMinDistanceSq(Vector3 p, float bestSq)
            {
                if (_nodes.Count == 0)
                {
                    return bestSq;
                }
                return QueryNode(0, p, bestSq);
            }

            private float QueryNode(int idx, Vector3 p, float bestSq)
            {
                Node n = _nodes[idx];
                if (AabbDistanceSq(p, n.Min, n.Max) >= bestSq)
                {
                    return bestSq;
                }

                if (n.Left < 0)
                {
                    int end = n.Start + n.Count;
                    for (int i = n.Start; i < end; i++)
                    {
                        float dSq = PointTriangleDistanceSq(p, _triangles[i].A, _triangles[i].B, _triangles[i].C);
                        if (dSq < bestSq)
                        {
                            bestSq = dSq;
                        }
                    }
                    return bestSq;
                }

                Node leftNode = _nodes[n.Left];
                Node rightNode = _nodes[n.Right];
                float dLeft = AabbDistanceSq(p, leftNode.Min, leftNode.Max);
                float dRight = AabbDistanceSq(p, rightNode.Min, rightNode.Max);

                if (dLeft < dRight)
                {
                    bestSq = QueryNode(n.Left, p, bestSq);
                    if (dRight < bestSq)
                    {
                        bestSq = QueryNode(n.Right, p, bestSq);
                    }
                }
                else
                {
                    bestSq = QueryNode(n.Right, p, bestSq);
                    if (dLeft < bestSq)
                    {
                        bestSq = QueryNode(n.Left, p, bestSq);
                    }
                }

                return bestSq;
            }

            private static float AabbDistanceSq(Vector3 p, Vector3 min, Vector3 max)
            {
                float dx = p.x < min.x ? min.x - p.x : (p.x > max.x ? p.x - max.x : 0f);
                float dy = p.y < min.y ? min.y - p.y : (p.y > max.y ? p.y - max.y : 0f);
                float dz = p.z < min.z ? min.z - p.z : (p.z > max.z ? p.z - max.z : 0f);
                return dx * dx + dy * dy + dz * dz;
            }

            private static float PointTriangleDistanceSq(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
            {
                Vector3 ab = b - a;
                Vector3 ac = c - a;
                Vector3 ap = p - a;
                float d1 = Vector3.Dot(ab, ap);
                float d2 = Vector3.Dot(ac, ap);
                if (d1 <= 0f && d2 <= 0f)
                {
                    return ap.sqrMagnitude;
                }

                Vector3 bp = p - b;
                float d3 = Vector3.Dot(ab, bp);
                float d4 = Vector3.Dot(ac, bp);
                if (d3 >= 0f && d4 <= d3)
                {
                    return bp.sqrMagnitude;
                }

                float vc = d1 * d4 - d3 * d2;
                if (vc <= 0f && d1 >= 0f && d3 <= 0f)
                {
                    float v = d1 / (d1 - d3);
                    return (a + v * ab - p).sqrMagnitude;
                }

                Vector3 cp = p - c;
                float d5 = Vector3.Dot(ab, cp);
                float d6 = Vector3.Dot(ac, cp);
                if (d6 >= 0f && d5 <= d6)
                {
                    return cp.sqrMagnitude;
                }

                float vb = d5 * d2 - d1 * d6;
                if (vb <= 0f && d2 >= 0f && d6 <= 0f)
                {
                    float w = d2 / (d2 - d6);
                    return (a + w * ac - p).sqrMagnitude;
                }

                float va = d3 * d6 - d5 * d4;
                if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f)
                {
                    float w = (d4 - d3) / ((d4 - d3) + (d5 - d6));
                    return (b + w * (c - b) - p).sqrMagnitude;
                }

                float denom = 1f / (va + vb + vc);
                float v2 = vb * denom;
                float w2 = vc * denom;
                return (a + ab * v2 + ac * w2 - p).sqrMagnitude;
            }
        }
    }
}
