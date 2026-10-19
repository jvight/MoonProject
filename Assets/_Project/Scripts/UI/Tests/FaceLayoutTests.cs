using System;
using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Input;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The layout of 07's face screen: the hold grid on the left (four columns, names under the cells, paged three
    /// rows at a time, one hint row where it grows with the page count beside it, a status line and the info box under
    /// it), 07's drawing between two upper and two lower device bays on the right, hit areas without dead strips,
    /// nothing overlapping, and words that fit in English and Vietnamese at look A's three text sizes.
    /// </summary>
    public sealed class FaceLayoutTests
    {
        // IBM Plex Mono advance as a share of its size: the width of one character.
        private const float MonoAdvance = 0.6f;

        // Chakra Petch SemiBold's widest average advance over the names and headings it sets (measured 0.48 to 0.55).
        private const float TitleAdvance = 0.55f;
        private const float TitleText = 28f;
        private const float BodyText = 22f;
        private const float MetaText = 18f;

        // A long word may carry a soft hyphen where it breaks under a cell ("Super\u00adconductor").
        private const char SoftHyphen = '\u00ad';
        private const int NameLines = 2;
        private const float NameLineHeight = 1.25f;
        private const int InfoLines = 2;
        private const int StateLines = 2;
        private const float GlassBottomMargin = 36f;
        private const float TitleLetterSpace = 1f;
        private const int TwoDigits = 99;
        private static readonly int[] Tiers = { 8, 16, 24 };

        private StringTable[] _tables;

        [SetUp]
        public void SetUp()
        {
            TextAsset[] assets = StringTableAssets.Load();
            _tables = new StringTable[assets.Length];
            for (int i = 0; i < assets.Length; i++)
            {
                _tables[i] = StringTable.Parse(assets[i].text, assets[i].name);
            }
        }

        [TestCase(8, 2, 1)]
        [TestCase(12, 3, 1)]
        [TestCase(16, 4, 2)]
        [TestCase(24, 6, 2)]
        [TestCase(0, 0, 1)]
        public void TheGrid_HasARowPerFourSlots_AndAPagePerThreeRows(int unlocked, int rows, int pages)
        {
            Assert.AreEqual(rows, FaceLayout.RowsOf(unlocked));
            Assert.AreEqual(pages, FaceLayout.PageCount(unlocked));
        }

        [TestCase(8, 0, 2)]
        [TestCase(16, 0, 3)]
        [TestCase(16, 1, 1)]
        [TestCase(24, 1, 3)]
        public void EachPage_ShowsOnlyItsUnlockedRows(int unlocked, int page, int rows)
        {
            Assert.AreEqual(rows, FaceLayout.RowsOnPage(page, unlocked));
        }

        [Test]
        public void TheRowsGrowDown_AndAPageStartsAgainAtTheTop()
        {
            Assert.AreEqual(FaceLayout.GridTop, FaceLayout.HoldCellRect(0).y, 1e-3f);
            Assert.AreEqual(FaceLayout.HoldCellRect(0).y + FaceLayout.RowPitch,
                FaceLayout.HoldCellRect(FaceLayout.Columns).y, 1e-3f);
            Assert.AreEqual(FaceLayout.HoldCellRect(0), FaceLayout.HoldCellRect(FaceLayout.PageSize),
                "the second page's first slot sits where the first page's did");
            Assert.AreEqual(1, FaceLayout.PageOf(FaceLayout.PageSize));
            Assert.AreEqual(0, FaceLayout.PageOf(FaceLayout.PageSize - 1));
        }

        [Test]
        public void TheCellsOfAPage_NeverOverlap_AndTheirNamesStayUnderThem()
        {
            for (int i = 0; i < FaceInventoryView.MaxHoldCells; i++)
            {
                Rect cell = FaceLayout.HoldCellRect(i);
                Rect name = FaceLayout.HoldNameRect(i);
                Assert.GreaterOrEqual(name.yMin, cell.yMax, $"cell {i}: the name is under the cell");
                Assert.AreEqual(cell.center.x, name.center.x, 1e-3f, $"cell {i}: the name is centred");
                Rect hitArea = FaceLayout.HoldHitRect(i);
                Assert.IsTrue(hitArea.xMin <= name.xMin && hitArea.xMax >= name.xMax && hitArea.yMin <= name.yMin &&
                              hitArea.yMax >= name.yMax, $"cell {i}: the hit area takes in the name");
                Assert.GreaterOrEqual(name.xMin, FaceLayout.Margin, $"cell {i}");
                Assert.LessOrEqual(name.xMax, FaceLayout.HoldRight, $"cell {i}: in the left half");
                for (int j = i + 1; j < FaceInventoryView.MaxHoldCells; j++)
                {
                    if (FaceLayout.PageOf(i) != FaceLayout.PageOf(j))
                    {
                        continue;
                    }

                    Assert.IsFalse(cell.Overlaps(FaceLayout.HoldCellRect(j)), $"cells {i} and {j}");
                    Assert.IsFalse(cell.Overlaps(FaceLayout.HoldNameRect(j)), $"cell {i} and the name of {j}");
                    Assert.IsFalse(name.Overlaps(FaceLayout.HoldNameRect(j)), $"the names of {i} and {j}");
                }
            }
        }

        [Test]
        public void TheHitAreas_OfNeighbouringCells_MeetWithoutAGap_AndKeepTheirOwnCell()
        {
            for (int i = 0; i < FaceInventoryView.MaxHoldCells; i++)
            {
                Rect hit = FaceLayout.HoldHitRect(i);
                Rect cell = FaceLayout.HoldCellRect(i);
                Assert.IsTrue(hit.Contains(cell.min) && hit.Contains(cell.max), $"cell {i}");
                if (i % FaceLayout.Columns + 1 < FaceLayout.Columns)
                {
                    Assert.AreEqual(hit.xMax, FaceLayout.HoldHitRect(i + 1).xMin, 1e-3f, $"cells {i} and {i + 1}");
                }

                int below = i + FaceLayout.Columns;
                if (below < FaceInventoryView.MaxHoldCells && FaceLayout.PageOf(below) == FaceLayout.PageOf(i))
                {
                    Assert.AreEqual(hit.yMax, FaceLayout.HoldHitRect(below).yMin, 1e-3f, $"cell {i} and the one below");
                }
            }
        }

        [Test]
        public void TheGrid_TheHint_TheStatusLine_AndTheInfoBox_StackDownTheLeftHalf_AtEverySize()
        {
            Rect toast = new Rect(FaceLayout.Margin, FaceLayout.ToastY, FaceLayout.HoldWidth, FaceLayout.ToastHeight);
            Rect info = new Rect(FaceLayout.InfoX, FaceLayout.InfoY, FaceLayout.InfoWidth, FaceLayout.InfoHeight);
            float lowestName = FaceLayout.HoldNameRect(FaceLayout.PageSize - 1).yMax;
            Assert.Greater(FaceLayout.GridTop, FaceLayout.TitleY + FaceLayout.TitleHeight, "the grid starts under the title");
            Assert.LessOrEqual(lowestName, toast.yMin, "a full page clears the status line");
            Assert.LessOrEqual(toast.yMax, info.yMin);
            Assert.LessOrEqual(info.yMax, FaceLayout.FooterY, "the info box clears the foot row");
            Assert.LessOrEqual(FaceLayout.FooterY + FaceLayout.FooterHeight,
                FaceInventoryView.ReferenceHeight - GlassBottomMargin, "inside the glass");
            Assert.LessOrEqual(info.xMax, FaceLayout.HoldRight);
            foreach (int unlocked in new[] { 8, 12, 16 })
            {
                Rect hint = FaceLayout.HintRect(unlocked);
                int last = Mathf.Min(unlocked, FaceInventoryView.MaxHoldCells) - 1;
                Assert.GreaterOrEqual(hint.yMin, FaceLayout.HoldNameRect(last).yMax,
                    $"{unlocked} slots: the hint row sits under the last row");
                Assert.LessOrEqual(hint.yMax, toast.yMin, $"{unlocked} slots: and above the status line");
                Assert.LessOrEqual(hint.yMin - FaceLayout.HoldNameRect(last).yMax, FaceLayout.RowGap + FaceLayout.HintGap + 1e-3f,
                    $"{unlocked} slots: right where the next row will grow");
            }

            for (int device = 0; device < FaceDevices.Count; device++)
            {
                Rect hit = FaceLayout.DeviceHitRect(device);
                Assert.Greater(hit.xMin, FaceLayout.HoldRight, $"device {device} stands in the right half");
                Assert.IsFalse(hit.Overlaps(info) || hit.Overlaps(toast), $"device {device}");
            }
        }

        [Test]
        public void ThePageCount_SharesTheStatusRow_RightOfTheHint()
        {
            Rect page = FaceLayout.PageRect;
            Assert.AreEqual(FaceLayout.HoldRight, page.xMax, 1e-3f);
            Assert.GreaterOrEqual(page.yMin, FaceLayout.HoldNameRect(FaceLayout.PageSize - 1).yMax, "under a full page");
            Assert.LessOrEqual(page.yMax, FaceLayout.ToastY, "above the status line");
            foreach (int unlocked in Tiers)
            {
                Assert.LessOrEqual(FaceLayout.HintRect(unlocked).xMax, page.xMin, $"{unlocked} slots: the hint row ends first");
            }
        }

        [Test]
        public void TheFrames_StandApart_TwoAboveAndTwoBelowTheDrawing()
        {
            Rect art = FaceLayout.ArtBox;
            for (int a = 0; a < FaceDevices.Count; a++)
            {
                Rect frame = FaceLayout.DeviceFrame(a);
                Assert.Greater(FaceLayout.DeviceHitRect(a).yMin, FaceLayout.TitleY + FaceLayout.TitleHeight, $"frame {a}");
                Assert.LessOrEqual(FaceLayout.DeviceHitRect(a).yMax, FaceLayout.FooterY, $"frame {a}");
                Assert.LessOrEqual(frame.xMax, FaceLayout.RightEdge, $"frame {a}");
                Assert.IsFalse(FaceLayout.DeviceHitRect(a).Overlaps(art), $"frame {a} clears the drawing's box");
                Assert.AreEqual(FaceLayout.IsTopFrame(a), frame.yMax < art.yMin, $"frame {a}");
                for (int b = a + 1; b < FaceDevices.Count; b++)
                {
                    Assert.IsFalse(FaceLayout.DeviceHitRect(a).Overlaps(FaceLayout.DeviceFrame(b)), $"frames {a} and {b}");
                }
            }

            Assert.IsTrue(FaceLayout.IsTopFrame(FaceDevices.Cutter) && FaceLayout.IsTopFrame(FaceDevices.Scan));
            Assert.IsFalse(FaceLayout.IsTopFrame(FaceDevices.SocketOne) || FaceLayout.IsTopFrame(FaceDevices.SocketTwo));
        }

        [Test]
        public void TheDrawing_FillsItsBox_AsFarAsItsShapeAllows_AndTheAnchorsLandOnIt()
        {
            var content = new Rect(0.04f, 0.17f, 0.907f, 0.673f);
            Rect box = FaceLayout.ArtBox;
            Rect image = FaceLayout.ArtImageRect(content);
            Vector2 min = FaceLayout.ArtPoint(content.min, content);
            Vector2 max = FaceLayout.ArtPoint(content.max, content);
            Assert.AreEqual(image.width, image.height, 1e-3f, "a square image keeps its shape");
            Assert.GreaterOrEqual(min.x, box.xMin - 1e-3f);
            Assert.GreaterOrEqual(min.y, box.yMin - 1e-3f);
            Assert.LessOrEqual(max.x, box.xMax + 1e-3f);
            Assert.LessOrEqual(max.y, box.yMax + 1e-3f);
            Assert.IsTrue(Mathf.Abs(max.x - min.x - box.width) < 1e-2f || Mathf.Abs(max.y - min.y - box.height) < 1e-2f,
                "the lines touch two sides of the box");
            Assert.AreEqual(box.center.x, (min.x + max.x) * 0.5f, 1e-2f, "centred");
            Assert.AreEqual(box.center.y, (min.y + max.y) * 0.5f, 1e-2f, "centred");
        }

        [Test]
        public void TheLeaders_LeaveTheEdgeFacingSeven_OverThePart_AndBendAtItsHeight()
        {
            Rect box = FaceLayout.ArtBox;
            Vector2[] targets = { box.min, box.center, box.max, new Vector2(box.xMin, box.yMax) };
            for (int device = 0; device < FaceDevices.Count; device++)
            {
                Rect frame = FaceLayout.DeviceFrame(device);
                foreach (Vector2 target in targets)
                {
                    Vector2 start = FaceLayout.LeaderStart(device, target);
                    Vector2 knee = FaceLayout.LeaderKneeOf(device, target);
                    Assert.AreEqual(FaceLayout.IsTopFrame(device) ? frame.yMax : frame.yMin, start.y, 1e-3f);
                    Assert.GreaterOrEqual(start.x, frame.xMin + FaceLayout.LeaderEdgeInset - 1e-3f);
                    Assert.LessOrEqual(start.x, frame.xMax - FaceLayout.LeaderEdgeInset + 1e-3f);
                    Assert.AreEqual(start.x, knee.x, 1e-3f, "straight out of the frame");
                    Assert.AreEqual(target.y, knee.y, 1e-3f, "to the part's height, then across to it");
                    Assert.AreEqual(FaceLayout.IsTopFrame(device), knee.y > start.y, "towards 07");
                    Assert.IsTrue(knee.x >= box.xMin && knee.x <= box.xMax, $"device {device}: the bend is over the box");
                }
            }
        }

        [Test]
        public void TheWordsInEveryBay_FitTheirBox_InEveryLanguage()
        {
            float room = FaceLayout.FrameWidth - 2f * FaceLayout.FramePadX;
            Assert.GreaterOrEqual(FaceLayout.FrameStateHeight, StateLines * MetaText * NameLineHeight,
                "two lines of state words fit");
            foreach (StringTable table in _tables)
            {
                for (int device = 0; device < FaceDevices.Count; device++)
                {
                    AssertFits(table, FaceDevices.NameKey(device), room, BodyText * MonoAdvance, $"device {device} name");
                    RoverSystem system = FaceDevices.SystemOf(device);
                    RoverSystemState[] states = system == RoverSystem.Sockets
                        ? new[] { RoverSystemState.Empty }
                        : new[] { RoverSystemState.Ok, RoverSystemState.Fault };
                    foreach (RoverSystemState state in states)
                    {
                        if (device == FaceDevices.Scan && state == RoverSystemState.Fault)
                        {
                            continue;
                        }

                        foreach (bool canFit in new[] { false, true })
                        {
                            AssertWrapsIn(table, FaceDevices.StateKey(device, state, canFit), room, StateLines,
                                $"device {device}", MetaText * MonoAdvance);
                        }
                    }
                }
            }
        }

        [Test]
        public void EveryBay_IsADropAreaBigEnoughToHit_AtTenEightyP()
        {
            // At 1920x1080 the glass shows the 1152 px panel about 1:1 (crt_crisp report: 2.05 texels/px of a 2x
            // texture); a drop area must stay at least 120 x 80 there.
            for (int device = 0; device < FaceDevices.Count; device++)
            {
                Rect frame = FaceLayout.DeviceFrame(device);
                Assert.GreaterOrEqual(frame.width, 120f, $"device {device}");
                Assert.GreaterOrEqual(frame.height, 80f, $"device {device}");
            }
        }

        [Test]
        public void EveryItemName_WrapsInTwoLinesUnderItsCell_InEveryLanguage()
        {
            float advance = MetaText * MonoAdvance;
            foreach (StringTable table in _tables)
            {
                foreach (SalvageMaterial material in Enum.GetValues(typeof(SalvageMaterial)))
                {
                    AssertWrapsIn(table, UiKeys.MaterialName(material), FaceLayout.ColumnPitch, NameLines,
                        material.ToString(), advance);
                }

                foreach (string part in new[] { "part.igniter", "part.icecore" })
                {
                    AssertWrapsIn(table, UiKeys.PickupName(part), FaceLayout.ColumnPitch, NameLines, part, advance);
                }
            }

            Assert.GreaterOrEqual(FaceLayout.NameHeight, NameLines * MetaText * NameLineHeight,
                "two lines of names fit");
        }

        [Test]
        public void TheTitle_ThePageCount_TheHint_AndTheStatusLine_FitTheirRows_InEveryLanguage()
        {
            foreach (StringTable table in _tables)
            {
                AssertFits(table, UiKeys.CargoHold, DatabankLayout.TitleRect.width,
                    TitleText * TitleAdvance + TitleLetterSpace, "the title row");
                int pages = FaceLayout.PageCount(FaceInventoryView.MaxHoldCells);
                AssertFits(table, UiKeys.CargoPage, FaceLayout.PageWidth, MetaText * MonoAdvance, "the page count",
                    pages, pages);
                AssertFits(table, UiKeys.CargoMoreSlots, FaceLayout.HintRect(FaceInventoryView.MaxHoldCells - 1).width,
                    MetaText * MonoAdvance, "the hint row", FaceInventoryView.MaxHoldCells);
                AssertFits(table, UiKeys.CargoWaiting, FaceLayout.HoldWidth, MetaText * MonoAdvance, "the status line",
                    TwoDigits);
            }
        }

        [Test]
        public void TheInfoBoxLines_FitInEveryLanguage()
        {
            float room = FaceLayout.InfoWidth;
            Assert.GreaterOrEqual(FaceLayout.InfoDescHeight, InfoLines * BodyText * NameLineHeight,
                "two lines of the description fit");
            Assert.AreEqual(FaceLayout.InfoHeight,
                FaceLayout.InfoNameHeight + FaceLayout.InfoDescHeight + FaceLayout.InfoUseHeight, 1e-3f,
                "name, description and where it goes fill the box");
            foreach (StringTable table in _tables)
            {
                foreach (string key in new[]
                         {
                             UiKeys.CargoEmptyDesc, "ui.device.cutter.fault", "ui.device.cutter.fit",
                             "ui.device.cutter.ok", "ui.device.scan.ok", "ui.device.socket.empty",
                             "ui.cargo.drag.not_docked", "ui.cargo.drag.to_cutter", "ui.cargo.drag.nowhere",
                         })
                {
                    AssertWrapsIn(table, key, room, InfoLines, key, BodyText * MonoAdvance);
                }

                Assert.IsTrue(table.TryGet(UiKeys.CargoGoesTo, out string goesTo), UiKeys.CargoGoesTo);
                Assert.IsTrue(table.TryGet(FaceDevices.NameKey(FaceDevices.Cutter), out string cutter));
                AssertFits(table, UiKeys.CargoGoesToOnPad,
                    room - goesTo.Length * MetaText * MonoAdvance - MetaText, BodyText * MonoAdvance,
                    "where the igniter goes", cutter);
            }
        }

        [Test]
        public void TheFootRow_NamesRealKeys_AndFitsOneLine_OnEveryDevice()
        {
            foreach (StringTable table in _tables)
            {
                foreach (InputDeviceKind device in new[] { InputDeviceKind.KeyboardMouse, InputDeviceKind.Gamepad })
                {
                    float width = 0f;
                    for (int i = 0; i < FaceKeyHints.Count; i++)
                    {
                        var hint = (FaceHint)i;
                        Assert.IsTrue(table.TryGet(FaceKeyHints.CapKey(hint, device), out string cap),
                            $"{table.Language}: {FaceKeyHints.CapKey(hint, device)}");
                        Assert.IsTrue(table.TryGet(FaceKeyHints.WordKey(hint), out string word),
                            $"{table.Language}: {FaceKeyHints.WordKey(hint)}");
                        Assert.LessOrEqual(cap.Length, 10, "a keycap word");
                        width += (cap.Length + word.Length) * MetaText * MonoAdvance + 80f;
                    }

                    Assert.LessOrEqual(width, FaceLayout.RightEdge - FaceLayout.Margin,
                        $"{table.Language} {device}: the foot row fits");
                }
            }
        }

        [Test]
        public void EachHint_HasItsOwnKey_ForTheKeyboardAndForThePad()
        {
            for (int a = 0; a < FaceKeyHints.Count; a++)
            {
                for (int b = a + 1; b < FaceKeyHints.Count; b++)
                {
                    Assert.AreNotEqual(FaceKeyHints.CapKey((FaceHint)a, InputDeviceKind.KeyboardMouse),
                        FaceKeyHints.CapKey((FaceHint)b, InputDeviceKind.KeyboardMouse));
                    Assert.AreNotEqual(FaceKeyHints.CapKey((FaceHint)a, InputDeviceKind.Gamepad),
                        FaceKeyHints.CapKey((FaceHint)b, InputDeviceKind.Gamepad));
                }
            }
        }

        [Test]
        public void EveryTier_FitsTheGrid_OnAtMostTwoPages()
        {
            foreach (int tier in Tiers)
            {
                Assert.LessOrEqual(FaceLayout.PageCount(tier), 2, $"{tier} slots");
                Assert.LessOrEqual(FaceLayout.RowsOnPage(0, tier), FaceLayout.RowsPerPage);
            }
        }

        private static void AssertWrapsIn(StringTable table, string key, float room, int lines, string what,
            float advance = MetaText * MonoAdvance)
        {
            Assert.IsTrue(table.TryGet(key, out string text), $"{table.Language}: {key}");
            string[] words = text.Split(' ', SoftHyphen);
            int used = 1;
            float width = 0f;
            foreach (string word in words)
            {
                float wordWidth = word.Length * advance;
                Assert.LessOrEqual(wordWidth, room, $"{table.Language} {what}: {word} is wider than {room}");
                if (width > 0f && width + advance + wordWidth > room)
                {
                    used++;
                    width = wordWidth;
                }
                else
                {
                    width += (width > 0f ? advance : 0f) + wordWidth;
                }
            }

            Assert.LessOrEqual(used, lines, $"{table.Language} {what}: {text} needs {used} lines in {room}");
        }

        private static void AssertFits(StringTable table, string key, float room, float advance, string what,
            params object[] args)
        {
            Assert.IsTrue(table.TryGet(key, out string text), $"{table.Language}: {key}");
            string line = args.Length > 0 ? string.Format(text, args) : text;
            Assert.LessOrEqual(line.Length * advance, room, $"{table.Language} {what}: {line} is wider than {room}");
        }
    }
}
