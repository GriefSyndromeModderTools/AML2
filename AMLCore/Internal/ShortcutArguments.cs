using AMLCore.Misc;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace AMLCore.Internal
{
    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLink
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out IntPtr pfd, int fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("45e2b4ae-b1c3-11d0-b92f-00a0c90312e1")]
    internal interface IShellLinkDataList
    {
        void AddDataBlock(IntPtr pDataBlock);
        void CopyDataBlock(uint dwSig, out IntPtr ppDataBlock);
        void RemoveDataBlock(uint dwSig);
        void GetFlags(out uint pdwFlags);
        void SetFlags(uint dwFlags);
    }

    enum ShortcutStartupMode
    {
        Game,
        GSO,
        Launcher,
    }

    class ShortcutArguments : CommonArguments
    {
        public ShortcutStartupMode Mode { get; private set; }

        public static ShortcutArguments Create(PluginContainer[] containers, ShortcutStartupMode mode)
        {
            var ret = new ShortcutArguments();
            ret.GetPluginOptions(containers);
            ret.Mode = mode;
            return ret;
        }

        public void Save(string filename, string presetOptions, bool useRelPath)
        {
            var link = (IShellLink)new ShellLink();

            if (useRelPath)
            {
                //Haven't got this working.
                var dataList = (IShellLinkDataList)link;
                dataList.GetFlags(out var flags);
                flags |= 0x100 /*SLDF_FORCE_NO_LINKINFO*/ | 0x40000 /*SLDF_FORCE_NO_LINKTRACK*/;
                dataList.SetFlags(flags);
                link.SetDescription("AML shortcut");
                link.SetPath("Launcher.exe");
            }
            else
            {
                link.SetDescription("AML shortcut");
                link.SetPath(PathHelper.GetPath("Launcher.exe"));
                link.SetWorkingDirectory(PathHelper.GetPath(""));
            }
            var processNameArg = "";
            var guiArg = "";
            switch (Mode)
            {
                case ShortcutStartupMode.Game:
                    guiArg = "NoGui ";
                    break;
                case ShortcutStartupMode.GSO:
                    processNameArg = "ProcessName=griefsyndrome_online.exe ";
                    guiArg = "NoGui ";
                    break;
                case ShortcutStartupMode.Launcher:
                    guiArg = "Gui ";
                    break;
            }
            var sb = new StringBuilder();
            sb.Append(guiArg);
            sb.Append(processNameArg);
            if (!string.IsNullOrWhiteSpace(Mods))
            {
                sb.Append("Mods=");
                sb.Append(Mods);
                sb.Append(' ');
            }
            foreach (var o in Options)
            {
                if (o.Item1 == null) continue;
                sb.Append(o.Item1);
                if (o.Item2 != null)
                {
                    sb.Append('=');
                    sb.Append(o.Item2);
                }
                sb.Append(' ');
            }
            sb.Append("PresetOptions=");
            sb.Append(presetOptions);
            link.SetArguments(sb.ToString());
            var linkFile = (IPersistFile)link;

            linkFile.Save(PathHelper.GetPath(filename + ".lnk"), false);
        }
    }
}
