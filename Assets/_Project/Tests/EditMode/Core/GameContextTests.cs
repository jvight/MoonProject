using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem;
using MoonProject.Core;
using MoonProject.Core.Input;

namespace MoonProject.Tests.EditMode.Core
{
    public sealed class GameContextTests
    {
        private const string ControlsPath = "Assets/_Project/Data/Input/Controls.inputactions";

        private interface IService
        {
        }

        private sealed class Service : IService
        {
        }

        private InputActionAsset _asset;
        private GameContext _context;

        [SetUp]
        public void SetUp()
        {
            _asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            Assert.IsNotNull(_asset, $"Controls asset missing at {ControlsPath}");
            _context = new GameContext(new EventBus(), new InputReader(_asset));
        }

        [Test]
        public void Register_ThenGet_ReturnsSameInstance()
        {
            var service = new Service();
            _context.Register<IService>(service);

            Assert.AreSame(service, _context.Get<IService>());
        }

        [Test]
        public void Register_Twice_Throws()
        {
            _context.Register<IService>(new Service());

            Assert.Throws<InvalidOperationException>(() => _context.Register<IService>(new Service()));
        }

        [Test]
        public void Get_Unregistered_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _context.Get<IService>());
        }

        [Test]
        public void TryGet_Unregistered_ReturnsFalse()
        {
            Assert.IsFalse(_context.TryGet(out IService service));
            Assert.IsNull(service);
        }

        [Test]
        public void InputReader_ResolvesEveryActionAndToggles()
        {
            var reader = new InputReader(_asset);

            reader.Enable();
            Assert.IsTrue(reader.Enabled);
            Assert.AreEqual(UnityEngine.Vector2.zero, reader.Drive);

            reader.Dispose();
            Assert.IsFalse(reader.Enabled);
        }
    }
}
