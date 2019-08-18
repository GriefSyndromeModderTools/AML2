using AMLCore.Injection.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.ActorSystem
{
    public static class UpdateFunctionRedirect
    {
        private static bool _injected;

        private static readonly List<Action> _handlers = new List<Action>();

        private static void Inject()
        {
            if (_injected) return;
            _injected = true;
            new BeforeCall();
        }

        public static void RegisterHandler(Action handler)
        {
            Inject();
            _handlers.Add(handler);
        }

        private static void RunHandlers()
        {
            foreach (var h in _handlers)
            {
                h();
            }
        }

        private class BeforeCall : CodeInjection
        {
            public BeforeCall() : base(0x57A9, 7)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                RunHandlers();
            }
        }
    }
}
