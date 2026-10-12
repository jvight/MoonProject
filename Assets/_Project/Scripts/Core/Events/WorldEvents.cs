namespace MoonProject.Core.Events
{
    /// <summary>
    /// One step of home's windows lit as the base wakes (polish wave 1): published by World's home lighting once per
    /// step, <see cref="Step"/> 1..<see cref="Steps"/>, never when a saved stage is restored at once. Audio plays a
    /// soft warm click on each, rising.
    /// </summary>
    public readonly struct HomeWindowLit
    {
        public HomeWindowLit(int step, int steps)
        {
            Step = step;
            Steps = steps;
        }

        /// <summary>The step just lit, 1-based.</summary>
        public int Step { get; }

        /// <summary>How many steps the windows wake in.</summary>
        public int Steps { get; }
    }
}
