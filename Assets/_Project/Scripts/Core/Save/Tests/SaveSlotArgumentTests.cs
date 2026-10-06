using NUnit.Framework;

namespace MoonProject.Core.Save.Tests
{
    public sealed class SaveSlotArgumentTests
    {
        [Test]
        public void ReadsTheValueAfterTheFlag()
        {
            Assert.IsTrue(SaveSlotArgument.TryRead(new[] { "LofiLunar.exe", "-saveSlot", "smoke", "-logFile", "x" },
                out string slot));
            Assert.AreEqual("smoke", slot);
        }

        [Test]
        public void FlagIsCaseInsensitiveAndTheLastOneWins()
        {
            Assert.IsTrue(SaveSlotArgument.TryRead(new[] { "-SAVESLOT", "a", "-saveslot", "b" }, out string slot));
            Assert.AreEqual("b", slot);
        }

        [Test]
        public void AbsentFlag_ReturnsFalse()
        {
            Assert.IsFalse(SaveSlotArgument.TryRead(new[] { "LofiLunar.exe", "-screen-width", "1280" },
                out string slot));
            Assert.IsNull(slot);
        }

        [Test]
        public void FlagWithoutValue_YieldsAnInvalidSlot()
        {
            Assert.IsTrue(SaveSlotArgument.TryRead(new[] { "LofiLunar.exe", "-saveSlot" }, out string slot));
            Assert.IsFalse(SaveService.IsValidSlot(slot));
            Assert.IsTrue(SaveSlotArgument.TryRead(new[] { "-saveSlot", "../main" }, out slot));
            Assert.IsFalse(SaveService.IsValidSlot(slot));
        }
    }
}
