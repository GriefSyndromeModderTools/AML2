using AMLCore.Injection.Engine.File;
using AMLCore.Internal;
using AMLCore.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.Input
{
    public static class KeyConfigRedirect
    {
        internal static bool Redirected;

        private static int[] _KeyConfig = new int[9 * 3];
        private static int[] _KeyConfigOriginal = new int[9 * 3];
        private static bool _FileInjected = false;

        public static void Redirect()
        {
            if (Redirected)
            {
                return;
            }
            if (_FileInjected)
            {
                throw new InvalidOperationException("try to redirect input after initialization");
            }
            Redirected = true;
            CoreLoggers.Input.Info("key redirected requested by {0}",
                StackTraceHelper.GetCallerMethodName());
        }

        internal static void Inject()
        {
            CalcKeyCodeList();
            FileReplacement.RegisterFile(PathHelper.GetPath("keyconfig.dat"),
                new KeyConfigFile
                {
                    KeyConfig = _KeyConfig,
                    KeyConfigOriginal = _KeyConfigOriginal
                });
        }

        private static void CalcKeyCodeList()
        {
            for (int i = 0; i < _KeyConfig.Length; ++i)
            {
                _KeyConfig[i] = i + 30;
            }
        }

        public static int GetKeyIndex(int key)
        {
            return Redirected ? _KeyConfig[key] : _KeyConfigOriginal[key];
        }

        private static bool[] _RedirectBuffer = new bool[9 * 3];
        internal static void Preprocess(IntPtr data)
        {
            for (int i = 0; i < _KeyConfig.Length; ++i)
            {
                _RedirectBuffer[i] = Marshal.ReadByte(data, _KeyConfigOriginal[i]) == 0x80;
            }
            for (int i = 0; i < _KeyConfig.Length; ++i)
            {
                Marshal.WriteByte(data, _KeyConfig[i], (byte)(_RedirectBuffer[i] ? 0x80 : 0));
            }
        }

        private class KeyConfigFile : CachedModificationFileProxyFactory
        {
            public int[] KeyConfig;
            public int[] KeyConfigOriginal;

            public override byte[] Modify(byte[] data)
            {
                _FileInjected = true;
                Buffer.BlockCopy(data, 0, KeyConfigOriginal, 0, 9 * 3 * 4);
                if (Redirected)
                {
                    Buffer.BlockCopy(KeyConfig, 0, data, 0, 9 * 3 * 4);
                }
                return data;
            }
        }
    }
}
