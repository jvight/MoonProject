using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// 07's databank page (docs/design/databank.md §4): the archive's index and focus, the turning model on its stage,
    /// the colour toggle and the page's view. UISystem hands it the face screen's input while it is the page on the
    /// tube; it reads 07's archive through <see cref="IDatabank"/> and marks a card read once the page shows it.
    /// </summary>
    internal sealed class DatabankPage : IDisposable
    {
        private readonly DatabankSettings _settings;
        private readonly IDatabank _databank;
        private readonly EventBus _events;
        private readonly DatabankIndex _index = new DatabankIndex();
        private readonly DatabankCursor _cursor;
        private readonly Turntable _turntable;
        private readonly DatabankText _text;
        private readonly DatabankView _view;
        private readonly DatabankStage _stage;
        private string _shownId;
        private DatabankProgress _shownProgress;
        private bool _colour;
        private bool _open;
        private bool _dragging;
        private Vector2 _lastPointer;
        private Vector2 _drag;

        public DatabankPage(DatabankSettings settings, IDatabank databank, EventBus events, ILocalization localization,
            VisualElement parent)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _databank = databank ?? throw new ArgumentNullException(nameof(databank));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _cursor = new DatabankCursor(_index);
            _turntable = new Turntable(settings);
            _text = new DatabankText(localization);
            _view = new DatabankView(parent, localization, settings);
            _stage = new DatabankStage(settings);
        }

        public DatabankView View => _view;

        public DatabankCursor Cursor => _cursor;

        public DatabankIndex Index => _index;

        public Turntable Turntable => _turntable;

        /// <summary>True in full colour (per session), false in the tube's amber wireframe.</summary>
        public bool IsColour => _colour;

        /// <summary>The entry the page shows, or null.</summary>
        public string ShownId => _shownId;

        /// <summary>The stage's film of the model (tests and captures).</summary>
        public RenderTexture StageTexture => _stage.Texture;

        public bool StageHasModel => _stage.HasModel;

        /// <summary>The page became the one on the tube (true) or left it.</summary>
        public void SetOpen(bool open, InputDeviceKind device)
        {
            _open = open;
            _dragging = false;
            _view.SetShown(open);
            if (open)
            {
                _view.WriteChrome(device);
                Refresh();
                _cursor.Restore();
                ShowFocused();
            }

            _stage.SetFilming(open);
        }

        /// <summary>
        /// The archive changed (an entry added or progressed): the list, the strip and the card follow.
        /// </summary>
        public void Refresh()
        {
            _index.Rebuild(_databank);
            if (!_open)
            {
                return;
            }

            ShowFocused();
        }

        public void Layout(float scale)
        {
            _view.ApplyScale(scale);
        }

        /// <param name="deltaTime">Unscaled seconds.</param>
        /// <param name="navigate">The menu's navigation this frame, quantized (up or down walk the list).</param>
        /// <param name="colourPressed">F (gamepad Y) went down.</param>
        /// <param name="stick">The right stick.</param>
        public void Tick(float deltaTime, NavigationMoveEvent.Direction navigate, bool colourPressed, Vector2 stick,
            InputDeviceKind device)
        {
            if (!_open)
            {
                return;
            }

            _view.WriteChrome(device);
            if (navigate == NavigationMoveEvent.Direction.Down || navigate == NavigationMoveEvent.Direction.Up)
            {
                if (navigate == NavigationMoveEvent.Direction.Down)
                {
                    _cursor.Down();
                }
                else
                {
                    _cursor.Up();
                }

                _events.Publish(new UiCue(UiCueKind.FocusMove));
                ShowFocused();
            }

            if (colourPressed)
            {
                _colour = !_colour;
                _events.Publish(new UiCue(UiCueKind.Confirm));
                ShowFocused(force: true);
            }

            _turntable.Tick(deltaTime, _drag, stick);
            _drag = Vector2.zero;
            _stage.Turn(_turntable.Yaw, _turntable.Pitch);
        }

        /// <summary>
        /// A press on the page at <paramref name="refPos"/> (reference pixels): a row opens, the model grabs.
        /// </summary>
        public void PointerDown(Vector2 refPos)
        {
            int row = _view.RowAt(refPos);
            if (row >= 0)
            {
                _cursor.Focus(_view.RowCategory(row), _view.RowEntry(row));
                _events.Publish(new UiCue(UiCueKind.FocusMove));
                ShowFocused();
                return;
            }

            _dragging = DatabankLayout.ModelRect.Contains(refPos);
            _lastPointer = refPos;
        }

        /// <summary>The pointer moved: a held drag over the model turns it on the next tick.</summary>
        public void PointerMove(Vector2 refPos)
        {
            if (!_dragging)
            {
                return;
            }

            _drag += refPos - _lastPointer;
            _lastPointer = refPos;
        }

        public void PointerUp()
        {
            _dragging = false;
        }

        public void Dispose()
        {
            _stage.Dispose();
        }

        private void ShowFocused(bool force = false)
        {
            string id = _cursor.ShownId;
            _view.WriteList(_index, _cursor, _databank, _text);
            if (id == null || !_databank.TryGetEntry(id, out DatabankEntryInfo entry))
            {
                _shownId = null;
                _stage.Show(null, null);
                _view.WriteEmpty();
                return;
            }

            DatabankProgress progress = _databank.Progress(id);
            bool changed = !string.Equals(id, _shownId, StringComparison.Ordinal);
            if (changed)
            {
                _shownId = id;
                _stage.Show(entry.View == DatabankViewKind.Model ? entry.Model : null, entry.HiddenNodes);
                _turntable.Reset(entry.FramingYaw);
                _stage.Turn(_turntable.Yaw, _turntable.Pitch);
            }

            if (changed || force || progress.Found != _shownProgress.Found || progress.Count != _shownProgress.Count)
            {
                _shownProgress = progress;
                _stage.SetLook(_colour, WireFade(progress));
                _view.WriteEntry(entry, progress, _text, _stage.Texture, _colour);
            }

            _stage.SetFilming(_open);
            if (_databank.IsUnread(id))
            {
                _databank.MarkRead(id);
                _view.WriteList(_index, _cursor, _databank, _text);
            }
        }

        /// <summary>The wireframe's strength: faint at a set's first piece, full once it is whole.</summary>
        private float WireFade(DatabankProgress progress)
        {
            if (progress.IsComplete || progress.Count < 2)
            {
                return 1f;
            }

            float share = (progress.Found - 1f) / (progress.Count - 1f);
            return Mathf.Lerp(_settings.WireFirstPieceFade, 1f, Mathf.Clamp01(share));
        }
    }
}
