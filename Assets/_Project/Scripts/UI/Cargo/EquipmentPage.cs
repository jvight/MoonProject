using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;

namespace MoonProject.UI
{
    /// <summary>
    /// The 07 tab's page (docs/design/0.5.5-owner-feedback.md §3.5): 07's live portrait (rover's render, fitted parts
    /// as they really look) with a socket where each mount point lands on it (no marker for a socket the portrait
    /// cannot place), and below it the parts row of crafted parts waiting to be fitted. An empty socket is a dim outline with no
    /// name until a part for it is known, then it carries the part's name and the small bay icon ("fit at the bay"); a
    /// fitted one carries the part's name. A part is fitted by dragging it onto its own socket (only that one lights) or,
    /// with a gamepad or keyboard, by focusing it and pressing Submit; a fitted part is taken off by dragging it to the
    /// row. Both work only while 07 rests at the part's station (<see cref="IRoverEquipment.CanFit"/>); elsewhere
    /// nothing happens and nothing pops up. A tooltip (hover or focus) is the part's name and its key glyph, no
    /// sentence. Rewritten only when something changed; an open page with nothing new allocates nothing.
    /// </summary>
    internal sealed class EquipmentPage
    {
        public const string SocketClass = "seven__socket";
        public const string UnknownClass = "seven__socket--unknown";
        public const string FittedClass = "seven__socket--fitted";
        public const string TargetClass = "seven__target";
        public const string AwayClass = "seven__card--away";
        private const string CardClass = "seven__card";
        private const string TooltipShownClass = "seven__tooltip--shown";
        private const int SocketCount = 12;

        private readonly CargoScreenSettings _settings;
        private readonly ILocalization _localization;
        private readonly IRoverEquipment _equipment;
        private readonly IRoverPortrait _portraitSource;
        private readonly GlyphLabels _glyphs;
        private readonly VisualElement _panel;
        private readonly VisualElement _portrait;
        private readonly VisualElement _row;
        private readonly VisualElement _tooltip;
        private readonly Label _tooltipName;
        private readonly GlyphView _tooltipGlyph;
        private readonly VisualElement _tooltipGlyphHolder;
        private readonly Label _ghost;
        private readonly SocketView[] _sockets = new SocketView[SocketCount];
        private readonly CardView[] _cards;
        private InputDeviceKind _device = InputDeviceKind.KeyboardMouse;
        private int _signature = -1;
        private bool _dirty = true;
        private bool _dragging;
        private bool _pressed;
        private bool _dragFromSocket;
        private int _dragIndex;
        private Vector2 _pressAt;

        public EquipmentPage(UiLayout layout, CargoScreenSettings settings, ILocalization localization,
            IRoverEquipment equipment, IRoverPortrait portrait, GlyphLabels glyphs)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _equipment = equipment ?? throw new ArgumentNullException(nameof(equipment));
            _portraitSource = portrait ?? throw new ArgumentNullException(nameof(portrait));
            _glyphs = glyphs ?? throw new ArgumentNullException(nameof(glyphs));
            VisualElement root = layout.Cargo;
            _panel = Require<VisualElement>(root, "cargo-panel");
            _portrait = Require<VisualElement>(root, "seven-portrait");
            _row = Require<VisualElement>(root, "seven-row");
            _tooltip = Require<VisualElement>(root, "seven-tooltip");
            _tooltipName = Require<Label>(root, "seven-tooltip-name");
            _tooltipGlyphHolder = Require<VisualElement>(root, "seven-tooltip-glyph");
            _tooltipGlyph = new GlyphView(_tooltipGlyphHolder, Require<Label>(root, "seven-tooltip-glyph-label"));
            _ghost = Require<Label>(root, "seven-ghost");
            _portrait.style.backgroundImage = Background.FromRenderTexture(portrait.Texture);
            for (int i = 0; i < SocketCount; i++)
            {
                _sockets[i] = BuildSocket(i);
            }

            _cards = new CardView[_equipment.RowCapacity];
            for (int i = 0; i < _cards.Length; i++)
            {
                _cards[i] = BuildCard(i);
            }
        }

        /// <summary>True when the last press or drop fitted a part (the screen then eases away for the install).</summary>
        public bool FitRequested { get; private set; }

        /// <summary>The tooltip's text now, empty when none shows (tests and captures).</summary>
        public string TooltipText => _tooltip.ClassListContains(TooltipShownClass)
            ? _tooltipName.text + (_tooltipGlyphHolder.style.display == DisplayStyle.None
                ? string.Empty
                : " [" + _tooltipGlyphHolder.Q<Label>().text + "]")
            : string.Empty;

        /// <summary>A socket's element (tests and captures).</summary>
        public VisualElement SocketElement(RoverSocket socket)
        {
            return _sockets[(int)socket].Root;
        }

        /// <summary>The name a socket carries now, empty when it has none (tests and captures).</summary>
        public string SocketName(RoverSocket socket)
        {
            return _sockets[(int)socket].Name.text;
        }

        /// <summary>True when a socket shows the bay icon (tests and captures).</summary>
        public bool SocketShowsBay(RoverSocket socket)
        {
            return _sockets[(int)socket].Bay.style.display != DisplayStyle.None;
        }

        /// <summary>A parts-row card's element (tests and captures).</summary>
        public VisualElement CardElement(int index)
        {
            return _cards[index].Root;
        }

        /// <summary>How many cards the row shows (tests and captures).</summary>
        public int CardCount
        {
            get
            {
                int shown = 0;
                for (int i = 0; i < _cards.Length; i++)
                {
                    shown += _cards[i].Root.style.display != DisplayStyle.None ? 1 : 0;
                }

                return shown;
            }
        }

        /// <summary>The first card's name, for tests: the part waiting at row place <paramref name="index"/>.</summary>
        public string CardName(int index)
        {
            return _cards[index].Name.text;
        }

        public void MarkDirty()
        {
            _dirty = true;
        }

        /// <summary>The page was opened: forget a stale press and write everything.</summary>
        public void Opened()
        {
            FitRequested = false;
            EndDrag();
            HideTooltip();
            _dirty = true;
        }

        /// <summary>The page is showing (or not): the portrait renders only then.</summary>
        public void SetShown(bool shown)
        {
            _portraitSource.SetActive(shown);
        }

        /// <param name="device">The active device: its glyph for each part's key.</param>
        public void Tick(InputDeviceKind device)
        {
            if (device != _device)
            {
                _device = device;
                _dirty = true;
            }

            PlaceSockets();
            int signature = Signature();
            if (_dirty || signature != _signature)
            {
                _signature = signature;
                _dirty = false;
                Refresh();
            }
        }

        /// <summary>Puts each socket where its mount lands on the portrait now (it follows 07's bob); none where it cannot.</summary>
        private void PlaceSockets()
        {
            for (int i = 0; i < SocketCount; i++)
            {
                SocketView view = _sockets[i];
                bool placed = _portraitSource.TryGetSocketViewport(view.Id, out Vector2 uv);
                if (placed != view.Placed)
                {
                    view.Placed = placed;
                    view.Root.style.display = placed ? DisplayStyle.Flex : DisplayStyle.None;
                }

                if (placed && uv != view.At)
                {
                    view.At = uv;
                    view.Root.style.left = Length.Percent(uv.x * 100f);
                    view.Root.style.top = Length.Percent((1f - uv.y) * 100f);
                }
            }
        }

        /// <summary>
        /// Everything the page shows hangs on this: what is fitted, known, waiting and fittable. Computed each frame
        /// from the equipment's allocation-free reads, so the page writes only on a change.
        /// </summary>
        private int Signature()
        {
            int hash = _equipment.RowCount + 1;
            for (int i = 0; i < SocketCount; i++)
            {
                var socket = (RoverSocket)i;
                int state = _equipment.IsKnown(socket) ? 1 : 0;
                if (_equipment.TryGetFitted(socket, out RoverKitPiece part))
                {
                    state += 2 + (int)part * 4;
                }

                hash = hash * 31 + state;
            }

            for (int i = 0; i < _equipment.RowCount; i++)
            {
                RoverKitPiece part = _equipment.RowPart(i);
                hash = hash * 31 + (int)part * 2 + (_equipment.CanFit(part) ? 1 : 0);
            }

            return hash;
        }

        private void Refresh()
        {
            for (int i = 0; i < SocketCount; i++)
            {
                WriteSocket(_sockets[i]);
            }

            int count = _equipment.RowCount;
            for (int i = 0; i < _cards.Length; i++)
            {
                CardView card = _cards[i];
                bool shown = i < count;
                card.Root.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
                if (!shown)
                {
                    continue;
                }

                RoverKitPiece part = _equipment.RowPart(i);
                card.Part = part;
                card.Name.text = _localization.Get(PartInfo.NameKey(part));
                bool away = !_equipment.CanFit(part);
                card.Root.EnableInClassList(AwayClass, away);
                card.Bay.style.display = away ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private void WriteSocket(SocketView view)
        {
            RoverSocket id = view.Id;
            bool fitted = _equipment.TryGetFitted(id, out RoverKitPiece part);
            bool known = fitted || _equipment.IsKnown(id);
            view.Fitted = fitted;
            view.HasPart = false;
            if (known && !fitted)
            {
                for (int i = 0; i < _equipment.RowCount && !view.HasPart; i++)
                {
                    RoverKitPiece waiting = _equipment.RowPart(i);
                    if (_equipment.SocketOf(waiting) == id)
                    {
                        part = waiting;
                        view.HasPart = true;
                    }
                }

                if (!view.HasPart)
                {
                    view.HasPart = PartInfo.TryDefaultPart(id, out part);
                }
            }
            else
            {
                view.HasPart = fitted;
            }

            view.Part = part;
            view.Root.EnableInClassList(UnknownClass, !known);
            view.Root.EnableInClassList(FittedClass, fitted);
            view.Root.focusable = known && view.HasPart;
            view.Name.text = known && view.HasPart ? _localization.Get(PartInfo.NameKey(part)) : string.Empty;
            view.Bay.style.display = known && !fitted ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private SocketView BuildSocket(int index)
        {
            var root = new VisualElement { pickingMode = PickingMode.Position, focusable = false };
            root.AddToClassList(SocketClass);
            root.style.display = DisplayStyle.None;
            var dot = new VisualElement { pickingMode = PickingMode.Ignore };
            dot.AddToClassList("seven__socket-dot");
            var bay = new VisualElement { pickingMode = PickingMode.Ignore };
            bay.AddToClassList("seven__bay");
            new BayIconPainter(bay);
            var name = new Label { pickingMode = PickingMode.Ignore };
            name.AddToClassList("seven__socket-name");
            root.Add(dot);
            root.Add(bay);
            root.Add(name);
            _portrait.Add(root);
            var view = new SocketView { Id = (RoverSocket)index, Root = root, Name = name, Bay = bay };
            root.RegisterCallback<PointerEnterEvent>(_ => ShowSocketTooltip(view));
            root.RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());
            root.RegisterCallback<FocusInEvent>(_ => ShowSocketTooltip(view));
            root.RegisterCallback<FocusOutEvent>(_ => HideTooltip());
            root.RegisterCallback<PointerDownEvent>(e => PressDown(e, true, index, root));
            root.RegisterCallback<PointerMoveEvent>(Move);
            root.RegisterCallback<PointerUpEvent>(e => Release(e, root));
            root.RegisterCallback<PointerCaptureOutEvent>(_ => EndDrag());
            return view;
        }

        private CardView BuildCard(int index)
        {
            var root = new VisualElement { pickingMode = PickingMode.Position, focusable = true };
            root.AddToClassList(CardClass);
            var name = new Label { pickingMode = PickingMode.Ignore };
            name.AddToClassList("seven__card-name");
            var bay = new VisualElement { pickingMode = PickingMode.Ignore };
            bay.AddToClassList("seven__bay");
            new BayIconPainter(bay);
            root.Add(name);
            root.Add(bay);
            root.style.display = DisplayStyle.None;
            _row.Add(root);
            var view = new CardView { Root = root, Name = name, Bay = bay };
            root.RegisterCallback<PointerEnterEvent>(_ => ShowPartTooltip(view.Part, root));
            root.RegisterCallback<PointerLeaveEvent>(_ => HideTooltip());
            root.RegisterCallback<FocusInEvent>(_ => ShowPartTooltip(view.Part, root));
            root.RegisterCallback<FocusOutEvent>(_ => HideTooltip());
            root.RegisterCallback<NavigationSubmitEvent>(_ => FitFromRow(index));
            root.RegisterCallback<PointerDownEvent>(e => PressDown(e, false, index, root));
            root.RegisterCallback<PointerMoveEvent>(Move);
            root.RegisterCallback<PointerUpEvent>(e => Release(e, root));
            root.RegisterCallback<PointerCaptureOutEvent>(_ => EndDrag());
            return view;
        }

        private void ShowSocketTooltip(SocketView view)
        {
            if (view.HasPart && (view.Fitted || _equipment.IsKnown(view.Id)))
            {
                ShowPartTooltip(view.Part, view.Root);
            }
        }

        /// <summary>The part's name and, when it has one, its key; nothing else.</summary>
        private void ShowPartTooltip(RoverKitPiece part, VisualElement anchor)
        {
            _tooltipName.text = _localization.Get(PartInfo.NameKey(part));
            bool hasKey = PartInfo.TryAction(part, out RoverAction action);
            _tooltipGlyphHolder.style.display = hasKey ? DisplayStyle.Flex : DisplayStyle.None;
            if (hasKey)
            {
                _tooltipGlyph.Set(_glyphs.For(action, _device), _device);
            }

            Rect target = _panel.WorldToLocal(anchor.worldBound);
            _tooltip.style.left = target.xMin;
            _tooltip.style.top = target.yMax + _settings.TooltipGap;
            _tooltip.AddToClassList(TooltipShownClass);
        }

        private void HideTooltip()
        {
            _tooltip.RemoveFromClassList(TooltipShownClass);
        }

        private void PressDown(PointerDownEvent press, bool fromSocket, int index, VisualElement source)
        {
            if (press.button != 0)
            {
                return;
            }

            _pressed = true;
            _dragging = false;
            _dragFromSocket = fromSocket;
            _dragIndex = index;
            _pressAt = press.position;
            source.CapturePointer(press.pointerId);
            press.StopPropagation();
        }

        private void Move(PointerMoveEvent move)
        {
            if (!_pressed)
            {
                return;
            }

            if (!_dragging && (move.position - (Vector3)_pressAt).magnitude >= _settings.DragThreshold && CanBeginDrag())
            {
                BeginDrag();
            }

            if (_dragging)
            {
                Vector2 local = _panel.WorldToLocal(move.position);
                _ghost.style.left = local.x;
                _ghost.style.top = local.y;
            }
        }

        private void Release(PointerUpEvent up, VisualElement source)
        {
            if (!_pressed)
            {
                return;
            }

            bool wasDragging = _dragging;
            bool fromSocket = _dragFromSocket;
            int index = _dragIndex;
            Vector2 at = up.position;
            source.ReleasePointer(up.pointerId);
            EndDrag();
            up.StopPropagation();
            if (!wasDragging)
            {
                if (!fromSocket)
                {
                    FitFromRow(index);
                }

                return;
            }

            if (fromSocket)
            {
                if (_row.worldBound.Contains(at))
                {
                    _equipment.TryUnequip(_sockets[index].Id);
                }

                return;
            }

            SocketView over = SocketAt(at);
            if (over != null && _equipment.SocketOf(_cards[index].Part) == over.Id)
            {
                FitFromRow(index);
            }
        }

        private bool CanBeginDrag()
        {
            if (_dragFromSocket)
            {
                SocketView view = _sockets[_dragIndex];
                return view.Fitted && _equipment.CanFit(view.Part);
            }

            return _equipment.CanFit(_cards[_dragIndex].Part);
        }

        private void BeginDrag()
        {
            _dragging = true;
            RoverKitPiece part = _dragFromSocket ? _sockets[_dragIndex].Part : _cards[_dragIndex].Part;
            _ghost.text = _localization.Get(PartInfo.NameKey(part));
            _ghost.style.display = DisplayStyle.Flex;
            if (_dragFromSocket)
            {
                _row.AddToClassList(TargetClass);
                return;
            }

            _sockets[(int)_equipment.SocketOf(part)].Root.AddToClassList(TargetClass);
        }

        private void EndDrag()
        {
            _pressed = false;
            _dragging = false;
            _ghost.style.display = DisplayStyle.None;
            _row.RemoveFromClassList(TargetClass);
            for (int i = 0; i < SocketCount; i++)
            {
                _sockets[i].Root.RemoveFromClassList(TargetClass);
            }
        }

        private SocketView SocketAt(Vector2 world)
        {
            for (int i = 0; i < SocketCount; i++)
            {
                if (_sockets[i].Root.worldBound.Contains(world))
                {
                    return _sockets[i];
                }
            }

            return null;
        }

        /// <summary>One press on a waiting part: it is fitted when 07 rests at its station, otherwise nothing.</summary>
        private void FitFromRow(int index)
        {
            if (index < _equipment.RowCount && _equipment.CanFit(_equipment.RowPart(index)) &&
                _equipment.TryFit(index))
            {
                FitRequested = true;
            }
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"GameUI.uxml has no {typeof(T).Name} named '{name}' under '{root.name}'.");
        }

        private sealed class SocketView
        {
            public RoverSocket Id;
            public VisualElement Root;
            public Label Name;
            public VisualElement Bay;
            public RoverKitPiece Part;
            public bool HasPart;
            public bool Fitted;
            public bool Placed;
            public Vector2 At = new Vector2(float.NaN, float.NaN);
        }

        private sealed class CardView
        {
            public VisualElement Root;
            public Label Name;
            public VisualElement Bay;
            public RoverKitPiece Part;
        }
    }
}
