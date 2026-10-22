using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public static class Resources
    {
        public static readonly Dictionary<Type, object[]> Loaded = new Dictionary<Type, object[]>();
        public static int Searches;
        public static T[] FindObjectsOfTypeAll<T>() where T : Object
        {
            Searches++;
            if (!Loaded.TryGetValue(typeof(T), out var values)) return Array.Empty<T>();
            var result = new T[values.Length]; for (int at = 0; at < result.Length; at++) result[at] = (T)values[at];
            return result;
        }
    }
}
