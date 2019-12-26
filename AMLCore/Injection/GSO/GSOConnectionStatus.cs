using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.GSO
{
    public class ConnectedPeer
    {
        public readonly ulong AddrLow, AddrHigh;

        public ConnectedPeer(ulong low, ulong high)
        {
            AddrLow = low;
            AddrHigh = high;
        }
    }

    public class ServerConnectionStatus
    {
        public ServerConnectionStatus()
        {
            _clients = new List<ConnectedPeer>();
            Clients = new ReadOnlyCollection<ConnectedPeer>(_clients);
        }

        internal readonly List<ConnectedPeer> _clients;
        public readonly ReadOnlyCollection<ConnectedPeer> Clients;

        public readonly IntPtr Socket;
        public readonly IntPtr Lock;
    }

    public class ClientConnectionStatus
    {
        public readonly ConnectedPeer Server;
        public readonly IntPtr Socket;
        public readonly IntPtr Lock;
    }

    public class GSOConnectionStatus
    {
        public static bool Connected => IsServer || IsClient;
        public static bool IsServer => ServerStatus != null;
        public static bool IsClient => ClientStatus != null;

        public static ServerConnectionStatus ServerStatus { get; internal set; }
        public static ClientConnectionStatus ClientStatus { get; internal set; }

        public static event Action OnStatusChange;

        internal static void InvokeStatusChange()
        {
            OnStatusChange?.Invoke();
        }
    }
}
