using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Art's status lamp on a load (e.g. the towable power module): the node's renderer, the material slot that wears
    /// the amber lens (found by name, so never the bezel, in whatever slot order Art exports), and where the lens sits
    /// and faces in the node's own space (the lens bulges out of its bezel, so its middle sits in front of the pivot).
    /// Found once; read each frame through the node's transform as the load moves.
    /// </summary>
    public sealed class StatusLampLens
    {
        private StatusLampLens(Transform node, Renderer renderer, int slot, Vector3 middle, Vector3 facing)
        {
            Node = node;
            Renderer = renderer;
            Slot = slot;
            Middle = middle;
            Facing = facing;
        }

        public Transform Node { get; }
        public Renderer Renderer { get; }

        /// <summary>The material slot that wears the lens.</summary>
        public int Slot { get; }

        /// <summary>The lens's middle in the node's space.</summary>
        public Vector3 Middle { get; }

        /// <summary>The way the lens faces in the node's space (unit).</summary>
        public Vector3 Facing { get; }

        /// <summary>The lens's middle in the world, <paramref name="lift"/> metres out in front of it.</summary>
        public Vector3 InFront(float lift)
        {
            return Node.TransformPoint(Middle) + Node.TransformDirection(Facing) * lift;
        }

        /// <summary>
        /// The lamp called <paramref name="nodeName"/> under <paramref name="root"/>, its lens wearing
        /// <paramref name="lensMaterial"/>; null with the <paramref name="problem"/> when there is none to blink.
        /// </summary>
        public static StatusLampLens Find(Transform root, string nodeName, string lensMaterial, out string problem)
        {
            Transform node = null;
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == nodeName)
                {
                    node = child;
                    break;
                }
            }

            var renderer = node != null ? node.GetComponent<MeshRenderer>() : null;
            var filter = node != null ? node.GetComponent<MeshFilter>() : null;
            Mesh mesh = filter != null ? filter.sharedMesh : null;
            if (renderer == null || mesh == null)
            {
                problem = $"'{root.name}' has no {nodeName} with a mesh to blink.";
                return null;
            }

            Material[] materials = renderer.sharedMaterials;
            for (int slot = 0; slot < materials.Length && slot < mesh.subMeshCount; slot++)
            {
                if (materials[slot] == null || materials[slot].name != lensMaterial)
                {
                    continue;
                }

                Vector3 middle = mesh.GetSubMesh(slot).bounds.center;
                if (middle.sqrMagnitude <= 0f)
                {
                    problem = $"'{root.name}''s {nodeName} has its pivot in its lens's middle, so the way it faces " +
                        "cannot be read.";
                    return null;
                }

                problem = null;
                return new StatusLampLens(node, renderer, slot, middle, middle.normalized);
            }

            problem = $"'{root.name}''s {nodeName} has no {lensMaterial} lens.";
            return null;
        }
    }
}
