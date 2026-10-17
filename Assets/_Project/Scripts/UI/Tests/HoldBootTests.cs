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
            Assert.AreEqual(FaceInventoryView.ReferenceWidth, asset.PanelWidth);
            Assert.AreEqual(FaceInventoryView.ReferenceHeight, asset.PanelHeight);
            Assert.AreEqual(_settings.PanelWidth, asset.PanelWidth);
            Assert.AreEqual(_settings.PanelHeight, asset.PanelHeight);
            Assert.AreEqual(_settings.MaxCell, asset.MaxCell);
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
                string expectedName = RoverDiagramData.SocketNodeName(socket);
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
        public void UiIcons_AllThirtySvgsParseAndValidate_AndUnknownIconIdThrowsLoudly()
        {
            UiIconBuilder.Build();
            RoverDiagramBuilder.Build();

            var icons = AssetDatabase.LoadAssetAtPath<UiIconSet>(UiAssetPaths.Icons);
            Assert.IsNotNull(icons, $"{UiAssetPaths.Icons} is missing: run UI/Icons");
            Assert.IsNull(icons.Validate());
            Assert.AreEqual(UiIconSet.ExpectedIconCount, icons.Count);

            var diagram = AssetDatabase.LoadAssetAtPath<RoverDiagramData>(UiAssetPaths.RoverDiagram);
            Assert.IsNotNull(diagram, $"{UiAssetPaths.RoverDiagram} is missing: run UI/Rover Diagram");
            Assert.IsNull(diagram.Validate());

            foreach (SalvageMaterial material in System.Enum.GetValues(typeof(SalvageMaterial)))
            {
                UiIconShape shape = diagram.GetIcon(material);
                Assert.IsNull(shape.Validate(), $"Invalid icon for material {material}: {shape.Validate()}");
            }

            for (int i = 0; i < RoverDiagramData.SocketCount; i++)
            {
                var socket = (RoverSocket)i;
                UiIconShape socketShape = diagram.GetIcon(socket);
                Assert.IsNull(socketShape.Validate(), $"Invalid icon for socket {socket}: {socketShape.Validate()}");
            }

            UiIconShape partShape = diagram.GetPartIcon("part.igniter");
            Assert.IsNull(partShape.Validate(), $"Invalid icon for the spare igniter: {partShape.Validate()}");

            Assert.Throws<System.InvalidOperationException>(() => icons.GetIcon("item_does_not_exist"));
            Assert.Throws<System.InvalidOperationException>(() => diagram.GetIcon("kit_does_not_exist"));
            Assert.Throws<System.InvalidOperationException>(() => diagram.GetPartIcon("Part_Unknown"));
        }

        [Test]
        public void FaceInventoryLayout_CellsAndDevicesNeverOverlap_AndTextBudgetHoldsInEnAndVi()
        {
            for (int i = 0; i < FaceInventoryView.MaxHoldCells; i++)
            {
                Rect r = FaceInventoryView.GetHoldCellReferenceRect(i);
                Assert.AreEqual(FaceInventoryView.CellSize, r.width, 1e-3f);
                Assert.AreEqual(FaceInventoryView.CellSize, r.height, 1e-3f);
                Assert.GreaterOrEqual(r.xMin, FaceInventoryView.HoldGridX - 1e-3f);
                Assert.LessOrEqual(r.xMax, FaceInventoryView.DividerX - 24f);
                Assert.GreaterOrEqual(r.yMin, FaceInventoryView.HoldGridY - 1e-3f);
                Assert.LessOrEqual(r.yMax, 690f);

                for (int j = i + 1; j < FaceInventoryView.MaxHoldCells; j++)
                {
                    Assert.IsFalse(r.Overlaps(FaceInventoryView.GetHoldCellReferenceRect(j)),
                        $"Hold cell {i} overlaps hold cell {j}");
                }
            }

            float minDistance = FaceInventoryView.BubbleRadius * 2f + 8f;
            for (int i = 0; i < FaceDevices.Count; i++)
            {
                Vector2 c = FaceDevices.CenterOf(i);
                Assert.GreaterOrEqual(c.x - FaceInventoryView.BubbleRadius, FaceInventoryView.DividerX + 16f);
                Assert.LessOrEqual(c.x + FaceInventoryView.BubbleRadius, FaceInventoryView.ReferenceWidth - 16f);
                Assert.GreaterOrEqual(c.y - FaceInventoryView.BubbleRadius, 60f);
                Assert.LessOrEqual(c.y + FaceInventoryView.BubbleRadius, 670f);

                Vector2 mount = RoverLineArtPainter.GetSocketMountPoint(FaceDevices.MountOf(i),
                    new Vector2(FaceInventoryView.RoverOriginX, FaceInventoryView.RoverOriginY),
                    FaceInventoryView.RoverScale);
                Assert.Greater(Vector2.Distance(c, mount), FaceInventoryView.BubbleRadius + 6f,
                    $"Device bubble {i} is too close to its mount point on 07");

                for (int j = i + 1; j < FaceDevices.Count; j++)
                {
                    Assert.GreaterOrEqual(Vector2.Distance(c, FaceDevices.CenterOf(j)), minDistance,
                        $"Device bubble {i} overlaps device bubble {j}");
                }
            }

            var uiTuning = AssetDatabase.LoadAssetAtPath<UiTuning>(UiAssetPaths.Tuning);
            Assert.IsNotNull(uiTuning);
            Assert.IsNull(uiTuning.Validate());
            CargoScreenSettings cargo = uiTuning.Cargo;
            TitleSettings title = uiTuning.Title;
            Assert.AreEqual(title.WheelRadius, cargo.RoverWheelRadius, 1e-4f, "Rover wheel radius matches title screen");
            Assert.AreEqual(title.WheelRimRadius, cargo.RoverWheelRimRadius, 1e-4f, "Rover wheel rim radius matches title screen");
            Assert.AreEqual(title.WheelbaseHalf, cargo.RoverWheelbaseHalf, 1e-4f, "Rover half-wheelbase matches title screen");
            Assert.AreEqual(title.FarWheelScale, cargo.RoverFarWheelScale, 1e-4f, "Far wheel scale matches title screen");
            Assert.AreEqual(title.FarWheelAlpha, cargo.RoverFarWheelAlpha, 1e-4f, "Far wheel alpha matches title screen");
            Assert.AreEqual(new Vector2(6f, -4f), cargo.RoverFarWheelOffset, "Far wheel offset is (+6, -4) at 82px/m");
            Assert.LessOrEqual(cargo.LeaderEmptyColor.a, 78, "Empty socket leader line is faint (~25-30% alpha)");
            Assert.Greater(cargo.LeaderInstalledColor.a, cargo.LeaderEmptyColor.a,
                "Installed socket leader is slightly brighter than empty leader");
            Assert.LessOrEqual(cargo.LeaderInstalledColor.a, 115, "Installed socket leader remains subtle");
            Assert.GreaterOrEqual(cargo.LeaderFocusColor.a, 220, "Focused/drag-target leader is bright cyan");

            var enAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(UiAssetPaths.StringTable("en"));
            var viAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(UiAssetPaths.StringTable("vi"));
            var localization = new LocalizationService(new EventBus(),
                new[] { StringTable.Parse(enAsset.text, "en"), StringTable.Parse(viAsset.text, "vi") });

            localization.SetLanguage("en");
            Assert.AreEqual("INVENTORY", localization.Get(UiKeys.CargoHold));
            Assert.AreEqual("EQUIPPED", localization.Get(UiKeys.CargoTabSeven));

            localization.SetLanguage("vi");
            Assert.AreEqual("KHOANG HÀNG", localization.Get(UiKeys.CargoHold));
            Assert.AreEqual("TRANG BỊ", localization.Get(UiKeys.CargoTabSeven));

            foreach (string lang in new[] { "en", "vi" })
            {
                localization.SetLanguage(lang);
                foreach (SalvageMaterial material in System.Enum.GetValues(typeof(SalvageMaterial)))
                {
                    string desc = PartInfo.MaterialDescription(material, localization);
                    Assert.IsNotEmpty(desc);
                    Assert.LessOrEqual(desc.Length, FaceInventoryView.MaxTooltipDescriptionChars,
                        $"{lang} {material} description exceeds 52 chars: '{desc}'");
                }

                for (int device = 0; device < FaceDevices.Count; device++)
                {
                    foreach (RoverSystemState state in System.Enum.GetValues(typeof(RoverSystemState)))
                    {
                        foreach (bool canFit in new[] { false, true })
                        {
                            string dDesc = localization.Get(FaceDevices.DescriptionKey(device, state, canFit));
                            Assert.IsNotEmpty(dDesc);
                            Assert.LessOrEqual(dDesc.Length, FaceInventoryView.MaxTooltipDescriptionChars,
                                $"{lang} device {device} ({state}, fit {canFit}) exceeds 52 chars: '{dDesc}'");
                        }
                    }

                    Assert.IsNotEmpty(localization.Get(FaceDevices.NameKey(device)));
                }

                Assert.IsNotEmpty(PartInfo.StoryPartName("part.igniter", localization));
                Assert.LessOrEqual(PartInfo.StoryPartDescription("part.igniter", localization).Length,
                    FaceInventoryView.MaxTooltipDescriptionChars);
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
