using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using MoonProject.Core;

namespace MoonProject.Tests.EditMode.Core
{
    public sealed class EventBusTests
    {
        private readonly struct Ping
        {
            public Ping(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }

        private EventBus _bus;

        [SetUp]
        public void SetUp()
        {
            _bus = new EventBus();
        }

        [Test]
        public void Publish_WithoutSubscribers_DoesNothing()
        {
            Assert.DoesNotThrow(() => _bus.Publish(new Ping(1)));
            Assert.AreEqual(0, _bus.SubscriberCount<Ping>());
        }

        [Test]
        public void Publish_DeliversPayloadToEverySubscriber()
        {
            int first = 0;
            int second = 0;
            _bus.Subscribe<Ping>(p => first = p.Value);
            _bus.Subscribe<Ping>(p => second = p.Value);

            _bus.Publish(new Ping(7));

            Assert.AreEqual(7, first);
            Assert.AreEqual(7, second);
        }

        [Test]
        public void Dispose_Unsubscribes_AndIsIdempotent()
        {
            int calls = 0;
            IDisposable token = _bus.Subscribe<Ping>(_ => calls++);

            token.Dispose();
            token.Dispose();
            _bus.Publish(new Ping(1));

            Assert.AreEqual(0, calls);
            Assert.AreEqual(0, _bus.SubscriberCount<Ping>());
        }

        [Test]
        public void UnsubscribingDuringDispatch_DoesNotSkipOtherHandlers()
        {
            int later = 0;
            IDisposable self = null;
            self = _bus.Subscribe<Ping>(_ => self.Dispose());
            _bus.Subscribe<Ping>(_ => later++);

            _bus.Publish(new Ping(1));
            _bus.Publish(new Ping(2));

            Assert.AreEqual(2, later);
            Assert.AreEqual(1, _bus.SubscriberCount<Ping>());
        }

        [Test]
        public void SubscribingDuringDispatch_TakesEffectFromNextPublish()
        {
            int added = 0;
            bool subscribed = false;
            _bus.Subscribe<Ping>(_ =>
            {
                if (!subscribed)
                {
                    subscribed = true;
                    _bus.Subscribe<Ping>(__ => added++);
                }
            });

            _bus.Publish(new Ping(1));
            Assert.AreEqual(0, added);

            _bus.Publish(new Ping(2));
            Assert.AreEqual(1, added);
        }

        [Test]
        public void ThrowingHandler_IsLogged_AndOthersStillRun()
        {
            int after = 0;
            _bus.Subscribe<Ping>(_ => throw new InvalidOperationException("boom"));
            _bus.Subscribe<Ping>(_ => after++);

            LogAssert.Expect(LogType.Exception, "InvalidOperationException: boom");
            _bus.Publish(new Ping(1));

            Assert.AreEqual(1, after);
        }

        [Test]
        public void Subscribe_NullHandler_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _bus.Subscribe<Ping>(null));
        }
    }
}
