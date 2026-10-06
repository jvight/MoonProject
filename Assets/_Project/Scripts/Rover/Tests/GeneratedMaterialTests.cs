using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Rover.Editor;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// The Rover builders save materials already in the state the editor's validation would put them in, so neither a
    /// builder re-run nor opening them in the editor ever produces a diff.
    /// </summary>
    public sealed class GeneratedMaterialTests
    {
        [TestCase(RoverAssetPaths.DustMaterial)]
        [TestCase(RoverAssetPaths.TrackMaterial)]
        public void GeneratedMaterial_IsAlreadyValidated(string path)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Assert.IsNotNull(material, $"{path} is missing: run the Rover builders.");

            Material copy = Object.Instantiate(material);
            try
            {
                copy.name = material.name;
                MaterialValidation.Validate(copy);
                Assert.AreEqual(EditorJsonUtility.ToJson(material, true), EditorJsonUtility.ToJson(copy, true),
                    "Editor validation would change this material: the builder must save it validated.");
            }
            finally
            {
                Object.DestroyImmediate(copy);
            }
        }
    }
}
