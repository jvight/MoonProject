using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.Core.Events;
using MoonProject.Gameplay;

namespace MoonProject.UI
{
    /// <summary>
    /// The pickup feed (Subnautica's pickup notices, docs/features/M3-13): no permanent HUD for the stock. When any
    /// material of 07's stock (home and hold together) grows, a small glass row for that material slides in at the left
    /// with its icon, its name, a "+N" and the new total; the "+N" and the total roll one unit at a time
    /// (<see cref="MaterialTally"/>), the icon gives a small pulse for each, and the row lingers a few seconds after the
    /// count settles and fades away. More of the same material while its row is up adds to its "+N". A few rows are
    /// shared by every material (<see cref="PickupFeedSettings.MaxRows"/>): a new one takes a free row, or the row that
    /// has rested longest. The stock is read each frame, so a load is taken as it is, with no row, and moving units
    /// from the hold to home's shelves (the total does not change) shows nothing. Only the materials the catalog
    /// describes in this version are read. Each unit rolled publishes a
    /// <see cref="StockCountTicked"/>, so Audio can tick and Rover can rumble in step.
    /// </summary>
    internal sealed class PickupFeed
    {
        public const string RowClass = "pickup-row";
        private const float WriteEpsilon = 1e-3f;

        private readonly PickupFeedSettings _settings;
        private readonly EventBus _events;
        private readonly ILocalization _localization;
        private readonly IntText _numbers;
        private readonly IMaterialStock _stock;
        private readonly IMaterialCatalog _catalog;
        private readonly MaterialTally _tally;
        private readonly Row[] _rows;
        private readonly RoverDiagramData _diagram;
        private readonly PartRows _partLogic;
        private readonly PartRow[] _partRows;
        private bool _armed;

        public PickupFeed(UiLayout layout, PickupFeedSettings settings, IntText numbers, ILocalization localization,
            IMaterialStock stock, IMaterialCatalog catalog, EventBus events, RoverDiagramData diagram)
        {
            if (layout == null)
            {
                throw new ArgumentNullException(nameof(layout));
            }

            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _numbers = numbers ?? throw new ArgumentNullException(nameof(numbers));
            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            _stock = stock ?? throw new ArgumentNullException(nameof(stock));
            _catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            _events = events ?? throw new ArgumentNullException(nameof(events));
            _diagram = diagram ?? throw new ArgumentNullException(nameof(diagram));
            _tally = new MaterialTally(settings);
            layout.PickupFeed.Clear();
            _rows = new Row[settings.MaxRows];
            for (int i = 0; i < _rows.Length; i++)
            {
                _rows[i] = BuildRow(layout.PickupFeed);
            }

            _partLogic = new PartRows(settings.PartRows, settings.PartLingerSeconds);
            _partRows = new PartRow[settings.PartRows];
            for (int i = 0; i < _partRows.Length; i++)
            {
                _partRows[i] = BuildPartRow(layout.PickupFeed);
            }
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

                for (int i = 0; i < _partRows.Length; i++)
                {
                    if (!_partRows[i].Reveal.IsHidden)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        /// <summary>True while story part <paramref name="partId"/> has a row up (or easing).</summary>
        public bool IsShowingPart(string partId)
        {
            PartRow row = ShowingPart(partId);
            return row != null && !row.Reveal.IsHidden;
        }

        /// <summary>The words on story part <paramref name="partId"/>'s row (tests and captures).</summary>
        public string PartText(string partId)
        {
            return ShowingPart(partId).Name.text;
        }

        /// <summary>
        /// 07 took story part <paramref name="partId"/> into its hold: the same glass row as a material, with the part's
        /// icon and "+ name" (no count: a part is one thing).
        /// </summary>
        public void PartCollected(string partId)
        {
            int index = _partLogic.Claim(partId, out bool opened);
            PartRow row = _partRows[index];
            if (opened)
            {
                row.PartId = partId;
                row.Shape = _diagram.GetPartIcon(partId);
                row.Icon.MarkDirtyRepaint();
                WritePartName(row);
                row.Reveal.Snap(false);
            }

            row.Reveal.Show();
        }

        /// <summary>True while <paramref name="material"/> has a row up (or easing).</summary>
        public bool IsShowing(SalvageMaterial material)
        {
            Row row = Showing(material);
            return row != null && !row.Reveal.IsHidden;
        }

        /// <summary>The number on <paramref name="material"/>'s row (or counting) right now.</summary>
        public int Shown(SalvageMaterial material)
        {
            return _tally.Shown(material);
        }

        /// <summary>The "+N" written on <paramref name="material"/>'s row (tests and captures).</summary>
        public string GainText(SalvageMaterial material)
        {
            return Showing(material).Gain.text;
        }

        /// <summary>The total written on <paramref name="material"/>'s row (tests and captures).</summary>
        public string TotalText(SalvageMaterial material)
        {
            return Showing(material).Total.text;
        }

        /// <summary>The name written on <paramref name="material"/>'s row (tests and captures).</summary>
        public string NameText(SalvageMaterial material)
        {
            return Showing(material).Name.text;
        }

        /// <summary>Writes the materials' names again (a new language).</summary>
        public void Relocalize()
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                if (_rows[i].Active)
                {
                    _rows[i].Name.text = _localization.Get(_catalog.NameKey(_rows[i].Material));
                }
            }

            for (int i = 0; i < _partRows.Length; i++)
            {
                if (_partLogic.IsActive(i))
                {
                    WritePartName(_partRows[i]);
                }
            }
        }

        /// <param name="deltaTime">Unscaled seconds since the last tick.</param>
        public void Tick(float deltaTime)
        {
            if (!_armed)
            {
                for (int i = 0; i < Materials.Count; i++)
                {
                    SalvageMaterial material = Materials.At(i);
                    if (_catalog.Contains(material))
                    {
                        _tally.Snap(material, _stock.Of(material));
                    }
                }

                _armed = true;
            }
            else
            {
                for (int i = 0; i < Materials.Count; i++)
                {
                    SalvageMaterial material = Materials.At(i);
                    if (!_catalog.Contains(material))
                    {
                        continue;
                    }

                    int amount = _stock.Of(material);
                    if (amount != _tally.Target(material))
                    {
                        Note(material, amount);
                        _tally.Apply(material, amount);
                    }
                }
            }

            if (_tally.Step(deltaTime))
            {
                PublishTicks();
            }

            for (int i = 0; i < _rows.Length; i++)
            {
                TickRow(_rows[i], deltaTime);
            }

            TickParts(deltaTime);
        }

        private void TickParts(float deltaTime)
        {
            _partLogic.Tick(deltaTime);
            for (int i = 0; i < _partRows.Length; i++)
            {
                if (!_partLogic.IsActive(i))
                {
                    continue;
                }

                PartRow row = _partRows[i];
                if (row.Reveal.Target && _partLogic.ShouldFade(i))
                {
                    row.Reveal.Hide();
                }

                row.Reveal.Tick(deltaTime);
                if (row.Reveal.IsHidden && !row.Reveal.Target)
                {
                    _partLogic.Release(i);
                    row.PartId = null;
                    continue;
                }

                float slide = -_settings.SlideDistance * (1f - Mathf.Clamp01(row.Reveal.Visibility));
                if (Mathf.Abs(slide - row.WrittenSlide) > WriteEpsilon)
                {
                    row.Body.style.translate = new StyleTranslate(new Translate(slide, 0f));
                    row.WrittenSlide = slide;
                }
            }
        }

        private PartRow BuildPartRow(VisualElement feed)
        {
            var root = new VisualElement { pickingMode = PickingMode.Ignore };
            root.AddToClassList(RowClass);
            var body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.AddToClassList(RowClass + "__body");
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList(RowClass + "__part-icon");
            var name = new Label { pickingMode = PickingMode.Ignore };
            name.AddToClassList(RowClass + "__name");
            body.Add(icon);
            body.Add(name);
            root.Add(body);
            feed.Add(root);
            var reveal = new Reveal(root, _settings.Reveal);
            reveal.Snap(false);
            var row = new PartRow(reveal, body, icon, name);
            icon.generateVisualContent += row.PaintIcon;
            return row;
        }

        private PartRow ShowingPart(string partId)
        {
            for (int i = 0; i < _partRows.Length; i++)
            {
                if (string.Equals(_partRows[i].PartId, partId, StringComparison.Ordinal))
                {
                    return _partRows[i];
                }
            }

            return null;
        }

        private void WritePartName(PartRow row)
        {
            row.Name.text = string.Concat("+ ", _localization.Get(UiKeys.PickupName(row.PartId)));
        }

        private Row BuildRow(VisualElement feed)
        {
            var root = new VisualElement { pickingMode = PickingMode.Ignore };
            root.AddToClassList(RowClass);
            var body = new VisualElement { pickingMode = PickingMode.Ignore };
            body.AddToClassList(RowClass + "__body");
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList(MaterialSlots.IconClass);
            var painter = new MaterialIconPainter(icon, SalvageMaterial.Metal);
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
            return new Row(reveal, body, icon, painter, name, gain, total);
        }

        private Row Showing(SalvageMaterial material)
        {
            for (int i = 0; i < _rows.Length; i++)
            {
                if (_rows[i].Active && _rows[i].Material == material)
                {
                    return _rows[i];
                }
            }

            return null;
        }

        /// <summary>The row <paramref name="material"/> takes: its own, a free one, or the one resting longest.</summary>
        private Row Claim(SalvageMaterial material)
        {
            Row own = Showing(material);
            if (own != null)
            {
                return own;
            }

            Row oldest = null;
            for (int i = 0; i < _rows.Length; i++)
            {
                if (!_rows[i].Active)
                {
                    return Assign(_rows[i], material);
                }

                if (oldest == null || _rows[i].Linger > oldest.Linger)
                {
                    oldest = _rows[i];
                }
            }

            oldest.Reveal.Snap(false);
            return Assign(oldest, material);
        }

        private Row Assign(Row row, SalvageMaterial material)
        {
            row.Active = true;
            row.Material = material;
            row.Linger = 0f;
            row.Base = _tally.Target(material);
            row.WrittenTotal = -1;
            row.WrittenGain = -1;
            row.Painter.SetMaterial(material);
            row.Name.text = _localization.Get(_catalog.NameKey(material));
            return row;
        }

        /// <summary>Opens (or feeds) a row when <paramref name="amount"/> is a gain; a spend closes nothing.</summary>
        private void Note(SalvageMaterial material, int amount)
        {
            int target = _tally.Target(material);
            if (amount > target)
            {
                Row row = Claim(material);
                if (!row.Reveal.Target)
                {
                    row.Reveal.Show();
                }

                row.Linger = 0f;
            }
            else
            {
                Row row = Showing(material);
                if (row != null)
                {
                    row.Base = Mathf.Min(row.Base, amount);
                }
            }
        }

        private void TickRow(Row row, float deltaTime)
        {
            if (!row.Active)
            {
                return;
            }

            if (row.Reveal.Target)
            {
                WriteRow(row);
                if (_tally.IsCountSettled(row.Material))
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
                row.Active = row.Reveal.Target;
                return;
            }

            float slide = -_settings.SlideDistance * (1f - Mathf.Clamp01(row.Reveal.Visibility));
            if (Mathf.Abs(slide - row.WrittenSlide) > WriteEpsilon)
            {
                row.Body.style.translate = new StyleTranslate(new Translate(slide, 0f));
                row.WrittenSlide = slide;
            }

            float grown = 1f + _tally.TickPulse(row.Material) * _settings.TickPulseScale;
            if (Mathf.Abs(grown - row.WrittenScale) > WriteEpsilon)
            {
                row.Icon.style.scale = new StyleScale(new Scale(new Vector2(grown, grown)));
                row.WrittenScale = grown;
            }
        }

        private void WriteRow(Row row)
        {
            int total = _tally.Shown(row.Material);
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

        /// <summary>A story part's row: the part's diagram icon and "+ name" on the same glass as a material row.</summary>
        private sealed class PartRow
        {
            public PartRow(Reveal reveal, VisualElement body, VisualElement icon, Label name)
            {
                Reveal = reveal;
                Body = body;
                Icon = icon;
                Name = name;
            }

            public Reveal Reveal { get; }

            public VisualElement Body { get; }

            public VisualElement Icon { get; }

            public Label Name { get; }

            public string PartId { get; set; }

            public UiIconShape Shape { get; set; }

            public float WrittenSlide { get; set; } = float.NaN;

            public void PaintIcon(MeshGenerationContext context)
            {
                Rect rect = Icon.contentRect;
                if (PartId == null || rect.width < 1f)
                {
                    return;
                }

                UiIconShape shape = Shape;
                UiIconPainter.Draw(context.painter2D, in shape, rect.center,
                    Mathf.Min(rect.width, rect.height), UiIconState.Lit);
            }
        }

        private sealed class Row
        {
            public Row(Reveal reveal, VisualElement body, VisualElement icon, MaterialIconPainter painter, Label name,
                Label gain, Label total)
            {
                Reveal = reveal;
                Body = body;
                Icon = icon;
                Painter = painter;
                Name = name;
                Gain = gain;
                Total = total;
            }

            public Reveal Reveal { get; }

            public VisualElement Body { get; }

            public VisualElement Icon { get; }

            public MaterialIconPainter Painter { get; }

            public Label Name { get; }

            public Label Gain { get; }

            public Label Total { get; }

            /// <summary>True from the claim until the row has faded out.</summary>
            public bool Active { get; set; }

            public SalvageMaterial Material { get; set; }

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
