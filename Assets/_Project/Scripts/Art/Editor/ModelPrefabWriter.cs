using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// Turns a <see cref="ModelNode"/> tree into assets through <see cref="GeneratedAssets"/> (GUIDs survive
    /// re-runs): one '&lt;folder&gt;/&lt;mesh name&gt;.asset' per distinct <see cref="ModelMesh"/> and
    /// '&lt;folder&gt;/&lt;root name&gt;.prefab' with a MeshFilter + MeshRenderer on every node that has a mesh.
    /// The prefab is assembled in a <see cref="BuilderScratchScene"/>, so the open scene is never touched, and is only
    /// saved when it changed.
    /// </summary>
    public static class ModelPrefabWriter
    {
        public static GameObject Write(ModelNode root, string folder, Material material)
        {
            return Write(root, folder, material, null);
        }

        /// <summary>
        /// As <see cref="Write(ModelNode, string, Material)"/>; nodes marked <see cref="ModelMaterial.PaletteGlowOff"/>
        /// render with <paramref name="glowOffMaterial"/>.
        /// </summary>
        public static GameObject Write(ModelNode root, string folder, Material material, Material glowOffMaterial)
        {
            return Write(root, folder, material, glowOffMaterial, null);
        }

        /// <summary>
        /// As <see cref="Write(ModelNode, string, Material, Material)"/>; nodes marked
        /// <see cref="ModelMaterial.PaletteWeather"/> (the weather skins) render with
        /// <paramref name="weatherMaterial"/>.
        /// </summary>
        public static GameObject Write(ModelNode root, string folder, Material material, Material glowOffMaterial,
            Material weatherMaterial)
        {
            if (root == null)
            {
                throw new ArgumentNullException(nameof(root));
            }

            if (material == null)
            {
                throw new ArgumentNullException(nameof(material), "Run the Art/Palette builder first.");
            }

            GeneratedAssets.EnsureFolder(folder);
            var meshes = new Dictionary<ModelMesh, Mesh>();
            WriteMeshes(root, folder, meshes);
            using (var scratch = new BuilderScratchScene())
            {
                var materials = new[] { material, glowOffMaterial, weatherMaterial };
                GameObject instance = Instantiate(scratch, root, null, meshes, materials);
                string path = $"{folder}/{root.Name}.prefab";

                // Unity matches objects by name when it overwrites a prefab, so nodes sharing a name (each salvage
                // piece's CutPoint) would get fresh file IDs on every save: an unchanged prefab is left untouched.
                var saved = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (saved != null && SameModel(saved.transform, instance.transform))
                {
                    return saved;
                }

                return GeneratedAssets.SavePrefab(instance, path);
            }
        }

        /// <summary>
        /// True when <paramref name="saved"/> already holds exactly what <see cref="Instantiate"/> built: names, active
        /// states, poses, components, meshes and materials, all the way down.
        /// </summary>
        private static bool SameModel(Transform saved, Transform built)
        {
            if (saved.name != built.name || saved.gameObject.activeSelf != built.gameObject.activeSelf
                || !saved.localPosition.Equals(built.localPosition)
                || !saved.localRotation.Equals(built.localRotation) || !saved.localScale.Equals(built.localScale)
                || saved.childCount != built.childCount || !SameRendering(saved.gameObject, built.gameObject))
            {
                return false;
            }

            for (int i = 0; i < saved.childCount; i++)
            {
                if (!SameModel(saved.GetChild(i), built.GetChild(i)))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SameRendering(GameObject saved, GameObject built)
        {
            Component[] savedComponents = saved.GetComponents<Component>();
            Component[] builtComponents = built.GetComponents<Component>();
            if (savedComponents.Length != builtComponents.Length)
            {
                return false;
            }

            for (int i = 0; i < savedComponents.Length; i++)
            {
                if (savedComponents[i].GetType() != builtComponents[i].GetType())
                {
                    return false;
                }
            }

            if (saved.TryGetComponent(out MeshFilter filter)
                && filter.sharedMesh != built.GetComponent<MeshFilter>().sharedMesh)
            {
                return false;
            }

            return !saved.TryGetComponent(out MeshRenderer renderer)
                || renderer.sharedMaterial == built.GetComponent<MeshRenderer>().sharedMaterial;
        }

        private static void WriteMeshes(ModelNode node, string folder, Dictionary<ModelMesh, Mesh> meshes)
        {
            if (node.Mesh != null && !meshes.ContainsKey(node.Mesh))
            {
                foreach (ModelMesh written in meshes.Keys)
                {
                    if (written.Name == node.Mesh.Name)
                    {
                        throw new InvalidOperationException($"Two different meshes are named '{written.Name}'.");
                    }
                }

                Mesh mesh = node.Mesh.Geometry.ToMesh(node.Mesh.Name);
                meshes.Add(node.Mesh, GeneratedAssets.CreateOrReplace(mesh, $"{folder}/{node.Mesh.Name}.asset"));
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                WriteMeshes(node.Children[i], folder, meshes);
            }
        }

        /// <summary>A node's material (<paramref name="materials"/> in <see cref="ModelMaterial"/> order).</summary>
        private static Material MaterialFor(ModelNode node, Material[] materials)
        {
            Material chosen = materials[(int)node.Material];
            if (chosen == null)
            {
                throw new InvalidOperationException(
                    $"'{node.Name}' renders with {node.Material} but no such material was given.");
            }

            return chosen;
        }

        private static GameObject Instantiate(BuilderScratchScene scratch, ModelNode node, Transform parent,
            Dictionary<ModelMesh, Mesh> meshes, Material[] materials)
        {
            GameObject gameObject = scratch.Create(node.Name, parent);
            Transform transform = gameObject.transform;
            transform.localPosition = node.LocalPosition;
            transform.localRotation = node.LocalRotation;
            gameObject.SetActive(node.Active);
            if (node.Mesh != null)
            {
                gameObject.AddComponent<MeshFilter>().sharedMesh = meshes[node.Mesh];
                gameObject.AddComponent<MeshRenderer>().sharedMaterial = MaterialFor(node, materials);
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                Instantiate(scratch, node.Children[i], transform, meshes, materials);
            }

            return gameObject;
        }
    }
}
