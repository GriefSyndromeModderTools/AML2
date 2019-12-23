using AMLCore.Injection.AntiCheating;
using AMLCore.Injection.Native;
using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace AMLCore.Injection.Engine.DirectX.ActorTransform
{
    public static class ActorTransformManager
    {
        private static readonly Dictionary<int, List<IActorTransformHandler>> _motionList =
            new Dictionary<int, List<IActorTransformHandler>>();
        private static ThreadLocal<bool> _rendererAvailable = new ThreadLocal<bool>(() => false);

        private delegate void DoDrawPUP(int t, int c, IntPtr b, int s, int fvf, int ia, int ib);
        private static readonly DoDrawPUP _doDrawPUP;
        private static readonly Renderer _renderer = new Renderer();

        static ActorTransformManager()
        {
            _doDrawPUP = (DoDrawPUP)Marshal.GetDelegateForFunctionPointer(
                AddressHelper.Code(0xC1860), typeof(DoDrawPUP));
        }

        private class Renderer : IActorRenderer
        {
            public void DrawPrimitive(int primitiveType, int count, IntPtr buffer, ActorImageRef img)
            {
                if (!_rendererAvailable.Value) throw new InvalidOperationException("Not during rendering.");
                AnitCheatingEntry.Disable();
                _doDrawPUP(primitiveType, count, buffer, 0x1C, 0x144, img.A, img.B);
                AnitCheatingEntry.Enable();
            }
        }

        public static void RegisterHandler(int motion, IActorTransformHandler handler)
        {
            if (!_motionList.TryGetValue(motion, out var list))
            {
                list = new List<IActorTransformHandler>();
                _motionList.Add(motion, list);
            }
            list.Add(handler);
        }

        internal static void BeforeDrawActor(IntPtr actor, IntPtr matrix)
        {
            _rendererAvailable.Value = true;
            try
            {
                var actorObj = new ActorObject(actor);
                var motion = actorObj.Motion;
                var matrixObj = new ActorTransformMatrix(matrix);
                if (_motionList.TryGetValue(motion, out var list))
                {
                    foreach (var h in list)
                    {
                        try
                        {
                            h.Handle(_renderer, actorObj, matrixObj);
                        }
                        catch (Exception e)
                        {
                            CoreLoggers.Rendering.Error("Exception in handling ActorTransform: {0}",
                                e.ToString());
                        }
                    }
                }
            }
            finally
            {
                _rendererAvailable.Value = false;
            }
        }
    }
}
