using NUnit.Framework;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Input;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The legibility layout of 07's face screen: one hint row for the locked slots, hit areas without dead strips,
    /// an info box and a foot row that clear the grid and the bubbles, and words that fit in English and Vietnamese
    /// at the sizes the screen is read at.
    /// </summary>
    public sealed class FaceLayoutTests
    {
        // IBM Plex Mono advance as a share of its size: the width of one character.
        private const float MonoAdvance = 0.6f;
        private const float SmallestText = 22f;
        private const float InfoPadding = 36f;

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

        [TestCase(8, 2)]
        [TestCase(9, 3)]
        [TestCase(16, 4)]
        [TestCase(24, 6)]
        [TestCase(0, 0)]
        public void TheRowsOfTheGrid_AreTheOnesThatHoldAnUnlockedSlot(int unlocked, int rows)
        {
            Assert.AreEqual(rows, FaceLayout.RowsOf(unlocked));
        }

        [Test]
        public void OnlyOneHintRow_StandsForTheLockedSlots_UnderTheLastUnlockedRow()
        {
            foreach (int unlocked in new[] { 8, 16 })
            {
                Assert.IsTrue(FaceLayout.HasMoreRow(unlocked));
                Rect row = FaceLayout.MoreRowRect(unlocked);
                for (int i = 0; i < unlocked; i++)
                {
                    Assert.IsFalse(row.Overlaps(FaceInventoryView.GetHoldCellReferenceRect(i)),
                        $"{unlocked} slots: the hint row overlaps cell {i}");
                }

                Assert.GreaterOrEqual(row.yMin, FaceInventoryView.GetHoldCellReferenceRect(unlocked - 1).yMax);
                Assert.LessOrEqual(row.xMax, FaceInventoryView.DividerX - 24f, "it stays in the hold pane");
                Assert.LessOrEqual(row.yMax, FaceLayout.InfoY, "it clears the info box");
            }

            Assert.IsFalse(FaceLayout.HasMoreRow(FaceInventoryView.MaxHoldCells), "nothing left to hint at");
        }

        [Test]
        public void TheHitAreas_OfNeighbouringCells_MeetWithoutAGap_AndKeepTheirOwnCell()
        {
            for (int i = 0; i < FaceInventoryView.MaxHoldCells; i++)
            {
                Rect hit = FaceLayout.HoldHitRect(i);
                Rect cell = FaceInventoryView.GetHoldCellReferenceRect(i);
                Assert.IsTrue(hit.Contains(cell.center));
                Assert.IsTrue(hit.Contains(cell.min) && hit.Contains(cell.max));
                if (i % FaceInventoryView.GridColumns + 1 < FaceInventoryView.GridColumns)
                {
                    Assert.AreEqual(hit.xMax, FaceLayout.HoldHitRect(i + 1).xMin, 1e-3f, $"cells {i} and {i + 1}");
                }

                if (i + FaceInventoryView.GridColumns < FaceInventoryView.MaxHoldCells)
                {
                    Assert.AreEqual(hit.yMax, FaceLayout.HoldHitRect(i + FaceInventoryView.GridColumns).yMin, 1e-3f);
                }
            }
        }

        [Test]
        public void TheInfoBoxAndTheFootRow_ClearTheGridAndTheBubbles_AndStayOnTheScreen()
        {
            float lastRowBottom = FaceInventoryView.GetHoldCellReferenceRect(FaceInventoryView.MaxHoldCells - 1).yMax;
            Assert.Greater(FaceLayout.InfoY, lastRowBottom, "even a full hold leaves the info box clear");
            Assert.Greater(FaceLayout.FooterY, FaceLayout.InfoY + FaceLayout.InfoHeight);
            Assert.LessOrEqual(FaceLayout.FooterY + FaceLayout.FooterHeight, FaceInventoryView.ReferenceHeight - 32f);
            Assert.LessOrEqual(FaceLayout.InfoX + FaceLayout.InfoWidth, FaceInventoryView.ReferenceWidth - 48f);
            Assert.Greater(FaceLayout.DividerBottom, FaceInventoryView.HoldGridY);
            Assert.LessOrEqual(FaceLayout.DividerBottom, FaceLayout.InfoY);
            for (int device = 0; device < FaceDevices.Count; device++)
            {
                Vector2 centre = FaceDevices.CenterOf(device);
                // Only the cutter's fault is long enough to wrap onto a second line.
                float lines = device == FaceDevices.Cutter ? 2f : 1f;
                float stateBottom = centre.y + FaceInventoryView.BubbleRadius + 4f + lines * SmallestText * 1.3f;
                Assert.Less(stateBottom, FaceLayout.InfoY, $"device {device}: its words clear the info box");
            }
        }

        [Test]
        public void TheWordsUnderEveryBubble_FitTheirBox_InEveryLanguage()
        {
            foreach (StringTable table in _tables)
            {
                for (int device = 0; device < FaceDevices.Count; device++)
                {
                    float x = FaceDevices.CenterOf(device).x;
                    float left = FaceLayout.DeviceStateHalfWidth(x, true);
                    float right = FaceLayout.DeviceStateHalfWidth(x, false);
                    Assert.Greater(x - left, 0f);
                    Assert.LessOrEqual(x + right, FaceInventoryView.ReferenceWidth);
                    RoverSystem system = FaceDevices.SystemOf(device);
                    if (system == RoverSystem.Sockets)
                    {
                        AssertWrapsIn(table, SystemText.StateKey(system, RoverSystemState.Empty), left + right, 2,
                            $"device {device}");
                        continue;
                    }

                    int lines = device == FaceDevices.Cutter ? 2 : 1;
                    foreach (RoverSystemState state in new[] { RoverSystemState.Ok, RoverSystemState.Fault })
                    {
                        if (device == FaceDevices.Scan && state == RoverSystemState.Fault)
                        {
                            continue;
                        }

                        AssertWrapsIn(table, SystemText.StateKey(system, state), left + right, lines,
                            $"device {device}");
                    }
                }
            }
        }

        [Test]
        public void TheInfoBoxLines_AndTheHintRow_FitInEveryLanguage()
        {
            float room = FaceLayout.InfoWidth - InfoPadding;
            foreach (StringTable table in _tables)
            {
                AssertFits(table, UiKeys.CargoEmptyDesc, room, SmallestText, "empty slot");
                AssertFits(table, "ui.device.cutter.fault", room, SmallestText, "cutter fault");
                AssertFits(table, "ui.device.cutter.fit", room, SmallestText, "cutter offer");
                Assert.IsTrue(table.TryGet(UiKeys.CargoMoreSlots, out string more));
                string line = string.Format(more, FaceInventoryView.MaxHoldCells);
                Assert.LessOrEqual(line.Length * SmallestText * MonoAdvance, FaceLayout.MoreRowRect(8).width - 80f,
                    $"{table.Language}: {line} fits the hint row beside its padlock");
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

                    Assert.LessOrEqual(width, FaceLayout.InfoWidth, $"{table.Language} {device}: the foot row fits");
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
