using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// What the camera frames of a Rover Bay fitting (M3-14, <see cref="BayFitting"/>), registered in the GameContext
    /// by <see cref="RoverController"/>. Positions are where things stand once 07 is parked and turned for the fitting,
    /// fixed from its start, so a shot composed from them holds still while 07 is centred and turned.
    /// </summary>
    public interface IBayFitView
    {
        /// <summary>A fitting is under way, from the bay centring 07 to letting it go.</summary>
        bool Active { get; }

        /// <summary>The arms bring the piece in, set it on and rise off it (not folding away yet).</summary>
        bool Working { get; }

        /// <summary>The piece rides the floor arm up under 07's belly.</summary>
        bool Belly { get; }

        /// <summary>Where the (first) part is set on (world).</summary>
        Vector3 Socket { get; }

        /// <summary>The shoulder of the arm setting it on, or the floor arm's tip at rest (world).</summary>
        Vector3 Shoulder { get; }

        /// <summary>07's pivot on the turntable (world).</summary>
        Vector3 Centre { get; }

        /// <summary>Which way 07 faces while the piece is set on (world, horizontal).</summary>
        Vector3 Facing { get; }

        /// <summary>Which way the bay opens from the turntable (world, horizontal): where one looks in from.</summary>
        Vector3 Front { get; }
    }
}
