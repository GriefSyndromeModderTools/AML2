using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Native
{
    public abstract class AbstractNativeInjection
    {
        internal enum Register
        {
            EAX,
            EBP,
        }

        public class NativeEnvironment
        {
            private readonly AbstractNativeInjection _Parent;
            private readonly IntPtr _Data;

            public NativeEnvironment(AbstractNativeInjection parent, IntPtr data)
            {
                _Parent = parent;
                _Data = data;
            }

            internal IntPtr GetRegister(Register r)
            {
                return (IntPtr)Marshal.ReadInt32(_Data, _Parent._RegisterIndex[(int)r] * 4);
            }

            internal void SetRegister(Register r, IntPtr val)
            {
                throw new InvalidOperationException();
            }

            public void SetReturnValue(IntPtr val)
            {
                SetReturnValue(val.ToInt32());
            }

            public void SetReturnValue(int val)
            {
                if (_Parent._ReturnValueIndex == -1)
                {
                    throw new InvalidOperationException();
                }
                Marshal.WriteInt32(_Data, _Parent._ReturnValueIndex * 4, val);
            }

            public IntPtr GetParameterP(int index)
            {
                return (IntPtr)GetParameterI(index);
            }

            public int GetParameterI(int index)
            {
                var ebp = GetRegister(Register.EBP);
                return Marshal.ReadInt32(ebp + 8 + index * 4);
            }

            public void SetParameter(int index, IntPtr val)
            {
                SetParameter(index, val.ToInt32());
            }

            public void SetParameter(int index, int val)
            {
                var ebp = GetRegister(Register.EBP);
                Marshal.WriteInt32(ebp + 8 + index * 4, val);
            }
        }

        internal int[] _RegisterIndex = new int[] { -1, -1, -1, -1, -1, -1, -1, -1 };
        internal int _ReturnValueIndex = -1;
        internal int _Count = 0;

        internal void AddRegisterRead(Register r)
        {
            var ir = (int)r;
            if (_RegisterIndex[ir] != -1)
            {
                throw new ArgumentException("register already added");
            }
            _RegisterIndex[ir] = _Count++;
        }

        private NativeEnvironment GetEnvironment(IntPtr data)
        {
            return new NativeEnvironment(this, data);
        }

        protected virtual void WriteCode(BinaryWriter bw)
        {
            var index = NativeEntrance.NextIndex();
            NativeEntrance.Register(index, WrappedNativeCallback);
            /*
             * push eax
             * 
             * sub esp, 4/8/12/...
             * mov [esp+0], eax
             * mov [esp+4], ...
             * 
             * push esp
             * push ???? (index)
             * 
             * mov eax, 0x????????
             * call eax
             * 
             * ;return value
             * mov eax, [esp+?]
             * 
             * add esp, 4/8/12/...
             * 
             * pop eax
             * 
             */

            if (_ReturnValueIndex == -1)
            {
                //push eax
                bw.Write((byte)0x50);
            }

            if (_Count > 0)
            {
                //sub esp, _Count
                bw.Write((byte)0x83);
                bw.Write((byte)0xEC);
                bw.Write((byte)(_Count * 4));
            }

            //save registers
            if (_RegisterIndex[(int)Register.EAX] != -1)
            {
                //mov [esp+?], eax
                var offset = _RegisterIndex[(int)Register.EAX] * 4;
                bw.Write((byte)0x89);
                bw.Write((byte)0x44);
                bw.Write((byte)0x24);
                bw.Write((byte)offset);
            }
            if (_RegisterIndex[(int)Register.EBP] != -1)
            {
                //mov [esp+?], ebp
                var offset = _RegisterIndex[(int)Register.EBP] * 4;
                bw.Write((byte)0x89);
                bw.Write((byte)0x6C);
                bw.Write((byte)0x24);
                bw.Write((byte)offset);
            }

            //push esp
            bw.Write((byte)0x54);

            //push ????????
            bw.Write((byte)0x68);
            bw.Write((int)index);

            //mov eax, ????????
            bw.Write((byte)0xB8);
            bw.Write(NativeEntrance.EntrancePtr.ToInt32());

            //call eax
            bw.Write((byte)0xFF);
            bw.Write((byte)0xD0);

            if (_ReturnValueIndex != -1)
            {
                //mov eax, [esp+?]
                bw.Write((byte)0x8B);
                bw.Write((byte)0x44);
                bw.Write((byte)0x24);
                bw.Write((byte)(_ReturnValueIndex * 4));
            }

            if (_Count > 0)
            {
                //add esp, _Count
                bw.Write((byte)0x83);
                bw.Write((byte)0xC4);
                bw.Write((byte)(_Count * 4));
            }

            if (_ReturnValueIndex == -1)
            {
                //pop eax
                bw.Write((byte)0x58);
            }
        }

        private void WrappedNativeCallback(IntPtr env)
        {
            Triggered(this.GetEnvironment(env));
        }

        protected abstract void Triggered(NativeEnvironment env);
    }
}
