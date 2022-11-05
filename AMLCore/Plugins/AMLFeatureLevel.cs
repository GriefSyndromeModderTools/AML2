using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Plugins
{
    public static class AMLFeatureLevel
    {
        //No effect.
        public const uint AML100 = 100;

        //AML version 1.10.
        //AC algorithm v2.
        public const uint AML110 = 110;

        private static readonly HashSet<string> _ignoreACList = new HashSet<string>();

        internal static uint Value;

        //Plugins should call this method with the value for the lowest version of AML that supports it.
        //This should be called before the start of post load event.
        //This should be called in the loader thread (i.e., only in preload or load event).
        public static void SetPluginFeatureLevel(uint value)
        {
            Value = Math.Max(Value, value);
        }

        //This may only be called after the start of postload event, but may be called on any thread.
        internal static bool CheckFeatureLevel(uint value)
        {
            return Value >= value;
        }

        public static void IgnorePluginAC(string id)
        {
            CoreLoggers.Loader.Info($"{id} added to AC ignore list");
            _ignoreACList.Add(id);
        }

        internal static bool IsInACIgnoreList(string id)
        {
            return _ignoreACList.Contains(id);
        }
    }
}
