using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Native
{
    public abstract class FunctionPointerInjection<T> : AbstractNativeInjection where T : class
    {
        private T _Original;
        private int _ParamCount;
        private object[] _ArgBuffer;

        protected T Original { get { return _Original; } }

        public FunctionPointerInjection(uint ptr)
        {
            Inject(AddressHelper.Code(ptr));
        }

        public FunctionPointerInjection(IntPtr ptr)
        {
            Inject(ptr);
        }

        private void Inject(IntPtr addrFunctionPointer)
        {
            //check delegate type
            var method = typeof(T).GetMethod("Invoke");
            if (method == null)
            {
                throw new ArgumentException("FunctionPointerInjection must be used with delegate");
            }
            foreach (var p in method.GetParameters())
            {
                var t = p.ParameterType;
                if (t != typeof(int) && t != typeof(uint) && t != typeof(IntPtr))
                {
                    throw new ArgumentException("delegate must only have int and pointer parameters");
                }
            }
            {
                var t = method.ReturnType;
                if (t != typeof(int) && t != typeof(uint) && t != typeof(IntPtr))
                {
                    throw new ArgumentException("delegate must only have int and pointer return value");
                }
            }
            _ParamCount = method.GetParameters().Length;
            var argSize = _ParamCount * 4;

            CoreLoggers.Injection.Info("inject virtual table with {0} at 0x{1}",
                this.GetType().Name, addrFunctionPointer.ToInt32().ToString("X8"));

            this.AddRegisterRead(Register.EAX);
            this.AddRegisterRead(Register.EBP);
            _ReturnValueIndex = _Count++;

            var original = Marshal.ReadIntPtr(addrFunctionPointer);
            var ret = (T)(object)Marshal.GetDelegateForFunctionPointer(original, typeof(T));

            IntPtr pCode;

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    //push ebp
                    bw.Write((byte)0x55);

                    //mov ebp, esp
                    bw.Write((byte)0x8B);
                    bw.Write((byte)0xEC);

                    WriteCode(bw);

                    //pop ebp
                    bw.Write((byte)0x5D);

                    //retn ?
                    bw.Write((byte)0xC2);
                    bw.Write((short)argSize);

                    var assemlyCode = ms.ToArray();
                    pCode = AssemblyCodeStorage.WriteCode(assemlyCode);
                }
            }

            CodeModification.WritePointer(addrFunctionPointer, pCode);

            _Original = ret;
            _ArgBuffer = new object[_ParamCount];
        }

        public int InvokeOriginal(NativeEnvironment env)
        {
            var method = typeof(T).GetMethod("Invoke");
            var parameters = method.GetParameters();
            for (int i = 0; i < _ParamCount; ++i)
            {
                var p = parameters[i];
                if (p.ParameterType == typeof(int))
                {
                    _ArgBuffer[i] = env.GetParameterI(i);
                }
                else if (p.ParameterType == typeof(uint))
                {
                    _ArgBuffer[i] = (uint)env.GetParameterI(i);
                }
                else if (p.ParameterType == typeof(IntPtr))
                {
                    _ArgBuffer[i] = env.GetParameterP(i);
                }
            }
            var ret = ((Delegate)(object)_Original).DynamicInvoke(_ArgBuffer);
            int iret;
            if (ret is IntPtr)
            {
                iret = ((IntPtr)ret).ToInt32();
            }
            else
            {
                iret = (int)ret;
            }
            env.SetReturnValue(iret);
            return iret;
        }
    }
}
