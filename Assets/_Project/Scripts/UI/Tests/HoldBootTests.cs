using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The first Tab press: a boot of about 1.2 s (type, light the slots, slide the items in), once per save, taught by
    /// one "Tab · Hold" chip at the tower.
    /// </summary>
    public sealed class HoldBootTests
    {
        private const float Frame = 1f / 60f;

        private CargoScreenSettings _settings;
        private HoldBoot _boot;

        [SetUp]
        public void SetUp()
        {
            _settings = new CargoScreenSettings();
            _boot = new HoldBoot(_settings);
        }

        [Test]
        public void TheBoot_TakesAboutOnePointTwoSeconds_ForTheStarterHold()
        {
            _boot.Start(8, 2);
            Assert.That(_boot.TotalSeconds, Is.InRange(1f, 1.4f));
        }

        [Test]
        public void TheSlotsLight_LeftToRight_SixtyMillisecondsApart_AfterTheLineTypes()
        {
            _boot.Start(8, 2);
            Assert.AreEqual(0, _boot.LitCount, "dark first");
            Assert.AreEqual(0f, _boot.TypedFraction);
            Run(_settings.BootTypeSeconds * 0.5f);
            Assert.AreEqual(0.5f, _boot.TypedFraction, 0.05f);
            Assert.AreEqual(0, _boot.LitCount, "no outline before the line is typed");

            Run(_settings.BootTypeSeconds * 0.5f + _settings.BootLightStepSeconds * 0.5f);
            Assert.AreEqual(1, _boot.LitCount);
            Run(_settings.BootLightStepSeconds * 3f);
            Assert.AreEqual(4, _boot.LitCount, 1, "one more every 60 ms");
            Run(5f);
            Assert.AreEqual(8, _boot.LitCount);
        }

        [Test]
        public void TheItems_SlideIn_OneAfterAnother_OnlyAfterTheOutlinesAreLit()
        {
            _boot.Start(8, 2);
            Run(_settings.BootTypeSeconds + _settings.BootLightStepSeconds * 7f);
            Assert.AreEqual(0f, _boot.ItemProgress(0), "the lights come first");
            Run(_settings.BootLightStepSeconds * 2f + _settings.BootSlideSeconds * 0.5f);
            Assert.Greater(_boot.ItemProgress(0), 0f);
            Assert.Less(_boot.ItemProgress(1), _boot.ItemProgress(0), "the second follows the first");
            Assert.AreEqual(0, _boot.ArrivedCount);

            Run(_boot.TotalSeconds);
            Assert.AreEqual(2, _boot.ArrivedCount);
            Assert.IsFalse(_boot.IsRunning, "done after the last item");
        }

        [Test]
        public void AnEmptyHold_StillLightsItsSlots_AndEnds()
        {
            _boot.Start(8, 0);
            Run(_boot.TotalSeconds + 0.1f);
            Assert.AreEqual(8, _boot.LitCount);
            Assert.AreEqual(0, _boot.ArrivedCount);
            Assert.IsFalse(_boot.IsRunning);
        }

        [Test]
        public void StoppingTheBoot_EndsItAtOnce()
        {
            _boot.Start(8, 2);
            _boot.Step(0.2f);
            _boot.Stop();
            Assert.IsFalse(_boot.IsRunning);
        }

        [Test]
        public void TheLedger_RemembersTheBoot_AndTheChip_InTheSave()
        {
            var ledger = new PromptLedger(new PromptSettings());
            Assert.IsFalse(ledger.HoldBooted);
            ledger.RecordHoldBooted();
            ledger.RecordHoldChipShown();

            var restored = new PromptLedger(new PromptSettings());
            restored.Restore(ledger.Capture());
            Assert.IsTrue(restored.HoldBooted);
            Assert.IsTrue(restored.HoldChipShown);
        }

        [Test]
        public void TheTabChip_ShowsOnceAtTheTower_AndLeavesWhenTheHoldOpens()
        {
            var stargaze = new StargazeSettings();
            var ledger = new PromptLedger(new PromptSettings());
            var hint = new HoldHint(stargaze, ledger);
            hint.Tick(Frame, true, false);
            Assert.IsFalse(hint.WantsShown, "only at the tower");

            hint.Tick(Frame, false, true);
            Assert.IsFalse(hint.WantsShown, "not while something else holds the screen");

            hint.Tick(Frame, true, true);
            Assert.IsTrue(hint.WantsShown);
            Assert.IsTrue(ledger.HoldChipShown, "once per save");

            ledger.RecordHoldBooted();
            hint.Tick(Frame, true, true);
            Assert.IsFalse(hint.WantsShown, "pressing Tab answers it");
            hint.Tick(Frame, true, true);
            Assert.IsFalse(hint.WantsShown, "and it never returns");
        }

        [Test]
        public void TheTabChip_NeverShows_ToAPlayerWhoAlreadyOpenedTheHold()
        {
            var ledger = new PromptLedger(new PromptSettings());
            ledger.RecordHoldBooted();
            var hint = new HoldHint(new StargazeSettings(), ledger);
            hint.Tick(Frame, true, true);
            Assert.IsFalse(hint.WantsShown);
            Assert.IsFalse(hint.Wants(true));
        }

        [Test]
        public void TheTabChip_LeavesAfterItsHoldTime()
        {
            var stargaze = new StargazeSettings();
            var hint = new HoldHint(stargaze, new PromptLedger(new PromptSettings()));
            hint.Tick(Frame, true, true);
            hint.Tick(stargaze.HintHoldSeconds + 1f, true, true);
            Assert.IsFalse(hint.WantsShown);
        }

        [Test]
        public void CargoScreenSettings_InUiTuningAsset_MatchesDefaultsAndValidates()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<UiTuning>(UiAssetPaths.Tuning);
            Assert.IsNotNull(tuning, $"{UiAssetPaths.Tuning} is missing");
            CargoScreenSettings asset = tuning.Cargo;
            Assert.IsNull(asset.Validate());
            Assert.AreEqual(_settings.PanelWidth, asset.PanelWidth);
            Assert.AreEqual(_settings.LabelWidth, asset.LabelWidth);
            Assert.AreEqual(_settings.LeftLabelWidth, asset.LeftLabelWidth);
            Assert.AreEqual(_settings.LabelHeight, asset.LabelHeight);
            Assert.AreEqual(_settings.LabelGap, asset.LabelGap);
            Assert.AreEqual(_settings.LabelMargin, asset.LabelMargin);
            Assert.AreEqual(_settings.PortraitReferenceWidth, asset.PortraitReferenceWidth);
            Assert.AreEqual(_settings.DiagramYaw, asset.DiagramYaw);
            Assert.AreEqual(_settings.DiagramElevation, asset.DiagramElevation);
            Assert.AreEqual(_settings.DiagramDistance, asset.DiagramDistance);
            Assert.AreEqual(_settings.DiagramLookHeight, asset.DiagramLookHeight);
            Assert.AreEqual(_settings.DiagramFieldOfView, asset.DiagramFieldOfView);
        }

        [Test]
        public void RoverDiagramAsset_MatchesRoverModelPrefabSocketTransforms()
        {
            var diagram = AssetDatabase.LoadAssetAtPath<RoverDiagramData>(UiAssetPaths.RoverDiagram);
            if (diagram == null || diagram.Validate() != null)
            {
                RoverDiagramBuilder.Build();
                diagram = AssetDatabase.LoadAssetAtPath<RoverDiagramData>(UiAssetPaths.RoverDiagram);
            }

            Assert.IsNotNull(diagram, $"{UiAssetPaths.RoverDiagram} is missing: run UI/Rover Diagram");
            Assert.IsNull(diagram.Validate());

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(UiAssetPaths.RoverModelPrefab);
            Assert.IsNotNull(prefab, $"{UiAssetPaths.RoverModelPrefab} is missing");
            Transform[] nodes = prefab.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < RoverDiagramData.SocketCount; i++)
            {
                var socket = (RoverSocket)i;
                string expectedName = socket == RoverSocket.Panniers ? "BinSocket_L" : socket + "Socket";
                Transform match = FindNode(nodes, expectedName);
                Assert.IsNotNull(match, $"Missing {expectedName} on {UiAssetPaths.RoverModelPrefab}");
                Vector3 expected = prefab.transform.InverseTransformPoint(match.position);
                Vector3 actual = diagram.SocketPosition(socket);
                Assert.Less(Vector3.Distance(expected, actual), 1e-4f, $"Socket {socket} ({expectedName}) drifted");
            }

            for (int i = 0; i < RoverDiagramData.WheelCount; i++)
            {
                string wheelName = RoverDiagramData.WheelNodeName(i);
                Transform match = FindNode(nodes, wheelName);
                Assert.IsNotNull(match, $"Missing {wheelName} on {UiAssetPaths.RoverModelPrefab}");
                Vector3 expected = prefab.transform.InverseTransformPoint(match.position);
                Vector3 actual = diagram.WheelPosition(i);
                Assert.Less(Vector3.Distance(expected, actual), 1e-4f, $"Wheel {wheelName} drifted");
            }
        }

        [Test]
        public void RoverDiagramLayout_InEnglishAndVietnamese_At1080pAnd720p_NeverOverlapsDrawingOrLeavesFrame()
        {
            var diagram = AssetDatabase.LoadAssetAtPath<RoverDiagramData>(UiAssetPaths.RoverDiagram);
            if (diagram == null || diagram.Validate() != null)
            {
                RoverDiagramBuilder.Build();
                diagram = AssetDatabase.LoadAssetAtPath<RoverDiagramData>(UiAssetPaths.RoverDiagram);
            }

            var tuning = AssetDatabase.LoadAssetAtPath<UiTuning>(UiAssetPaths.Tuning);
            Assert.IsNotNull(tuning);
            CargoScreenSettings cargo = tuning.Cargo;
            var enAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(UiAssetPaths.StringTable("en"));
            var viAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(UiAssetPaths.StringTable("vi"));
            var localization = new LocalizationService(new EventBus(),
                new[] { StringTable.Parse(enAsset.text, "en"), StringTable.Parse(viAsset.text, "vi") });
            var painter = new RoverDiagramPainter(new UnityEngine.UIElements.VisualElement(), diagram, cargo);

            // PanelSettings scales 1280x720 from 1920x1080 (match=1), so the portrait element is 798.5x410 in panel space;
            // we also verify a narrower 720p-scaled portrait (532.3x273.3) to prove proportional column scaling.
            Rect[] frames =
            {
                new Rect(0f, 0f, 798.5f, 410f),
                new Rect(0f, 0f, 532.3f, 273.3f),
            };
            string[] languages = { "en", "vi" };
            var points = new Vector2[RoverDiagramData.SocketCount];
            var wants = new bool[RoverDiagramData.SocketCount];
            var heights = new float[RoverDiagramData.SocketCount];
            var centreY = new float[RoverDiagramData.SocketCount];
            var rightCol = new bool[RoverDiagramData.SocketCount];
            var labelRects = new Rect[RoverDiagramData.SocketCount];

            foreach (string lang in languages)
            {
                localization.SetLanguage(lang);
                foreach (Rect area in frames)
                {
                    Rect drawing = painter.DrawingBounds(area);
                    float widthShare = drawing.width / area.width;
                    Assert.That(widthShare, Is.InRange(0.54f, 0.60f),
                        $"{lang} {area.width}x{area.height}: drawing width share {widthShare:P1} outside 55-60%");
                    Assert.GreaterOrEqual(drawing.xMin, 0f, $"{lang}: drawing left leaves frame");
                    Assert.GreaterOrEqual(drawing.yMin, 0f, $"{lang}: drawing top leaves frame");
                    Assert.LessOrEqual(drawing.xMax, area.width, $"{lang}: drawing right leaves frame");
                    Assert.LessOrEqual(drawing.yMax, area.height, $"{lang}: drawing bottom leaves frame");

                    Rect image = RoverDiagramPainter.FrameRect(area);
                    float scale = Mathf.Min(1f, area.width / cargo.PortraitReferenceWidth);
                    float leftWidth = Mathf.Floor(cargo.LeftLabelWidth * scale);
                    float rightWidth = Mathf.Floor(cargo.LabelWidth * scale);
                    float margin = Mathf.Floor(cargo.LabelMargin * scale);
                    float lineH = cargo.LabelHeight * scale;
                    float gap = cargo.LabelGap * scale;
                    float charW = 10.8f * scale;

                    for (int i = 0; i < RoverDiagramData.SocketCount; i++)
                    {
                        var socket = (RoverSocket)i;
                        Vector2 uv = painter.SocketViewport(socket);
                        points[i] = new Vector2(image.x + uv.x * image.width, image.y + (1f - uv.y) * image.height);
                        wants[i] = PartInfo.TryDefaultPart(socket, out RoverKitPiece part);
                        string text = wants[i] ? localization.Get(PartInfo.NameKey(part)) : string.Empty;
                        float colW = points[i].x >= area.width * 0.5f ? rightWidth : leftWidth;
                        heights[i] = wants[i] && text.Length * charW > colW - 2f ? lineH * 2f : lineH;
                    }

                    LabelLayout.Place(points, wants, area.width, area.height, heights, gap, centreY, rightCol);
                    for (int i = 0; i < RoverDiagramData.SocketCount; i++)
                    {
                        if (!wants[i])
                        {
                            continue;
                        }

                        float w = rightCol[i] ? rightWidth : leftWidth;
                        float h = heights[i];
                        float x = rightCol[i] ? area.width - margin - w : margin;
                        float y = centreY[i] - h * 0.5f;
                        var rect = new Rect(x, y, w, h);
                        labelRects[i] = rect;

                        Assert.GreaterOrEqual(rect.xMin, -0.01f, $"{lang} {(RoverSocket)i} leaves left of frame");
                        Assert.GreaterOrEqual(rect.yMin, -0.01f, $"{lang} {(RoverSocket)i} leaves top of frame");
                        Assert.LessOrEqual(rect.xMax, area.width + 0.01f, $"{lang} {(RoverSocket)i} leaves right of frame");
                        Assert.LessOrEqual(rect.yMax, area.height + 0.01f, $"{lang} {(RoverSocket)i} leaves bottom of frame");
                        Assert.IsFalse(rect.Overlaps(drawing),
                            $"{lang} {(RoverSocket)i} label {rect} overlaps drawing bounds {drawing}");
                    }

                    for (int i = 0; i < RoverDiagramData.SocketCount; i++)
                    {
                        if (!wants[i])
                        {
                            continue;
                        }

                        for (int j = i + 1; j < RoverDiagramData.SocketCount; j++)
                        {
                            if (!wants[j])
                            {
                                continue;
                            }

                            Assert.IsFalse(labelRects[i].Overlaps(labelRects[j]),
                                $"{lang}: {(RoverSocket)i} overlaps {(RoverSocket)j}");
                        }
                    }
                }
            }
        }

        private static Transform FindNode(Transform[] nodes, string name)
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i] != null && nodes[i].name == name)
                {
                    return nodes[i];
                }
            }

            return null;
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _boot.Step(Frame);
            }
        }
    }
}
