using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// Typed publish/subscribe hub owned by <see cref="GameContext"/>. Events are readonly structs declared in
    /// <c>Core/Events</c>. Publishing does not allocate; subscribing allocates once per subscription.
    /// Handlers may subscribe or unsubscribe while an event is being dispatched.
    /// </summary>
    public sealed class EventBus
    {
        private readonly Dictionary<Type, object> _channels = new Dictionary<Type, object>();

        /// <summary>Registers <paramref name="handler"/>; dispose the returned token to unsubscribe.</summary>
        public IDisposable Subscribe<T>(Action<T> handler) where T : struct
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Channel<T> channel = GetOrCreateChannel<T>();
            channel.Add(handler);
            return new Subscription<T>(channel, handler);
        }

        /// <summary>Delivers <paramref name="evt"/> to every current subscriber of <typeparamref name="T"/>.</summary>
        public void Publish<T>(in T evt) where T : struct
        {
            if (_channels.TryGetValue(typeof(T), out object channel))
            {
                ((Channel<T>)channel).Dispatch(evt);
            }
        }

        /// <summary>Number of live subscribers for <typeparamref name="T"/>.</summary>
        public int SubscriberCount<T>() where T : struct
        {
            return _channels.TryGetValue(typeof(T), out object channel) ? ((Channel<T>)channel).Count : 0;
        }

        private Channel<T> GetOrCreateChannel<T>() where T : struct
        {
            if (!_channels.TryGetValue(typeof(T), out object channel))
            {
                channel = new Channel<T>();
                _channels.Add(typeof(T), channel);
            }

            return (Channel<T>)channel;
        }

        /// <summary>
        /// Copy-on-write handler list: dispatch iterates an immutable snapshot, so (un)subscribing during dispatch
        /// is safe and publishing never allocates.
        /// </summary>
        private sealed class Channel<T> where T : struct
        {
            private Action<T>[] _handlers = Array.Empty<Action<T>>();

            public int Count => _handlers.Length;

            public void Add(Action<T> handler)
            {
                var next = new Action<T>[_handlers.Length + 1];
                Array.Copy(_handlers, next, _handlers.Length);
                next[_handlers.Length] = handler;
                _handlers = next;
            }

            public void Remove(Action<T> handler)
            {
                int index = Array.IndexOf(_handlers, handler);
                if (index < 0)
                {
                    return;
                }

                var next = new Action<T>[_handlers.Length - 1];
                Array.Copy(_handlers, 0, next, 0, index);
                Array.Copy(_handlers, index + 1, next, index, _handlers.Length - index - 1);
                _handlers = next;
            }

            public void Dispatch(in T evt)
            {
                Action<T>[] snapshot = _handlers;
                for (int i = 0; i < snapshot.Length; i++)
                {
                    try
                    {
                        snapshot[i](evt);
                    }
                    catch (Exception exception)
                    {
                        // One faulty listener must not silence the others; the exception is still surfaced.
                        Debug.LogException(exception);
                    }
                }
            }
        }

        private sealed class Subscription<T> : IDisposable where T : struct
        {
            private Channel<T> _channel;
            private Action<T> _handler;

            public Subscription(Channel<T> channel, Action<T> handler)
            {
                _channel = channel;
                _handler = handler;
            }

            public void Dispose()
            {
                if (_channel == null)
                {
                    return;
                }

                _channel.Remove(_handler);
                _channel = null;
                _handler = null;
            }
        }
    }
}
