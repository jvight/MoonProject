using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// Art's face drawing reaches the screen through the built <see cref="RoverDiagramData"/>: the line drawing itself,
    /// where its lines sit in the image, one leader anchor per device on those lines (read once from Art's JSON), and
    /// the face icons parsed from their 256-unit SVGs into the painter's 64 units.
    /// </summary>
    public sealed class FaceArtTests
    {
        [Test]
        public void TheContentRect_IsTheBoundsOfTheDrawnPixels_WithATopLeftOrigin()
        {
            const int width = 8;
            const int height = 4;
            var pixels = new Color32[width * height];
            pixels[1 * width + 2] = new Color32(255, 255, 255, 255);
            pixels[2 * width + 5] = new Color32(255, 255, 255, 255);
            pixels[3 * width + 7] = new Color32(255, 255, 255, 4);

            Rect content = FaceArtReader.ContentOf(pixels, width, height);

            Assert.AreEqual(2f / width, content.x, 1e-5f);
            Assert.AreEqual(4f / width, content.width, 1e-5f, "columns 2..5");
            Assert.AreEqual(1f / height, content.y, 1e-5f, "row 2 from the bottom is row 1 from the top");
            Assert.AreEqual(2f / height, content.height, 1e-5f, "rows 1..2");
        }

        [Test]
        public void TheBuiltDiagram_CarriesArtsDrawing_ItsAnchors_AndTheFaceIcons()
        {
            RoverDiagramBuilder.Build();
            var diagram = AssetDatabase.LoadAssetAtPath<RoverDiagramData>(UiAssetPaths.RoverDiagram);
            Assert.IsNotNull(diagram, $"{UiAssetPaths.RoverDiagram} is missing: run UI/Rover Diagram");
            Assert.IsNull(diagram.Validate());
            Assert.AreSame(AssetDatabase.LoadAssetAtPath<Texture2D>(UiAssetPaths.FaceArt), diagram.FaceArt);

            Rect content = diagram.FaceArtContent;
            for (int device = 0; device < FaceDevices.Count; device++)
            {
                Assert.IsTrue(content.Contains(diagram.FaceAnchor(device)), $"device {device}'s anchor is on the lines");
            }

            Assert.Less(diagram.FaceAnchor(FaceDevices.Cutter).x, diagram.FaceAnchor(FaceDevices.Scan).x,
                "the cutter's nozzle is at 07's front, left of the lens");
            Assert.Less(diagram.FaceAnchor(FaceDevices.Scan).y, diagram.FaceAnchor(FaceDevices.SocketTwo).y,
                "the lens sits high on the head, the sockets low on the body");

            foreach (string id in RoverDiagramData.FaceIconIds)
            {
                UiIconShape icon = diagram.GetIcon(id);
                Assert.IsNull(icon.Validate(), $"{id} has an outline or filled tones");
                AssertInRange(icon.Primary, id);
                Assert.Less(icon.Primary.StrokeWidth, UiIconShape.ViewBoxSize * 0.05f, $"{id}: strokes scaled to 64");
                foreach (UiIconTone tone in icon.Tones)
                {
                    AssertInRange(tone.Path, id);
                    Assert.IsTrue(tone.Opacity > 0f && tone.Opacity <= 1f, $"{id}: a tone's opacity is a share");
                }
            }
        }

        private static void AssertInRange(UiIconPath path, string id)
        {
            for (int i = 0; i < path.Values.Length; i++)
            {
                Assert.LessOrEqual(Mathf.Abs(path.Values[i]), 360f, $"{id}: values stay in range");
            }
        }
    }
}
