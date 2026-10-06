using UnityEditor;
using UnityEngine;

namespace MoonProject.Rover.Editor
{
    /// <summary>
    /// Runs the same validation the editor applies when a material is imported or shown in the inspector: the
    /// shader's property drawers (keywords such as _FLIPBOOKBLENDING_OFF) and its ShaderGUI.ValidateMaterial (URP
    /// keywords, passes and derived properties). Builders call it before saving so a generated material is already in
    /// its final state and never dirties on a later re-run or editor visit.
    /// </summary>
    public static class MaterialValidation
    {
        public static void Validate(Material material)
        {
            MaterialEditor.ApplyMaterialPropertyDrawers(material);
            var editor = (MaterialEditor)UnityEditor.Editor.CreateEditor(material);
            try
            {
                editor.customShaderGUI?.ValidateMaterial(material);
            }
            finally
            {
                Object.DestroyImmediate(editor);
            }
        }
    }
}
