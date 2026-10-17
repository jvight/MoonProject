namespace MoonProject.UI
{
    /// <summary>
    /// Which input last drove the face inventory's selection (pure, EditMode-tested): the keys and the pad, or the
    /// mouse. The selection is one slot; a hover may move it only while the mouse is the input in use, so a pointer
    /// that merely rests over the screen (or a cell that slides under it) never steals the arrows' place, and the next
    /// arrow press continues from wherever the player last was, hovered or keyed, without a jump.
    /// </summary>
    internal sealed class InputHandoff
    {
        /// <summary>True while the mouse (it actually moved or pressed) is the last input used.</summary>
        public bool MouseIsActive { get; private set; }

        /// <summary>A key or pad direction was used: the selection is the keys' again.</summary>
        public void NoteNavigation()
        {
            MouseIsActive = false;
        }

        /// <summary>The mouse really moved or pressed (not a synthetic pointer event from a layout change).</summary>
        public void NoteMouse()
        {
            MouseIsActive = true;
        }

        /// <summary>A hover over a slot may take the selection now.</summary>
        public bool HoverMaySelect => MouseIsActive;

        /// <summary>The screen opened: the keys own the selection until the mouse really moves.</summary>
        public void Reset()
        {
            MouseIsActive = false;
        }
    }
}
