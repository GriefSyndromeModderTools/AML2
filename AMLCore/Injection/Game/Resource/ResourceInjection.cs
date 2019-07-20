﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Resource
{
    public interface IResourceContentProvider
    {
        byte[] GetResourceContent(string path);
    }

    public class ResourceInjection
    {
        private static Dictionary<string, int> _pathMap = new Dictionary<string, int>();
        private static List<byte[]> _items = new List<byte[]>();
        private static object _lock = new object();
        private static List<IResourceContentProvider> _providers = new List<IResourceContentProvider>();

        public static void AddResource(string path, byte[] content)
        {
            lock (_lock)
            {
                if (_pathMap.TryGetValue(path, out var oldId))
                {
                    _items[oldId] = null;
                }
                var id = _items.Count;
                _items.Add(content);
                _pathMap[path] = id;
            }
        }

        public static void AddProvider(IResourceContentProvider p)
        {
            lock (_lock)
            {
                _providers.Add(p);
            }
        }

        internal static byte[] GetResource(string path)
        {
            lock (_lock)
            {
                if (_pathMap.TryGetValue(path, out var id))
                {
                    return _items[id];
                }
                foreach (var p in _providers)
                {
                    var c = p.GetResourceContent(path);
                    if (c != null)
                    {
                        id = _items.Count;
                        _items.Add(c);
                        _pathMap[path] = id;
                        return c;
                    }
                }
                return null;
            }
        }

        internal static byte[] GetResource(int id)
        {
            lock (_lock)
            {
                if (id == -1) return null;
                return _items[id];
            }
        }

        internal static int GetResourceId(string path)
        {
            lock (_lock)
            {
                if (_pathMap.TryGetValue(path, out var id))
                {
                    return id;
                }
                foreach (var p in _providers)
                {
                    var c = p.GetResourceContent(path);
                    if (c != null)
                    {
                        id = _items.Count;
                        _items.Add(c);
                        _pathMap[path] = id;
                        return id;
                    }
                }
                return -1;
            }
        }
    }
}