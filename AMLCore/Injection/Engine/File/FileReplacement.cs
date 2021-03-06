﻿using AMLCore.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.File
{
    public static class FileReplacement
    {
        private static Dictionary<string, IFileProxyFactory> _FactoryList = new Dictionary<string, IFileProxyFactory>();
        private static Dictionary<int, IFileProxy> _ActiveFiles = new Dictionary<int, IFileProxy>();

        public static void RegisterFile(string fullPath, IFileProxyFactory p)
        {
            if (_FactoryList.ContainsKey(fullPath))
            {
                throw new Exception("duplicate file replacement");
            }
            _FactoryList.Add(fullPath, p);
        }

        internal static void OpenFile(string path, int mode, int handle)
        {
            var fullPath = PathHelper.GetPath(path);
            IFileProxyFactory fac;
            if (_FactoryList.TryGetValue(fullPath, out fac))
            {
                _ActiveFiles.Add(handle, fac.Create(fullPath));
            }
        }

        internal static bool ReadFile(int handle, IntPtr buffer, int len, IntPtr read)
        {
            IFileProxy p;
            if (_ActiveFiles.TryGetValue(handle, out p))
            {
                Marshal.WriteInt32(read, p.Read(buffer, len));
                return true;
            }
            return false;
        }

        internal static bool SetFilePointer(int handle, int dist, IntPtr dist2, int method, out int ret)
        {
            IFileProxy p;
            if (_ActiveFiles.TryGetValue(handle, out p))
            {
                if (dist2 != IntPtr.Zero)
                {
                    //this should not happen
                    ret = -1;
                    return true;
                }
                ret = p.Seek(method, dist);
                return true;
            }
            ret = 0;
            return false;
        }

        internal static void CloseHandle(int handle)
        {
            _ActiveFiles.Remove(handle);
        }
    }
}
