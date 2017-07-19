using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Plugins
{
    public interface IEntryPointPreload
    {
        void Run();
    }
    public interface IEntryPointLoad
    {
        void Run();
    }
    public interface IEntryPointPostload
    {
        void Run();
    }
}
