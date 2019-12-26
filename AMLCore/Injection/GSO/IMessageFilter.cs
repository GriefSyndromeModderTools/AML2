using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.GSO
{
    interface IMessageFilter
    {
        void FilterSend(ConnectedPeer peer, IntPtr buffer, ref int len);
        void FilterReceive(ConnectedPeer peer, IntPtr buffer, ref int len);
    }
}
