using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnderDeep.Core
{
    /// <summary>Minimal ServiceLocator for WebGL — no GC in runtime, explicit lifetime in Boot.</summary>
    public static class ServiceLocator
    {
        private static readonly Dictionary<Type, object> _services = new();

        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            _services[type] = service;
            Debug.Log($"[ServiceLocator] Registered {type.Name}");
        }

        public static T Get<T>() where T : class
        {
            var type = typeof(T);
            if (_services.TryGetValue(type, out var svc)) return (T)svc;
            // try interface lookup via is
            foreach (var kv in _services)
                if (kv.Value is T t) return t;
            throw new InvalidOperationException($"Service {type.Name} not registered");
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            if (_services.TryGetValue(typeof(T), out var svc) && svc is T t) { service = t; return true; }
            foreach (var kv in _services) if (kv.Value is T v) { service = v; return true; }
            service = null; return false;
        }

        public static void Clear() => _services.Clear();
    }
}
