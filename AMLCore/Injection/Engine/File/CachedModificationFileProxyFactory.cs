﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Engine.File
{
    public abstract class CachedModificationFileProxyFactory : IFileProxyFactory
    {
        public abstract byte[] Modify(byte[] data);
        private byte[] _Data;

        public IFileProxy Create(string fullpath)
        {
            if (_Data == null)
            {
                _Data = Modify(System.IO.File.ReadAllBytes(fullpath));
            }
            return new SimpleFileProxy(_Data);
        }
    }
}
