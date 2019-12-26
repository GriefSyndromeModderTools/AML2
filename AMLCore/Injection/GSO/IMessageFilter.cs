using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.GSO
{
    //Note that this interface will be executed on other threads
    interface IMessageFilter
    {
        void FilterSend(ConnectedPeer peer, IntPtr buffer, ref int len);
        void FilterReceive(ConnectedPeer peer, IntPtr buffer, ref int len);
    }
}
