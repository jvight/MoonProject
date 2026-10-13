using NUnit.Framework;
using UnityEngine;

namespace MoonProject.UI.Tests
{
    /// <summary>The socket names on the 07 tab never overlap, however close the sockets are on 07's body.</summary>
    public sealed class LabelLayoutTests
    {
        private const float Width = 600f;
        private const float Height = 400f;
        private const float LabelHeight = 24f;
        private const float Gap = 4f;

        private readonly Vector2[] _points = new Vector2[12];
        private readonly bool[] _wants = new bool[12];
        private readonly float[] _y = new float[12];
        private readonly bool[] _right = new bool[12];

        [SetUp]
        public void SetUp()
        {
            System.Array.Clear(_wants, 0, _wants.Length);
        }

        [Test]
        public void SocketsClusteredOnTheBody_GetNamesThatDoNotOverlap()
        {
            // Back, Flanks, Wing and Deck sit within a few percent of each other in the 3/4 view.
            Cluster(6, 0.38f, 0.59f);
            Cluster(4, 0.45f, 0.62f);
            Cluster(8, 0.42f, 0.65f);
            Cluster(9, 0.40f, 0.60f);
            Cluster(0, 0.78f, 0.30f);
            Place();
            AssertClear(6, 4, 8, 9);
        }

        [Test]
        public void ANameStaysAsNearItsOwnHeightAsTheOthersAllow_AndInOrder()
        {
            Cluster(0, 0.2f, 0.2f);
            Cluster(1, 0.25f, 0.5f);
            Cluster(2, 0.3f, 0.8f);
            Place();
            Assert.AreEqual(Height * 0.2f, _y[0], 0.01f, "nothing in the way: it keeps its own height");
            Assert.Less(_y[0], _y[1]);
            Assert.Less(_y[1], _y[2], "the order down the column follows the order down the body");
        }

        [Test]
        public void TheLeftHalfGoesToTheLeftColumn_AndTheRightHalfToTheRight()
        {
            Cluster(0, 0.3f, 0.5f);
            Cluster(1, 0.7f, 0.5f);
            Place();
            Assert.IsFalse(_right[0]);
            Assert.IsTrue(_right[1]);
        }

        [Test]
        public void ANameNeverLeavesThePortrait_EvenWhenManyShareTheBottom()
        {
            for (int i = 0; i < 6; i++)
            {
                Cluster(i, 0.8f, 0.95f);
            }

            Place();
            for (int i = 0; i < 6; i++)
            {
                Assert.GreaterOrEqual(_y[i] - LabelHeight * 0.5f, -0.01f, $"{i} above the top");
                Assert.LessOrEqual(_y[i] + LabelHeight * 0.5f, Height + 0.01f, $"{i} below the bottom");
            }

            AssertClear(0, 1, 2, 3, 4, 5);
        }

        [Test]
        public void WrongSizedArrays_AreRefused()
        {
            Assert.Throws<System.ArgumentException>(() =>
                LabelLayout.Place(_points, new bool[3], Width, Height, LabelHeight, Gap, _y, _right));
        }

        private void Cluster(int index, float u, float vFromTop)
        {
            _points[index] = new Vector2(u * Width, vFromTop * Height);
            _wants[index] = true;
        }

        private void Place()
        {
            LabelLayout.Place(_points, _wants, Width, Height, LabelHeight, Gap, _y, _right);
        }

        private void AssertClear(params int[] indices)
        {
            for (int a = 0; a < indices.Length; a++)
            {
                for (int b = a + 1; b < indices.Length; b++)
                {
                    int i = indices[a];
                    int j = indices[b];
                    if (_right[i] != _right[j])
                    {
                        continue;
                    }

                    Assert.GreaterOrEqual(Mathf.Abs(_y[i] - _y[j]), LabelHeight - 0.01f, $"{i} and {j} overlap");
                }
            }
        }
    }
}
