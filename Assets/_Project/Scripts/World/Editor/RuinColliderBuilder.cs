using System;
using UnityEngine;
using MoonProject.Art.Editor;
using MoonProject.Core;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// Makes a Lumen ruin solid from Art's authored collision proxies (docs/art/collision-proxies.md): every
    /// <c>UCX_&lt;Part&gt;_&lt;n&gt;</c> child mesh becomes a convex <see cref="MeshCollider"/> on
    /// <see cref="Layers.Prop"/> with its renderer hidden, so walls, modules and props are solid exactly where Art
    /// drew them while doorways, the workshop floor, the airlock face and the gaps between pieces stay open. Edit
    /// time: the scene build fits them once and saves them with the scene.
    /// </summary>
    public static class RuinColliderBuilder
    {
        /// <summary>Gives every collision proxy under <paramref name="ruin"/> its convex collider.</summary>
        /// <returns>The number of colliders built.</returns>
        /// <exception cref="InvalidOperationException">The ruin carries no collision proxy.</exception>
        public static int Fit(GameObject ruin)
        {
            if (ruin == null)
            {
                throw new ArgumentNullException(nameof(ruin));
            }

            ruin.layer = Layers.Prop;
            int built = 0;
            foreach (MeshFilter filter in ruin.GetComponentsInChildren<MeshFilter>(true))
            {
                if (!filter.name.StartsWith(R1ModelPostprocessor.CollisionProxyPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                if (filter.sharedMesh == null)
                {
                    throw new InvalidOperationException($"{ruin.name}'s collision proxy '{filter.name}' has no mesh.");
                }

                GameObject proxy = filter.gameObject;
                proxy.layer = Layers.Prop;
                var collider = proxy.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                collider.convex = true;
                if (proxy.TryGetComponent(out Renderer renderer))
                {
                    renderer.enabled = false;
                }

                built++;
            }

            if (built == 0)
            {
                throw new InvalidOperationException($"{ruin.name} carries no " +
                    $"{R1ModelPostprocessor.CollisionProxyPrefix}* collision proxy; rebuild Art's R1 ruins.");
            }

            return built;
        }
    }
}
