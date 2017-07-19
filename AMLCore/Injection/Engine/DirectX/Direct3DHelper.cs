using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Engine.DirectX
{
    public class Direct3DHelper
    {
        public static IntPtr Device { get; private set; }
        private static List<Action<IntPtr>> _InjectActions = new List<Action<IntPtr>>();

        public static void InjectDevice(Action<IntPtr> func)
        {
            if (Device != IntPtr.Zero)
            {
                func(Device);
                _InjectActions.Add(func);
            }
            else
            {
                _InjectActions.Add(func);
            }
        }

        internal static void OnDeviceCreated(IntPtr device)
        {
            Device = device;
            _InjectActions.ForEach(a => a(device));
        }
    }
}
