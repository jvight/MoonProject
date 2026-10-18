using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Input;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The layout of 07's face screen: device frames at the corners, the hold strip along the foot with its names,
    /// hit areas without dead strips, an info box and a foot row that clear the strip and the frames, and words that
    /// fit in English and Vietnamese at the sizes the screen is read at.
    /// </summary>
    public sealed class FaceLayoutTests
    {
        // IBM Plex Mono advance as a share of its size: the width of one character.
        private const float MonoAdvance = 0.6f;
        private const float SmallestText = 22f;
        private const float InfoPadding = 36f;
        private const float GlassBottomMargin = 36f;
        private const float StripHeadingLetterSpace = 2f;
        private const float NameInset = 68f;

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

        [TestCase(8, 1)]
        [TestCase(9, 2)]
        [TestCase(16, 2)]
        [TestCase(24, 3)]
        [TestCase(0, 0)]
        public void TheRowsOfTheStrip_AreTheOnesThatHoldAnUnlockedSlot(int unlocked, int rows)
        {
            Assert.AreEqual(rows, FaceLayout.RowsOf(unlocked));
        }

        [Test]
        public void TheBottomRowNeverMoves_AndMoreRowsStackAboveIt()
        {
            Assert.AreEqual(FaceLayout.HoldCellRect(0).y, FaceLayout.HoldCellRect(FaceInventoryView.GridColumns - 1).y);
            Assert.AreEqual(FaceLayout.HoldCellRect(0).y - FaceLayout.RowPitch,
                FaceLayout.HoldCellRect(FaceInventoryView.GridColumns).y, 1e-3f);
            Assert.AreEqual(FaceLayout.BottomRowY, FaceLayout.StripTop(8), "a hold of one row");
            Assert.Less(FaceLayout.StripTop(16), FaceLayout.StripTop(8));
        }

        [Test]
        public void TheCellsNeverOverlap_AndTheirNamesStayUnderThem()
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
                for (int j = i + 1; j < FaceInventoryView.MaxHoldCells; j++)
                {
                    Assert.IsFalse(cell.Overlaps(FaceLayout.HoldCellRect(j)), $"cells {i} and {j}");
                    Assert.IsFalse(cell.Overlaps(FaceLayout.HoldNameRect(j)), $"cell {i} and the name of {j}");
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
                Assert.IsTrue(hit.Contains(cell.center));
                Assert.IsTrue(hit.Contains(cell.min) && hit.Contains(cell.max));
                if (i % FaceInventoryView.GridColumns + 1 < FaceInventoryView.GridColumns)
                {
                    Assert.AreEqual(hit.xMax, FaceLayout.HoldHitRect(i + 1).xMin, 1e-3f, $"cells {i} and {i + 1}");
                }

                if (i + FaceInventoryView.GridColumns < FaceInventoryView.MaxHoldCells)
                {
                    Assert.AreEqual(hit.yMin, FaceLayout.HoldHitRect(i + FaceInventoryView.GridColumns).yMax, 1e-3f,
                        $"cells {i} and the one above");
                }
            }
        }

        [Test]
        public void TheStrip_ClearsTheFrames_TheInfoBox_AndTheFootRow_AtEverySize()
        {
            float lowestFrame = Mathf.Max(FaceLayout.DeviceFrame(FaceDevices.SocketOne).yMax,
                FaceLayout.DeviceFrame(FaceDevices.SocketTwo).yMax);
            Rect bottomNames = FaceLayout.HoldNameRect(0);
            foreach (int unlocked in new[] { 8, 16, 24 })
            {
                Assert.GreaterOrEqual(FaceLayout.StripHeadingRect(unlocked).yMin, lowestFrame + 4f,
                    $"{unlocked} slots: the strip's heading clears the frame above it");
            }

            Assert.LessOrEqual(FaceLayout.HoldNameRect(0).xMax, FaceLayout.InfoX - 8f, "the strip clears the info box");
            Assert.LessOrEqual(bottomNames.yMax, FaceLayout.InfoY + FaceLayout.InfoHeight, "on the same foot line");
            Assert.Greater(FaceLayout.FooterY, FaceLayout.InfoY + FaceLayout.InfoHeight);
            Assert.Greater(FaceLayout.FooterY, bottomNames.yMax);
            Assert.LessOrEqual(FaceLayout.FooterY + FaceLayout.FooterHeight,
                FaceInventoryView.ReferenceHeight - GlassBottomMargin, "inside the glass");
            Assert.LessOrEqual(FaceLayout.InfoX + FaceLayout.InfoWidth, FaceLayout.RightEdge);
            Assert.Greater(FaceLayout.InfoY, FaceLayout.DeviceFrame(FaceDevices.SocketTwo).yMax + FaceLayout.ToastHeight,
                "the toast fits between the lower right frame and the info box");
            Assert.AreEqual(FaceLayout.InfoY - FaceLayout.ToastHeight - 8f, FaceLayout.ToastY, 1e-3f,
                "the toast sits right above the info box");
        }

        [Test]
        public void TheFrames_StandApartFromEachOther_AndBelowTheTitle()
        {
            for (int a = 0; a < FaceDevices.Count; a++)
            {
                Assert.Greater(FaceLayout.DeviceFrame(a).yMin, FaceLayout.TitleRuleY, $"frame {a}");
                for (int b = a + 1; b < FaceDevices.Count; b++)
                {
                    Assert.IsFalse(FaceLayout.DeviceHitRect(a).Overlaps(FaceLayout.DeviceFrame(b)), $"frames {a} and {b}");
                }
            }
        }

        [Test]
        public void TheLeaders_LeaveTheInnerEdgeOfTheirFrame_AndBendTowardsSeven()
        {
            for (int device = 0; device < FaceDevices.Count; device++)
            {
                Rect frame = FaceLayout.DeviceFrame(device);
                Vector2 anchor = FaceLayout.LeaderAnchor(device);
                Vector2 knee = FaceLayout.LeaderKneeOf(device);
                Assert.AreEqual(FaceLayout.IsLeftFrame(device) ? frame.xMax : frame.xMin, anchor.x, 1e-3f);
                Assert.AreEqual(anchor.y, knee.y, 1e-3f, "level out of the frame");
                Assert.Greater(Mathf.Abs(knee.x - anchor.x), 0f);
                Assert.IsTrue(FaceLayout.IsLeftFrame(device) ? knee.x > anchor.x : knee.x < anchor.x);
            }
        }

        [Test]
        public void TheSelection_EntersTheHoldBelowAFrame_AndLeavesItAboveTheTopRow()
        {
            Assert.AreEqual(-1, FaceLayout.HoldEntryBelow(FaceDevices.SocketOne, 0));
            foreach (int unlocked in new[] { 8, 16 })
            {
                int top = FaceLayout.RowsOf(unlocked) - 1;
                int entry = FaceLayout.HoldEntryBelow(FaceDevices.SocketOne, unlocked);
                Assert.AreEqual(top, entry / FaceInventoryView.GridColumns, $"{unlocked} slots: the top row");
                Assert.Less(entry, unlocked);
                Assert.AreEqual(FaceDevices.SocketOne, FaceLayout.DeviceAbove(entry));
                int right = FaceLayout.HoldEntryBelow(FaceDevices.SocketTwo, unlocked);
                Assert.AreEqual(top, right / FaceInventoryView.GridColumns);
                Assert.AreEqual(FaceDevices.SocketTwo, FaceLayout.DeviceAbove(right));
            }

            Assert.AreEqual(5, FaceLayout.HoldEntryBelow(FaceDevices.SocketTwo, 6), "never past the last unlocked slot");
        }

        [Test]
        public void OnlyOneHint_StandsForTheLockedSlots_OnTheHeadingRow()
        {
            foreach (int unlocked in new[] { 8, 16 })
            {
                Assert.IsTrue(FaceLayout.HasMoreRow(unlocked));
                Rect row = FaceLayout.MoreRowRect(unlocked);
                Assert.LessOrEqual(row.yMax, FaceLayout.StripRuleY(unlocked), "on the heading row, above the rule");
                Assert.LessOrEqual(row.xMax, FaceLayout.Margin + FaceLayout.StripWidth, "it stays over the strip");
            }

            Assert.IsFalse(FaceLayout.HasMoreRow(FaceInventoryView.MaxHoldCells), "nothing left to hint at");
        }

        [Test]
        public void TheWordsInEveryFrame_FitTheirBox_InEveryLanguage()
        {
            float room = FaceLayout.FrameWidth - FaceLayout.FrameTextX - FaceLayout.FrameEdgeInset;
            float nameRoom = FaceLayout.FrameWidth - NameInset;
            foreach (StringTable table in _tables)
            {
                for (int device = 0; device < FaceDevices.Count; device++)
                {
                    AssertFits(table, FaceDevices.NameKey(device), nameRoom, SmallestText, $"device {device} name");
                    RoverSystem system = FaceDevices.SystemOf(device);
                    if (system == RoverSystem.Sockets)
                    {
                        AssertWrapsIn(table, SystemText.StateKey(system, RoverSystemState.Empty), room, 1,
                            $"device {device}");
                        continue;
                    }

                    foreach (RoverSystemState state in new[] { RoverSystemState.Ok, RoverSystemState.Fault })
                    {
                        if (device == FaceDevices.Scan && state == RoverSystemState.Fault)
                        {
                            continue;
                        }

                        AssertWrapsIn(table, SystemText.StateKey(system, state), room, 2, $"device {device}");
                    }
                }
            }
        }

        [Test]
        public void TheTitle_TheStripHeading_AndTheHint_FitTheirRows_InEveryLanguage()
        {
            foreach (StringTable table in _tables)
            {
                AssertFits(table, UiKeys.FaceTitle, FaceLayout.RightEdge - FaceLayout.Margin, 28f + StripHeadingLetterSpace,
                    "the title row");
                Assert.IsTrue(table.TryGet(UiKeys.CargoHold, out string heading));
                float headingWidth = heading.Length * (SmallestText * MonoAdvance + StripHeadingLetterSpace);
                Assert.LessOrEqual(headingWidth, FaceLayout.StripWidth - FaceLayout.MoreRowWidth,
                    $"{table.Language}: the heading leaves room for the hint on its row");
                Assert.IsTrue(table.TryGet(UiKeys.CargoMoreSlots, out string more));
                string line = string.Format(more, FaceInventoryView.MaxHoldCells);
                Assert.LessOrEqual(line.Length * SmallestText * MonoAdvance, FaceLayout.MoreRowWidth - 30f,
                    $"{table.Language}: {line} fits the hint row beside its padlock");
            }
        }

        [Test]
        public void TheInfoBoxLines_FitInEveryLanguage()
        {
            float room = FaceLayout.InfoWidth - InfoPadding;
            foreach (StringTable table in _tables)
            {
                foreach (string key in new[]
                         {
                             UiKeys.CargoEmptyDesc, "ui.device.cutter.fault", "ui.device.cutter.fit",
                             "ui.device.cutter.ok", "ui.device.scan.ok", "ui.device.socket.empty",
                         })
                {
                    AssertWrapsIn(table, key, room, 4, key);
                }
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
                        width += (cap.Length + word.Length) * SmallestText * MonoAdvance + 80f;
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

        private static void AssertWrapsIn(StringTable table, string key, float room, int lines, string what)
        {
            Assert.IsTrue(table.TryGet(key, out string text), $"{table.Language}: {key}");
            string[] words = text.Split(' ');
            int used = 1;
            float width = 0f;
            float space = SmallestText * MonoAdvance;
            foreach (string word in words)
            {
                float wordWidth = word.Length * SmallestText * MonoAdvance;
                Assert.LessOrEqual(wordWidth, room, $"{table.Language} {what}: {word} is wider than {room}");
                if (width > 0f && width + space + wordWidth > room)
                {
                    used++;
                    width = wordWidth;
                }
                else
                {
                    width += (width > 0f ? space : 0f) + wordWidth;
                }
            }

            Assert.LessOrEqual(used, lines, $"{table.Language} {what}: {text} needs {used} lines in {room}");
        }

        private static void AssertFits(StringTable table, string key, float room, float size, string what)
        {
            Assert.IsTrue(table.TryGet(key, out string text), $"{table.Language}: {key}");
            Assert.LessOrEqual(text.Length * size * MonoAdvance, room,
                $"{table.Language} {what}: {text} is wider than {room}");
        }
    }
}
