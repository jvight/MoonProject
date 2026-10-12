using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.UI
{
    /// <summary>
    /// The pickup feed (Subnautica's pickup notices, docs/features/M3-13): no permanent HUD for the stock. When 07's
    /// metal, wiring or optics grow, a small glass row for that material slides in at the left with its icon, its name,
    /// a "+N" and the new total; the "+N" and the total roll one unit at a time (<see cref="MaterialTally"/>), the
    /// icon gives a small pulse for each, and the row lingers a few seconds after the count settles and fades away.
    /// More of the same material while its row is up adds to its "+N"; the three materials' rows stack, so at most
    /// three are up. Changes before the UI's first frame are a loaded save: they are taken as they are, with no row.
    /// Each unit rolled publishes a <see cref="StockCountTicked"/>, so Audio can tick and Rover can rumble in step.
    /// </summary>
    internal sealed class PickupFeed
    {
        public const string RowClass = "pickup-row";
        private const float WriteEpsilon = 1e-3f;

        private readonly PickupFeedSettings _settings;
        private readonly EventBus _events;
        private readonly ILocalization _localization;
        private readonly IntText _numbers;
        private readonly MaterialTally _tally;
        private readonly Row[] _rows = new Row[Materials.Count];
        private bool _armed;

        public PickupFeed(UiLayout layout, PickupFeedSettings settings, IntText numbers, ILocalization localization,
            EventBus events)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _tally = new MaterialTally(settings);
            layout.PickupFeed.Clear();
            for (int i = 0; i < Materials.Count; i++)
            {
                _rows[i] = BuildRow(layout.PickupFeed, Materials.At(i));
            }

            Relocalize();
        }

        /// <summary>True while any row is up (or easing).</summary>
        public bool IsVisible
        {
            get
            {
                for (int i = 0; i < _rows.Length; i++)
                {
                    if (!_rows[i].Reveal.IsHidden)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>True while <paramref name="material"/>'s row is up (or easing).</summary>
        public bool IsShowing(SalvageMaterial material)
        {
            return !_rows[(int)material].Reveal.IsHidden;
        }

        /// <summary>The number on <paramref name="material"/>'s row (or counting) right now.</summary>
        public int Shown(SalvageMaterial material)
        {
            return _tally.Shown(material);
        }

        /// <summary>The "+N" written on <paramref name="material"/>'s row (tests and captures).</summary>
        public string GainText(SalvageMaterial material)
        {
            return _rows[(int)material].Gain.text;
        }

        /// <summary>The total written on <paramref name="material"/>'s row (tests and captures).</summary>
        public string TotalText(SalvageMaterial material)
        {
            return _rows[(int)material].Total.text;
        }

        /// <summary>The name written on <paramref name="material"/>'s row (tests and captures).</summary>
        public string NameText(SalvageMaterial material)
        {
            return _rows[(int)material].Name.text;
        }

        /// <summary>Writes the materials' names again (a new language).</summary>
        public void Relocalize()
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i].Name.text = _localization.Get(UiKeys.MaterialName(Materials.At(i)));
            }
        }

        /// <summary>The stock changed: before the first frame (a load) it is taken as it is; after, shown.</summary>
        public void Change(int metal, int wiring, int optics)
        {
            if (!_armed)
            {
                _tally.Snap(metal, wiring, optics);
                WriteAll();
                return;
            }

            Note(SalvageMaterial.Metal, metal);
            Note(SalvageMaterial.Wiring, wiring);
            Note(SalvageMaterial.Optics, optics);
            _tally.Apply(metal, wiring, optics);
        }

        /// <param name="deltaTime">Unscaled seconds since the last tick.</param>
        public void Tick(float deltaTime)
        {
            _armed = true;
            if (_tally.Step(deltaTime))
            {
                PublishTicks();
            }

            for (int i = 0; i < _rows.Length; i++)
            {
                TickRow(_rows[i], Materials.At(i), deltaTime);
            }
        }

        private Row BuildRow(VisualElement feed, SalvageMaterial material)
        {
            var root = new VisualElement { pickingMode = PickingMode.Ignore };
            root.AddToClassList(RowClass);
            var body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.AddToClassList(RowClass + "__body");
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList(MaterialSlots.IconClass);
            icon.AddToClassList(MaterialSlots.IconClass + "--" + Materials.ClassSuffix(material));
            new MaterialIconPainter(icon, material);
            var name = new Label { pickingMode = PickingMode.Ignore };
            name.AddToClassList(RowClass + "__name");
            var plus = new Label("+") { pickingMode = PickingMode.Ignore };
            plus.AddToClassList(RowClass + "__plus");
            var gain = new Label { pickingMode = PickingMode.Ignore };
            gain.AddToClassList(RowClass + "__gain");
            var total = new Label { pickingMode = PickingMode.Ignore };
            total.AddToClassList(RowClass + "__total");
            body.Add(icon);
            body.Add(name);
            body.Add(plus);
            body.Add(gain);
            body.Add(total);
            root.Add(body);
            feed.Add(root);
            var reveal = new Reveal(root, _settings.Reveal);
            reveal.Snap(false);
            return new Row(reveal, body, icon, name, gain, total);
        }

        /// <summary>Opens (or feeds) a row when <paramref name="amount"/> is a gain; a spend closes nothing.</summary>
        private void Note(SalvageMaterial material, int amount)
        {
            Row row = _rows[(int)material];
            int target = _tally.Target(material);
            if (amount > target)
            {
                if (!row.Reveal.Target)
                {
                    row.Base = target;
                    row.Reveal.Show();
                }

                row.Linger = 0f;
            }
            else if (amount < target)
            {
                row.Base = Mathf.Min(row.Base, amount);
            }
        }

        private void TickRow(Row row, SalvageMaterial material, float deltaTime)
        {
            if (row.Reveal.Target)
            {
                WriteRow(row, material);
                if (_tally.IsCountSettled(material))
                {
                    row.Linger += deltaTime;
                    if (row.Linger >= _settings.LingerSeconds)
                    {
                        row.Reveal.Hide();
                    }
                }
            }

            row.Reveal.Tick(deltaTime);
            if (row.Reveal.IsHidden)
            {
                return;
            }

            float slide = -_settings.SlideDistance * (1f - Mathf.Clamp01(row.Reveal.Visibility));
            if (Mathf.Abs(slide - row.WrittenSlide) > WriteEpsilon)
            {
                row.Body.style.translate = new StyleTranslate(new Translate(slide, 0f));
                row.WrittenSlide = slide;
            }

            float grown = 1f + _tally.TickPulse(material) * _settings.TickPulseScale;
            if (Mathf.Abs(grown - row.WrittenScale) > WriteEpsilon)
            {
                row.Icon.style.scale = new StyleScale(new Scale(new Vector2(grown, grown)));
                row.WrittenScale = grown;
            }
        }

        private void WriteAll()
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                WriteRow(_rows[i], Materials.At(i));
            }
        }

        private void WriteRow(Row row, SalvageMaterial material)
        {
            int total = _tally.Shown(material);
            if (total != row.WrittenTotal)
            {
                row.Total.text = _numbers.Get(total);
                row.WrittenTotal = total;
            }

            int gain = Mathf.Max(0, total - row.Base);
            if (gain != row.WrittenGain)
            {
                row.Gain.text = _numbers.Get(gain);
                row.WrittenGain = gain;
            }
        }

        /// <summary>One event per unit a rising count just rolled; a spend rolls down in silence.</summary>
        private void PublishTicks()
        {
            for (int i = 0; i < Materials.Count; i++)
            {
                SalvageMaterial material = Materials.At(i);
                int units = _tally.Moved(material);
                if (units <= 0)
                {
                    continue;
                }

                int last = _tally.Rolled(material) - 1;
                int remaining = _tally.Remaining(material);
                for (int unit = 0; unit < units; unit++)
                {
                    int toCome = remaining + units - 1 - unit;
                    _events.Publish(new StockCountTicked(material, last - (units - 1 - unit), toCome));
                }
            }
        }

        private sealed class Row
        {
            public Row(Reveal reveal, VisualElement body, VisualElement icon, Label name, Label gain, Label total)
            {
                Reveal = reveal;
                Body = body;
                Icon = icon;
                Name = name;
                Gain = gain;
                Total = total;
            }

            public Reveal Reveal { get; }

            public VisualElement Body { get; }

            public VisualElement Icon { get; }

            public Label Name { get; }

            public Label Gain { get; }

            public Label Total { get; }

            /// <summary>The total the row opened on: its "+N" counts up from here.</summary>
            public int Base { get; set; }

            public float Linger { get; set; }

            public int WrittenTotal { get; set; } = -1;

            public int WrittenGain { get; set; } = -1;

            public float WrittenSlide { get; set; } = float.NaN;

            public float WrittenScale { get; set; } = float.NaN;
        }
    }
}
