namespace MoonProject.UI
{
    /// <summary>What the mouse is for right now, which decides whether the cursor shows.</summary>
    internal enum CursorMode
    {
        /// <summary>The mouse turns the camera (driving, aiming a placement, welding): locked and hidden.</summary>
        Drive = 0,

        /// <summary>The title or the pause menu: free and visible.</summary>
        Menu = 1,

        /// <summary>07's face screen: visible and kept inside the window, so a drag cannot leave it.</summary>
        Screen = 2,
    }
}
