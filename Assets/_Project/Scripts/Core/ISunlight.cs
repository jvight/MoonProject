using UnityEngine;
using MoonProject.Core.Events;

namespace MoonProject.Core
{
    /// <summary>
    /// The low circling Sun, the opening eclipse hold and the local terrain-horizon sunlight query (0.5.7 SKY-CYCLE),
    /// registered in the <see cref="GameContext"/> by the World domain. Solar arrays and 07's solar wing call
    /// <see cref="SunlightAt"/> to ask how much direct sunlight reaches a world position right now (earthshine lights
    /// the scene but never charges anything); the charging-bay repair beat calls <see cref="ReleaseOpeningEclipse"/>
    /// to let the opening eclipse end in sunrise. Allocation-free.
    /// </summary>
    public interface ISunlight
    {
        /// <summary>Current lunar clock time in seconds since the start of Day 1 after the opening hold.</summary>
        float TimeSeconds { get; }

        /// <summary>True while the opening eclipse hold is still active on a fresh game.</summary>
        bool IsHoldingEclipse { get; }

        /// <summary>Unit vector from the world toward the Sun's centre.</summary>
        Vector3 SunDirection { get; }

        /// <summary>Fraction of the Sun's disc hidden by Earth's disc right now, 0..1.</summary>
        float EclipseFactor { get; }

        /// <summary>Illuminated fraction of Earth's disc seen from the Moon, 0 (new / eclipse) .. 1 (full).</summary>
        float EarthPhase { get; }

        /// <summary>Current phase of the lunar sky cycle.</summary>
        SkyPhase Phase { get; }

        /// <summary>
        /// Real seconds until the Sun's lower limb meets the rim (the terrain horizon seen from home along the Sun's
        /// bearing) at the current pace: <see cref="float.PositiveInfinity"/> while the opening holds the Sun short of
        /// it (the eclipse, golden hour, last light), 0 once it has set.
        /// </summary>
        float SecondsToSunset { get; }

        /// <summary>Elevation, degrees above the horizon, where the first day's Sun holds at last light until the night is ready.</summary>
        float LastLightElevationDegrees { get; }

        /// <summary>
        /// Direct sunlight reaching <paramref name="position"/> right now, 0..1: 0 when the Sun is eclipsed or hidden
        /// behind the local terrain horizon along the Sun's bearing, with a soft ramp across the horizon edge.
        /// Safe to call ~20 times per second without allocations.
        /// </summary>
        float SunlightAt(Vector3 position);

        /// <summary>
        /// Releases the opening eclipse hold so the lunar clock runs on and the Sun's limb breaks out from behind
        /// Earth's edge. Safe to call when already released.
        /// </summary>
        void ReleaseOpeningEclipse();
    }
}
