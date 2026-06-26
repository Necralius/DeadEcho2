using System;
using System.Collections.Generic;

namespace Game.Core.Events
{
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, Delegate> _handlers = new();

        public void Publish<T>(T evt) where T : struct
        {
            if (_handlers.TryGetValue(typeof(T), out var del) && del is Action<T> action)
                action.Invoke(evt);
        }

        public IDisposable Subscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (_handlers.TryGetValue(type, out var del))
                _handlers[type] = (Action<T>)del + handler;
            else
                _handlers[type] = handler;

            return new Subscription(() => Unsubscribe(handler));
        }

        public void Unsubscribe<T>(Action<T> handler) where T : struct
        {
            var type = typeof(T);
            if (!_handlers.TryGetValue(type, out var del)) return;

            var current = (Action<T>)del - handler;
            if (current == null) _handlers.Remove(type);
            else _handlers[type] = current;
        }

        private sealed class Subscription : IDisposable
        {
            private Action _dispose;
            public Subscription(Action dispose) => _dispose = dispose;
            public void Dispose()
            {
                _dispose?.Invoke();
                _dispose = null;
            }
        }
    }
}
