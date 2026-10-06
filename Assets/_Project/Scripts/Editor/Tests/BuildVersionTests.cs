using NUnit.Framework;
using MoonProject.Editor.Build;

namespace MoonProject.Editor.Tests
{
    public sealed class BuildVersionTests
    {
        [Test]
        public void UntrackedMetaFilesAloneAreNotSourceChanges()
        {
            Assert.IsFalse(BuildVersion.HasSourceChanges(string.Empty));
            Assert.IsFalse(BuildVersion.HasSourceChanges(
                "?? Assets/_Project/Scripts/Editor/Build.meta\n?? \"Assets/A B.cs.meta\"\r\n"));
        }

        [Test]
        public void ModifiedOrUntrackedSourceIsAChange()
        {
            Assert.IsTrue(BuildVersion.HasSourceChanges(" M Assets/_Project/Scenes/Main.unity\n"));
            Assert.IsTrue(BuildVersion.HasSourceChanges("?? Assets/_Project/Scripts/New.cs\n?? Assets/x.meta\n"));
            Assert.IsTrue(BuildVersion.HasSourceChanges(" M Assets/_Project/Scripts/Old.cs.meta\n"));
        }
    }
}
