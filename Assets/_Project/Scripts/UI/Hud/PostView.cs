using System;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// Draws <see cref="PostLog"/> on 07's CRT: a terminal column of typed lines in the face panel, alerts in amber.
    /// Once the lines fill the face the column scrolls up, as a terminal does, so the newest line is always in view.
    /// It writes a label only when what it shows changes, so the self-test costs nothing between keystrokes.
    /// </summary>
    internal sealed class PostView
    {
        public const string RootClass = "post";
        public const string ColumnClass = "post__column";
        public const string LineClass = "post__line";
        public const string AlertClass = "post__line--alert";

        private static readonly string[] Dots = { string.Empty, ".", "..", "..." };

        private readonly PostLog _log;
        private readonly VisualElement _root;
        private readonly VisualElement _column;
        private readonly Label[] _lines = new Label[PostLog.Capacity];
        private readonly int[] _shownChars = new int[PostLog.Capacity];
        private readonly int[] _shownDots = new int[PostLog.Capacity];
        private readonly string[] _shownTargets = new string[PostLog.Capacity];
        private bool _shown = true;

        public PostView(VisualElement parent, PostLog log)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            _log = log ?? throw new ArgumentNullException(nameof(log));
            _root = new VisualElement { name = "post", pickingMode = PickingMode.Ignore };
            _root.AddToClassList(RootClass);
            _column = new VisualElement { name = "post-column", pickingMode = PickingMode.Ignore };
            _column.AddToClassList(ColumnClass);
            _root.Add(_column);
            for (int i = 0; i < PostLog.Capacity; i++)
            {
                var line = new Label { name = "post-line-" + i, pickingMode = PickingMode.Ignore };
                line.AddToClassList(LineClass);
                _lines[i] = line;
                _column.Add(line);
            }

            _column.RegisterCallback<GeometryChangedEvent>(_ => Scroll());
            _root.RegisterCallback<GeometryChangedEvent>(_ => Scroll());
            parent.Add(_root);
            Clear();
            SetShown(false);
        }

        /// <summary>The log's column (tests and captures).</summary>
        public VisualElement Root => _root;

        /// <summary>How far (px) the column has scrolled up to keep the newest line in view (tests and captures).</summary>
        public float ScrolledBy { get; private set; }

        /// <summary>The label of line <paramref name="index"/> (tests).</summary>
        public Label Line(int index) => _lines[index];

        /// <summary>Shows or hides the column.</summary>
        public void SetShown(bool shown)
        {
            if (shown == _shown)
            {
                return;
            }

            _shown = shown;
            _root.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>Empties every line.</summary>
        public void Clear()
        {
            for (int i = 0; i < PostLog.Capacity; i++)
            {
                _lines[i].text = string.Empty;
                _lines[i].style.display = DisplayStyle.None;
                _lines[i].RemoveFromClassList(AlertClass);
                _shownChars[i] = -1;
                _shownDots[i] = -1;
                _shownTargets[i] = null;
            }

            ScrolledBy = 0f;
            _column.style.top = 0f;
        }

        /// <summary>Scrolls the column up just enough that its last line sits at the bottom of the face.</summary>
        private void Scroll()
        {
            float room = _root.contentRect.height;
            float height = _column.layout.height;
            if (float.IsNaN(room) || float.IsNaN(height))
            {
                return;
            }

            float scrolled = height > room ? height - room : 0f;
            if (scrolled != ScrolledBy)
            {
                ScrolledBy = scrolled;
                _column.style.top = -scrolled;
            }
        }

        /// <summary>Writes what changed since the last call.</summary>
        public void Refresh()
        {
            for (int i = 0; i < _log.Count; i++)
            {
                string target = _log.Target(i);
                int chars = _log.Visible(i);
                int dots = _log.DotsOf(i);
                if (chars == _shownChars[i] && dots == _shownDots[i] && ReferenceEquals(target, _shownTargets[i]))
                {
                    continue;
                }

                _shownChars[i] = chars;
                _shownDots[i] = dots;
                _shownTargets[i] = target;
                _lines[i].text = target.Substring(0, chars) + Dots[dots];
                _lines[i].style.display = DisplayStyle.Flex;
                _lines[i].EnableInClassList(AlertClass, _log.IsAlertLine(i));
            }
        }
    }
}
