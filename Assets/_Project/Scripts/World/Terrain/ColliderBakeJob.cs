using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Cooks PhysX data for many meshes on worker threads. Assigning a baked mesh to a MeshCollider with the same
    /// (default) cooking options afterwards reuses the cooked data instead of cooking again on the main thread.
    /// </summary>
    internal struct ColliderBakeJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<int> MeshIds;

        public void Execute(int index)
        {
            Physics.BakeMesh(MeshIds[index], false);
        }
    }
}
