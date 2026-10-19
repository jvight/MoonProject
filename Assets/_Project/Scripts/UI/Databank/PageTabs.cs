using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The face screen's page tabs, HOLD and DATABANK, at the right of its title row on both pages: the page on the
    /// tube is lit, one accent underline glides between them while the page changes, and a click on the other flips to
    /// it, as Q does (docs/design/databank.md §4).
    /// </summary>
    internal sealed class PageTabs
    {
        private const string TabClass = "page-tab";
        private const string ActiveClass = "page-tab--active";
        private const string UnderlineClass = "page-tab__underline";
        private const float UnderlineHeight = 2f;
        private static readonly FacePage[] Pages = { FacePage.Hold, FacePage.Databank };

        private readonly ILocalization _localization;
        private readonly Label[] _tabs = new Label[Pages.Length];
        private readonly VisualElement _underline;
        private FacePage _lit = (FacePage)(-1);
        private float _scale = 1f;
        private float _position;

        public PageTabs(VisualElement parent, ILocalization localization)
        {
            if (parent == null)
            {
                throw new ArgumentNullException(nameof(parent));
            }

            _localization = localization ?? throw new ArgumentNullException(nameof(localization));
            for (int i = 0; i < _tabs.Length; i++)
            {
                _tabs[i] = new Label { name = "page-tab-" + i, pickingMode = PickingMode.Ignore };
                _tabs[i].AddToClassList(TabClass);
                parent.Add(_tabs[i]);
            }

            _underline = new VisualElement { name = "page-tab-underline", pickingMode = PickingMode.Ignore };
            _underline.AddToClassList(UnderlineClass);
            parent.Add(_underline);
            Relocalize();
        }

        /// <summary>The tabs' elements, which stay up whichever page shows.</summary>
        public VisualElement Tab(int index) => _tabs[index];

        /// <summary>The underline under the lit tab, which glides while the page changes.</summary>
        public VisualElement Underline => _underline;

        public int Count => _tabs.Length;

        /// <summary>The tabs show only where the screen has pages: on 07's face.</summary>
        public void SetShown(bool shown)
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                _tabs[i].style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            }

            _underline.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        /// <summary>
        /// Puts the underline at <paramref name="position"/> between the tabs (0 under HOLD, 1 under DATABANK).
        /// </summary>
        public void Glide(float position)
        {
            _position = position;
            Rect from = DatabankLayout.TabRect(0);
            Rect to = DatabankLayout.TabRect(1);
            _underline.style.left = Mathf.Lerp(from.x, to.x, position) * _scale;
            _underline.style.top = from.yMax * _scale;
            _underline.style.width = from.width * _scale;
            _underline.style.height = UnderlineHeight * _scale;
        }

        /// <summary>Keeps the tabs over a page that was just put on the tube.</summary>
        public void BringToFront()
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                _tabs[i].BringToFront();
            }

            _underline.BringToFront();
        }

        public void Relocalize()
        {
            _tabs[0].text = _localization.Get(DatabankText.HoldPageKey);
            _tabs[1].text = _localization.Get(DatabankText.TitleKey);
        }

        /// <summary>Lights the tab of <paramref name="page"/>.</summary>
        public void Light(FacePage page)
        {
            if (page == _lit)
            {
                return;
            }

            _lit = page;
            for (int i = 0; i < _tabs.Length; i++)
            {
                _tabs[i].EnableInClassList(ActiveClass, Pages[i] == page);
            }
        }

        public void Layout(float scale)
        {
            _scale = scale;
            for (int i = 0; i < _tabs.Length; i++)
            {
                Rect rect = DatabankLayout.TabRect(i);
                _tabs[i].style.left = rect.x * scale;
                _tabs[i].style.top = rect.y * scale;
                _tabs[i].style.width = rect.width * scale;
                _tabs[i].style.height = rect.height * scale;
            }

            Glide(_position);
        }

        /// <summary>The page whose tab is under <paramref name="refPos"/> (reference pixels), or false.</summary>
        public static bool TryPageAt(Vector2 refPos, out FacePage page)
        {
            for (int i = 0; i < Pages.Length; i++)
            {
                if (DatabankLayout.TabRect(i).Contains(refPos))
                {
                    page = Pages[i];
                    return true;
                }
            }

            page = FacePage.Hold;
            return false;
        }
    }
}
