using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.GSO
{
    public static class GSOExtensionMenu
    {
        public sealed class MenuItemInfo
        {
            public string Text { get; }
            public bool StartGroup { get; }
            public bool FinishGroup { get; }
            public MenuItemState State;

            public event EventHandler BeforeShow;
            public event EventHandler Click;

            public MenuItemInfo(string text)
            {
                Text = text;
            }

            public MenuItemInfo(string text, bool startGroup, bool finishGroup)
            {
                Text = text;
                StartGroup = startGroup;
                FinishGroup = finishGroup;
            }

            internal void TriggerBeforeShow()
            {
                BeforeShow?.Invoke(this, EventArgs.Empty);
            }

            internal void TriggerClick()
            {
                Click?.Invoke(this, EventArgs.Empty);
            }
        }

        public struct MenuItemState : IEquatable<MenuItemState>
        {
            public bool Disabled;
            public bool Selected;

            public override bool Equals(object obj)
            {
                return obj is MenuItemState state && Equals(state);
            }

            public bool Equals(MenuItemState other)
            {
                return Disabled == other.Disabled &&
                       Selected == other.Selected;
            }

            public override int GetHashCode()
            {
                int hashCode = -1994631550;
                hashCode = hashCode * -1521134295 + Disabled.GetHashCode();
                hashCode = hashCode * -1521134295 + Selected.GetHashCode();
                return hashCode;
            }

            public static bool operator ==(MenuItemState left, MenuItemState right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(MenuItemState left, MenuItemState right)
            {
                return !(left == right);
            }
        }

        internal sealed class CreatedMenuItemInfo
        {
            public MenuItemInfo Item;
            public MenuItemState State;
        }

        internal static readonly List<MenuItemInfo> _menuItems = new List<MenuItemInfo>();
        internal static readonly List<CreatedMenuItemInfo> _createdMenuItems = new List<CreatedMenuItemInfo>();

        public static void AddMenuItems(MenuItemInfo item)
        {
            _menuItems.Add(item);
        }
    }
}
