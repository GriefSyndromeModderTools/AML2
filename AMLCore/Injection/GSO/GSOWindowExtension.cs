using AMLCore.Injection.Native;
using AMLCore.Internal;
using AMLCore.Logging;
using AMLCore.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace AMLCore.Injection.GSO
{
    internal static class GSOWindowExtension
    {
        private struct MenuItemInfoW
        {
            public uint Size;
            public uint Mask;
            public uint Type;
            public uint State;
            public uint ID;
            public IntPtr SubMenu;
            public IntPtr BmpChecked;
            public IntPtr BmpUnchecked;
            public IntPtr ItemData;
            public IntPtr TypeData;
            public uint Cch;
            public IntPtr BmpItem;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();
        [DllImport("user32.dll")]
        private static extern bool InsertMenuItemW(IntPtr menu, uint item, int fByPosition, ref MenuItemInfoW mi);
        [DllImport("user32.dll")]
        private static extern bool SetMenuItemInfoW(IntPtr menu, uint item, int fByPosition, ref MenuItemInfoW mi);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int TrackPopupMenuEx(IntPtr menu, uint flags, int x, int y, IntPtr hwnd, IntPtr tpm);
        [DllImport("user32.dll")]
        private static extern bool ClientToScreen(IntPtr hwnd, ref int point);

        private static IntPtr _window;
        private static IntPtr _button;
        private static IntPtr _menu;

        private const int ExtButtonWidth = 36;
        private const int ExtButtonHeight = 23;

        private class InjectionGSOCreateWindow : CodeInjection
        {
            public InjectionGSOCreateWindow() : base(AddressHelper.Code("gso", 0x1A52), 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var ebp = env.GetRegister(Register.EBP);
                var hInst = AddressHelper.Code(0);
                var hMenu = (IntPtr)0x5001;
                var hParent = Marshal.ReadIntPtr(ebp + 4 * 3);
                _button = Natives.CreateWindowEx(0, "BUTTON", "...", 0x50010000,
                    5, 215, ExtButtonWidth, ExtButtonHeight, hParent, hMenu, hInst, IntPtr.Zero);
                _window = hParent;
                InitMenu();
            }
        }

        private class InjectGSOWindowCommand : CodeInjection
        {
            private static readonly int[] _pt = new int[2];

            public InjectGSOWindowCommand() : base(AddressHelper.Code("gso", 0x1F80), 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var msg = env.GetParameterI(01);
                var id = (ushort)env.GetParameterI(2);
                if (msg == 0x111 /* WM_COMMAND */ && id == 0x5001)
                {
                    _pt[0] = 0;
                    _pt[1] = ExtButtonHeight;
                    if (!ClientToScreen(_button, ref _pt[0]))
                    {
                        var mouse = Control.MousePosition;
                        _pt[0] = mouse.X;
                        _pt[1] = mouse.Y;
                    }
                    RefreshMenu();
                    var sel = TrackPopupMenuEx(_menu, 0x0180, _pt[0], _pt[1], _window, IntPtr.Zero);
                    if (sel == 0)
                    {
                        var err = Marshal.GetLastWin32Error();
                        if (err != 0)
                        {
                            CoreLoggers.GSO.Info("TrackPopupMenuEx error:" + err);
                        }
                    }
                    else
                    {
                        TriggerMenu(sel - 1);
                    }
                }
            }
        }

        public static void Inject()
        {
            new InjectionGSOCreateWindow();
            new InjectGSOWindowCommand();
        }

        private static MenuItemInfoW CreateMenuInfoW()
        {
            MenuItemInfoW mi = new MenuItemInfoW();
            mi.Size = (uint)Marshal.SizeOf(typeof(MenuItemInfoW));
            mi.Mask = 0; //set below
            mi.Type = 0; //string
            mi.State = 0; //set below
            mi.ID = 0; //set below
            mi.SubMenu = IntPtr.Zero;
            mi.BmpChecked = IntPtr.Zero;
            mi.BmpUnchecked = IntPtr.Zero;
            mi.ItemData = IntPtr.Zero; //custom data, maybe useful
            mi.TypeData = IntPtr.Zero; //set below
            mi.Cch = 0;
            mi.BmpItem = IntPtr.Zero;
            return mi;
        }

        private static void InitMenu()
        {
            var menu = CreatePopupMenu();
            var mi = CreateMenuInfoW();
            mi.Mask = 0x0143; //type + string + state + ID
            var createdList = GSOExtensionMenu._createdMenuItems;
            foreach (var item in GSOExtensionMenu._menuItems)
            {
                var s = item.State;
                InsertMenu(menu, createdList.Count, ref mi, item.Text, s);
                createdList.Add(new GSOExtensionMenu.CreatedMenuItemInfo
                {
                    Item = item,
                    State = s,
                });
            }

            _menu = menu;
        }

        private static void InsertMenu(IntPtr menu, int index, ref MenuItemInfoW mi, string text, GSOExtensionMenu.MenuItemState s)
        {
            var p = Marshal.StringToHGlobalUni(text);
            mi.TypeData = p;
            mi.State = (s.Disabled ? 3u : 0u) | (s.Selected ? 8u : 0u);
            mi.ID = (uint)(index + 1);
            var ret = InsertMenuItemW(menu, (uint)index, 1, ref mi);
            Marshal.FreeHGlobal(p);
            if (!ret)
            {
                CoreLoggers.GSO.Error("Cannot insert menu");
            }
        }

        private static void RefreshMenu()
        {
            var mi = CreateMenuInfoW();
            mi.Mask = 1; //state
            for (int i = 0; i < GSOExtensionMenu._createdMenuItems.Count; ++i)
            {
                var item = GSOExtensionMenu._createdMenuItems[i];
                item.Item.TriggerBeforeShow();
                if (item.State != item.Item.State)
                {
                    var s = item.Item.State;
                    mi.State = (s.Disabled ? 3u : 0u) | (s.Selected ? 8u : 0u);
                    item.State = s;
                    SetMenuItemInfoW(_menu, (uint)(i), 1, ref mi);
                }
            }
        }

        private static void TriggerMenu(int index)
        {
            GSOExtensionMenu._createdMenuItems[index].Item.TriggerClick();
        }
    }
}
