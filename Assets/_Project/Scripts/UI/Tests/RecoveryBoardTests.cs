using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The tech board's labels (docs/features/M3-21, M3-19 §3.2): the recovery panel's rows with their lamps and the
    /// pins the board knows show only while 07 looks at the board; an unknown kit with scanned fragments shows a torn
    /// sheet growing piece by piece until it joins into the full decoded sheet; the components', targets' and
    /// completion lines exist in every language.
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
            var board = new RecoveryBoard(layout.LabHud, settings.Board, localization, game, game, game);
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
        public void TornSheets_GrowPieceByPieceForUnknownKits_AndJoinIntoFullSheetWhenCompleted()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiAssetPaths.Uxml);
            var layout = new UiLayout(uxml.Instantiate());
            ILocalization localization = new LocalizationService(new EventBus(), RealTables());
            var settings = new LabHudSettings();
            var game = new Game();
            game.SetSizes["rover.cargo_cradle"] = 2;
            game.SetSizes["rover.warm_headlamp"] = 3;
            var board = new RecoveryBoard(layout.LabHud, settings.Board, localization, game, game, game);
            VisualElement root = layout.LabHud.Q("lab-board");
            List<Label> nodes = root.Query<Label>(className: "recovery-board__node").ToList();
            Label cradle = nodes[(int)TechNodeId.CargoCradle];
            Label headlamp = nodes[(int)TechNodeId.WarmHeadlamp];

            board.Look(true);
            Ticks(board, settings.Board.FadeIn + 0.5f, true);
            Assert.AreEqual(DisplayStyle.None, cradle.style.display.value, "s == 0: bare pin, hidden on the board");
            Assert.IsFalse(board.IsNodeTorn(TechNodeId.CargoCradle));
            Assert.AreEqual(0, board.NodePieceCount(TechNodeId.CargoCradle));

            game.Scanned["rover.cargo_cradle"] = 1;
            int readsBefore = game.ScanReads;
            Ticks(board, 0.5f, true);
            Assert.AreEqual(readsBefore, game.ScanReads, "never polled per frame without an event");

            board.OnFragmentScanned(new FragmentScanned("frag.cradle.1", "rover.cargo_cradle", 0, 2, false, 0));
            Ticks(board, Step, true);
            int readsAfterRefresh = game.ScanReads;
            Assert.Greater(readsAfterRefresh, readsBefore, "refreshes on FragmentScanned");
            Ticks(board, 0.5f, true);
            Assert.AreEqual(readsAfterRefresh, game.ScanReads, "refreshes once, not per frame");
            Assert.AreEqual(DisplayStyle.Flex, cradle.style.display.value, "0 < s < z: torn sheet with 1 of 2 pieces");
            Assert.IsTrue(cradle.ClassListContains(RecoveryBoard.TornClass));
            Assert.IsFalse(cradle.ClassListContains("recovery-board__node--decoded"));
            Assert.IsTrue(board.IsNodeTorn(TechNodeId.CargoCradle));
            Assert.AreEqual(1, board.NodePieceCount(TechNodeId.CargoCradle));
            Assert.AreEqual(2, board.NodeSetSize(TechNodeId.CargoCradle));
            Assert.AreEqual(string.Empty, cradle.text, "a pencil-and-paper mark, not a number badge or label");
            Assert.AreEqual(DisplayStyle.None, headlamp.style.display.value, "other unknown kits with s == 0 stay hidden");

            game.Scanned["rover.warm_headlamp"] = 2;
            board.OnFragmentScanned(new FragmentScanned("frag.lamp.2", "rover.warm_headlamp", 1, 3, false, 0));
            Ticks(board, Step, true);
            Assert.IsTrue(board.IsNodeTorn(TechNodeId.WarmHeadlamp));
            Assert.AreEqual(2, board.NodePieceCount(TechNodeId.WarmHeadlamp), "2 of 3 pieces");
            Assert.AreEqual(3, board.NodeSetSize(TechNodeId.WarmHeadlamp));

            game.Scanned["rover.cargo_cradle"] = 2;
            game.States[(int)TechNodeId.CargoCradle] = TechNodeState.Decoded;
            board.OnFragmentScanned(new FragmentScanned("frag.cradle.2", "rover.cargo_cradle", 1, 2, false, 0));
            board.MarkDirty();
            Ticks(board, Step, true);
            Assert.IsFalse(board.IsNodeTorn(TechNodeId.CargoCradle), "completed set joins into the full sheet");
            Assert.AreEqual(0, board.NodePieceCount(TechNodeId.CargoCradle));
            Assert.IsFalse(cradle.ClassListContains(RecoveryBoard.TornClass));
            Assert.IsTrue(cradle.ClassListContains("recovery-board__node--decoded"));
            Assert.AreEqual("Cargo Cradle", cradle.text);

            game.States[(int)TechNodeId.CargoCradle] = TechNodeState.Built;
            board.MarkDirty();
            Ticks(board, Step, true);
            Assert.IsFalse(board.IsNodeTorn(TechNodeId.CargoCradle));
            Assert.IsFalse(cradle.ClassListContains(RecoveryBoard.TornClass));
            Assert.IsFalse(cradle.ClassListContains("recovery-board__node--decoded"));
            Assert.AreEqual("Cargo Cradle", cradle.text);
        }

        [Test]
        public void TheProtocolReadout_ShowsOncePerFullOpening_PowerFirstWithItsNeed_ThenFades()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiAssetPaths.Uxml);
            var layout = new UiLayout(uxml.Instantiate());
            ILocalization localization = new LocalizationService(new EventBus(), RealTables());
            var settings = new GuidanceSettings();
            var readout = new ProtocolReadout(layout.Hud, settings, localization, new Game());
            VisualElement root = layout.Hud.Q("protocol");
            readout.Cue();
            ReadoutTicks(readout, 1f);
            Assert.IsFalse(readout.IsVisible, "never without a full opening (the short wake)");

            readout.OpeningBegan();
            readout.Cue();
            ReadoutTicks(readout, settings.Protocol.FadeIn + 0.5f);
            Assert.IsTrue(readout.IsVisible);
            Assert.AreEqual(1f, root.style.opacity.value, 1e-3f);
            Assert.AreEqual("LUMEN RECOVERY PROTOCOL", root.Q<Label>("protocol-title").text);
            VisualElement rows = root.Q("protocol-rows");
            Assert.AreEqual("Power the station", rows[0].Q<Label>(className: "recovery-board__target").text,
                "Power first");
            Assert.AreEqual("› the radio tower", ((Label)rows[1]).text, "its first need right under it");
            Assert.IsTrue(rows[0].ClassListContains("protocol-readout__row--first"));
            Assert.AreEqual(4, root.Query(className: "recovery-board__row").ToList().Count, "the four targets");

            float onScreen = settings.Protocol.FadeIn + settings.ProtocolHoldSeconds + settings.Protocol.FadeOut;
            Assert.That(onScreen, Is.InRange(3f, 5.5f), "about 4 s on screen");
            ReadoutTicks(readout, settings.ProtocolHoldSeconds + settings.Protocol.FadeOut + 0.5f);
            Assert.IsFalse(readout.IsVisible, "it fades by itself");
            readout.Cue();
            ReadoutTicks(readout, 1f);
            Assert.IsFalse(readout.IsVisible, "once: the chase frame does not show it again");
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
                if (target != RecoveryTarget.Power)
                {
                    keys.Add(UiKeys.RecoveryComplete(target));
                }
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

        private static void ReadoutTicks(ProtocolReadout readout, float seconds)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                readout.Tick(Step);
            }
        }

        private static void Ticks(RecoveryBoard board, float seconds, bool shown)
        {
            for (float t = 0f; t < seconds; t += Step)
            {
                board.Tick(Step, shown);
            }
        }

        private sealed class Game : ITechTree, IRecoveryProgress, IScanSites
        {
            public readonly TechNodeState[] States = new TechNodeState[Enum.GetValues(typeof(TechNodeId)).Length];
            public readonly int[] Lit = new int[4];
            public readonly Dictionary<string, int> Scanned = new Dictionary<string, int>(StringComparer.Ordinal);
            public readonly Dictionary<string, int> SetSizes = new Dictionary<string, int>(StringComparer.Ordinal);
            private readonly int[] _lamps = { 3, 3, 4, 1 };

            public int KnownCount => Array.FindAll(States, state => state != TechNodeState.Unknown).Length;

            public int Count => 0;

            public TechNodeState StateOf(TechNodeId node)
            {
                return States[(int)node];
            }

            public string KitOf(TechNodeId node)
            {
                switch (node)
                {
                    case TechNodeId.HoverJump: return "rover.hover_jump";
                    case TechNodeId.CargoCradle: return "rover.cargo_cradle";
                    case TechNodeId.WarmHeadlamp: return "rover.warm_headlamp";
                    case TechNodeId.CapacitorDrums: return "rover.capacitor_drums";
                    case TechNodeId.CellPackII: return "rover.cell_pack_2";
                    case TechNodeId.FocusLens: return "rover.focus_lens";
                    case TechNodeId.SideBins: return "rover.side_bins";
                    default: return null;
                }
            }

            public string PlaceId(int index) => string.Empty;

            public Vector3 Position(int index) => Vector3.zero;

            public bool IsKnown(int index) => false;

            public int Left(int index) => 0;

            public int FragmentCount(int index) => 0;

            public int IndexOf(string placeId) => -1;

            public int ScanReads { get; private set; }

            public int ScannedOf(string blueprintId)
            {
                ScanReads++;
                return blueprintId != null && Scanned.TryGetValue(blueprintId, out int s) ? s : 0;
            }

            public int SetSize(string blueprintId)
            {
                return blueprintId != null && SetSizes.TryGetValue(blueprintId, out int z) ? z : 0;
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
