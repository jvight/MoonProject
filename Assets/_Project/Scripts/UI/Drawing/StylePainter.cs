using System;
using UnityEngine.UIElements;

namespace MoonProject.UI
{
    /// <summary>
    /// Paints vector content into one element, with its colours and sizes read from custom USS properties (so the
    /// look stays in the stylesheets and the palette). Repaints only when the style resolves or a subclass marks it
    /// dirty.
    /// </summary>
    internal abstract class StylePainter
    {
        protected StylePainter(VisualElement element)
        {
            Element = element ?? throw new ArgumentNullException(nameof(element));
            element.generateVisualContent += Paint;
            element.RegisterCallback<CustomStyleResolvedEvent>(OnStyleResolved);
        }

        public VisualElement Element { get; }

        protected abstract void ReadStyle(ICustomStyle style);

        protected abstract void Paint(MeshGenerationContext context);

        protected void Repaint()
        {
            Element.MarkDirtyRepaint();
        }

        private void OnStyleResolved(CustomStyleResolvedEvent evt)
        {
            ReadStyle(evt.customStyle);
            Repaint();
        }
    }
}
