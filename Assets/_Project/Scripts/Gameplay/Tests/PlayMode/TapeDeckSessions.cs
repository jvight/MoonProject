using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;
using MoonProject.Core.Events;
using MoonProject.Testing;
using Object = UnityEngine.Object;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>
    /// Tilly's glint is an old tape deck (docs/features/M3-24 C), on the real components behind the fake rover: it is
    /// not there while Tilly sleeps, glints once she is out, flies onto 07 when driven through (TapeDeckFound, once),
    /// is saved, and a loaded game announces it as restored and never lays it on the ground again.
    /// </summary>
    public sealed class TapeDeckSessions : InputTestFixture
    {
        private InputActionAsset _controls;
        private GameplayFixture _fixture;

        public override void Setup()
        {
            base.Setup();
            _controls = BootstrapHarness.LoadControlsCopy();
        }

        public override void TearDown()
        {
            _fixture?.Dispose();
            Object.Destroy(_controls);
            base.TearDown();
        }

        [UnityTest]
        public IEnumerator TillysGlint_IsAnOldTapeDeck_PickedUpOnce_SavedAndRestored()
        {
            _fixture = GameplayFixture.Boot(_controls);
            yield return null;
            GameplaySystem gameplay = _fixture.Gameplay;
            TapeDeckFind deck = gameplay.TapeDeck;
            var found = new List<TapeDeckFound>();
            using var listening = _fixture.Bootstrap.Context.Events.Subscribe<TapeDeckFound>(found.Add);
            yield return null;
            Assert.IsFalse(deck.IsResting, "no glint while Tilly sleeps");

            gameplay.Friends.Restore(new FriendsSaveData
            {
                friends = new[] { new FriendSaveData { id = "tilly", state = (int)FriendState.Awake, welcomed = true } },
            });
            yield return Waits.Until(() => deck.IsResting, 4f);
            _fixture.Rover.Place(deck.RestPosition, 0f);
            yield return Waits.Until(() => deck.Found, 8f);
            Assert.AreEqual(1, found.Count, "picked up once");
            Assert.IsFalse(found[0].Restored, "the live find: Rover fits it with its moment");
            Assert.IsFalse(deck.IsResting);

            TapeDeckSaveData saved = deck.Capture();
            Assert.IsTrue(saved.found, "saved");
            deck.Restore(saved);
            deck.Announce();
            Assert.AreEqual(2, found.Count);
            Assert.IsTrue(found[1].Restored, "a loaded game's deck is back on 07 at once");
            yield return null;
            yield return null;
            Assert.IsFalse(deck.IsResting, "a found deck never lies on the ground again");
        }
    }
}
