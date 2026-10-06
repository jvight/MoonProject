using UnityEngine;
using UnityEngine.Rendering;

namespace MoonProject.Gameplay
{
    /// <summary>Creates the GameObjects that carry glow meshes: no shadows, no probes, nothing but light.</summary>
    public static class GlowObject
    {
        public static MeshRenderer Create(string name, Transform parent, Mesh mesh, Material material)
        {
            var host = new GameObject(name);
            host.transform.SetParent(parent, false);
            host.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = host.AddComponent<MeshRenderer>();
            Configure(meshRenderer, material);
            return meshRenderer;
        }

        public static void Configure(Renderer target, Material material)
        {
            target.sharedMaterial = material;
            target.shadowCastingMode = ShadowCastingMode.Off;
            target.receiveShadows = false;
            target.lightProbeUsage = LightProbeUsage.Off;
            target.reflectionProbeUsage = ReflectionProbeUsage.Off;
            target.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
        }
    }
}
