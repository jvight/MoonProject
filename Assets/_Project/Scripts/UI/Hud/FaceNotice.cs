using System;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// One soft line on 07's CRT face (the face panel, like the self-test column): why a held E did nothing. It writes
    /// its label only when the words change.
    /// </summary>
    internal sealed class FaceNotice
    {
        public const string RootClass = "face-notice";
        public const string LineClass = "face-notice__line";

        private readonly VisualElement _root;
        private readonly Label _line;
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
            _root.Add(_line);
            parent.Add(_root);
            SetShown(false);
        }

        public bool IsShown => _shown;

        /// <summary>The words on the face (tests).</summary>
        public string Text => _line.text;

        public void Show(string text)
        {
            if (!ReferenceEquals(text, _line.text))
            {
                _line.text = text;
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
