using System;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The face screen's page tabs, HOLD and DATABANK, at the right of its title row on both pages: the page on the
    /// tube is lit, and a click on the other flips to it, as Q does (docs/design/databank.md §4).
    /// </summary>
    internal sealed class PageTabs
    {
        private const string TabClass = "page-tab";
        private const string ActiveClass = "page-tab--active";
        private static readonly FacePage[] Pages = { FacePage.Hold, FacePage.Databank };

        private readonly ILocalization _localization;
        private readonly Label[] _tabs = new Label[Pages.Length];
        private FacePage _lit = (FacePage)(-1);

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

            Relocalize();
        }

        /// <summary>The tabs' elements, which stay up whichever page shows.</summary>
        public VisualElement Tab(int index) => _tabs[index];

        public int Count => _tabs.Length;

        /// <summary>The tabs show only where the screen has pages: on 07's face.</summary>
        public void SetShown(bool shown)
        {
            for (int i = 0; i < _tabs.Length; i++)
            {
                _tabs[i].style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            }
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
            for (int i = 0; i < _tabs.Length; i++)
            {
                Rect rect = DatabankLayout.TabRect(i);
                _tabs[i].style.left = rect.x * scale;
                _tabs[i].style.top = rect.y * scale;
                _tabs[i].style.width = rect.width * scale;
                _tabs[i].style.height = rect.height * scale;
            }
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
