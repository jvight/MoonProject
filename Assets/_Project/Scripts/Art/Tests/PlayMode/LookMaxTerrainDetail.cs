using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MoonProject.Art.PlayModeTests
{
    /// <summary>
    /// Look spike only: pushes the terrain's near-field detail harder (a stronger near normal and micro-cavity
    /// occlusion, the near layer kept out to a longer distance) through a property block on every terrain renderer, so
    /// the shared material asset is never touched. Without CC0 layers it also tiles the shipped regolith denser; with
    /// them it swaps in their regolith and rock maps (loaded from disk, never imported), tiled to the ground each set
    /// covers. <see cref="Dispose"/> clears the blocks and frees the textures.
    /// </summary>
    public sealed class LookMaxTerrainDetail : IDisposable
    {
        private const string TerrainShader = "MoonProject/World/R1Terrain";
        private const float NearTiling = 0.9f;
        private const float NearBumpScale = 1.25f;
        private const float OcclusionStrength = 0.9f;
        private const float NearFadeStart = 60f;
        private const float NearFadeEnd = 180f;
        private const float MacroToNear = 0.22f;
        private const int Anisotropy = 16;

        private readonly List<Renderer> _renderers = new List<Renderer>();
        private readonly List<Texture2D> _textures = new List<Texture2D>();

        public LookMaxTerrainDetail(LookMaxVista.Cc0 cc0)
        {
            var block = new MaterialPropertyBlock();
            block.SetFloat("_NearBumpScale", NearBumpScale);
            block.SetFloat("_OcclusionStrength", OcclusionStrength);
            block.SetFloat("_NearFadeStart", NearFadeStart);
            block.SetFloat("_NearFadeEnd", NearFadeEnd);
            if (cc0 == null)
            {
                block.SetFloat("_NearTiling", NearTiling);
            }
            else
            {
                string root = Path.GetFullPath(Path.Combine(Application.dataPath, "..", cc0.folder));
                string regolith = Path.Combine(root, cc0.regolith);
                string rock = Path.Combine(root, cc0.rock);
                block.SetTexture("_BaseMap", Load(Path.Combine(regolith, "albedo.png"), false));
                block.SetTexture("_BumpMap", Load(Path.Combine(regolith, "normal.png"), true));
                block.SetTexture("_MaskMap", Load(Path.Combine(regolith, "mask.png"), true));
                block.SetTexture("_RockMap", Load(Path.Combine(rock, "albedo.png"), false));
                block.SetTexture("_RockBumpMap", Load(Path.Combine(rock, "normal.png"), true));
                block.SetTexture("_RockMaskMap", Load(Path.Combine(rock, "mask.png"), true));
                block.SetFloat("_NearTiling", 1f / cc0.regolithMetres);
                block.SetFloat("_NearBumpScale", cc0.nearBumpScale);
                block.SetFloat("_MacroTiling", MacroToNear / cc0.regolithMetres);
                block.SetFloat("_RockTiling", 1f / cc0.rockMetres);
            }

            foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    Material material = renderer.sharedMaterial;
                    if (material != null && material.shader.name == TerrainShader)
                    {
                        renderer.SetPropertyBlock(block);
                        _renderers.Add(renderer);
                    }
                }
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

            foreach (Texture2D texture in _textures)
            {
                Object.Destroy(texture);
            }
        }

        private Texture2D Load(string path, bool linear)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Run tools/art/look_max_cc0.py first.", path);
            }

            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, true, linear)
            {
                name = Path.GetFileName(Path.GetDirectoryName(path)) + "/" + Path.GetFileName(path),
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = Anisotropy,
            };
            texture.LoadImage(File.ReadAllBytes(path), false);
            texture.Apply(true, true);
            _textures.Add(texture);
            return texture;
        }
    }
}
