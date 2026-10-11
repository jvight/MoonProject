using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.Tests
{
    /// <summary>
    /// Pickup glints read from far away only: none near the camera (where one blows out into a disc, worst under the
    /// title's depth of field), none while the title is up, easing back in as play begins.
    /// </summary>
    public sealed class PickupGlintTests
    {
        private const float Time0 = 10f;

        private GlintTuning _tuning;
        private Material _material;
        private GameObject _parent;
        private GlintVeil _veil;
        private PickupGlints _glints;

        [SetUp]
        public void Build()
        {
            _tuning = ScriptableObject.CreateInstance<GlintTuning>();
            _material = new Material(Shader.Find("Hidden/InternalErrorShader"));
            _parent = new GameObject("GlintTestParent");
            _veil = new GlintVeil();
            _glints = new PickupGlints(_parent.transform, _material, _tuning, _veil, 4, 0);
        }

        [TearDown]
        public void Clean()
        {
            Release(_glints);
            Object.DestroyImmediate(_parent);
            Object.DestroyImmediate(_material);
            Object.DestroyImmediate(_tuning);
        }

        [Test]
        public void AGlintNearTheCamera_IsGone_AndAFarOneShines()
        {
            Assert.GreaterOrEqual(_tuning.GlintFadeNear, 20f, "glints fade in from about 25 m");
            Draw(Time0, _tuning.GlintFadeNear - 1f);
            Assert.AreEqual(0, _glints.Drawn, "a pickup nearer than the fade reads by its model alone");
            Draw(Time0, _tuning.GlintFadeFar + 5f);
            Assert.AreEqual(1, _glints.Drawn);
            Assert.Greater(_glints.Brightest, 0f);
        }

        [Test]
        public void UnderTheTitle_NoGlintShows_ThenTheyEaseBackInAsPlayBegins()
        {
            float far = _tuning.GlintFadeFar + 5f;
            _veil.Hide();
            Draw(Time0, far);
            Assert.AreEqual(0, _glints.Drawn, "hidden while the title is up");

            _veil.Show(Time0);
            var open = new PickupGlints(_parent.transform, _material, _tuning, new GlintVeil(), 4, 0);
            try
            {
                float easing = Time0 + _tuning.GlintRevealSeconds * 0.3f;
                Draw(easing, far);
                Draw(open, easing, far);
                Assert.Greater(_glints.Brightest, 0f, "coming back as play begins");
                Assert.Less(_glints.Brightest, open.Brightest, "eased in, not switched on");
                float settled = Time0 + _tuning.GlintRevealSeconds * 2f;
                Draw(settled, far);
                Draw(open, settled, far);
                Assert.AreEqual(open.Brightest, _glints.Brightest, 1e-5f, "fully back once the reveal is over");
            }
            finally
            {
                Release(open);
            }
        }

        [Test]
        public void TheVeil_IsFullOutsideTheTitle_AndEasesFromZeroToOne()
        {
            var veil = new GlintVeil();
            Assert.AreEqual(1f, veil.Level(0f, 2f), "a game without a title never hides them");
            veil.Hide();
            Assert.AreEqual(0f, veil.Level(5f, 2f));
            veil.Show(5f);
            Assert.AreEqual(0f, veil.Level(5f, 2f), 1e-5f);
            Assert.AreEqual(0.5f, veil.Level(6f, 2f), 1e-5f);
            Assert.AreEqual(1f, veil.Level(7f, 2f), 1e-5f);
            veil.Show(9f);
            Assert.AreEqual(1f, veil.Level(9f, 2f), "a second session start changes nothing once shown");
        }

        /// <summary>PickupGlints.Dispose uses the play-mode Destroy; edit mode frees its mesh at once.</summary>
        private static void Release(PickupGlints glints)
        {
            Object.DestroyImmediate(glints.Renderer.GetComponent<MeshFilter>().sharedMesh);
            Object.DestroyImmediate(glints.Renderer.gameObject);
        }

        private void Draw(float time, float distance)
        {
            Draw(_glints, time, distance);
        }

        private void Draw(PickupGlints glints, float time, float distance)
        {
            glints.Begin(Vector3.zero, time);
            glints.Add(new Vector3(0f, -_tuning.GlintLift, distance), 0f);
            glints.End();
        }
    }
}
