using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The tech board's labels (docs/features/M3-21): the recovery panel's rows with their lamps and the pins the board
    /// knows show only while 07 looks at the board; the components', targets' and completion lines exist in every
    /// language.
    /// </summary>
    public sealed class RecoveryBoardTests
    {
        private const float Step = 0.1f;

        [Test]
        public void LookingAtTheBoard_ShowsEachRowsLamps_AndOnlyTheKnownPins()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiAssetPaths.Uxml);
            var layout = new UiLayout(uxml.Instantiate());
            ILocalization localization = new LocalizationService(new EventBus(), RealTables());
            var settings = new LabHudSettings();
            var game = new Game();
            var board = new RecoveryBoard(layout.LabHud, settings.Board, localization, game, game);
            VisualElement root = layout.LabHud.Q("lab-board");
            List<VisualElement> lamps = root.Query(className: "recovery-board__lamp").ToList();
            Assert.AreEqual(3 + 3 + 4 + 1, lamps.Count, "Power 3, Link 3, Dish 4, Answer 1");

            game.Lit[(int)RecoveryTarget.Power] = 2;
            game.States[(int)TechNodeId.TowerL1] = TechNodeState.Built;
            game.States[(int)TechNodeId.HoverJump] = TechNodeState.Decoded;
            Ticks(board, 1f, true);
            Assert.AreEqual(DisplayStyle.None, root.style.display.value, "hidden until looked at");

            board.Look(true);
            Ticks(board, settings.Board.FadeIn + 0.5f, true);
            Assert.AreEqual(DisplayStyle.Flex, root.style.display.value, "looking at the board shows its labels");
            Assert.AreEqual(1f, root.style.opacity.value, 1e-3f);
            Assert.IsTrue(lamps[0].ClassListContains("recovery-board__lamp--lit"));
            Assert.IsTrue(lamps[1].ClassListContains("recovery-board__lamp--lit"));
            Assert.IsFalse(lamps[2].ClassListContains("recovery-board__lamp--lit"), "Power 2/3");
            List<Label> nodes = root.Query<Label>(className: "recovery-board__node").ToList();
            Assert.AreEqual(DisplayStyle.Flex, nodes[(int)TechNodeId.TowerL1].style.display.value);
            Assert.AreEqual(DisplayStyle.None, nodes[(int)TechNodeId.TowerL2].style.display.value, "unknown: no name");
            Assert.AreEqual("Hover-Jump", nodes[(int)TechNodeId.HoverJump].text, "a decoded sheet is named");
            Assert.IsTrue(nodes[(int)TechNodeId.HoverJump].ClassListContains("recovery-board__node--decoded"));

            board.Look(false);
            Ticks(board, settings.Board.FadeOut + 0.5f, true);
            Assert.AreEqual(DisplayStyle.None, root.style.display.value, "looking away hides them");
        }

        [Test]
        public void ComponentsTargetsAndNodes_HaveTheirLines_InEveryLanguage()
        {
            Assert.AreEqual("component.tower_coil.needed", UiKeys.ComponentNeeded("Part_TowerCoil"));
            Assert.AreEqual("component.relay_spare.needed", UiKeys.ComponentNeeded("Part_RelaySpare"));
            var keys = new List<string>
            {
                UiKeys.ComponentNeeded("Part_TowerCoil"), UiKeys.ComponentNeeded("Part_Transformer"),
                UiKeys.ComponentNeeded("Part_RelaySpare"), "ui.recovery.title", "ui.board.known",
            };
            foreach (RecoveryTarget target in Enum.GetValues(typeof(RecoveryTarget)))
            {
                keys.Add(UiKeys.RecoveryComplete(target));
            }

            foreach (string id in new[] { "power", "link", "dish", "answer" })
            {
                keys.Add("recovery." + id + ".name");
            }

            foreach (string id in new[]
                     {
                         "tower_l1", "tower_l2", "tower_l3", "relay_network", "hover_jump", "cargo_cradle",
                         "warm_headlamp", "capacitor_drums", "cell_pack_2",
                     })
            {
                keys.Add("tech." + id + ".name");
            }

            foreach (TextAsset asset in StringTableAssets.Load())
            {
                var table = new HashSet<string>(StringTable.Parse(asset.text, asset.name).Keys);
                foreach (string key in keys)
                {
                    Assert.IsTrue(table.Contains(key), $"{asset.name} has no '{key}'");
                }
            }
        }

        private static StringTable[] RealTables()
        {
            TextAsset[] assets = StringTableAssets.Load();
            var tables = new StringTable[assets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                tables[i] = StringTable.Parse(assets[i].text, assets[i].name);
            }

            return tables;
        }

        private static void Ticks(RecoveryBoard board, float seconds, bool shown)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                board.Tick(Step, shown);
            }
        }

        private sealed class Game : ITechTree, IRecoveryProgress
        {
            public readonly TechNodeState[] States = new TechNodeState[Enum.GetValues(typeof(TechNodeId)).Length];
            public readonly int[] Lit = new int[4];
            private readonly int[] _lamps = { 3, 3, 4, 1 };

            public int KnownCount => Array.FindAll(States, state => state != TechNodeState.Unknown).Length;

            public TechNodeState StateOf(TechNodeId node)
            {
                return States[(int)node];
            }

            public int LitLamps(RecoveryTarget target)
            {
                return Lit[(int)target];
            }

            public int Lamps(RecoveryTarget target)
            {
                return _lamps[(int)target];
            }

            public bool IsComplete(RecoveryTarget target)
            {
                return Lit[(int)target] >= _lamps[(int)target];
            }
        }
    }
}
