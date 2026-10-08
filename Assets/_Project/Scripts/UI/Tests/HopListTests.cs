using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Input;
using MoonProject.Gameplay;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// The radio-hop list and fade over GameUI.uxml, driven by a scripted <see cref="IRadioHop"/>: the list names the
    /// lit nodes while gameplay has it open, highlights the chosen one with the hold ring, waits for the dial readout,
    /// bows out when the hop starts, reuses its rows and changes language in place; the fade follows the hop.
    /// </summary>
    public sealed class HopListTests
    {
        private const float Frame = 1f / 60f;
        private const string Glyph = "E";
        private const string Home = "test.hop.home";
        private const string Mound = "test.hop.mound";
        private const string Rim = "test.hop.rim";

        private ScriptedHop _hop;
        private LocalizationService _localization;
        private UiLayout _layout;
        private HopList _list;
        private UiTuning _tuning;

        [SetUp]
        public void SetUp()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiAssetPaths.Uxml);
            Assert.IsNotNull(uxml, UiAssetPaths.Uxml);
            _localization = TestTables.Localization(new EventBus(),
                Home, "Lumen Station", "Lumen Station",
                Mound, "Mound relay", "Trạm trên gò",
                Rim, "Rim shoulder relay", "Trạm vai vành hố");
            _tuning = ScriptableObject.CreateInstance<UiTuning>();
            _layout = new UiLayout(uxml.Instantiate());
            _hop = new ScriptedHop();
            _list = new HopList(_layout, _tuning.Relays, _localization, _hop);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void TheList_ShowsOnlyWhileGameplayHasItOpen()
        {
            Run(1f);
            Assert.IsFalse(_list.IsVisible);
            Assert.IsFalse(_list.IsBusy);

            _hop.OpenWith(Home, Mound, Rim);
            Assert.IsTrue(_list.IsBusy, "busy from the moment it opens: prompts and the ticker make way");
            Run(_tuning.Relays.List.FadeIn + 0.1f);
            Assert.IsTrue(_list.IsVisible);
            Assert.AreEqual(3, _list.RowCount);
            CollectionAssert.AreEqual(new[] { "Lumen Station", "Mound relay", "Rim shoulder relay" }, Names());

            _hop.Phase = RadioHopPhase.Leaving;
            Run(_tuning.Relays.List.FadeOut + 0.1f);
            Assert.IsFalse(_list.IsVisible, "the hop starts: the list bows out under the fade");
            Assert.IsFalse(_list.IsBusy);
        }

        [Test]
        public void TheChosenNode_IsHighlighted_AndItsRingFillsWithTheHold()
        {
            _hop.OpenWith(Home, Mound, Rim);
            Run(0.5f);
            Assert.IsTrue(Row(0).ClassListContains(HopList.SelectedClass), "the first node is chosen when it opens");
            Assert.AreEqual(Glyph, Row(0).Q<Label>(className: HopList.GlyphClass).text);

            _hop.ConfirmHold = 0.6f;
            Run(Frame);
            Assert.AreEqual(0.6f, _list.RowHold(0), 1e-3f);
            Assert.AreEqual(0f, _list.RowHold(1), "only the chosen ring fills");

            _hop.ConfirmHold = 0f;
            _hop.Selected = 1;
            Run(Frame);
            Assert.IsFalse(Row(0).ClassListContains(HopList.SelectedClass));
            Assert.IsTrue(Row(1).ClassListContains(HopList.SelectedClass), "a tap moves the highlight");
            Assert.AreEqual(0f, _list.RowHold(0), "the old ring empties");
        }

        [Test]
        public void TheList_WaitsForTheDialReadout()
        {
            _hop.OpenWith(Home, Mound);
            for (int i = 0; i < 60; i++)
            {
                _list.Tick(Frame, false, Glyph, InputDeviceKind.KeyboardMouse);
            }

            Assert.IsFalse(_list.IsVisible, "never on top of the readout");
            Run(0.5f);
            Assert.IsTrue(_list.IsVisible);
        }

        [Test]
        public void Rows_AreReused_WhenTheListOpensAgain()
        {
            _hop.OpenWith(Home, Mound, Rim);
            Run(0.5f);
            _hop.Phase = RadioHopPhase.Closed;
            Run(1f);
            int made = _layout.HopListRows.childCount;

            _hop.OpenWith(Mound, Rim);
            Run(0.5f);
            Assert.AreEqual(made, _layout.HopListRows.childCount, "no new rows for a shorter list");
            Assert.AreEqual(2, _list.RowCount);
            CollectionAssert.AreEqual(new[] { "Mound relay", "Rim shoulder relay" }, Names());
            Assert.AreEqual(DisplayStyle.None, Row(2).style.display.value, "the spare row is out of the layout");
        }

        [Test]
        public void ANewLanguage_RenamesTheOpenList()
        {
            _hop.OpenWith(Home, Mound);
            Run(0.5f);
            _localization.SetLanguage(TestTables.Vietnamese);
            _list.Relocalize();
            CollectionAssert.AreEqual(new[] { "Lumen Station", "Trạm trên gò" }, Names());
        }

        [Test]
        public void TheFade_FollowsTheHop_AndLeavesTheLayoutWhenClear()
        {
            var fade = new HopFade(_layout, _tuning.Relays, _hop);
            try
            {
                Assert.AreEqual(DisplayStyle.None, _layout.HopVeil.style.display.value);
                _hop.Fade = 1f;
                for (int i = 0; i < 60; i++)
                {
                    fade.Tick(Frame);
                }

                Assert.IsTrue(fade.IsVisible);
                Assert.AreEqual(_tuning.Relays.VeilMaxDarkness, _layout.HopVeilDark.style.opacity.value, 1e-3f);
                Assert.AreEqual(_tuning.Relays.GrainOpacity, _layout.HopVeilGrain.style.opacity.value, 1e-3f);
                Assert.IsNotNull(_layout.HopVeilGrain.style.backgroundImage.value.texture, "the grain is drawn");

                _hop.Fade = 0f;
                for (int i = 0; i < 120; i++)
                {
                    fade.Tick(Frame);
                }

                Assert.IsFalse(fade.IsVisible);
                Assert.AreEqual(DisplayStyle.None, _layout.HopVeil.style.display.value);
            }
            finally
            {
                fade.Dispose();
            }
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                _list.Tick(Frame, true, Glyph, InputDeviceKind.KeyboardMouse);
            }
        }

        private VisualElement Row(int index)
        {
            return _layout.HopListRows[index];
        }

        private List<string> Names()
        {
            var names = new List<string>();
            for (int i = 0; i < _list.RowCount; i++)
            {
                names.Add(_list.RowName(i));
            }

            return names;
        }

        /// <summary>A radio-hop the test moves by hand.</summary>
        private sealed class ScriptedHop : IRadioHop
        {
            private readonly List<string> _labels = new List<string>();

            public RadioHopPhase Phase { get; set; }

            public bool CanOpen => Phase == RadioHopPhase.Closed;

            public int Here => 0;

            public int ChoiceCount => _labels.Count;

            public int Selected { get; set; }

            public float ConfirmHold { get; set; }

            public float Fade { get; set; }

            public float Progress => 0f;

            public void OpenWith(params string[] labels)
            {
                _labels.Clear();
                _labels.AddRange(labels);
                Selected = 0;
                Phase = RadioHopPhase.Choosing;
            }

            public int ChoiceNode(int choice)
            {
                return choice;
            }

            public string ChoiceLabelKey(int choice)
            {
                return _labels[choice];
            }

            public bool Open()
            {
                Phase = RadioHopPhase.Choosing;
                return true;
            }

            public void Next()
            {
                Selected = (Selected + 1) % _labels.Count;
            }

            public void Previous()
            {
                Selected = (Selected + _labels.Count - 1) % _labels.Count;
            }

            public bool Confirm()
            {
                Phase = RadioHopPhase.Leaving;
                return true;
            }

            public void Cancel()
            {
                Phase = RadioHopPhase.Closed;
            }
        }
    }
}
