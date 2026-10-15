using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Pure, allocation-free placement validator (0.5.7). Checks play-area bounds, distance from 07, terrain slope
    /// across the structure's footprint, and oriented bounding-box (OBB) overlap including clearance against placed
    /// structures.
    /// </summary>
    public static class PlacementRules
    {
        private const float OverlapEpsilon = 1e-4f;
        private const float SlopeHeightTolerance = 0.02f;

        /// <summary>
        /// Evaluates whether <paramref name="candidate"/> can be placed at <paramref name="pose"/> using distances
        /// from <paramref name="tuning"/>. Returns <see cref="PlacementReason.Valid"/> or the first reason it is not.
        /// </summary>
        public static PlacementReason Evaluate(
            StructureDefinition candidate,
            StructurePose pose,
            ITerrainQuery terrain,
            IReadOnlyList<PlacedStructure> placed,
            Vector3 roverPosition,
            BuildingTuning tuning)
        {
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            return Evaluate(
                candidate,
                pose,
                terrain,
                placed,
                roverPosition,
                tuning.MinPlacementDistance,
                tuning.MaxPlacementDistance);
        }

        /// <summary>
        /// Evaluates whether <paramref name="candidate"/> can be placed at <paramref name="pose"/>. Pure and
        /// allocation-free.
        /// </summary>
        public static PlacementReason Evaluate(
            StructureDefinition candidate,
            StructurePose pose,
            ITerrainQuery terrain,
            IReadOnlyList<PlacedStructure> placed,
            Vector3 roverPosition,
            float minDistanceFrom07,
            float maxDistanceFrom07)
        {
            if (candidate == null)
            {
                throw new ArgumentNullException(nameof(candidate));
            }

            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            PlacementReason terrainOrRange = CheckTerrainAndRange(
                candidate,
                pose,
                terrain,
                roverPosition,
                minDistanceFrom07,
                maxDistanceFrom07);
            if (terrainOrRange != PlacementReason.Valid)
            {
                return terrainOrRange;
            }

            if (placed != null)
            {
                int count = placed.Count;
                for (int i = 0; i < count; i++)
                {
                    PlacedStructure other = placed[i];
                    if (Overlaps(candidate, pose, other.Definition, other.Pose))
                    {
                        return PlacementReason.Overlaps;
                    }
                }
            }

            return PlacementReason.Valid;
        }

        /// <summary>
        /// Array overload for evaluating placement against a raw buffer of <paramref name="placedCount"/> structures.
        /// </summary>
        public static PlacementReason Evaluate(
            StructureDefinition candidate,
            StructurePose pose,
            ITerrainQuery terrain,
            PlacedStructure[] placed,
            int placedCount,
            Vector3 roverPosition,
            float minDistanceFrom07,
            float maxDistanceFrom07)
        {
            if (candidate == null)
            {
                throw new ArgumentNullException(nameof(candidate));
            }

            if (terrain == null)
            {
                throw new ArgumentNullException(nameof(terrain));
            }

            PlacementReason terrainOrRange = CheckTerrainAndRange(
                candidate,
                pose,
                terrain,
                roverPosition,
                minDistanceFrom07,
                maxDistanceFrom07);
            if (terrainOrRange != PlacementReason.Valid)
            {
                return terrainOrRange;
            }

            if (placed != null)
            {
                int limit = Mathf.Min(placedCount, placed.Length);
                for (int i = 0; i < limit; i++)
                {
                    PlacedStructure other = placed[i];
                    if (Overlaps(candidate, pose, other.Definition, other.Pose))
                    {
                        return PlacementReason.Overlaps;
                    }
                }
            }

            return PlacementReason.Valid;
        }

        /// <summary>
        /// True when two structures' oriented footprints (inflated by each structure's <c>Clearance</c>) penetrate in
        /// the horizontal XZ plane. Touching clearance boundaries are allowed.
        /// </summary>
        public static bool Overlaps(
            StructureDefinition a,
            StructurePose poseA,
            StructureDefinition b,
            StructurePose poseB)
        {
            if (a == null || b == null)
            {
                return false;
            }

            float hxA = a.Width * 0.5f + a.Clearance;
            float hzA = a.Depth * 0.5f + a.Clearance;
            float hxB = b.Width * 0.5f + b.Clearance;
            float hzB = b.Depth * 0.5f + b.Clearance;

            Vector3 rightA = poseA.Right;
            Vector3 forwardA = poseA.Forward;
            Vector3 rightB = poseB.Right;
            Vector3 forwardB = poseB.Forward;

            float dx = poseB.Position.x - poseA.Position.x;
            float dz = poseB.Position.z - poseA.Position.z;

            if (SeparatedOnAxis(rightA.x, rightA.z, dx, dz, rightA, forwardA, hxA, hzA, rightB, forwardB, hxB, hzB) ||
                SeparatedOnAxis(forwardA.x, forwardA.z, dx, dz, rightA, forwardA, hxA, hzA, rightB, forwardB, hxB, hzB) ||
                SeparatedOnAxis(rightB.x, rightB.z, dx, dz, rightA, forwardA, hxA, hzA, rightB, forwardB, hxB, hzB) ||
                SeparatedOnAxis(forwardB.x, forwardB.z, dx, dz, rightA, forwardA, hxA, hzA, rightB, forwardB, hxB, hzB))
            {
                return false;
            }

            return true;
        }

        private static PlacementReason CheckTerrainAndRange(
            StructureDefinition candidate,
            StructurePose pose,
            ITerrainQuery terrain,
            Vector3 roverPosition,
            float minDistanceFrom07,
            float maxDistanceFrom07)
        {
            float halfX = candidate.Width * 0.5f;
            float halfZ = candidate.Depth * 0.5f;
            Vector3 right = pose.Right;
            Vector3 forward = pose.Forward;
            float cx = pose.Position.x;
            float cz = pose.Position.z;

            float c0x = cx + right.x * halfX + forward.x * halfZ;
            float c0z = cz + right.z * halfX + forward.z * halfZ;
            float c1x = cx - right.x * halfX + forward.x * halfZ;
            float c1z = cz - right.z * halfX + forward.z * halfZ;
            float c2x = cx - right.x * halfX - forward.x * halfZ;
            float c2z = cz - right.z * halfX - forward.z * halfZ;
            float c3x = cx + right.x * halfX - forward.x * halfZ;
            float c3z = cz + right.z * halfX - forward.z * halfZ;

            if (!terrain.IsDrivable(cx, cz) ||
                !terrain.IsDrivable(c0x, c0z) ||
                !terrain.IsDrivable(c1x, c1z) ||
                !terrain.IsDrivable(c2x, c2z) ||
                !terrain.IsDrivable(c3x, c3z))
            {
                return PlacementReason.OutsidePlayArea;
            }

            float distSq = SurfaceRules.HorizontalDistanceSquared(pose.Position, roverPosition);
            float minDist = Mathf.Max(0f, minDistanceFrom07);
            Vector3 localRover = pose.InverseTransformPoint(roverPosition);
            bool insideFootprint = Mathf.Abs(localRover.x) < halfX && Mathf.Abs(localRover.z) < halfZ;
            if (distSq < minDist * minDist || insideFootprint)
            {
                return PlacementReason.TooCloseTo07;
            }

            if (maxDistanceFrom07 > 0f && distSq > maxDistanceFrom07 * maxDistanceFrom07)
            {
                return PlacementReason.TooFarFrom07;
            }

            float minNormalY = SurfaceRules.MinNormalY(candidate.MaxGroundSlope);
            if (terrain.SampleNormal(cx, cz).y < minNormalY ||
                terrain.SampleNormal(c0x, c0z).y < minNormalY ||
                terrain.SampleNormal(c1x, c1z).y < minNormalY ||
                terrain.SampleNormal(c2x, c2z).y < minNormalY ||
                terrain.SampleNormal(c3x, c3z).y < minNormalY)
            {
                return PlacementReason.TooSteep;
            }

            float cornerDist = Mathf.Sqrt(halfX * halfX + halfZ * halfZ);
            if (cornerDist > 0f)
            {
                float maxHeightDelta = cornerDist * Mathf.Tan(candidate.MaxGroundSlope * Mathf.Deg2Rad) +
                                       SlopeHeightTolerance;
                float centerHeight = terrain.SampleHeight(cx, cz);
                if (Mathf.Abs(terrain.SampleHeight(c0x, c0z) - centerHeight) > maxHeightDelta ||
                    Mathf.Abs(terrain.SampleHeight(c1x, c1z) - centerHeight) > maxHeightDelta ||
                    Mathf.Abs(terrain.SampleHeight(c2x, c2z) - centerHeight) > maxHeightDelta ||
                    Mathf.Abs(terrain.SampleHeight(c3x, c3z) - centerHeight) > maxHeightDelta)
                {
                    return PlacementReason.TooSteep;
                }
            }

            return PlacementReason.Valid;
        }

        private static bool SeparatedOnAxis(
            float ax,
            float az,
            float dx,
            float dz,
            Vector3 rightA,
            Vector3 forwardA,
            float hxA,
            float hzA,
            Vector3 rightB,
            Vector3 forwardB,
            float hxB,
            float hzB)
        {
            float distance = Mathf.Abs(dx * ax + dz * az);
            float radiusA = hxA * Mathf.Abs(rightA.x * ax + rightA.z * az) +
                            hzA * Mathf.Abs(forwardA.x * ax + forwardA.z * az);
            float radiusB = hxB * Mathf.Abs(rightB.x * ax + rightB.z * az) +
                            hzB * Mathf.Abs(forwardB.x * ax + forwardB.z * az);
            return distance >= radiusA + radiusB - OverlapEpsilon;
        }
    }
}
