using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;
using MoonProject.Rover.Editor;

namespace MoonProject.Rover.Tests
{
    /// <summary>
    /// On 07's real build (the generated art and tuning on disk), the comparison the Rover/Rover builder trusts before
    /// skipping a save: two builds are alike, and changes inside the nested kit prefabs are seen, including which of
    /// the two capacitor drums (instances of one prefab) is which.
    /// </summary>
    public sealed class RoverBuildComparisonTests
    {
        private const string DrumName = "Kit_CapacitorDrum";

        [Test]
        public void TwoBuilds_AreAlike()
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject first = RoverPrefabBuilder.Assemble(scratch);
                GameObject second = RoverPrefabBuilder.Assemble(scratch);
                Assert.IsNull(HierarchyComparison.FirstDifference(first, second));
            }
        }

        [Test]
        public void ShownDrum_IsADifference()
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject first = RoverPrefabBuilder.Assemble(scratch);
                GameObject second = RoverPrefabBuilder.Assemble(scratch);
                Transform socket = BuildWiring.Node(second.transform, RoverModelNodes.DrumSocketRight);
                BuildWiring.Node(socket, DrumName).gameObject.SetActive(true);
                StringAssert.Contains($"{RoverModelNodes.DrumSocketRight}/{DrumName}: GameObject.m_IsActive",
                    HierarchyComparison.FirstDifference(first, second));
            }
        }

        [Test]
        public void SwappedDrums_AreADifference()
        {
            using (var scratch = new BuilderScratchScene())
            {
                GameObject first = RoverPrefabBuilder.Assemble(scratch);
                GameObject second = RoverPrefabBuilder.Assemble(scratch);
                var kit = new SerializedObject(second.GetComponentInChildren<RoverKit>(true));
                SerializedProperty drums = kit.FindProperty("_drums");
                SerializedProperty left = drums.GetArrayElementAtIndex(0);
                SerializedProperty right = drums.GetArrayElementAtIndex(1);
                Object leftDrum = left.objectReferenceValue;
                left.objectReferenceValue = right.objectReferenceValue;
                right.objectReferenceValue = leftDrum;
                kit.ApplyModifiedPropertiesWithoutUndo();
                StringAssert.Contains("RoverKit._drums", HierarchyComparison.FirstDifference(first, second));
            }
        }
    }
}
