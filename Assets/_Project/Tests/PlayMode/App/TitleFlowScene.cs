namespace MoonProject.Tests.PlayMode.App
{
    /// <summary>The title flow sessions' own copy of Main.unity and save slot ("title-flow-&lt;process id&gt;").</summary>
    public sealed class TitleFlowScene : MainSceneCopy
    {
        public TitleFlowScene()
            : base("TitleFlow", "title-flow")
        {
        }
    }
}
