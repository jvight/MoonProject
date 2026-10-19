using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace MoonProject.Art.Tests
{
    /// <summary>
    /// Verifies the R1 Maker Brands deliverables:
    /// 1. Clean 2048x2048 transparent colour marks and 1-colour white mono stencils in Assets/_Project/Art/R1/Brands/.
    /// 2. Kessler-Arai CRT boot monoline polyline asset (<= 12 strokes in 0..1 space) and 1024x1024 preview PNG.
    /// 3. Databank archive prints (print_&lt;brand&gt;.png at 1024x1024).
    /// </summary>
    public sealed class R1BrandAssetsTests
    {
        private const string BrandsDir = "Assets/_Project/Art/R1/Brands";

        private static readonly string[] ExpectedMarks2048 =
        {
            "ilc_mark.png",
            "ilc_mark_mono.png",
            "kessler_arai_mark.png",
            "kessler_arai_mark_mono.png",
            "tycho_mark.png",
            "tycho_mark_mono.png",
            "vis_mark.png",
            "vis_mark_mono.png",
            "vis_1967_mark.png",
            "vis_1967_mark_mono.png"
        };

        private static readonly string[] ExpectedArchivePrints1024 =
        {
            "print_ilc.png",
            "print_kessler_arai.png",
            "print_tycho.png",
            "print_vis.png"
        };

        private static readonly string[] ForbiddenDuplicates =
        {
            "ka_mark.png",
            "ka_mark_mono.png",
            "ka_crt_linework.json",
            "ka_crt_linework_preview.png",
            "print_ka.png"
        };

        [Test]
        public void ProductionMarks_ExistWith2048DimensionsAndTransparency()
        {
            foreach (string fileName in ExpectedMarks2048)
            {
                string fullPath = Path.Combine(BrandsDir, fileName);
                Assert.IsTrue(File.Exists(fullPath), $"Missing production mark: {fullPath}");

                byte[] bytes = File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Assert.IsTrue(tex.LoadImage(bytes), $"Failed to decode PNG: {fullPath}");
                    Assert.AreEqual(2048, tex.width, $"Expected width 2048 for {fileName}");
                    Assert.AreEqual(2048, tex.height, $"Expected height 2048 for {fileName}");

                    // Top-left corner should be transparent (alpha == 0)
                    Color32 corner = tex.GetPixel(4, 4);
                    Assert.AreEqual(0, corner.a, $"Expected transparent background corner in {fileName}");
                }
                finally
                {
                    Object.DestroyImmediate(tex);
                }
            }

            foreach (string dup in ForbiddenDuplicates)
            {
                string fullPath = Path.Combine(BrandsDir, dup);
                Assert.IsFalse(File.Exists(fullPath), $"Duplicate brand file must not exist: {fullPath}");
            }
        }

        [Test]
        public void ArchivePrints_ExistWith1024Dimensions()
        {
            foreach (string fileName in ExpectedArchivePrints1024)
            {
                string fullPath = Path.Combine(BrandsDir, fileName);
                Assert.IsTrue(File.Exists(fullPath), $"Missing databank archive print: {fullPath}");

                byte[] bytes = File.ReadAllBytes(fullPath);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    Assert.IsTrue(tex.LoadImage(bytes), $"Failed to decode PNG: {fullPath}");
                    Assert.AreEqual(1024, tex.width, $"Expected width 1024 for {fileName}");
                    Assert.AreEqual(1024, tex.height, $"Expected height 1024 for {fileName}");
                }
                finally
                {
                    Object.DestroyImmediate(tex);
                }
            }
        }

        [Test]
        public void KesslerAraiCrtLinework_ParsesWithinStrokeBudgetAndNormalizedBounds()
        {
            Assert.IsTrue(File.Exists(KesslerAraiCrtLinework.DefaultAssetPath),
                $"Missing CRT linework JSON at {KesslerAraiCrtLinework.DefaultAssetPath}");

            string json = File.ReadAllText(KesslerAraiCrtLinework.DefaultAssetPath);
            KesslerAraiCrtLinework linework = KesslerAraiCrtLinework.FromJson(json);

            Assert.IsNotNull(linework);
            Assert.AreEqual("kessler_arai", linework.brand);
            Assert.AreEqual("B", linework.direction);
            Assert.Greater(linework.strokes.Count, 0, "Expected at least one stroke in CRT linework.");
            Assert.LessOrEqual(linework.strokes.Count, KesslerAraiCrtLinework.MaxStrokeBudget,
                $"Expected <= {KesslerAraiCrtLinework.MaxStrokeBudget} strokes.");
            Assert.AreEqual(linework.strokes.Count, linework.strokeCount);

            foreach (var stroke in linework.strokes)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(stroke.id), "Stroke id should not be empty.");
                Assert.GreaterOrEqual(stroke.points.Count, 2, $"Stroke '{stroke.id}' must have at least 2 points.");
                foreach (Vector2 pt in stroke.points)
                {
                    Assert.GreaterOrEqual(pt.x, 0f, $"Stroke '{stroke.id}' x={pt.x} out of 0..1 range.");
                    Assert.LessOrEqual(pt.x, 1f, $"Stroke '{stroke.id}' x={pt.x} out of 0..1 range.");
                    Assert.GreaterOrEqual(pt.y, 0f, $"Stroke '{stroke.id}' y={pt.y} out of 0..1 range.");
                    Assert.LessOrEqual(pt.y, 1f, $"Stroke '{stroke.id}' y={pt.y} out of 0..1 range.");
                }
            }

            string previewPath = Path.Combine(BrandsDir, "kessler_arai_crt_linework_preview.png");
            Assert.IsTrue(File.Exists(previewPath), $"Missing CRT linework preview PNG at {previewPath}");
        }
    }
}
