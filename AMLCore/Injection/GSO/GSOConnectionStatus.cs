using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.GSO
{
    [StructLayout(LayoutKind.Sequential)]
    public class ConnectedPeer
    {
        public readonly IPAddress Address;

        public struct IPAddress
        {
            public ulong AddrLow, AddrHigh;
        }

        public ConnectedPeer(ulong low, ulong high)
        {
            Address = new IPAddress
            {
                AddrLow = low,
                AddrHigh = high,
            };
        }

        public override string ToString()
        {
            var port = (ushort)(0xFFFF & (Address.AddrLow >> 16));
            var ip = (uint)(Address.AddrLow >> 32);
            var ip0 = ip >> 24;
            var ip1 = (ip >> 16) & 0xFF;
            var ip2 = (ip >> 8) & 0xFF;
            var ip3 = ip & 0xFF;
            var port0 = port >> 8;
            var port1 = port & 0xFF;
            var portx = port0 | port1 << 8;
            return string.Format("{0}.{1}.{2}.{3}:{4}", ip3, ip2, ip1, ip0, portx);
        }
    }

    public class ServerConnectionStatus
    {
        internal ServerConnectionStatus(IntPtr server)
        {
            _clients = new List<ConnectedPeer>();
            Clients = new ReadOnlyCollection<ConnectedPeer>(_clients);
            Server = server;
            Socket = Marshal.ReadIntPtr(server, 0x18);
            Lock = server + 0x8BC;
            UpdateClientList();
        }

        internal readonly List<ConnectedPeer> _clients;
        public readonly ReadOnlyCollection<ConnectedPeer> Clients;

        public readonly IntPtr Server;
        public readonly IntPtr Socket;
        public readonly IntPtr Lock;

        internal void UpdateClientList()
        {
            _clients.Clear();
            for (int i = 0; i < 2; ++i)
            {
                ulong low = (ulong)Marshal.ReadInt64(Server, 0x2C + i * 0x10);
                if ((low & 0xFFFF) == 0)
                {
                    _clients.Add(null);
                }
                else
                {
                    ulong high = (ulong)Marshal.ReadInt64(Server, 0x34);
                    _clients.Add(new ConnectedPeer(low, high));
                }
            }
            GSOWindowLog.WriteLine("Client 1: {0}", _clients[0]?.ToString() ?? "null");
            GSOWindowLog.WriteLine("Client 2: {0}", _clients[1]?.ToString() ?? "null");
        }

        internal void DisconnectClient(ConnectedPeer peer)
        {
            if (_clients[0] == peer)
            {
                _clients[0] = null;
            }
            else if (_clients[1] == peer)
            {
                _clients[1] = null;
            }
            GSOWindowLog.WriteLine("Client 1: {0}", _clients[0]?.ToString() ?? "null");
            GSOWindowLog.WriteLine("Client 2: {0}", _clients[1]?.ToString() ?? "null");
        }

        public void Send(ConnectedPeer peer, IntPtr buffer, int len)
        {
            Natives.EnterCriticalSection(Lock);
            ConnectedPeer.IPAddress addr = peer.Address;
            Natives.SendTo(Socket, buffer, len, 0, ref addr.AddrLow, 16);
            Natives.LeaveCriticalSection(Lock);
        }

        public void Send(IntPtr buffer, int len)
        {
            Natives.EnterCriticalSection(Lock);
            for (int i = 0; i < _clients.Count; ++i)
            {
                if (_clients[i] != null)
                {
                    ConnectedPeer.IPAddress addr = _clients[i].Address;
                    Natives.SendTo(Socket, buffer, len, 0, ref addr.AddrLow, 16);
                }
            }
            Natives.LeaveCriticalSection(Lock);
        }

        public void Send(ConnectedPeer peer, byte[] data)
        {
            Natives.EnterCriticalSection(Lock);
            ConnectedPeer.IPAddress addr = peer.Address;
            Natives.SendTo(Socket, ref data[0], data.Length, 0, ref addr.AddrLow, 16);
            Natives.LeaveCriticalSection(Lock);
        }

        public void Send(byte[] data)
        {
            Natives.EnterCriticalSection(Lock);
            for (int i = 0; i < _clients.Count; ++i)
            {
                if (_clients[i] != null)
                {
                    ConnectedPeer.IPAddress addr = _clients[i].Address;
                    Natives.SendTo(Socket, ref data[0], data.Length, 0, ref addr.AddrLow, 16);
                }
            }
            Natives.LeaveCriticalSection(Lock);
        }

        public bool IsKnownPeer(ConnectedPeer peer)
        {
            return peer == _clients[0] || peer == _clients[1];
        }
    }

    public class ClientConnectionStatus
    {
        public readonly ConnectedPeer Server;
        public readonly IntPtr Socket;
        public readonly IntPtr Lock;

        public ClientConnectionStatus(IntPtr client, ConnectedPeer peer)
        {
            Socket = Marshal.ReadIntPtr(client, 0x18);
            Lock = client + 0x8BC;
            Server = peer;
        }

        public void Send(IntPtr buffer, int len)
        {
            Natives.EnterCriticalSection(Lock);
            ConnectedPeer.IPAddress addr = Server.Address;
            Natives.SendTo(Socket, buffer, len, 0, ref addr.AddrLow, 16);
            Natives.LeaveCriticalSection(Lock);
        }

        public void Send(byte[] data)
        {
            Natives.EnterCriticalSection(Lock);
            ConnectedPeer.IPAddress addr = Server.Address;
            Natives.SendTo(Socket, ref data[0], data.Length, 0, ref addr.AddrLow, 16);
            Natives.LeaveCriticalSection(Lock);
        }
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
