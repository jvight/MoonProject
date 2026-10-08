using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Input;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The radio-hop's tiny list (docs/features/M3-06): while gameplay has it open on 07's pad, a small glass panel
    /// near the bottom centre names the other lit nodes ("hop.node.*"), one per row. The highlighted row's marker is a
    /// ring around the Interact glyph that fills as Interact is held (<see cref="IRadioHop.ConfirmHold"/>); the other
    /// rows keep a faint empty ring. Gameplay drives it (tap: next node, hold: hop, drive off: close); this only shows
    /// it. It shares the dial readout's place, so it waits for the readout to leave (and asks it to), and the UI keeps
    /// prompts and the ticker away while it is up. Rows are made when the list first needs them and then reused.
    /// </summary>
    internal sealed class HopList
    {
        public const string RowClass = "hop-list__row";
        public const string SelectedClass = "hop-list__row--selected";
        public const string MarkerClass = "hop-list__marker";
        public const string GlyphClass = "hop-list__glyph";
        public const string NameClass = "hop-list__name";

        private readonly IRadioHop _hop;
        private readonly ILocalization _localization;
        private readonly Reveal _reveal;
        private readonly VisualElement _rows;
        private readonly List<Row> _pool = new List<Row>();
        private int _count = -1;
        private int _selected = -1;
        private string _glyph;
        private InputDeviceKind _device = InputDeviceKind.KeyboardMouse;

        public HopList(UiLayout layout, RelaySettings settings, ILocalization localization, IRadioHop hop)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _hop = hop ?? throw new ArgumentNullException(nameof(hop));
            _reveal = new Reveal(layout.HopList, settings.List);
            _reveal.Snap(false);
            _rows = layout.HopListRows;
            new ShadowPainter(layout.HopListShadow);
        }

        /// <summary>True while the list is on screen (or easing).</summary>
        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>True while gameplay has the list open or it is still on screen.</summary>
        public bool IsBusy => _hop.Phase == RadioHopPhase.Choosing || IsVisible;

        /// <summary>Rows showing a node (the open list's choices).</summary>
        public int RowCount => _count < 0 ? 0 : _count;

        /// <summary>The name on row <paramref name="row"/> (tests and captures).</summary>
        public string RowName(int row)
        {
            return _pool[row].Name.text;
        }

        /// <summary>The fill of row <paramref name="row"/>'s hold ring (tests and captures).</summary>
        public float RowHold(int row)
        {
            return _pool[row].Ring.Progress;
        }

        /// <param name="deltaTime">Unscaled seconds since the last tick.</param>
        /// <param name="stageClear">False while the dial readout or a prompt is on screen: the list waits.</param>
        /// <param name="glyph">The Interact control's label for the active device.</param>
        /// <param name="device">The active device (key cap or round glyph).</param>
        public void Tick(float deltaTime, bool stageClear, string glyph, InputDeviceKind device)
        {
            bool open = _hop.Phase == RadioHopPhase.Choosing;
            if (open && _hop.ChoiceCount != _count)
            {
                Fill(_hop.ChoiceCount);
            }

            if (_count > 0)
            {
                if (open && _hop.Selected != _selected && _hop.Selected < _count)
                {
                    Select(_hop.Selected);
                }

                if (!ReferenceEquals(glyph, _glyph) || device != _device)
                {
                    WriteGlyph(glyph, device);
                }

                if (_selected >= 0)
                {
                    _pool[_selected].Ring.Progress = open ? _hop.ConfirmHold : 0f;
                }
            }

            _reveal.Set(open && stageClear);
            _reveal.Tick(deltaTime);
            if (_reveal.IsHidden && !open)
            {
                _count = -1;
                _selected = -1;
            }
        }

        /// <summary>Re-reads the node names in the new language (an open list changes in place).</summary>
        public void Relocalize()
        {
            for (int i = 0; i < RowCount; i++)
            {
                _pool[i].Name.text = _localization.Get(_hop.ChoiceLabelKey(i));
            }
        }

        private void Fill(int count)
        {
            while (_pool.Count < count)
            {
                _pool.Add(NewRow());
            }

            for (int i = 0; i < _pool.Count; i++)
            {
                bool used = i < count;
                _pool[i].Root.style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
                if (used)
                {
                    _pool[i].Name.text = _localization.Get(_hop.ChoiceLabelKey(i));
                }
            }

            _count = count;
            _selected = -1;
            _glyph = null;
        }

        private void Select(int row)
        {
            if (_selected >= 0)
            {
                _pool[_selected].Root.RemoveFromClassList(SelectedClass);
                _pool[_selected].Ring.Progress = 0f;
            }

            _pool[row].Root.AddToClassList(SelectedClass);
            _selected = row;
        }

        private void WriteGlyph(string glyph, InputDeviceKind device)
        {
            for (int i = 0; i < _count; i++)
            {
                _pool[i].Glyph.text = glyph;
                _pool[i].Marker.EnableInClassList(GlyphView.PadClass, device == InputDeviceKind.Gamepad);
            }

            _glyph = glyph;
            _device = device;
        }

        private Row NewRow()
        {
            var root = new VisualElement { pickingMode = PickingMode.Ignore };
            root.AddToClassList(RowClass);
            var marker = new VisualElement { pickingMode = PickingMode.Ignore };
            marker.AddToClassList(MarkerClass);
            var glyph = new Label { pickingMode = PickingMode.Ignore };
            glyph.AddToClassList(GlyphClass);
            marker.Add(glyph);
            var name = new Label { pickingMode = PickingMode.Ignore };
            name.AddToClassList(NameClass);
            root.Add(marker);
            root.Add(name);
            _rows.Add(root);
            return new Row(root, marker, new ProgressRingPainter(marker), glyph, name);
        }

        private readonly struct Row
        {
            public Row(VisualElement root, VisualElement marker, ProgressRingPainter ring, Label glyph, Label name)
            {
                Root = root;
                Marker = marker;
                Ring = ring;
                Glyph = glyph;
                Name = name;
            }

            public VisualElement Root { get; }

            public VisualElement Marker { get; }

            public ProgressRingPainter Ring { get; }

            public Label Glyph { get; }

            public Label Name { get; }
        }
    }
}
