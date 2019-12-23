using AMLCore.Injection.Game.Scene;
using AMLCore.Injection.Native;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.Renderer
{
    class SkipRendererEntry : IEntryPointLoad
    {
        public void Run()
        {
            SceneInjectionManager.RegisterSceneHandler(SystemScene.StageMain, new SkipRenderer.SceneHandler());
        }
    }

    public class SkipRenderer
    {
        private static IntPtr _flag;
        private static int _skipFlag;
        private static object _lock = new object();

        internal static event Action HandleSkip;

        public static void SkipOnce()
        {
            lock (_lock)
            {
                _skipFlag = 1;
            }
        }

        public static void PreventSkip()
        {
            lock (_lock)
            {
                _skipFlag = 0;
            }
        }

        private static void InitFlag()
        {
            if (_flag == IntPtr.Zero)
            {
                //_flag = Marshal.ReadIntPtr(AddressHelper.Code(0xC612F));
                _flag = Marshal.AllocHGlobal(1);
                CodeModification.Modify(0xC612F, BitConverter.GetBytes(_flag.ToInt32()));
            }
        }

        internal class SceneHandler : ISceneEventHandler
        {
            public void Exit()
            {
            }

            public void PostInit(SceneEnvironment env)
            {
            }

            public void PostUpdate()
            {
                HandleSkip?.Invoke();

                lock (_lock)
                {
                    InitFlag();

                    if (_skipFlag != 0)
                    {
                        _skipFlag = 0;
                        Marshal.WriteByte(_flag, 1);
                    }
                    else
                    {
                        Marshal.WriteByte(_flag, 0);
                    }
                }
            }

            public void PreUpdate()
            {
            }
        }
    }
}
