using System;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.SceneBuild;

namespace MoonProject.World.Editor
{
    /// <summary>
    /// Puts 07's lens view (<see cref="LensView"/>, registers ILensView) into Main.unity, right after the World's own
    /// systems (order 210) so it is registered before Rover's opening asks for it.
    /// </summary>
    public sealed class LensViewSceneContributor : ISceneContributor
    {
        public const string ShaderPath = "Assets/_Project/Shaders/World/LofiLensView.shader";

        /// <summary>The UI's own type, read here for 07's boot readout.</summary>
        public const string ReadoutFontPath = "Assets/_Project/UI/Fonts/BeVietnamPro-Regular.ttf";

        public int Order => 210;

        public void Contribute(SceneBuildContext context)
        {
            var lens = context.CreateChild("Lens View", context.WorldRoot.transform).AddComponent<LensView>();
            var serialized = new SerializedObject(lens);
            Assign(serialized, "_settings", context.LoadAsset<WorldSettings>(WorldPaths.Settings));
            Assign(serialized, "_shader", context.LoadAsset<Shader>(ShaderPath));
            Assign(serialized, "_readoutFont", context.LoadAsset<Font>(ReadoutFontPath));
            serialized.ApplyModifiedPropertiesWithoutUndo();
            context.AddSystem(lens);
        }

        private static void Assign(SerializedObject serialized, string field, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{nameof(LensView)} has no serialized field '{field}'; update {nameof(LensViewSceneContributor)}.");
            }

            property.objectReferenceValue = value;
        }
    }
}
