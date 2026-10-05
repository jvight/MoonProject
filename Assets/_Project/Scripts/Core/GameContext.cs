using System;
using System.Collections.Generic;
using MoonProject.Core.Input;

namespace MoonProject.Core
{
    /// <summary>
    /// Everything a game system may depend on, created once by the composition root (GameBootstrap) and handed to
    /// systems through <see cref="IGameSystem.Initialize"/>. Domain services are registered by interface so that
    /// domains depend on contracts in Core, never on each other.
    /// </summary>
    public sealed class GameContext
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();

        public GameContext(EventBus events, InputReader input)
        {
            Events = events ?? throw new ArgumentNullException(nameof(events));
            Input = input ?? throw new ArgumentNullException(nameof(input));
        }

        public EventBus Events { get; }

        public InputReader Input { get; }

        /// <summary>Registers <paramref name="service"/> as the single provider of <typeparamref name="T"/>.</summary>
        public void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            if (_services.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"A {typeof(T).Name} service is already registered.");
            }

            _services.Add(typeof(T), service);
        }

        /// <summary>Returns the registered <typeparamref name="T"/>; throws if none was registered (wiring bug).</summary>
        public T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out object service))
            {
                return (T)service;
            }

            throw new InvalidOperationException(
                $"No {typeof(T).Name} service registered. Check the system order in GameBootstrap.");
        }

        /// <summary>For genuinely optional collaborators only (e.g. a debug overlay).</summary>
        public bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out object found))
            {
                service = (T)found;
                return true;
            }

            service = null;
            return false;
        }
    }
}
