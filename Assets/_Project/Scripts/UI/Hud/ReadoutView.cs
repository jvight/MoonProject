using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// 07's readout on the screen, high on the left like its other plates (docs/design/opening-v2.md §10, §2): the
    /// protocol line when the base gets power or hears the west, the lander's reserve once it gave out, one system's
    /// line when the cutter mends or refuses, and the text of a scanned panel. The boot diagnostics are not here: the
    /// New Game wake prints them on 07's own face, so the first objective after it stands alone. One card at a time, in the guidance slot's prompt kind, so the objective line waits for it.
    /// Lines are written when a card starts or changes; a frame only eases the panel and lets flashes die down.
    /// </summary>
    internal sealed class ReadoutView
    {
        private const string LineClass = "readout__line";
        private const string FaultClass = "readout__line--fault";
        private const string DimClass = "readout__line--dim";
        private const string FlashClass = "readout__line--flash";

        private readonly GuidanceSettings _settings;
        private readonly ILocalization _localization;
        private readonly IRoverSystems _systems;
        private readonly BuildField _build;
        private readonly OpeningProtocol _protocol;
        private readonly ReadoutQueue _queue;
        private readonly ReadoutText _text;
        private readonly ReadoutContent _content = new ReadoutContent();
        private readonly Reveal _reveal;
        private readonly Label[] _lines = new Label[ReadoutContent.MaxLines];
        private readonly float[] _flash = new float[ReadoutContent.MaxLines];
        private int _reservePercent;

        /// <param name="build">The base building service, or null in a rig without one (no blueprints are known).</param>
        public ReadoutView(VisualElement hud, GuidanceSettings settings, ILocalization localization,
            IRoverSystems systems, BuildField build, OpeningProtocol protocol)
        {
            if (hud == null)
            {
                throw new ArgumentNullException(nameof(hud));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _systems = systems ?? throw new ArgumentNullException(nameof(systems));
            _protocol = protocol ?? throw new ArgumentNullException(nameof(protocol));
            _build = build;
            _queue = new ReadoutQueue(settings);
            _text = new ReadoutText(localization);
            VisualElement root = Require<VisualElement>(hud, "readout");
            VisualElement lines = Require<VisualElement>(root, "readout-lines");
            _reveal = new Reveal(root, settings.Readout);
            _reveal.Snap(false);
            for (int i = 0; i < _lines.Length; i++)
            {
                _lines[i] = new Label { pickingMode = PickingMode.Ignore };
                _lines[i].AddToClassList(LineClass);
                lines.Add(_lines[i]);
            }
        }

        /// <summary>True from the moment a card is asked for until its fade is over: it wants the guidance slot.</summary>
        public bool Wants => _queue.Wants;

        public bool IsVisible => !_reveal.IsHidden;

        /// <summary>The plate the card sits on (tests and captures).</summary>
        public VisualElement Panel => _reveal.Element;

        /// <summary>The kind of card on the screen or easing out.</summary>
        public ReadoutKind Showing => _queue.Current.Kind;

        /// <summary>The number of lines of the card on the screen.</summary>
        public int LineCount => _content.Count;

        public string LineText(int index) => _lines[index].text;

        /// <summary>True while line <paramref name="index"/> is flashing after a change or a refusal.</summary>
        public bool IsFlashing(int index) => _flash[index] > 0f;

        /// <summary>The protocol line changed by a milestone reached now (not by a replayed one).</summary>
        public void ProtocolChanged()
        {
            _queue.Offer(new ReadoutRequest(ReadoutKind.Protocol));
        }

        /// <summary>The lander's cells gave out: the reserve card shows once.</summary>
        public void ReserveEmpty(int percent)
        {
            _reservePercent = percent;
            _queue.Offer(new ReadoutRequest(ReadoutKind.Reserve));
        }

        /// <summary>A story panel was scanned: its text shows.</summary>
        public void StoryScanned(string textKey)
        {
            _queue.Offer(new ReadoutRequest(ReadoutKind.Story, textKey: textKey));
        }

        /// <summary>
        /// Piece <paramref name="step"/> of the <paramref name="of"/> in <paramref name="nameKey"/>'s scan set is done:
        /// a short card says how far its blueprint has come (and learned, on the last).
        /// </summary>
        public void ScanSetProgress(string nameKey, int step, int of)
        {
            if (_queue.Offer(new ReadoutRequest(ReadoutKind.ScanSet, textKey: nameKey, step: step, of: of)))
            {
                Write(_queue.Current);
            }
        }

        /// <summary>A system changed now: its line shows on a card of its own.</summary>
        public void SystemChanged(RoverSystem system)
        {
            ShowSystem(system);
        }

        /// <summary>A first-use chip wants the slot: any system card yields to it (<see cref="ReadoutQueue.YieldSystemCards"/>).</summary>
        public void YieldSystemCards()
        {
            _queue.YieldSystemCards();
        }

        /// <summary>The faulted cutter refused a cut: its line flashes.</summary>
        public void CutterRefused()
        {
            ShowSystem(RoverSystem.Cutter);
        }

        /// <param name="deltaTime">Unscaled seconds; 0 while paused.</param>
        /// <param name="allowed">The guidance slot lets a card show now.</param>
        public void Tick(float deltaTime, bool allowed)
        {
            if (_queue.Tick(deltaTime, allowed, _reveal.IsHidden))
            {
                Write(_queue.Current);
                if (_queue.Current.Kind == ReadoutKind.System)
                {
                    Flash(0);
                }
            }

            _reveal.Set(_queue.IsShowing && allowed);
            _reveal.Tick(deltaTime);
            for (int i = 0; i < _content.Count; i++)
            {
                if (_flash[i] > 0f)
                {
                    _flash[i] -= deltaTime;
                    if (_flash[i] <= 0f)
                    {
                        _flash[i] = 0f;
                        _lines[i].RemoveFromClassList(FlashClass);
                    }
                }
            }
        }

        public void Relocalize()
        {
            if (_queue.Current.Kind != ReadoutKind.None)
            {
                Write(_queue.Current);
            }
        }

        private void ShowSystem(RoverSystem system)
        {
            var request = new ReadoutRequest(ReadoutKind.System, system);
            if (_queue.Offer(request))
            {
                Write(request);
                Flash(0);
            }
        }

        private void Write(ReadoutRequest request)
        {
            switch (request.Kind)
            {
                case ReadoutKind.System:
                    RoverSystemState state = _systems.StateOf(request.System);
                    _content.Clear();
                    _content.Add(_text.SystemLine(request.System, state, Blueprints(),
                        request.System == RoverSystem.Cutter && _systems.CanFitIgniter), ReadoutText.StyleOf(state));
                    break;
                case ReadoutKind.Protocol:
                    _text.Protocol(_content, _protocol.Line);
                    break;
                case ReadoutKind.Reserve:
                    _text.Reserve(_content, _reservePercent);
                    break;
                case ReadoutKind.Story:
                    _text.Story(_content, request.TextKey);
                    break;
                case ReadoutKind.ScanSet:
                    _text.ScanSet(_content, request.TextKey, request.Step, request.Of);
                    break;
            }

            for (int i = 0; i < _lines.Length; i++)
            {
                _flash[i] = 0f;
                _lines[i].RemoveFromClassList(FlashClass);
                Apply(i);
            }
        }

        private void Apply(int index)
        {
            Label line = _lines[index];
            if (index >= _content.Count)
            {
                line.style.display = DisplayStyle.None;
                return;
            }

            line.style.display = DisplayStyle.Flex;
            line.text = _content.TextAt(index);
            ReadoutLineStyle style = _content.StyleAt(index);
            line.EnableInClassList(FaultClass, style == ReadoutLineStyle.Fault);
            line.EnableInClassList(DimClass, style == ReadoutLineStyle.Dim);
        }

        private void Flash(int index)
        {
            if (index < 0 || index >= _content.Count)
            {
                return;
            }

            _flash[index] = _settings.FlashSeconds;
            _lines[index].AddToClassList(FlashClass);
        }

        private int Blueprints()
        {
            return _build != null ? _build.Known.Count : 0;
        }

        private static T Require<T>(VisualElement root, string name) where T : VisualElement
        {
            return root.Q<T>(name) ?? throw new InvalidOperationException(
                $"GameUI.uxml has no {typeof(T).Name} named '{name}' under '{root.name}'.");
        }
    }
}
