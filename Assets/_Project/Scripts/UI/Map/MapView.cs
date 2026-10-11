using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Core.Input;
using MoonProject.Core.Map;

namespace MoonProject.UI.Map
{
    /// <summary>
    /// The crew's map seen through the deck hatch (docs/features/M3-07): Ines's pencil sheet on Cream traced from the
    /// <see cref="MapSheet"/>, the light table glowing through it wherever the station reaches, a pencil ring and "?"
    /// for places only seen, pencil for places visited beyond reach, and the chart pen's warm ink (outline, the crew's
    /// name, tiny icons of what 07 did there) for places visited inside reach, drawn stroke by stroke as each
    /// <see cref="MapInkCued"/> arrives. Ines's tally (no total) and her notes beyond the rim. Look pans, the winch
    /// zooms between the whole basin and a closer view, the place nearest the view's centre is focused and Tether shows
    /// its one-line card. On the first look, once the ink rests, the view drifts close to each of Ines's notes beyond
    /// the rim in turn (step 3) unless the player has taken the view. Nothing points anywhere. Every stroke is tessellated when the map is built or opened; the open
    /// view only moves, reveals and fades, so a frame allocates nothing, and a closed map is not drawn at all.
    /// </summary>
    internal sealed class MapView : IDisposable
    {
        private const int RingSegments = 30;
        private const float RingOvershoot = 0.35f;
        private const int GlowSegments = 48;
        private const float GlowCore = 0.55f;
        private const float NameGap = 0.35f;
        private const float QuestionLift = 1.5f;
        private const float IconSize = 0.45f;
        private const float IconGap = 0.3f;
        private const float EdgeMargin = 0.04f;
        private const float RegionNoteWidth = 0.22f;
        private const int TallyGroup = 5;
        private const float TallyStrokeGap = 0.32f;
        private const float TallyGroupGap = 0.6f;
        private const float TallySlash = 0.9f;
        private const uint MarkSeed = 0xA511E5u;
        private const int ContourIndexEvery = 5;

        private readonly MapViewTuning _tuning;
        private readonly IMapKnowledge _knowledge;
        private readonly IStationReach _reach;
        private readonly ILocalization _text;
        private readonly InputReader _input;
        private readonly PaperSpace _space;
        private readonly List<IDisposable> _tokens = new List<IDisposable>();
        private readonly List<MapMark> _marks = new List<MapMark>();
        private readonly Dictionary<string, MapMark> _byId = new Dictionary<string, MapMark>(StringComparer.Ordinal);
        private readonly List<Vector2> _scratch = new List<Vector2>();
        private readonly StrokeBuffer _sheet = new StrokeBuffer();
        private readonly StrokeBuffer _glow = new StrokeBuffer();
        private readonly StrokeBuffer _pencil = new StrokeBuffer();
        private readonly StrokeBuffer _tallyStrokes = new StrokeBuffer();
        private readonly VisualElement _root;
        private readonly VisualElement _paper;
        private readonly VisualElement _inkLayer;
        private readonly VisualElement _tally;
        private readonly Label _title;
        private readonly Label _tallyLabel;
        private readonly Label _complete;
        private readonly VisualElement _card;
        private readonly Label _cardName;
        private readonly Label _cardLine;
        private readonly List<Label> _regionNotes = new List<Label>();
        private readonly List<Vector2> _edges = new List<Vector2>();
        private Vector2 _pan;
        private Vector2 _panTarget;
        private float _zoom;
        private float _zoomTarget;
        private float _open;
        private float _card01;
        private float _sinceInk;
        private bool _shown;
        private bool _displayed;
        private bool _firstLook;
        private int _edgeIndex;
        private bool _touched;
        private bool _zoomHeld;
        private bool _cardShown;
        private bool _inking;
        private MapMark _focus;

        public MapView(VisualElement before, MapSheet sheet, MapViewTuning tuning, IMapKnowledge knowledge,
            IStationReach reach, ILocalization text, EventBus events, InputReader input)
        {
            if (before == null || before.parent == null)
            {
                throw new ArgumentException("The map view needs the layer it sits under.", nameof(before));
            }

            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _knowledge = knowledge ?? throw new ArgumentNullException(nameof(knowledge));
            _reach = reach ?? throw new ArgumentNullException(nameof(reach));
            _text = text ?? throw new ArgumentNullException(nameof(text));
            _input = input ?? throw new ArgumentNullException(nameof(input));
            if (sheet == null || sheet.Validate() != null)
            {
                throw new ArgumentException($"The map sheet is missing or broken: {sheet?.Validate()}", nameof(sheet));
            }

            float size = tuning.PaperSize;
            _space = new PaperSpace(sheet.Centre, sheet.Radius, size);
            _root = Fill("crew-map");
            _root.style.backgroundColor = tuning.Veil;
            _root.style.display = DisplayStyle.None;
            _root.style.opacity = 0f;
            var viewport = Fill("crew-map-viewport");
            viewport.style.overflow = Overflow.Hidden;
            _root.Add(viewport);
            _paper = Box("crew-map-paper", size);
            _paper.style.backgroundColor = tuning.Paper;
            _paper.style.transformOrigin = new TransformOrigin(0f, 0f);
            viewport.Add(_paper);
            _paper.Add(Layer("crew-map-glow", size, _glow.Draw));
            _paper.Add(Layer("crew-map-sheet", size, _sheet.Draw));
            _paper.Add(Layer("crew-map-pencil", size, _pencil.Draw));
            _inkLayer = Layer("crew-map-ink", size, PaintInk);
            _paper.Add(_inkLayer);

            float margin = size * EdgeMargin;
            _title = NewLabel("crew-map-title", tuning.NoteSize, tuning.Pencil);
            _title.style.left = margin;
            _title.style.top = margin;
            _paper.Add(_title);
            _tally = new VisualElement { name = "crew-map-tally", pickingMode = PickingMode.Ignore };
            _tally.style.position = Position.Absolute;
            _tally.style.left = margin;
            _tally.style.bottom = margin;
            _tally.style.flexDirection = FlexDirection.Row;
            _tally.style.alignItems = Align.Center;
            _tallyLabel = NewLabel("crew-map-tally-label", tuning.NoteSize, tuning.Pencil);
            _tallyLabel.style.position = Position.Relative;
            _tally.Add(_tallyLabel);
            var strokes = new VisualElement { name = "crew-map-tally-strokes", pickingMode = PickingMode.Ignore };
            strokes.style.height = tuning.TallyHeight;
            strokes.style.width = tuning.TallyHeight * TallyGroup;
            strokes.generateVisualContent += _tallyStrokes.Draw;
            _tally.Add(strokes);
            _paper.Add(_tally);
            _complete = NewLabel("crew-map-complete", tuning.NoteSize, tuning.Pencil);
            _complete.style.left = margin;
            _complete.style.bottom = margin;
            _paper.Add(_complete);

            _card = new VisualElement { name = "crew-map-card", pickingMode = PickingMode.Ignore };
            _card.style.position = Position.Absolute;
            _card.style.left = Length.Percent(50f);
            _card.style.bottom = Length.Percent(8f);
            _card.style.translate = new Translate(Length.Percent(-50f), 0f);
            _card.style.backgroundColor = tuning.Paper;
            _card.style.opacity = 0f;
            _cardName = NewLabel("crew-map-card-name", tuning.CardNameSize, tuning.Ink);
            _cardName.style.position = Position.Relative;
            _cardLine = NewLabel("crew-map-card-line", tuning.CardLineSize, tuning.Pencil);
            _cardLine.style.position = Position.Relative;
            _card.Add(_cardName);
            _card.Add(_cardLine);
            _root.Add(_card);

            TraceSheet(sheet);
            PlaceMarks(sheet);
            Relabel();
            before.parent.Insert(before.parent.IndexOf(before), _root);

            _tokens.Add(events.Subscribe<MapLookStarted>(OnLookStarted));
            _tokens.Add(events.Subscribe<MapLookEnded>(OnLookEnded));
            _tokens.Add(events.Subscribe<MapInkCued>(OnInkCued));
            _tokens.Add(events.Subscribe<LanguageChanged>(OnLanguageChanged));
        }

        /// <summary>0 while closed, 1 while the view rests on the map (eased): the backdrop's blur follows it.</summary>
        public float Openness => UiEase.InOutSine(_open);

        /// <summary>The map is opening or open.</summary>
        public bool IsOpen => _shown;

        public VisualElement Root => _root;

        /// <summary>The place the view's centre rests on, or null.</summary>
        public MapMark Focus => _focus;

        public bool CardShown => _cardShown;

        /// <summary>The marks on the paper, in place order (tests and capture sessions).</summary>
        public IReadOnlyList<MapMark> Marks => _marks;

        /// <summary>Strokes in Ines's tally (tests).</summary>
        public int TallyStrokes => _tallyStrokes.QuadCount;

        /// <summary>The complete note shows instead of the tally (tests).</summary>
        public bool ShowsComplete => _complete.style.display == DisplayStyle.Flex;

        /// <summary>Advances the view; <paramref name="paused"/> freezes its input (the pause menu is on top).</summary>
        public void Tick(float deltaTime, bool paused)
        {
            float target = _shown ? 1f : 0f;
            _open = Mathf.MoveTowards(_open, target, deltaTime / _tuning.OpenSeconds);
            if (_open <= 0f && !_shown)
            {
                if (_displayed)
                {
                    _displayed = false;
                    _root.style.display = DisplayStyle.None;
                }

                return;
            }

            float eased = Openness;
            _root.style.opacity = eased;
            if (_shown && !paused)
            {
                ReadInput(deltaTime);
            }

            StepInk(deltaTime);
            StepView(deltaTime, eased);
            StepCard(deltaTime);
        }

        public void Dispose()
        {
            foreach (IDisposable token in _tokens)
            {
                token.Dispose();
            }

            _tokens.Clear();
            _root.RemoveFromHierarchy();
        }

        private void OnLookStarted(MapLookStarted look)
        {
            _shown = true;
            _firstLook = look.FirstLook;
            _edgeIndex = 0;
            _touched = false;
            _inking = false;
            _sinceInk = 0f;
            _zoom = 0f;
            _zoomTarget = 0f;
            _pan = _panTarget = Vector2.one * (_tuning.PaperSize * 0.5f);
            _cardShown = false;
            _focus = null;
            _root.style.display = DisplayStyle.Flex;
            _displayed = true;
            Refresh();
        }

        private void OnLookEnded(MapLookEnded ended)
        {
            _shown = false;
            _cardShown = false;
        }

        private void OnInkCued(MapInkCued cue)
        {
            if (!_byId.TryGetValue(cue.PlaceId, out MapMark mark))
            {
                return;
            }

            mark.Animating = true;
            mark.Reveal = 0f;
            mark.Since = 0f;
            mark.PencilName = false;
            _inking = true;
            _sinceInk = 0f;
        }

        private void OnLanguageChanged(LanguageChanged changed)
        {
            Relabel();
        }

        /// <summary>
        /// Reads what the station knows into the marks, the pencil, the glow and the tally: called as the view opens,
        /// never per frame.
        /// </summary>
        private void Refresh()
        {
            _pencil.Clear();
            _glow.Clear();
            int found = 0;
            int places = 0;
            foreach (MapMark mark in _marks)
            {
                MapPlace place = mark.Place;
                string id = place.Id;
                MapPlaceState state = _knowledge.GetState(id);
                bool drawn = _knowledge.IsInkDrawn(id) && _knowledge.IsInked(id);
                bool region = place.Kind == MapPlaceKind.Region;
                mark.Drawn = drawn;
                mark.Animating = false;
                mark.Reveal = drawn ? 1f : 0f;
                mark.Since = drawn ? float.MaxValue : 0f;
                mark.PencilName = !drawn && (state == MapPlaceState.Visited || place.Kind == MapPlaceKind.Home);
                mark.Question.style.display = state == MapPlaceState.Seen && !region ? DisplayStyle.Flex
                    : DisplayStyle.None;
                if (!drawn && state != MapPlaceState.Sketched)
                {
                    PencilMark(mark, place.Kind == MapPlaceKind.Relay && !IsLitMast(place.Position));
                }

                BuildInk(mark);
                if (!region && place.Kind != MapPlaceKind.Home)
                {
                    places++;
                    found += state == MapPlaceState.Visited ? 1 : 0;
                }
            }

            for (int i = 0; i < _reach.NodeCount; i++)
            {
                RelayNode node = _reach.GetNode(i);
                if (node.Lit)
                {
                    _glow.AddDisc(_space.ToPaper(node.Position), node.Radius * _space.UnitsPerMetre, GlowCore,
                        _tuning.Glow, GlowSegments);
                }
            }

            _pencil.Finish();
            _glow.Finish();
            BuildTally(found);
            bool complete = places > 0 && found == places;
            _tally.style.display = complete ? DisplayStyle.None : DisplayStyle.Flex;
            _complete.style.display = complete ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (VisualElement layer in _paper.Children())
            {
                layer.MarkDirtyRepaint();
            }

            ApplyMarkStyles();
        }

        private bool IsLitMast(Vector3 position)
        {
            for (int i = 1; i < _reach.NodeCount; i++)
            {
                RelayNode node = _reach.GetNode(i);
                if (node.Lit && (node.Position - position).sqrMagnitude < 1f)
                {
                    return true;
                }
            }

            return false;
        }

        private void ReadInput(float deltaTime)
        {
            float zoomFactor = Mathf.Lerp(1f, _tuning.CloseZoom, _zoom);
            float scale = PaperScale(zoomFactor);
            Vector2 delta = _input.LookDelta;
            Vector2 rate = _input.LookRate;
            Vector2 move = new Vector2(rate.x, -rate.y) * (_tuning.PanSpeed * deltaTime / zoomFactor);
            if (scale > 0f)
            {
                move += new Vector2(delta.x, -delta.y) / scale;
            }

            if (move.sqrMagnitude > 0f)
            {
                _touched = true;
                float size = _tuning.PaperSize;
                _panTarget = new Vector2(Mathf.Clamp(_panTarget.x + move.x, 0f, size),
                    Mathf.Clamp(_panTarget.y + move.y, 0f, size));
            }

            float winch = _input.Winch;
            bool pushed = Mathf.Abs(winch) >= _tuning.ZoomThreshold;
            if (pushed && !_zoomHeld)
            {
                _zoomTarget = winch > 0f ? 1f : 0f;
                _touched = true;
            }

            _zoomHeld = pushed;
            if (_input.TetherPressed)
            {
                ShowCard(!_cardShown && _focus != null);
            }
        }

        private void StepInk(float deltaTime)
        {
            bool animating = false;
            foreach (MapMark mark in _marks)
            {
                if (!mark.Animating)
                {
                    continue;
                }

                mark.Since += deltaTime;
                mark.Reveal = Mathf.Clamp01(mark.Since / _tuning.InkSeconds);
                float name = Mathf.Clamp01((mark.Since - _tuning.InkSeconds) / _tuning.NameFadeSeconds);
                mark.Name.style.opacity = name;
                mark.Name.style.color = _tuning.Ink;
                mark.Name.style.display = DisplayStyle.Flex;
                if (name >= 1f)
                {
                    mark.Animating = false;
                    mark.Drawn = true;
                }

                animating = true;
            }

            if (animating)
            {
                _inkLayer.MarkDirtyRepaint();
                _sinceInk = 0f;
                return;
            }

            _sinceInk += deltaTime;
            if (_inking && _firstLook && !_touched && _edgeIndex < _edges.Count
                && _sinceInk >= _tuning.EdgeDriftDelay)
            {
                _panTarget = _edges[_edgeIndex];
                _zoomTarget = 1f;
                _edgeIndex++;
                _sinceInk = 0f;
            }
        }

        private void StepView(float deltaTime, float eased)
        {
            float settle = 1f - Mathf.Exp(-deltaTime / _tuning.SettleSeconds);
            _pan = Vector2.Lerp(_pan, _panTarget, settle);
            _zoom = Mathf.Lerp(_zoom, _zoomTarget, settle);
            float zoomFactor = Mathf.Lerp(1f, _tuning.CloseZoom, _zoom);
            MapMark focus = Nearest(_pan, _tuning.FocusRadius / zoomFactor);
            if (focus != _focus)
            {
                _focus = focus;
                ShowCard(false);
            }

            float scale = PaperScale(zoomFactor) * Mathf.Lerp(_tuning.OpenScale, 1f, eased);
            Vector2 view = _root.layout.size;
            if (scale <= 0f || float.IsNaN(view.x))
            {
                return;
            }

            Vector2 offset = view * 0.5f - _pan * scale;
            _paper.style.translate = new Translate(offset.x, offset.y);
            _paper.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }

        private void StepCard(float deltaTime)
        {
            float target = _cardShown && _shown ? 1f : 0f;
            _card01 = Mathf.MoveTowards(_card01, target, deltaTime / _tuning.CardFadeSeconds);
            _card.style.opacity = UiEase.InOutSine(_card01);
        }

        /// <summary>Paper units to screen pixels at the far zoom times <paramref name="zoomFactor"/>.</summary>
        private float PaperScale(float zoomFactor)
        {
            Vector2 view = _root.layout.size;
            float shorter = Mathf.Min(view.x, view.y);
            return float.IsNaN(shorter) || shorter <= 0f ? 0f
                : shorter * _tuning.FitFraction / _tuning.PaperSize * zoomFactor;
        }

        /// <summary>The place on the paper nearest <paramref name="at"/> within <paramref name="radius"/>.</summary>
        private MapMark Nearest(Vector2 at, float radius)
        {
            MapMark nearest = null;
            float best = radius * radius;
            foreach (MapMark mark in _marks)
            {
                bool known = mark.Place.Kind == MapPlaceKind.Home || mark.Place.Kind == MapPlaceKind.Region
                    || _knowledge.GetState(mark.Place.Id) != MapPlaceState.Sketched;
                float distance = (mark.At - at).sqrMagnitude;
                if (known && distance <= best)
                {
                    best = distance;
                    nearest = mark;
                }
            }

            return nearest;
        }

        /// <summary>Shows the focused place's card (its name and one line), or hides it.</summary>
        private void ShowCard(bool show)
        {
            _cardShown = show && _focus != null;
            if (!_cardShown)
            {
                return;
            }

            string id = _focus.Place.Id;
            _cardName.text = Text(MapKeys.Prefix + id + MapKeys.NameSuffix);
            string line = _knowledge.GetStateLineKey(id);
            _cardLine.text = Text(line ?? MapKeys.Prefix + id + MapKeys.NoteSuffix);
        }

        private void PaintInk(MeshGenerationContext context)
        {
            foreach (MapMark mark in _marks)
            {
                if (mark.Drawn)
                {
                    mark.Ink.Draw(context);
                }
                else if (mark.Animating)
                {
                    mark.Ink.Write(context, Mathf.CeilToInt(mark.Reveal * mark.Ink.QuadCount), 1f);
                }
            }
        }

        /// <summary>Ines's lines, traced once: contours (every fifth darker), the rim, the canyon, the craters.</summary>
        private void TraceSheet(MapSheet sheet)
        {
            uint seed = (uint)sheet.Seed;
            float index = sheet.ContourInterval * ContourIndexEvery;
            for (int i = 0; i < sheet.LineCount; i++)
            {
                MapLine line = sheet.GetLine(i);
                _scratch.Clear();
                for (int p = 0; p < line.Count; p++)
                {
                    _scratch.Add(_space.ToPaper(sheet.GetPoint(line.Start + p)));
                }

                bool bold = line.Kind == MapLineKind.RimCrest || line.Kind == MapLineKind.CanyonWall
                    || line.Kind == MapLineKind.Chasm;
                bool indexContour = line.Kind == MapLineKind.Contour
                    && Mathf.Abs(Mathf.Repeat(line.Height + index * 0.5f, index) - index * 0.5f) < 0.01f;
                float alpha = bold ? 1f
                    : line.Kind == MapLineKind.Contour && !indexContour ? _tuning.ContourAlpha
                    : _tuning.IndexContourAlpha;
                Color pencil = _tuning.Pencil;
                pencil.a *= alpha;
                _sheet.AddLine(_scratch, line.Closed, bold ? _tuning.BoldWidth : _tuning.ThinWidth, pencil,
                    _tuning.Wobble, _tuning.WobbleLength, _tuning.Grain, seed * 31u + (uint)i);
            }

            _sheet.Finish();
        }

        /// <summary>A mark and its labels for every place on the map; the regions get Ines's edge notes.</summary>
        private void PlaceMarks(MapSheet sheet)
        {
            _edges.Clear();
            for (int i = 0; i < _knowledge.PlaceCount; i++)
            {
                MapPlace place = _knowledge.GetPlace(i);
                Vector2 at = _space.ToPaper(place.Position);
                float radius = Mathf.Max(_tuning.RingRadius, place.Radius * _space.UnitsPerMetre);
                Label name = NewLabel("crew-map-name", _tuning.NameSize, _tuning.Ink);
                name.style.left = at.x + radius * (1f + NameGap);
                name.style.top = at.y - _tuning.NameSize * 0.5f;
                Label question = NewLabel("crew-map-question", _tuning.NameSize, _tuning.Pencil);
                question.text = "?";
                question.style.left = at.x + radius * 0.5f;
                question.style.top = at.y - radius * QuestionLift - _tuning.NameSize * 0.5f;
                _paper.Add(name);
                _paper.Add(question);
                var mark = new MapMark(place, at, radius, name, question);
                _marks.Add(mark);
                _byId[place.Id] = mark;
                if (place.Kind == MapPlaceKind.Region)
                {
                    Label note = NewLabel("crew-map-note", _tuning.NoteSize, _tuning.Pencil);
                    note.style.left = at.x - _tuning.PaperSize * RegionNoteWidth * 0.5f;
                    note.style.top = at.y + radius * (1f + NameGap);
                    note.style.width = _tuning.PaperSize * RegionNoteWidth;
                    note.style.whiteSpace = WhiteSpace.Normal;
                    note.style.unityTextAlign = TextAnchor.UpperCenter;
                    note.userData = place.Id;
                    _paper.Add(note);
                    _regionNotes.Add(note);
                    _edges.Add(new Vector2(at.x, at.y + radius * (1f + NameGap) + _tuning.NoteSize));
                }
            }

        }

        /// <summary>A pencil ring for a place seen or visited beyond reach, a pencil cross for a dark mast.</summary>
        private void PencilMark(MapMark mark, bool darkMast)
        {
            uint seed = MarkSeed ^ StableHash(mark.Place.Id);
            if (darkMast)
            {
                Cross(_pencil, mark.At, _tuning.CrossSize, _tuning.ThinWidth * 1.5f, _tuning.Pencil, seed);
                return;
            }

            Ring(_pencil, mark.At, mark.Radius, _tuning.ThinWidth * 1.5f, _tuning.Pencil, seed);
        }

        /// <summary>The place's ink: its outline (a cross through a ring for a mast), then its deeds' icons.</summary>
        private void BuildInk(MapMark mark)
        {
            StrokeBuffer ink = mark.Ink;
            ink.Clear();
            uint seed = MarkSeed ^ StableHash(mark.Place.Id) ^ 0x1u;
            Ring(ink, mark.At, mark.Radius, _tuning.InkWidth, _tuning.Ink, seed);
            if (mark.Place.Kind == MapPlaceKind.Relay)
            {
                Cross(ink, mark.At, _tuning.CrossSize, _tuning.InkWidth, _tuning.Ink, seed);
            }

            MapPlaceDeeds deeds = _knowledge.GetDeeds(mark.Place.Id);
            float icon = _tuning.NameSize * IconSize;
            var at = new Vector2(mark.At.x + mark.Radius * (1f + NameGap) + icon * 0.5f,
                mark.At.y + _tuning.NameSize * 0.5f + icon);
            for (int bit = 0; bit < 4; bit++)
            {
                var deed = (MapPlaceDeeds)(1 << bit);
                if ((deeds & deed) == 0)
                {
                    continue;
                }

                Icon(ink, deed, at, icon);
                at.x += icon * (1f + IconGap);
            }

            ink.Finish();
        }

        private void Icon(StrokeBuffer buffer, MapPlaceDeeds deed, Vector2 at, float size)
        {
            float half = size * 0.5f;
            float width = _tuning.InkWidth * 0.6f;
            Color ink = _tuning.Ink;
            _scratch.Clear();
            switch (deed)
            {
                case MapPlaceDeeds.Relic:
                    _scratch.Add(at + new Vector2(0f, -half));
                    _scratch.Add(at + new Vector2(half, 0f));
                    _scratch.Add(at + new Vector2(0f, half));
                    _scratch.Add(at + new Vector2(-half, 0f));
                    buffer.AddLine(_scratch, true, width, ink, 0f, 1f, 0f, 0u);
                    break;
                case MapPlaceDeeds.Friend:
                    Ring(buffer, at, half, width, ink, (uint)deed);
                    Ring(buffer, at, half * 0.3f, width, ink, (uint)deed + 1u);
                    break;
                case MapPlaceDeeds.Blueprint:
                    _scratch.Add(at + new Vector2(-half, -half * 0.6f));
                    _scratch.Add(at + new Vector2(half, -half * 0.6f));
                    _scratch.Add(at + new Vector2(half, half * 0.6f));
                    _scratch.Add(at + new Vector2(-half, half * 0.6f));
                    buffer.AddLine(_scratch, true, width, ink, 0f, 1f, 0f, 0u);
                    break;
                case MapPlaceDeeds.Mast:
                    _scratch.Add(at + new Vector2(0f, half));
                    _scratch.Add(at + new Vector2(0f, -half * 0.4f));
                    buffer.AddLine(_scratch, false, width, ink, 0f, 1f, 0f, 0u);
                    Ring(buffer, at + new Vector2(0f, -half * 0.7f), half * 0.3f, width, ink, (uint)deed);
                    break;
            }
        }

        /// <summary>A hand ring: a little more than once round, from a seeded start.</summary>
        private void Ring(StrokeBuffer buffer, Vector2 centre, float radius, float width, Color color, uint seed)
        {
            _scratch.Clear();
            float start = StrokeBuffer.Noise01(seed, 0.5f) * Mathf.PI * 2f;
            float sweep = Mathf.PI * 2f * (1f + RingOvershoot / (Mathf.PI * 2f));
            for (int s = 0; s <= RingSegments; s++)
            {
                float angle = start + sweep * s / RingSegments;
                _scratch.Add(centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }

            buffer.AddLine(_scratch, false, width, color, _tuning.Wobble, _tuning.WobbleLength, _tuning.Grain, seed);
        }

        private void Cross(StrokeBuffer buffer, Vector2 centre, float size, float width, Color color, uint seed)
        {
            _scratch.Clear();
            _scratch.Add(centre + new Vector2(-size, -size));
            _scratch.Add(centre + new Vector2(size, size));
            buffer.AddLine(_scratch, false, width, color, _tuning.Wobble, _tuning.WobbleLength, _tuning.Grain, seed);
            _scratch.Clear();
            _scratch.Add(centre + new Vector2(size, -size));
            _scratch.Add(centre + new Vector2(-size, size));
            buffer.AddLine(_scratch, false, width, color, _tuning.Wobble, _tuning.WobbleLength, _tuning.Grain,
                seed + 1u);
        }

        /// <summary>Ines's tally: pencil strokes in fives, the fifth across the four, and no total.</summary>
        private void BuildTally(int found)
        {
            _tallyStrokes.Clear();
            float height = _tuning.TallyHeight;
            float gap = height * TallyStrokeGap;
            float x = 0f;
            for (int i = 0; i < found; i++)
            {
                int inGroup = i % TallyGroup;
                if (inGroup == TallyGroup - 1)
                {
                    float left = x - gap * (TallyGroup - 1);
                    _scratch.Clear();
                    _scratch.Add(new Vector2(left - gap * 0.5f, height * TallySlash));
                    _scratch.Add(new Vector2(x - gap * 0.5f, height * (1f - TallySlash)));
                    _tallyStrokes.AddLine(_scratch, false, _tuning.ThinWidth * 1.5f, _tuning.Pencil, _tuning.Wobble,
                        _tuning.WobbleLength, _tuning.Grain, MarkSeed + (uint)i);
                    x += height * TallyGroupGap;
                    continue;
                }

                _scratch.Clear();
                _scratch.Add(new Vector2(x, 0f));
                _scratch.Add(new Vector2(x, height));
                _tallyStrokes.AddLine(_scratch, false, _tuning.ThinWidth * 1.5f, _tuning.Pencil, _tuning.Wobble,
                    _tuning.WobbleLength, _tuning.Grain, MarkSeed + (uint)i);
                x += gap;
            }

            _tallyStrokes.Finish();
        }

        /// <summary>Every label's text in the current language (on build and on a language change).</summary>
        private void Relabel()
        {
            _title.text = Text(MapKeys.SheetTitle);
            _tallyLabel.text = Text(MapKeys.TallyLabel);
            _complete.text = Text(MapKeys.CompleteNote);
            foreach (MapMark mark in _marks)
            {
                mark.Name.text = Text(MapKeys.Prefix + mark.Place.Id + MapKeys.NameSuffix);
            }

            foreach (Label note in _regionNotes)
            {
                string id = (string)note.userData;
                note.text = Text(MapKeys.Prefix + id + MapKeys.NameSuffix) + "\n"
                    + Text(MapKeys.Prefix + id + MapKeys.NoteSuffix);
            }

            if (_cardShown)
            {
                ShowCard(true);
            }
        }

        /// <summary>Who shows a name now: ink once drawn, pencil once visited (and home), nothing otherwise.</summary>
        private void ApplyMarkStyles()
        {
            foreach (MapMark mark in _marks)
            {
                bool region = mark.Place.Kind == MapPlaceKind.Region;
                bool named = !region && (mark.Drawn || mark.PencilName);
                mark.Name.style.display = named ? DisplayStyle.Flex : DisplayStyle.None;
                mark.Name.style.opacity = 1f;
                mark.Name.style.color = mark.Drawn ? _tuning.Ink : _tuning.Pencil;
            }
        }

        /// <summary>FNV-1a of an id: the same hand for the same place in every session.</summary>
        private static uint StableHash(string id)
        {
            uint hash = 2166136261u;
            foreach (char c in id)
            {
                hash = (hash ^ c) * 16777619u;
            }

            return hash;
        }

        private string Text(string key)
        {
            return _text.TryGet(key, out string text) ? text : string.Empty;
        }

        private static VisualElement Fill(string name)
        {
            var element = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            element.style.position = Position.Absolute;
            element.style.left = 0f;
            element.style.top = 0f;
            element.style.right = 0f;
            element.style.bottom = 0f;
            return element;
        }

        private static VisualElement Box(string name, float size)
        {
            var element = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            element.style.position = Position.Absolute;
            element.style.left = 0f;
            element.style.top = 0f;
            element.style.width = size;
            element.style.height = size;
            return element;
        }

        private static VisualElement Layer(string name, float size, Action<MeshGenerationContext> paint)
        {
            VisualElement layer = Box(name, size);
            layer.generateVisualContent += paint;
            return layer;
        }

        private static Label NewLabel(string name, float size, Color color)
        {
            var label = new Label { name = name, pickingMode = PickingMode.Ignore };
            label.style.position = Position.Absolute;
            label.style.fontSize = size;
            label.style.color = color;
            label.style.whiteSpace = WhiteSpace.NoWrap;
            return label;
        }
    }
}
