using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.GSO
{
    public interface ICustomMessageHandler
    {
        void Receive(byte[] buffer, int start, int length);
    }
}
