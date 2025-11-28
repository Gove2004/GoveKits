using System;
using System.Collections.Generic;

namespace GoveKits.Singleton
{
    public class SingletonManager : Singleton<SingletonManager>
    {
        private readonly Dictionary<Type, object> _singletons = new Dictionary<Type, object>();
        private readonly object _lock = new object();

        public void Register<T>(T instance) where T : class
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (!_singletons.ContainsKey(type))
                {
                    _singletons[type] = instance;
                }
            }
        }

        public T Get<T>() where T : class
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (_singletons.TryGetValue(type, out var instance))
                {
                    return instance as T;
                }
                return null;
            }
        }

        public void Unregister<T>() where T : class
        {
            lock (_lock)
            {
                var type = typeof(T);
                if (_singletons.ContainsKey(type))
                {
                    _singletons.Remove(type);
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _singletons.Clear();
            }
        }
    }
}