using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MoonProject.Autoplay.Tests
{
    /// <summary>
    /// The bot's mouse buttons (the tether key is the right one): the buttons are bits of one field, so pressing and
    /// releasing goes in as whole mouse states that keep the position the bot has set.
    /// </summary>
    public sealed class VirtualMouseTests
    {
        private static readonly Vector2 Spot = new Vector2(640f, 360f);

        [Test]
        public void TheRightAndLeftButtons_PressAndRelease_WithoutMovingThePointer()
        {
            using (var input = new VirtualInput())
            {
                Mouse mouse = Find();
                input.MoveMouseTo(Spot);
                InputSystem.Update();
                Assert.AreEqual(Spot, mouse.position.ReadValue());

                input.SetRightButton(true);
                InputSystem.Update();
                Assert.IsTrue(mouse.rightButton.isPressed, "the right button goes down");
                Assert.IsFalse(mouse.leftButton.isPressed, "and only it");
                Assert.AreEqual(Spot, mouse.position.ReadValue(), "the pointer stays where the bot put it");

                input.SetLeftButton(true);
                InputSystem.Update();
                Assert.IsTrue(mouse.leftButton.isPressed && mouse.rightButton.isPressed, "both held");

                input.SetRightButton(false);
                InputSystem.Update();
                Assert.IsFalse(mouse.rightButton.isPressed, "the right button comes up");
                Assert.IsTrue(mouse.leftButton.isPressed, "the left one stays down");

                input.SetLeftButton(false);
                InputSystem.Update();
                Assert.IsFalse(mouse.leftButton.isPressed);
                Assert.AreEqual(Spot, mouse.position.ReadValue());
            }
        }

        [Test]
        public void TheWheel_TurnsByWholeNotches()
        {
            using (var input = new VirtualInput())
            {
                Mouse mouse = Find();
                input.Scroll(1f);
                InputSystem.Update();
                Assert.AreEqual(120f, mouse.scroll.ReadValue().y, 1e-3f, "one notch away from the player");
                input.Scroll(-2f);
                InputSystem.Update();
                Assert.AreEqual(-240f, mouse.scroll.ReadValue().y, 1e-3f, "two notches back");
            }
        }

        private static Mouse Find()
        {
            foreach (InputDevice device in InputSystem.devices)
            {
                if (device is Mouse mouse && device.name == "AutoplayMouse")
                {
                    return mouse;
                }
            }

            Assert.Fail("the virtual mouse was not added");
            return null;
        }
    }
}
