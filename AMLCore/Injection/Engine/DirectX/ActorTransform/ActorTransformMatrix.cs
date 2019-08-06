using AMLCore.Injection.Native;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.DirectX.ActorTransform
{
    public struct ActorTransformMatrix
    {
        [DllImport("d3dx9_43.dll", EntryPoint = "D3DXVec3TransformArray")]
        private static extern IntPtr D3DXVec3TransformArray1(IntPtr bo, int so, IntPtr bi, int si, IntPtr m, int n);

        private readonly IntPtr _unmanagedMatrix;

        internal ActorTransformMatrix(IntPtr ptr)
        {
            _unmanagedMatrix = ptr;
        }

        //Note bufferIn is float3 and bufferOut is float4
        public void TransformBuffer(IntPtr bufferOut, int strideOut, IntPtr bufferIn, int strideIn,
            int points)
        {
            D3DXVec3TransformArray1(bufferOut, strideOut, bufferIn, strideIn, _unmanagedMatrix, points);
        }
    }
}
