using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Engine.File
{
    public interface IFileProxyFactory
    {
        IFileProxy Create(string fullpath);
    }

    public interface IFileProxy
    {
        int Seek(int method, int num);
        int Read(IntPtr buffer, int max);
    }
}
