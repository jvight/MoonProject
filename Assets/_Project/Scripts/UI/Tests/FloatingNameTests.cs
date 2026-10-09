using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using MoonProject.Core;
using MoonProject.UI.Editor;

namespace MoonProject.UI.Tests
{
    /// <summary>
    /// Every floating name (a fitted piece of kit, a waking friend, an answering site) sits on the kit title's soft
    /// shadow, painted behind its words and fading with them, so names stay legible over the bright Earth; a world
    /// name eases in, rests, leaves, and hides while its point is off screen.
    /// </summary>
    public sealed class FloatingNameTests
    {
        private static readonly Vector2 Panel = new Vector2(1920f, 1080f);
        private static readonly Vector3 Ahead = new Vector3(0f, 0f, 10f);

        private UiLayout _layout;
        private GameObject _cameraHost;
        private Camera _camera;

        [SetUp]
        public void SetUp()
        {
            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UiAssetPaths.Uxml);
            Assert.IsNotNull(uxml, UiAssetPaths.Uxml);
            _layout = new UiLayout(uxml.Instantiate());
            _cameraHost = new GameObject("FloatingNameTestCamera");
            _camera = _cameraHost.AddComponent<Camera>();
            _camera.aspect = Panel.x / Panel.y;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cameraHost);
        }

        [Test]
        public void EveryFloatingName_SitsOnTheSharedSoftShadow_BehindItsWords()
        {
            AssertNameTag(_layout.KitTitleShadow.parent, _layout.KitTitleShadow, _layout.KitTitleName);
            AssertNameTag(_layout.FriendName, _layout.FriendNameShadow, _layout.FriendNameText);
            AssertNameTag(_layout.SiteName, _layout.SiteNameShadow, _layout.SiteNameText);
        }

        [Test]
        public void AWorldName_EasesInWithItsShadow_Rests_HidesOffScreen_AndLeaves()
        {
            var settings = new RevealSettings(0.5f, 0.5f, 10f, 0.9f, 1.4f, 0.6f);
            var name = new FloatingName(_layout.FriendNameAnchor, _layout.FriendName, _layout.FriendNameShadow,
                _layout.FriendNameText, settings, new PromptSettings(), new View(_camera));
            Assert.IsFalse(name.IsUp);
            Assert.AreEqual(DisplayStyle.None, _layout.FriendName.style.display.value, "nothing until it is shown");

            name.Show("Tilly", 1f);
            Assert.IsTrue(name.IsUp);
            name.Tick(Ahead, 0.25f, Panel);
            Assert.AreEqual("Tilly", _layout.FriendNameText.text);
            Assert.That(_layout.FriendName.style.opacity.value, Is.GreaterThan(0f).And.LessThan(1f),
                "the tag eases in, and the shadow inside it with it");

            name.Tick(Ahead, 0.3f, Panel);
            Assert.AreEqual(1f, _layout.FriendName.style.opacity.value, 1e-3f);
            name.Tick(-Ahead, 0.1f, Panel);
            Assert.IsFalse(_layout.FriendName.visible, "hidden while its point is behind the camera");
            name.Tick(Ahead, 0.1f, Panel);
            Assert.IsTrue(_layout.FriendName.visible);
            Assert.IsTrue(name.IsVisible);

            name.Rename("Tilly (vi)");
            Assert.AreEqual("Tilly (vi)", _layout.FriendNameText.text, "a new language changes it in place");
            for (int i = 0; i < 20; i++)
            {
                name.Tick(Ahead, 0.1f, Panel);
            }

            Assert.IsFalse(name.IsUp, "it rested, then faded away");
            Assert.AreEqual(DisplayStyle.None, _layout.FriendName.style.display.value);
        }

        private static void AssertNameTag(VisualElement tag, VisualElement shadow, Label text)
        {
            Assert.IsTrue(tag.ClassListContains("name-tag"), tag.name);
            Assert.AreSame(tag, shadow.parent, $"{shadow.name} fades with its tag");
            Assert.AreEqual(0, tag.IndexOf(shadow), $"{shadow.name} is painted behind the words");
            Assert.IsTrue(shadow.ClassListContains("soft-shadow"), shadow.name);
            Assert.IsTrue(shadow.ClassListContains("soft-shadow--name"), $"{shadow.name} shares the name shadow");
            Assert.AreSame(tag, text.parent, text.name);
            Assert.IsTrue(text.ClassListContains("name-tag__name"), $"{text.name} shares the name type");
        }

        /// <summary>The test's own camera as the player's view.</summary>
        private sealed class View : IViewCamera
        {
            public View(Camera camera)
            {
                Camera = camera;
            }

            public Camera Camera { get; }
        }
    }
}
