namespace MoonProject.Tests.PlayMode.Look
{
    /// <summary>The look capture's own copy of Main.unity and save slot ("look-capture-&lt;process id&gt;").</summary>
    public sealed class LookCaptureScene : MainSceneCopy
    {
        public LookCaptureScene()
            : base("LookCapture", "look-capture")
        {
        }
    }
}
