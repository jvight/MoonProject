using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Pure soft-snapping solver for structure placement (0.5.7). If snapping is enabled and a candidate structure's
    /// port or edge lies within <c>snapDistance</c> and <c>snapAngleDegrees</c> of a placed neighbour's port or edge,
    /// returns the snapped <see cref="StructurePose"/>; otherwise returns the unmodified free pose.
    /// </summary>
    public static class SnapSolver
    {
        /// <summary>
        /// Solves soft snapping against <paramref name="neighbours"/> using <paramref name="tuning"/>. When
        /// <paramref name="snapEnabled"/> is false, always returns <paramref name="freePose"/>.
        /// </summary>
        public static StructurePose Solve(
            StructureDefinition candidate,
            StructurePose freePose,
            IReadOnlyList<PlacedStructure> neighbours,
            BuildingTuning tuning,
            bool snapEnabled = true)
        {
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            return Solve(
                candidate,
                freePose,
                neighbours,
                tuning.SnapDistance,
                tuning.SnapAngleDegrees,
                snapEnabled,
                out _);
        }

        /// <summary>
        /// Solves soft snapping against <paramref name="neighbours"/>. Returns <paramref name="freePose"/> when
        /// <paramref name="snapEnabled"/> is false or no neighbour port/edge is within tolerance.
        /// </summary>
        public static StructurePose Solve(
            StructureDefinition candidate,
            StructurePose freePose,
            IReadOnlyList<PlacedStructure> neighbours,
            float snapDistance,
            float snapAngleDegrees,
            bool snapEnabled = true)
        {
            return Solve(
                candidate,
                freePose,
                neighbours,
                snapDistance,
                snapAngleDegrees,
                snapEnabled,
                out _);
        }

        /// <summary>
        /// Solves soft snapping against <paramref name="neighbours"/> and reports via <paramref name="snapped"/>
        /// whether a port or edge snap was applied.
        /// </summary>
        public static StructurePose Solve(
            StructureDefinition candidate,
            StructurePose freePose,
            IReadOnlyList<PlacedStructure> neighbours,
            float snapDistance,
            float snapAngleDegrees,
            bool snapEnabled,
            out bool snapped)
        {
            snapped = false;
            if (candidate == null)
            {
                throw new ArgumentNullException(nameof(candidate));
            }

            if (!snapEnabled || neighbours == null || neighbours.Count == 0 || snapDistance <= 0f ||
                snapAngleDegrees <= 0f)
            {
                return freePose;
            }

            if (TrySnapPorts(candidate, freePose, neighbours, snapDistance, snapAngleDegrees, out StructurePose portPose))
            {
                snapped = true;
                return portPose;
            }

            if (TrySnapEdges(candidate, freePose, neighbours, snapDistance, snapAngleDegrees, out StructurePose edgePose))
            {
                snapped = true;
                return edgePose;
            }

            return freePose;
        }

        private static bool TrySnapPorts(
            StructureDefinition candidate,
            StructurePose freePose,
            IReadOnlyList<PlacedStructure> neighbours,
            float snapDistance,
            float snapAngleDegrees,
            out StructurePose bestPose)
        {
            bestPose = freePose;
            int candidatePorts = candidate.PortCount;
            if (candidatePorts == 0)
            {
                return false;
            }

            float bestDistSq = snapDistance * snapDistance;
            bool found = false;
            int neighbourCount = neighbours.Count;

            for (int n = 0; n < neighbourCount; n++)
            {
                PlacedStructure neighbour = neighbours[n];
                StructureDefinition neighbourDef = neighbour.Definition;
                int neighbourPorts = neighbourDef.PortCount;
                for (int np = 0; np < neighbourPorts; np++)
                {
                    StructurePort targetPort = neighbourDef.PortAt(np);
                    if (!targetPort.HasValidDirection)
                    {
                        continue;
                    }

                    Vector3 targetWorldPos = targetPort.WorldPosition(neighbour.Pose);
                    Vector3 targetWorldDir = targetPort.WorldDirection(neighbour.Pose);
                    Vector3 desiredCandidateDir = new Vector3(-targetWorldDir.x, 0f, -targetWorldDir.z);

                    for (int cp = 0; cp < candidatePorts; cp++)
                    {
                        StructurePort sourcePort = candidate.PortAt(cp);
                        if (!sourcePort.HasValidDirection)
                        {
                            continue;
                        }

                        Vector3 sourceFreePos = sourcePort.WorldPosition(freePose);
                        float distSq = SurfaceRules.HorizontalDistanceSquared(sourceFreePos, targetWorldPos);
                        if (distSq > bestDistSq)
                        {
                            continue;
                        }

                        Vector3 sourceFreeDir = sourcePort.WorldDirection(freePose);
                        float angle = Vector3.Angle(sourceFreeDir, desiredCandidateDir);
                        if (angle > snapAngleDegrees)
                        {
                            continue;
                        }

                        float desiredBearing = SurfaceRules.Bearing(desiredCandidateDir);
                        float localBearing = SurfaceRules.Bearing(sourcePort.LocalDirection);
                        float snappedYaw = StructurePose.NormalizeYaw(desiredBearing - localBearing);
                        var yawOnly = new StructurePose(Vector3.zero, snappedYaw);
                        Vector3 rotatedOffset = yawOnly.TransformPoint(sourcePort.LocalPosition);
                        Vector3 snappedPosition = new Vector3(
                            targetWorldPos.x - rotatedOffset.x,
                            freePose.Position.y,
                            targetWorldPos.z - rotatedOffset.z);

                        bestDistSq = distSq;
                        bestPose = new StructurePose(snappedPosition, snappedYaw);
                        found = true;
                    }
                }
            }

            return found;
        }

        private static bool TrySnapEdges(
            StructureDefinition candidate,
            StructurePose freePose,
            IReadOnlyList<PlacedStructure> neighbours,
            float snapDistance,
            float snapAngleDegrees,
            out StructurePose bestPose)
        {
            bestPose = freePose;
            float bestGap = snapDistance;
            bool found = false;

            float candHalfX = candidate.Width * 0.5f + candidate.Clearance;
            float candHalfZ = candidate.Depth * 0.5f + candidate.Clearance;

            int neighbourCount = neighbours.Count;
            for (int i = 0; i < neighbourCount; i++)
            {
                PlacedStructure neighbour = neighbours[i];
                StructureDefinition neighbourDef = neighbour.Definition;

                if (!TrySnapCardinalYaw(freePose.Yaw, neighbour.Pose.Yaw, snapAngleDegrees, out float snappedYaw,
                        out int quarterTurns))
                {
                    continue;
                }

                float effCandHalfX = (quarterTurns & 1) == 0 ? candHalfX : candHalfZ;
                float effCandHalfZ = (quarterTurns & 1) == 0 ? candHalfZ : candHalfX;
                float neighHalfX = neighbourDef.Width * 0.5f + neighbourDef.Clearance;
                float neighHalfZ = neighbourDef.Depth * 0.5f + neighbourDef.Clearance;

                Vector3 localPos = neighbour.Pose.InverseTransformPoint(freePose.Position);
                float reqX = neighHalfX + effCandHalfX;
                float reqZ = neighHalfZ + effCandHalfZ;

                float gapRight = Mathf.Abs(localPos.x - reqX);
                if (gapRight <= bestGap && Mathf.Abs(localPos.z) <= reqZ + snapDistance)
                {
                    bestGap = gapRight;
                    float alignedZ = Mathf.Abs(localPos.z) <= snapDistance ? 0f : Mathf.Clamp(localPos.z, -reqZ, reqZ);
                    Vector3 worldPos = neighbour.Pose.TransformPoint(new Vector3(reqX, 0f, alignedZ));
                    bestPose = new StructurePose(new Vector3(worldPos.x, freePose.Position.y, worldPos.z), snappedYaw);
                    found = true;
                }

                float gapLeft = Mathf.Abs(localPos.x + reqX);
                if (gapLeft <= bestGap && Mathf.Abs(localPos.z) <= reqZ + snapDistance)
                {
                    bestGap = gapLeft;
                    float alignedZ = Mathf.Abs(localPos.z) <= snapDistance ? 0f : Mathf.Clamp(localPos.z, -reqZ, reqZ);
                    Vector3 worldPos = neighbour.Pose.TransformPoint(new Vector3(-reqX, 0f, alignedZ));
                    bestPose = new StructurePose(new Vector3(worldPos.x, freePose.Position.y, worldPos.z), snappedYaw);
                    found = true;
                }

                float gapFront = Mathf.Abs(localPos.z - reqZ);
                if (gapFront <= bestGap && Mathf.Abs(localPos.x) <= reqX + snapDistance)
                {
                    bestGap = gapFront;
                    float alignedX = Mathf.Abs(localPos.x) <= snapDistance ? 0f : Mathf.Clamp(localPos.x, -reqX, reqX);
                    Vector3 worldPos = neighbour.Pose.TransformPoint(new Vector3(alignedX, 0f, reqZ));
                    bestPose = new StructurePose(new Vector3(worldPos.x, freePose.Position.y, worldPos.z), snappedYaw);
                    found = true;
                }

                float gapBack = Mathf.Abs(localPos.z + reqZ);
                if (gapBack <= bestGap && Mathf.Abs(localPos.x) <= reqX + snapDistance)
                {
                    bestGap = gapBack;
                    float alignedX = Mathf.Abs(localPos.x) <= snapDistance ? 0f : Mathf.Clamp(localPos.x, -reqX, reqX);
                    Vector3 worldPos = neighbour.Pose.TransformPoint(new Vector3(alignedX, 0f, -reqZ));
                    bestPose = new StructurePose(new Vector3(worldPos.x, freePose.Position.y, worldPos.z), snappedYaw);
                    found = true;
                }
            }

            return found;
        }

        private static bool TrySnapCardinalYaw(
            float candidateYaw,
            float neighbourYaw,
            float snapAngleDegrees,
            out float snappedYaw,
            out int quarterTurns)
        {
            snappedYaw = candidateYaw;
            quarterTurns = 0;
            float bestDelta = snapAngleDegrees;
            bool matched = false;

            for (int q = 0; q < 4; q++)
            {
                float target = StructurePose.NormalizeYaw(neighbourYaw + q * 90f);
                float delta = StructurePose.DeltaYaw(candidateYaw, target);
                if (delta <= bestDelta)
                {
                    bestDelta = delta;
                    snappedYaw = target;
                    quarterTurns = q;
                    matched = true;
                }
            }

            return matched;
        }
    }
}
