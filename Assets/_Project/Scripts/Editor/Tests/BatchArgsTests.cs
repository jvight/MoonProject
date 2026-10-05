using System;
using System.IO;
using NUnit.Framework;
using MoonProject.Editor.Automation;

namespace MoonProject.Editor.Tests
{
    public sealed class BatchArgsTests
    {
        [Test]
        public void Parse_ReadsPairsAndOutputDirectory()
        {
            string output = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "moon-out"));
            BatchArgs args = BatchArgs.Parse(new[]
            {
                "Unity.exe", "-batchmode", "-moonArg", "size=640", "-moonOutDir", output, "-moonArg", "Filter=^Art/",
            });

            Assert.AreEqual(output, args.OutputDirectory);
            Assert.AreEqual(640, args.GetInt("size", 0));
            Assert.AreEqual("^Art/", args.GetString("filter", null), "keys are case-insensitive");
            Assert.AreEqual(2, args.Keys.Count);
        }

        [Test]
        public void Parse_ValueMayContainEqualsSign()
        {
            BatchArgs args = BatchArgs.FromPairs(null, "expr=a=b");

            Assert.AreEqual("a=b", args.GetString("expr", null));
        }

        [Test]
        public void Parse_LastOccurrenceWins()
        {
            BatchArgs args = BatchArgs.FromPairs(null, "size=1", "size=2");

            Assert.AreEqual(2, args.GetInt("size", 0));
        }

        [Test]
        public void Parse_MissingValue_Throws()
        {
            Assert.Throws<ArgumentException>(() => BatchArgs.Parse(new[] { "Unity.exe", "-moonArg" }));
        }

        [Test]
        public void Parse_PairWithoutKey_Throws()
        {
            Assert.Throws<ArgumentException>(() => BatchArgs.FromPairs(null, "=value"));
            Assert.Throws<ArgumentException>(() => BatchArgs.FromPairs(null, "novalue"));
        }

        [Test]
        public void Getters_UseFallbackWhenAbsent()
        {
            BatchArgs args = BatchArgs.FromPairs(null);

            Assert.AreEqual(7, args.GetInt("n", 7));
            Assert.AreEqual(0.5f, args.GetFloat("f", 0.5f));
            Assert.IsTrue(args.GetBool("b", true));
            Assert.AreEqual("x", args.GetString("s", "x"));
            Assert.IsEmpty(args.GetList("l"));
            Assert.IsFalse(args.Has("n"));
        }

        [Test]
        public void TypedGetters_RejectMalformedValues()
        {
            BatchArgs args = BatchArgs.FromPairs(null, "n=seven", "f=half", "b=maybe");

            Assert.Throws<ArgumentException>(() => args.GetInt("n", 0));
            Assert.Throws<ArgumentException>(() => args.GetFloat("f", 0f));
            Assert.Throws<ArgumentException>(() => args.GetBool("b", false));
        }

        [Test]
        public void GetFloat_UsesInvariantCulture()
        {
            BatchArgs args = BatchArgs.FromPairs(null, "f=1.25");

            Assert.AreEqual(1.25f, args.GetFloat("f", 0f));
        }

        [Test]
        public void GetBool_AcceptsCommonSpellings()
        {
            BatchArgs args = BatchArgs.FromPairs(null, "a=YES", "b=0", "c=on", "d=False");

            Assert.IsTrue(args.GetBool("a", false));
            Assert.IsFalse(args.GetBool("b", true));
            Assert.IsTrue(args.GetBool("c", false));
            Assert.IsFalse(args.GetBool("d", true));
        }

        [Test]
        public void GetList_SplitsOnSemicolonAndComma()
        {
            BatchArgs args = BatchArgs.FromPairs(null, "prefab=A.prefab; B.prefab,,C.prefab ");

            CollectionAssert.AreEqual(new[] { "A.prefab", "B.prefab", "C.prefab" }, args.GetList("prefab"));
        }

        [Test]
        public void GetRequiredString_MissingOrEmpty_Throws()
        {
            BatchArgs args = BatchArgs.FromPairs(null, "empty=");

            Assert.Throws<ArgumentException>(() => args.GetRequiredString("absent"));
            Assert.Throws<ArgumentException>(() => args.GetRequiredString("empty"));
        }

        [Test]
        public void WithoutOutputFlag_DefaultsUnderLogsBatch()
        {
            BatchArgs args = BatchArgs.Parse(new[] { "Unity.exe" });

            StringAssert.EndsWith(Path.Combine("Logs", "batch", "manual"), args.OutputDirectory);
        }
    }
}
