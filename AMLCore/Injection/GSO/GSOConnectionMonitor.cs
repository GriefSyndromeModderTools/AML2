using AMLCore.Injection.GSO.Localization;
using AMLCore.Injection.Native;
using AMLCore.Internal;
using AMLCore.Misc;
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
            new BeforeClientLoop();
            new ServerLoopTimeout();
            new InjectCloseSocket();
            new InjectSend();
            new InjectReceive();
            new DragRepFile();
            new GSOStatusChange();
            Filters.Add(new CrcProtection());
            Filters.Add(new ServerConnectionMonitor());
            Filters.Add(new ClientConnectionMonitor());
            Filters.Add(new CustomMessageManager.MessageFilter());
            //should also consider client status update after game start
            //TODO load mods when starting playing rep
        }

        private static void StartServer(IntPtr server)
        {
            ThreadHelper.InitInternalThread("GSOServer");
            GSOConnectionStatus.ClientStatus = null;
            GSOConnectionStatus.ServerStatus = new ServerConnectionStatus(server);
            CoreLoggers.GSO.Info("server started");
            GSOConnectionStatus.InvokeStatusChange(GSOConnectionStatusChangeType.ServerStart);
        }

        private static void StartClient(IntPtr client, ConnectedPeer peer, int clientNumber)
        {
            ThreadHelper.InitInternalThread("GSOClient");

            GSOConnectionStatus.ClientStatus = new ClientConnectionStatus(client, peer, clientNumber);
            GSOConnectionStatus.InvokeStatusChange(GSOConnectionStatusChangeType.ClientConnected);

            if (GSOLoadingInjection.ModCheck)
            {
                GSOConnectionStatus.ClientStatus.Send(new byte[] { 0, InternalMessageId.RequestModString });
            }
        }

        private static void CloseSocket(IntPtr socket)
        {
            if (GSOConnectionStatus.ServerStatus?.Socket == socket)
            {
                GSOConnectionStatus.InvokeStatusChange(GSOConnectionStatusChangeType.ConnectionClose);
                GSOConnectionStatus.ServerStatus = null;
                CoreLoggers.GSO.Info("socket closed");
            }
            else if (GSOConnectionStatus.ClientStatus?.Socket == socket)
            {
                GSOConnectionStatus.InvokeStatusChange(GSOConnectionStatusChangeType.ConnectionClose);
                GSOConnectionStatus.ClientStatus = null;
                CoreLoggers.GSO.Info("socket closed");
            }
        }

        internal static List<IMessageFilter> Filters = new List<IMessageFilter>();

        private static ConnectedPeer FindPeer(ulong low, ulong high)
        {
            var client = GSOConnectionStatus.ClientStatus;
            var server = GSOConnectionStatus.ServerStatus;
            if (client != null)
            {
                var p = client.Server;
                if (low == p.Address.AddrLow && high == p.Address.AddrHigh)
                {
                    return p;
                }
            }
            else if (server != null)
            {
                foreach (var p in server.Clients)
                {
                    if (p != null && low == p.Address.AddrLow && high == p.Address.AddrHigh)
                    {
                        return p;
                    }
                }
            }
            //TODO check and remove this
            //CoreLoggers.GSO.Info("unrecognized peer");
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
                    CoreLoggers.GSO.Error("invalid message after filter");
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
                    CoreLoggers.GSO.Error("invalid message after filter");
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

        private static IntPtr _clientPtr;

        private class BeforeClientLoop : CodeInjection
        {
            public BeforeClientLoop() : base(AddressHelper.Code("gso", 0x7C9C), 7)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                _clientPtr = env.GetParameterP(3);
            }
        }

        private class ServerLoopTimeout : CodeInjection
        {
            public ServerLoopTimeout() : base(AddressHelper.Code("gso", 0x77F4), 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                GSOConnectionStatus.ServerStatus?.UpdateClientList();
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
                env.SetReturnValue(Original(env.GetParameterP(0)));
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
                if (len == 516 && Marshal.ReadInt32(addrLen) == 16 && received > 0)
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
            private Crc32 _crc = new Crc32();

            public void FilterReceive(ConnectedPeer peer, IntPtr buffer, ref int len)
            {
                var msg = Marshal.ReadByte(buffer, 1);
                if (msg == InternalMessageId.RequestModString)
                {
                    if (!GSOConnectionStatus.ServerStatus.IsKnownPeer(peer))
                    {
                        return;
                    }
                    byte[] strData = GSOLoadingInjection.ServerGetModString();
                    byte[] data = new byte[strData.Length + 6];
                    data[0] = 0;
                    data[1] = InternalMessageId.ReplyModString;
                    Array.Copy(strData, 0, data, 2, strData.Length);
                    Array.Copy(BitConverter.GetBytes(_crc.ComputeChecksum(data, 1, strData.Length + 1)), 0, data, strData.Length + 2, 4);
                    GSOConnectionStatus.ServerStatus.Send(peer, data);
                    CoreLoggers.GSO.Info("mod list request replied");
                    GSOWindowLog.WriteLine(GSOLocalization.ModListReplied);
                }
            }

            public void FilterSend(ConnectedPeer peer, IntPtr buffer, ref int len)
            {
                var msg = Marshal.ReadByte(buffer, 1);
                if (msg == 0x41)
                {
                    GSOConnectionStatus.ServerStatus?.UpdateClientList();
                }
                else if (msg == 0x44)
                {
                    GSOConnectionStatus.ServerStatus?.DisconnectClient(peer);
                }
                else if (msg == 0x45)
                {
                    GSOLoadingInjection.ServerGameStart();
                }
            }
        }

        private class ClientConnectionMonitor : IMessageFilter
        {
            private Crc32 _crc = new Crc32();

            public void FilterReceive(ConnectedPeer peer, IntPtr buffer, ref int len)
            {
                var msg = Marshal.ReadByte(buffer, 1);
                if (msg == 0x41)
                {
                    StartClient(_clientPtr, peer, Marshal.ReadByte(buffer, 2));
                }
                else if (msg == 0x45)
                {
                    GSOLoadingInjection.ClientGameStart();
                }
                else if (msg == InternalMessageId.ReplyModString)
                {
                    if (GSOLoadingInjection.ModCheck)
                    {
                        //Check crc
                        var copy = new byte[len - 5];
                        Marshal.Copy(buffer + 1, copy, 0, copy.Length);
                        var calcCrc = _crc.ComputeChecksum(copy, 0, copy.Length);
                        var readCrc = (uint)Marshal.ReadInt32(buffer, len - 4);
                        if (calcCrc != readCrc)
                        {
                            CoreLoggers.GSO.Error("mod list message corrupted");
                            GSOWindowLog.WriteLine(GSOLocalization.ModListReceivedCorrupted);
                        }
                        else
                        {
                            var serverModString = copy.Skip(1).ToArray();
                            CoreLoggers.GSO.Info("mod list message received");
                            GSOWindowLog.WriteLine(GSOLocalization.ModListReceived);

                            if (GSOLoadingInjection.ModCheckSync)
                            {
                                var matchAll = GSOLoadingInjection.ClientCheckModVersion(serverModString, out var foundAll);
                                if (matchAll)
                                {
                                    GSOLoadingInjection.ReplaceArguments(serverModString);
                                    CoreLoggers.GSO.Info("mod sync: replaced");
                                    GSOWindowLog.WriteLine(GSOLocalization.ModListReplaced);
                                }
                                else if (foundAll)
                                {
                                    GSOLoadingInjection.ReplaceArguments(serverModString);
                                    CoreLoggers.GSO.Info("mod sync: replaced, version inconsitent");
                                    GSOWindowLog.WriteLine(GSOLocalization.ModListReplacedVersionInconsistent);
                                }
                                else
                                {
                                    CoreLoggers.GSO.Info("mod sync: not replaced");
                                    GSOWindowLog.WriteLine(GSOLocalization.ModListReplaceFailed);
                                }
                            }
                            else if (GSOLoadingInjection.ModCheck)
                            {
                                GSOLoadingInjection.ClientCheckArgs(serverModString, out var argCheck, out var versionCheck);
                                if (argCheck && versionCheck)
                                {
                                    CoreLoggers.GSO.Info("mod validate: passed");
                                    GSOWindowLog.WriteLine(GSOLocalization.ModListValidated);
                                }
                                else if (argCheck)
                                {
                                    CoreLoggers.GSO.Info("mod validate: passed, version inconsitent");
                                    GSOWindowLog.WriteLine(GSOLocalization.ModListValidatedVersionInconsistent);
                                }
                                else
                                {
                                    CoreLoggers.GSO.Info("mod validate: failed");
                                    GSOWindowLog.WriteLine(GSOLocalization.ModListValidateFailed);
                                }
                            }
                        }
                    }
                }
            }

            public void FilterSend(ConnectedPeer peer, IntPtr buffer, ref int len)
            {
            }
        }

        private class DragRepFile : CodeInjection
        {
            public DragRepFile() : base(AddressHelper.Code("gso", 0x250B), 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                //TODO in the future we may extract mod information
                GSOLoadingInjection.ReplayGameStart();
            }
        }

        private class GSOStatusChange : CodeInjection
        {
            public GSOStatusChange() : base(AddressHelper.Code("gso", 0x1A63), 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                GSOConnectionStatus.OnGSOStatusChange(env.GetParameterI(0), env.GetParameterP(1));
            }
        }
    }
}
