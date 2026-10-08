using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>The box around Art meshes in another transform's space (colliders for meshes-only prefabs).</summary>
    public static class MeshBounds
    {
        /// <summary>
        /// The box, in <paramref name="space"/>'s local space, around every mesh under <paramref name="under"/> (itself
        /// included) as posed now; false when there is none. Runs at set-up (allocates).
        /// </summary>
        public static bool TryLocal(Transform space, Transform under, out Bounds bounds)
        {
            bool found = false;
            bounds = new Bounds();
            foreach (MeshFilter filter in under.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null)
                {
                    continue;
                }

                Bounds mesh = filter.sharedMesh.bounds;
                for (int corner = 0; corner < 8; corner++)
                {
                    var local = new Vector3((corner & 1) == 0 ? mesh.min.x : mesh.max.x,
                        (corner & 2) == 0 ? mesh.min.y : mesh.max.y, (corner & 4) == 0 ? mesh.min.z : mesh.max.z);
                    Vector3 point = space.InverseTransformPoint(filter.transform.TransformPoint(local));
                    if (found)
                    {
                        bounds.Encapsulate(point);
                    }
                    else
                    {
                        bounds = new Bounds(point, Vector3.zero);
                        found = true;
                    }
                }
            }

            return found;
        }
    }
}
