using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MoonProject.Art.PlayModeTests
{
    /// <summary>
    /// Look spike only: pushes the terrain's existing near-field detail harder (denser regolith tiling, a stronger near
    /// normal and micro-cavity occlusion, the near layer kept out to a longer distance) through a property block on
    /// every terrain renderer, so the shared material asset is never touched. <see cref="Dispose"/> clears the blocks.
    /// </summary>
    public sealed class LookMaxTerrainDetail : IDisposable
    {
        private const string TerrainShader = "MoonProject/World/R1Terrain";
        private const float NearTiling = 0.9f;
        private const float NearBumpScale = 1.25f;
        private const float OcclusionStrength = 0.9f;
        private const float NearFadeStart = 60f;
        private const float NearFadeEnd = 180f;

        private readonly List<Renderer> _renderers = new List<Renderer>();

        public LookMaxTerrainDetail()
        {
            var block = new MaterialPropertyBlock();
            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                Collect(root, block);
            }

            if (_renderers.Count == 0)
            {
                throw new InvalidOperationException($"No renderer uses {TerrainShader}.");
            }
        }

        public void Dispose()
        {
            foreach (Renderer renderer in _renderers)
            {
                if (renderer != null)
                {
                    renderer.SetPropertyBlock(null);
                }
            }
        }

        private void Collect(GameObject root, MaterialPropertyBlock block)
        {
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                Material material = renderer.sharedMaterial;
                if (material == null || material.shader.name != TerrainShader)
                {
                    continue;
                }

                renderer.GetPropertyBlock(block);
                block.SetFloat("_NearTiling", NearTiling);
                block.SetFloat("_NearBumpScale", NearBumpScale);
                block.SetFloat("_OcclusionStrength", OcclusionStrength);
                block.SetFloat("_NearFadeStart", NearFadeStart);
                block.SetFloat("_NearFadeEnd", NearFadeEnd);
                renderer.SetPropertyBlock(block);
                _renderers.Add(renderer);
            }
        }
    }
}
