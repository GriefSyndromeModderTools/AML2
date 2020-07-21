using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Internal
{
    internal class FunctionalModListHelper
    {
        private static Dictionary<string, bool> _isFunctional = new Dictionary<string, bool>();
        private static Dictionary<string, string> _version = new Dictionary<string, string>();
        private static Dictionary<string, string[]> _optionIgnoreList = new Dictionary<string, string[]>();

        private static void LoadCache(string name, out bool isF, out string v, out string[] ignoreList)
        {
            var container = PluginLoader.GetTemporaryContainer(name);
            isF = container != null && (container.Type == PluginType.EffectOnly || container.Type == PluginType.Functional);
            _isFunctional.Add(name, isF);
            v = container?.AssemblyVersion ?? "0";
            _version.Add(name, v);
            ignoreList = container.GetExtension<IPluginOptionExtSyncArgument>()?.GetOptionSyncIgnoreList() ?? new string[0];
            _optionIgnoreList.Add(name, ignoreList);
        }

        private static string GetLocalVersion(string name)
        {
            lock (_isFunctional)
            {
                if (!_version.TryGetValue(name, out var ret))
                {
                    LoadCache(name, out _, out ret, out _);
                }
                return ret;
            }
        }

        private static bool IsFunctionalMod(string name)
        {
            lock (_isFunctional)
            {
                if (!_isFunctional.TryGetValue(name, out var ret))
                {
                    LoadCache(name, out ret, out _, out _);
                }
                return ret;
            }
        }

        private static string[] GetOptionIgnoreList(string name)
        {
            lock (_isFunctional)
            {
                if (!_optionIgnoreList.TryGetValue(name, out var ret))
                {
                    LoadCache(name, out _, out _, out ret);
                }
                return ret;
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
                var splitOptionName = o.Item1.Split('.');
                if (mods.Any(modName => modName == splitOptionName[0] &&
                        !GetOptionIgnoreList(modName).Contains(splitOptionName[1])))
                {
                    ret.Options.Add(o);
                }
            }
            return ret;
        }

        private static CommonArguments SelectNonfunctionalMods(CommonArguments arguments)
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

        private static void CopyIgnoredOptions(CommonArguments copyFrom, CommonArguments copyTo, CommonArguments filterMods)
        {
            var filterList = filterMods.Mods.Split(',');
            foreach (var option in copyFrom.Options)
            {
                var splitOptionName = option.Item1.Split('.');
                if (filterList.Contains(splitOptionName[0]))
                {
                    copyTo.Options.Add(option);
                }
            }
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

            //Copy effect-only options for mods existing in newArgs into nonfunctional.
            CopyIgnoredOptions(oldArgs, nonfunctional, newArgs);

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
            var options1 = selected.Options.Select(tt => tt.Item1 + "=" + tt.Item2).OrderBy(ss => ss);
            var options2 = functionalOnly.Options.Select(tt => tt.Item1 + "=" + tt.Item2).OrderBy(ss => ss);
            if (!options1.SequenceEqual(options2))
            {
                return false;
            }
            return true;
        }

        public static void AddModVersionInfo(CommonArguments args)
        {
            foreach (var mod in args.Mods.Split(',').Where(mm => IsFunctionalMod(mm)))
            {
                args.ModVersions[mod] = GetLocalVersion(mod);
            }
        }

        public static bool CheckModVersion(CommonArguments args)
        {
            foreach (var vv in args.ModVersions)
            {
                if (GetLocalVersion(vv.Key) != vv.Value)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
