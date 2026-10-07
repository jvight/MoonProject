using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// One rock slab of a canyon gate face (the chasm's far face or the exit's step): a box whose front is a true
    /// vertical face, whatever the terrain mesh's cell size makes of the step behind it, and whose top is flush with
    /// the high side so it can be driven and landed on.
    /// </summary>
    public readonly struct CanyonSlab
    {
        public CanyonSlab(Vector3 centre, float yawDegrees, Vector3 size)
        {
            Centre = centre;
            YawDegrees = yawDegrees;
            Size = size;
        }

        /// <summary>World centre of the box.</summary>
        public Vector3 Centre { get; }

        /// <summary>Yaw of the box: its local +Z faces up the step (toward the high side).</summary>
        public float YawDegrees { get; }

        /// <summary>Width across the corridor, height, depth along it.</summary>
        public Vector3 Size { get; }
    }
}
