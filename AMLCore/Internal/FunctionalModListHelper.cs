using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Internal
{
    internal class FunctionalModListHelper
    {
        private static Dictionary<string, bool> _cache = new Dictionary<string, bool>();

        private static bool IsFunctionalMod(string name)
        {
            lock (_cache)
            {
                if (_cache.TryGetValue(name, out var ret))
                {
                    return ret;
                }
                var container = PluginLoader.GetTemporaryContainer(name);
                if (container != null)
                {
                    var type = container.Type;
                    if (type == PluginType.EffectOnly || type == PluginType.Functional)
                    {
                        _cache.Add(name, true);
                        return true;
                    }
                }
                _cache.Add(name, false);
                return false;
            }
        }

        public static CommonArguments SelectFunctionalMods(CommonArguments arguments)
        {
            var ret = new CommonArguments();
            if (arguments == null || arguments.Mods == null)
            {
                return ret;
            }
            var mods = arguments.Mods.Split(',').Where(IsFunctionalMod).Distinct().ToArray();
            ret.Mods = string.Join(",", mods);
            foreach (var o in arguments.Options)
            {
                if (mods.Any(o.Item1.StartsWith))
                {
                    ret.Options.Add(o);
                }
            }
            return ret;
        }

        public static CommonArguments SelectNonfunctionalMods(CommonArguments arguments)
        {
            var ret = new CommonArguments();
            if (arguments == null || arguments.Mods == null)
            {
                return ret;
            }
            var mods = arguments.Mods.Split(',').Where(mm => !IsFunctionalMod(mm)).Distinct().ToArray();
            ret.Mods = string.Join(",", mods);
            foreach (var o in arguments.Options)
            {
                if (mods.Any(o.Item1.StartsWith))
                {
                    ret.Options.Add(o);
                }
            }
            return ret;
        }

        public static void ReplaceFunctionalMods(CommonArguments oldArgs, CommonArguments newArgs)
        {
            if (oldArgs == null || oldArgs.Mods == null)
            {
                oldArgs = new CommonArguments();
            }
            if (newArgs == null || newArgs.Mods == null)
            {
                newArgs = new CommonArguments();
            }
            var nonfunctional = SelectNonfunctionalMods(oldArgs);
            oldArgs.Mods = nonfunctional.Mods + "," + newArgs.Mods;
            oldArgs.Options.Clear();
            oldArgs.Options.AddRange(nonfunctional.Options);
            oldArgs.Options.AddRange(newArgs.Options);
        }

        public static bool CompareFunctionalMods(CommonArguments target, CommonArguments functionalOnly)
        {
            if (target == null || target.Mods == null)
            {
                target = new CommonArguments();
            }
            if (functionalOnly == null || functionalOnly.Mods == null)
            {
                functionalOnly = new CommonArguments();
            }
            var selected = SelectFunctionalMods(target);
            if (!selected.Mods.Split(',').OrderBy(ss => ss).SequenceEqual(functionalOnly.Mods.Split(',').OrderBy(ss => ss)))
            {
                return false;
            }
            var options1 = target.Options.Select(tt => tt.Item1 + "=" + tt.Item2).OrderBy(ss => ss);
            var options2 = functionalOnly.Options.Select(tt => tt.Item1 + "=" + tt.Item2).OrderBy(ss => ss);
            if (!options1.SequenceEqual(options2))
            {
                return false;
            }
            return true;
        }
    }
}
