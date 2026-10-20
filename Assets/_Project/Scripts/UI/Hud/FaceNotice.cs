using System;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// One soft line on 07's CRT face (the face panel, like the self-test column): why a held E did nothing, or a
    /// story line (TX · NO RANGE), with a smaller detail line under it when the moment reads one out (relay.2 joining
    /// the network). It writes its labels only when the words change.
    /// </summary>
    internal sealed class FaceNotice
    {
        public const string RootClass = "face-notice";
        public const string LineClass = "face-notice__line";
        public const string DetailClass = "face-notice__detail";

        private readonly VisualElement _root;
        private readonly Label _line;
        private readonly Label _detail;
        private bool _shown = true;

        public FaceNotice(VisualElement parent)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            _root = new VisualElement { name = "face-notice", pickingMode = PickingMode.Ignore };
            _root.AddToClassList(RootClass);
            _line = new Label { name = "face-notice-line", pickingMode = PickingMode.Ignore };
            _line.AddToClassList(LineClass);
            _detail = new Label { name = "face-notice-detail", pickingMode = PickingMode.Ignore };
            _detail.AddToClassList(DetailClass);
            _root.Add(_line);
            _root.Add(_detail);
            parent.Add(_root);
            SetShown(false);
        }

        public bool IsShown => _shown;

        /// <summary>The words on the face (tests).</summary>
        public string Text => _line.text;

        /// <summary>The detail line under them, or empty (tests).</summary>
        public string Detail => _detail.text;

        public void Show(string text)
        {
            Show(text, string.Empty);
        }

        /// <param name="detail">The smaller line under it; empty for none.</param>
        public void Show(string text, string detail)
        {
            if (!ReferenceEquals(text, _line.text))
            {
                _line.text = text;
            }

            if (!ReferenceEquals(detail, _detail.text))
            {
                _detail.text = detail;
                _detail.style.display = detail.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            }

            SetShown(true);
        }

        public void SetShown(bool shown)
        {
            if (shown == _shown)
            {
                return;
            }

            _shown = shown;
            _root.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
