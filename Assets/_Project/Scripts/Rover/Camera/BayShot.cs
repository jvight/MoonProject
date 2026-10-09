using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>A camera pose composed for a Rover Bay fitting: what it looks at and where it stands (world).</summary>
    public readonly struct BayShot
    {
        public BayShot(Vector3 focus, Vector3 eye)
        {
            Focus = focus;
            Eye = eye;
        }

        public Vector3 Focus { get; }

        public Vector3 Eye { get; }
    }
}
