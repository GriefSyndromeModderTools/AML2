using AMLCore.Injection.Native;
using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.GSO
{
    class GSOConnectionMonitor
    {
        public static void Inject()
        {
            new BeforeServerLoop();
            new InjectCloseSocket();
            Filters.Add(new CrcProtection());
            Filters.Add(new ServerConnectionMonitor());
            Filters.Add(new ClientConnectionMonitor());
            //TODO handle timeout (server 0x77DE, client 0x8003)
        }

        private static void StartServer(IntPtr server)
        {
            GSOWindowLog.WriteLine("Server started");
        }

        private static void CloseSocket(IntPtr socket)
        {
            //TODO need to confirm server/client
            GSOWindowLog.WriteLine("Socket closed");
        }

        internal static List<IMessageFilter> Filters = new List<IMessageFilter>();

        private static ConnectedPeer FindPeer(ulong low, ulong high)
        {
            var client = GSOConnectionStatus.ClientStatus;
            var server = GSOConnectionStatus.ServerStatus;
            if (client != null)
            {
                var p = client.Server;
                if (low == p.AddrLow && high == p.AddrHigh)
                {
                    return p;
                }
            }
            else if (server != null)
            {
                foreach (var p in server.Clients)
                {
                    if (low == p.AddrLow && high == p.AddrHigh)
                    {
                        return p;
                    }
                }
            }
            //TODO check and remove this
            CoreLoggers.GSO.Info("Unrecognized peer");
            return new ConnectedPeer(low, high);
        }

        private static int FilterSendMessage(ulong addrLow, ulong addrHigh, IntPtr buffer, int len)
        {
            var peer = FindPeer(addrLow, addrHigh);
            for (int i = Filters.Count - 1; i >= 0; --i)
            {
                Filters[i].FilterSend(peer, buffer, ref len);
                if (len < 0 || len > 516)
                {
                    CoreLoggers.GSO.Error("Invalid message after filter");
                    return 0;
                }
            }
            return len;
        }

        private static int FilterReceiveMessage(ulong addrLow, ulong addrHigh, IntPtr buffer, int len)
        {
            var peer = FindPeer(addrLow, addrHigh);
            foreach (var f in Filters)
            {
                f.FilterReceive(peer, buffer, ref len);
                if (len < 0 || len > 516)
                {
                    CoreLoggers.GSO.Error("Invalid message after filter");
                    return 0;
                }
            }
            return len;
        }

        private delegate int CloseSocketDelegate(IntPtr s);
        private delegate int SendToDelegate(IntPtr socket, IntPtr buffer, int len, int flags, IntPtr addr, int addrLen);
        private delegate int ReceiveFromDelegate(IntPtr socket, IntPtr buffer, int len, int flags, IntPtr addr, IntPtr addrLen);

        private class BeforeServerLoop : CodeInjection
        {
            public BeforeServerLoop() : base(AddressHelper.Code("gso", 0x7455), 8)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                StartServer(env.GetParameterP(3));
            }
        }

        private class InjectCloseSocket : FunctionPointerInjection<CloseSocketDelegate>
        {
            public InjectCloseSocket() : base(AddressHelper.Code("gso", 0x1C264))
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                CloseSocket(env.GetParameterP(0));
            }
        }

        private class InjectSend : FunctionPointerInjection<SendToDelegate>
        {
            private IntPtr _buffer = Marshal.AllocHGlobal(516);

            public InjectSend() : base(AddressHelper.Code("gso", 0x1C274))
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var s = env.GetParameterP(0);
                var b = env.GetParameterP(1);
                var len = env.GetParameterI(2);
                var f = env.GetParameterI(3);
                var addr = env.GetParameterP(4);
                var addrLen = env.GetParameterI(5);
                if (addrLen == 16)
                {
                    var addrLow = (ulong)Marshal.ReadInt64(addr, 0);
                    var addrHigh = (ulong)Marshal.ReadInt64(addr, 8);
                    Natives.CopyMemory(_buffer, b, len);
                    len = FilterSendMessage(addrLow, addrHigh, _buffer, len);
                    env.SetReturnValue(Original(s, _buffer, len, f, addr, addrLen));
                }
                else
                {
                    env.SetReturnValue(Original(s, b, len, f, addr, addrLen));
                }
            }
        }

        private class InjectReceive : FunctionPointerInjection<ReceiveFromDelegate>
        {
            public InjectReceive() : base(AddressHelper.Code("gso", 0x1C288))
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                var s = env.GetParameterP(0);
                var b = env.GetParameterP(1);
                var len = env.GetParameterI(2);
                var f = env.GetParameterI(3);
                var addr = env.GetParameterP(4);
                var addrLen = env.GetParameterP(5);
                var received = Original(s, b, len, f, addr, addrLen);
                if (len == 516 && Marshal.ReadInt32(addrLen) == 16)
                {
                    var addrLow = (ulong)Marshal.ReadInt64(addr, 0);
                    var addrHigh = (ulong)Marshal.ReadInt64(addr, 8);
                    received = FilterReceiveMessage(addrLow, addrHigh, b, received);
                    env.SetReturnValue(received);
                }
                else
                {
                    env.SetReturnValue(received);
                }
            }
        }

        private class ServerConnectionMonitor : IMessageFilter
        {
            public void FilterReceive(ConnectedPeer peer, IntPtr buffer, ref int len)
            {
                throw new NotImplementedException();
            }

            public void FilterSend(ConnectedPeer peer, IntPtr buffer, ref int len)
            {
                throw new NotImplementedException();
            }
        }

        private class ClientConnectionMonitor : IMessageFilter
        {
            public void FilterReceive(ConnectedPeer peer, IntPtr buffer, ref int len)
            {
                throw new NotImplementedException();
            }

            public void FilterSend(ConnectedPeer peer, IntPtr buffer, ref int len)
            {
                throw new NotImplementedException();
            }
        }
    }
}
