using AMLCore.Misc;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AMLCore.Internal
{
    public class CommonArguments
    {
        public string Mods { get; set; }
        public List<Tuple<string, string>> Options { get; protected set; }

        public CommonArguments()
        {
            Mods = "";
            Options = new List<Tuple<string, string>>();
        }
        
        public CommonArguments(IEnumerable<CommonArguments> aa)
        {
            Mods = String.Join(",", aa.SelectMany(a => a.SplitMods()).Distinct());
            Options = aa.SelectMany(a => a.Options).Distinct().ToList();
        }

        public byte[] Serialize()
        {
            var ms = new MemoryStream();
            using (var bw = new BinaryWriter(ms, Encoding.UTF8))
            {
                bw.Write(0);
                bw.Write(Mods != null);
                if (Mods != null) bw.Write(Mods);
                bw.Write(Options.Count);
                foreach (var o in Options)
                {
                    if (o.Item1 != null && o.Item2 != null)
                    {
                        bw.Write(o.Item1);
                        bw.Write(o.Item2);
                    }
                }
                ms.Position = 0;
                ms.Write(BitConverter.GetBytes(ms.Length - 4), 0, 4);
                return ms.ToArray();
            }
        }

        public void Read(BinaryReader br)
        {
            Mods = br.ReadBoolean() ? br.ReadString() : null;
            Options = new List<Tuple<string, string>>();
            int c = br.ReadInt32();
            for (int i = 0; i < c; ++i)
            {
                var key = br.ReadString();
                var val = br.ReadString();
                Options.Add(new Tuple<string, string>(key, val));
            }
        }

        public string[] SplitMods()
        {
            return Mods?.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries) ?? new string[0];
        }

        private string[] GetModFileListFromMods()
        {
            var d = PathHelper.GetPath("aml/mods");
            List<string> dlls = new List<string>();
            foreach (var m in SplitMods())
            {
                var f = Path.Combine(d, m + ".dll");
                if (File.Exists(f))
                {
                    dlls.Add(f);
                }
                else
                {
                    CoreLoggers.Loader.Error("cannot initialize mod {0}", m);
                }
            }
            return dlls.ToArray();
        }

        public string[] GetPluginFiles()
        {
            return GetModFileListFromMods();
        }

        internal void SetPluginOptions(PluginContainer[] plugins)
        {
            foreach (var p in plugins)
            {
                p.ResetOption();
            }

            Dictionary<string, PluginContainer> pluginDict = plugins
                .ToDictionary(p => p.AssemblyName);
            foreach (var o in Options)
            {
                var key = o.Item1;
                var seg = key.Split('.');
                if (seg.Length != 2)
                {
                    continue;
                }
                if (!pluginDict.TryGetValue(seg[0], out var p) || !p.HasOptions)
                {
                    CoreLoggers.Loader.Error("unrecognized option {0}", key);
                    continue;
                }
                p.AddOption(seg[1], o.Item2);
            }
        }

        internal void GetPluginOptions(PluginContainer[] plugins)
        {
            Mods = String.Join(",", plugins.Select(p => p.AssemblyName));
            var options = new List<Tuple<string, string>>();
            var currentPlugin = "";
            Action<string, string> append = (string a, string b) =>
            {
                options.Add(new Tuple<string, string>(currentPlugin + "." + a, b));
            };
            foreach (var p in plugins)
            {
                if (p.HasOptions)
                {
                    currentPlugin = p.AssemblyName;
                    p.GetOptions(append);
                }
            }
            Options = options;
        }

        protected void ParseArgumentList(string[] args)
        {
            foreach (var a in args)
            {
                ParseArgument(a);
            }
        }

        private void ParseArgument(string arg)
        {
            //if (arg.Contains('"') || arg.Contains('\\'))
            //{
            //    CoreLoggers.Loader.Error("invalid launcher argument {0}", arg);
            //    return;
            //}
            if (!arg.Contains('='))
            {
                ParseKeyValuePair(arg, null);
            }
            else
            {
                int index = arg.IndexOf('=');
                ParseKeyValuePair(arg.Substring(0, index).Trim(),
                    arg.Substring(index + 1).Trim());
            }
        }

        protected virtual void ParseKeyValuePair(string key, string value)
        {
            if (key == "Mods")
            {
                if (String.IsNullOrEmpty(Mods))
                {
                    Mods = value;
                }
                else
                {
                    Mods = Mods + "," + value;
                }
            }
            else
            {
                Options.Add(new Tuple<string, string>(key, value));
            }
        }
    }
}
