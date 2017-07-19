using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Engine.Input
{
    public interface IInputHandler
    {
        bool HandleInput(IntPtr ptr);
    }
}
