using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using MoonProject.Editor.Builders;
using static MoonProject.Art.Place;

namespace MoonProject.Art.Editor
{
    /// <summary>
    /// The scannable fragments (0.5.5 owner feedback §1), <c>Fragment_&lt;Blueprint&gt;_&lt;n&gt;</c> in
    /// Generated/Art/Fragments: the root on the surface it rests on at its footprint's centre, +Y up from it, +Z its
    /// front (the half-tilt is modelled in), holding
    /// <list type="bullet">
    /// <item><c>Solid</c> (M_ScanSolid) and <c>Lines</c> (M_ScanLines, its white line drawing), each driven with a
    /// MaterialPropertyBlock: <c>_ScanProgress</c> 0..1 (the cyan sweep climbing it; below it the solid pales and the
    /// lines show) and <c>_Dissolve</c> 0..1 (the 900 ms close: the solid fades out by 0.33, the lines unthread inward
    /// by 0.89, then the lens flares);</item>
    /// <item><c>ScanTarget</c>, the point 07 looks at and where the ribbon leaves for its lens;</item>
    /// <item><c>Brackets</c> (M_LowPolyGlowOff, cyan), its four corner brackets, inactive until gameplay shows
    /// them;</item>
    /// <item><c>DustOutline</c> (M_ScanSolid), the faint outline left on the ground, inactive until the dissolve ends,
    /// then faded with <c>_Fade</c> 0..1 over 2 s.</item>
    /// </list>
    /// Also writes the two scan materials, M_ScanSolid and M_ScanLines.
    /// </summary>
    public static class FragmentBuilder
    {
        public const string PrefabPrefix = "Fragment_";
        public const string SolidName = "Solid";
        public const string LinesName = "Lines";
        public const string ScanTargetName = "ScanTarget";
        public const string BracketsName = "Brackets";
        public const string DustOutlineName = "DustOutline";

        private const float BracketArm = 0.08f;
        private const float BracketBar = 0.014f;
        private const float OutlineMargin = 0.06f;
        private const float SweepIntensity = 0.52f;
        private const float SweepWidth = 0.022f;
        private static readonly Color LineColor = new Color(0.88f, 0.92f, 0.96f, 1f);

        /// <summary>The blueprints with fragments (RoverAbility's names) and how many each has.</summary>
        public static IReadOnlyList<(string Blueprint, int Count)> Sets => FragmentMeshes.Sets;

        public static string PrefabName(string blueprint, int index)
        {
            return PrefabPrefix + blueprint + "_" + index.ToString(CultureInfo.InvariantCulture);
        }

        [MoonBuilder("Art/Fragments", 127)]
        public static void Build()
        {
            Material palette = PaletteAssetBuilder.LoadMaterial();
            Material scanSolid = GeneratedAssets.CreateOrReplace(CreateScanSolidMaterial(palette),
                ArtPaths.ScanSolidMaterial);
            Material scanLines = GeneratedAssets.CreateOrReplace(CreateScanLinesMaterial(), ArtPaths.ScanLinesMaterial);
            var materials = new[] { palette, PaletteAssetBuilder.LoadGlowOffMaterial(), null, scanSolid, scanLines };
            foreach ((string blueprint, int count) in Sets)
            {
                for (int i = 0; i < count; i++)
                {
                    ModelPrefabWriter.Write(CreateFragment(blueprint, i), ArtPaths.FragmentFolder, materials);
                }
            }

            AssetDatabase.SaveAssets();
        }

        public static ModelNode CreateFragment(string blueprint, int index)
        {
            string name = PrefabName(blueprint, index);
            LowPolyMeshBuilder solid = FragmentMeshes.Fragment(blueprint, index);
            Vector2 tilt = FragmentMeshes.Tilt(blueprint, index);
            MeshRange all = solid.RangeFrom(0);
            solid.Transform(all, Matrix4x4.Rotate(Quaternion.Euler(tilt.x, 0f, tilt.y)));
            Bounds tilted = solid.Bounds;
            solid.Transform(all, Matrix4x4.Translate(new Vector3(-tilted.center.x, -tilted.min.y, -tilted.center.z)));
            Bounds bounds = solid.Bounds;
            float thickness = Mathf.Lerp(0.006f, 0.012f, Mathf.InverseLerp(0.3f, 1.4f, bounds.size.magnitude));

            var root = new ModelNode(name, Vector3.zero);
            root.Add(new ModelNode(SolidName, Vector3.zero, Quaternion.identity, new ModelMesh(name + "_" + SolidName,
                solid), ModelMaterial.ScanSolid));
            root.Add(new ModelNode(LinesName, Vector3.zero, Quaternion.identity, new ModelMesh(name + "_" + LinesName,
                LineDrawing.FromMesh(solid, thickness)), ModelMaterial.ScanLines));
            root.Add(new ModelNode(ScanTargetName, bounds.center));
            root.Add(new ModelNode(BracketsName, Vector3.zero, Quaternion.identity,
                new ModelMesh(name + "_" + BracketsName, Brackets(bounds)), ModelMaterial.PaletteGlowOff, false));
            root.Add(new ModelNode(DustOutlineName, Vector3.zero, Quaternion.identity,
                new ModelMesh(name + "_" + DustOutlineName, DustOutline(bounds)), ModelMaterial.ScanSolid, false));
            return root;
        }

        /// <summary>The scan material for a fragment's solid (not saved): the palette's maps on LofiScanSolid, no glow
        /// of its own.</summary>
        public static Material CreateScanSolidMaterial(Material palette)
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ArtPaths.ScanSolidShader);
            if (shader == null)
            {
                throw new InvalidOperationException($"The scan solid shader is missing at {ArtPaths.ScanSolidShader}.");
            }

            Material material = PaletteAssetBuilder.CreateMaterial(shader, palette.GetTexture("_BaseMap"),
                palette.GetTexture("_EmissionMap"), true);
            material.SetColor("_SweepColor", Palette.Linear(Palette.Get(PaletteSwatch.TechGlow)) * SweepIntensity);
            material.SetColor("_DrawnColor", Palette.GetSurface(PaletteSwatch.Cream));
            material.SetFloat("_SweepWidth", SweepWidth);
            return material;
        }

        /// <summary>The line drawing's material (not saved): unlit, pale blueprint white under the bloom threshold.</summary>
        public static Material CreateScanLinesMaterial()
        {
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(ArtPaths.ScanLinesShader);
            if (shader == null)
            {
                throw new InvalidOperationException($"The scan lines shader is missing at {ArtPaths.ScanLinesShader}.");
            }

            var material = new Material(shader) { enableInstancing = true };
            material.SetColor("_LineColor", LineColor);
            return material;
        }

        /// <summary>The four corner brackets on the fragment's top corners, each three short bars along the edges
        /// that meet there, so they read as brackets from any side.</summary>
        private static LowPolyMeshBuilder Brackets(Bounds bounds)
        {
            var b = new LowPolyMeshBuilder(160);
            Vector3 min = bounds.min - Vector3.one * 0.02f;
            Vector3 max = bounds.max + Vector3.one * 0.02f;
            float arm = Mathf.Min(BracketArm, bounds.size.y * 0.5f);
            for (int x = 0; x <= 1; x++)
            {
                for (int z = 0; z <= 1; z++)
                {
                    var corner = new Vector3(x == 0 ? min.x : max.x, max.y, z == 0 ? min.z : max.z);
                    var inward = new Vector3(x == 0 ? 1f : -1f, -1f, z == 0 ? 1f : -1f);
                    foreach (Vector3 axis in new[] { Vector3.right, Vector3.up, Vector3.forward })
                    {
                        Vector3 along = Vector3.Scale(axis, inward) * (axis == Vector3.up ? arm : BracketArm);
                        SiteKit.Bar(b, corner, corner + along, BracketBar, PaletteSwatch.EyeGlass);
                    }
                }
            }

            return b;
        }

        /// <summary>The faint dust outline the fragment leaves where it lay: a thin dark rim round its
        /// footprint.</summary>
        private static LowPolyMeshBuilder DustOutline(Bounds bounds)
        {
            var b = new LowPolyMeshBuilder(60);
            float x = bounds.extents.x + OutlineMargin;
            float z = bounds.extents.z + OutlineMargin;
            const float rim = 0.04f;
            for (int side = -1; side <= 1; side += 2)
            {
                b.Box(At(side * x, 0.004f, 0f), new Vector3(rim, 0.006f, z * 2f), PaletteSwatch.DustShadow);
                b.Box(At(0f, 0.004f, side * z), new Vector3(x * 2f, 0.006f, rim), PaletteSwatch.DustShadow);
            }

            return b;
        }
    }
}
