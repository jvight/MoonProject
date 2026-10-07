using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Bell's choice (docs/features/M3-05 "Bell's signals"): among the undiscovered things 07 can reach, the nearest
    /// cassette to home; failing that the nearest crew log; failing that the nearest relic that has not answered yet.
    /// Distances are measured from home (Bell's corner), so the pick never depends on where 07 happens to be. Ties go
    /// to the earlier target. Pure and allocation-free.
    /// </summary>
    public static class BellSignalPicker
    {
        /// <summary>Bell's order of preference.</summary>
        private static readonly BellSignalTarget[] Preference =
        {
            BellSignalTarget.Cassette, BellSignalTarget.CrewLog, BellSignalTarget.Relic,
        };

        /// <summary>The index of the target Bell points at, or -1 when nothing reachable is left.</summary>
        public static int Pick(ISignalTargets targets, Vector3 home)
        {
            if (targets == null)
            {
                throw new ArgumentNullException(nameof(targets));
            }

            for (int p = 0; p < Preference.Length; p++)
            {
                int best = -1;
                float bestDistance = float.MaxValue;
                for (int i = 0; i < targets.Count; i++)
                {
                    SignalCandidate candidate = targets.Get(i);
                    if (candidate.Kind != Preference[p] || !candidate.Waiting || !candidate.Reachable)
                    {
                        continue;
                    }

                    float distance = SurfaceRules.HorizontalDistanceSquared(home, candidate.Position);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        best = i;
                    }
                }

                if (best >= 0)
                {
                    return best;
                }
            }

            return -1;
        }
    }
}
