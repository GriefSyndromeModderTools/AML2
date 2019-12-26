using AMLCore.Injection.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.GSO
{
    class GSOConnectionMonitor
    {
        public static void Inject()
        {
            //TODO generate events when gso connects and disconnects.
            new BeforeServerLoop();
            new InjectCloseSocket();
        }

        private static void StartServer(IntPtr server)
        {
            GSOWindowLog.WriteLine("Server started");
        }

        private static void CloseSocket(IntPtr socket)
        {
            GSOWindowLog.WriteLine("Socket closed");
        }

        private delegate int CloseSocketDelegate(IntPtr s);

        private class BeforeServerLoop : CodeInjection
        {
            public BeforeServerLoop() : base (AddressHelper.Code("gso", 0x7455), 8)
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
    }
}
