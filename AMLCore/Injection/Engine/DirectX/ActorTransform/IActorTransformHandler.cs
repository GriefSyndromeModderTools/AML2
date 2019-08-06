using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Engine.DirectX.ActorTransform
{
    public interface IActorRenderer
    {
        void DrawPrimitive(int primitiveType, int count, IntPtr buffer, ActorImageRef img);
    }

    public static class IActorRendererExt
    {
        public static void DrawActor(this IActorRenderer renderer, ActorObject actor)
        {
            renderer.DrawPrimitive(5, 2, actor.DestBuffer, actor.CurrentImage);
        }
    }

    public interface IActorTransformHandler
    {
        void Handle(IActorRenderer renderer, ActorObject actor, ActorTransformMatrix matrix);
    }
}
