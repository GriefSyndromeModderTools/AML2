using AMLCore.Injection.Engine.Renderer;
using AMLCore.Injection.Engine.Script;
using AMLCore.Injection.Game.Scene;
using AMLCore.Injection.Native;
using AMLCore.Internal;
using AMLCore.Misc;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.AntiCheating
{
    class AnitCheatingEntry : IEntryPointPostload
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int StdlibRandDelegate();

        private StdlibRandDelegate _rand;
        private IntPtr _renderUpdateInt = Marshal.AllocHGlobal(4);
        private static IntPtr _disableFlag = Marshal.AllocHGlobal(1);
        private static IntPtr _drawSkipFlag = Marshal.AllocHGlobal(1);

        public static void Disable()
        {
            Marshal.WriteByte(_disableFlag, 1);
        }

        public static void Enable()
        {
            Marshal.WriteByte(_disableFlag, 0);
        }

        public void Run()
        {
            Marshal.WriteByte(_disableFlag, 0);
            Marshal.WriteInt32(_renderUpdateInt, 0);
            Marshal.WriteByte(_drawSkipFlag, 0);

            //
            //if (!PluginLoader.ContainsFunctionalMods())
            //{
            //    CoreLoggers.Main.Info("No AML mods loaded. AntiCheating function disabled.");
            //    return;
            //}
            //var configDisabled = new IniFile("Core").Read("AntiCheating", "CompatibleMode", "false");
            //if (configDisabled != "false" && configDisabled != "0")
            //{
            //    CoreLoggers.Main.Info("AntiCheating disabled by config file.");
            //    return;
            //}
            CoreLoggers.Main.Info("AntiCheating function enabled.");
            //
            _rand = (StdlibRandDelegate)Marshal.GetDelegateForFunctionPointer(AddressHelper.Code(0x1B0DEF), typeof(StdlibRandDelegate));
            //
            ModifyRenderFunction();
            SkipRenderer.HandleSkip += HandleSkip;
            //
            //new Branch11Begin();
            //new Branch11End();
            //new Branch12Begin();
            //new Branch12End();
            //new Draw_GetTexture();
            //new Draw_End();
            //new BeforeCreateTextureInjection();
            //new AfterCreateTextureInjection();
            //ModifySkip();
            //new Draw_GetTexture();
        }

        private class Branch11Begin : CodeInjection
        {
            public Branch11Begin() : base(0x64E48, 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                Marshal.WriteByte(_disableFlag, 0);
            }
        }

        private class Branch11End : CodeInjection
        {
            public Branch11End() : base(0x106227, 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                Marshal.WriteByte(_disableFlag, 1);
            }
        }

        private class Branch12Begin : CodeInjection
        {
            public Branch12Begin() : base(0x7CDF, 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                Marshal.WriteByte(_disableFlag, 0);
            }
        }

        private class Branch12End : CodeInjection
        {
            public Branch12End() : base(0x7D24, 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                Marshal.WriteByte(_disableFlag, 1);
            }
        }

        //private delegate void InjectDrawCallDelegate(IntPtr buffer);
        //private static InjectDrawCallDelegate _handleDrawCall = HandleDrawCall;
        private static IntPtr _lastTexture, _bufferPtrStorage = Marshal.AllocHGlobal(4);
        //private static BinaryWriter _log = new BinaryWriter(File.OpenWrite(@"E:\drawcall.dat"));

        private static float[] _modifyBuffer = new float[2];

        private static void HandleDrawCall(IntPtr buffer)
        {
            //_log.Write((byte)2);
            //_log.Write(Marshal.ReadInt32(buffer, 0));
            //_log.Write(Marshal.ReadInt32(buffer, 4));
            //_log.Write(_lastTexture.ToInt32());
            ////_log.Write(Marshal.ReadInt32(buffer, 16));
        }

        private class Draw_GetTexture : CodeInjection
        {
            public Draw_GetTexture() : base(0xC18A3, 6)
            {
            }
        
            protected override void Triggered(NativeEnvironment env)
            {
                var tex = env.GetRegister(Register.EAX);
                if (tex == _skipTexture)
                {
                    //Marshal.WriteByte(_drawSkipFlag, 1);
                }
                else
                {
                    Marshal.WriteByte(_drawSkipFlag, 0);
                }
                var buffer = env.GetParameterP(2);
                for (int i = 0; i < 4; ++i)
                {
                    Marshal.Copy(buffer, _modifyBuffer, 0, 2);
                    _modifyBuffer[0] /= 2;
                    _modifyBuffer[1] /= 2;
                    Marshal.Copy(_modifyBuffer, 0, buffer, 2);
                    buffer += 0x1C;
                }
            }
        }
        
        private class Draw_End : CodeInjection
        {
            //Conflict with skip
            public Draw_End() : base(0xC18E3, 6)
            {
            }
        
            protected override void Triggered(NativeEnvironment env)
            {
                HandleDrawCall(Marshal.ReadIntPtr(_bufferPtrStorage));
            }
        }

        private static IntPtr _pTexture;
        private static IntPtr _pFileName;
        private static IntPtr _skipTexture;
        
        private class BeforeCreateTextureInjection : CodeInjection
        {
            public BeforeCreateTextureInjection() : base(0xB2E49, 8)
            {
            }
        
            protected override void Triggered(NativeEnvironment env)
            {
                _pTexture = env.GetRegister(Register.EAX);
                _pFileName = Marshal.ReadIntPtr(env.GetRegister(Register.EBP) + 0xC);
            }
        }
        
        private class AfterCreateTextureInjection : CodeInjection
        {
        
            public AfterCreateTextureInjection() : base(0xB2F01, 8)
            {
            }
        
            private static string GamePath = PathHelper.GetPath("").Replace('\\', '/');
        
            protected override void Triggered(NativeEnvironment env)
            {
                var filename = Marshal.PtrToStringAnsi(_pFileName);
                var texture = Marshal.ReadIntPtr(_pTexture);
        
                if (filename.StartsWith(GamePath))
                {
                    filename = filename.Substring(GamePath.Length + 1);
                }
                if (filename.StartsWith("./"))
                {
                    filename = filename.Substring(2);
                }
                //CoreLoggers.Main.Info("Texture {0} {1}", texture.ToInt32().ToString("X8"), filename);
                if (filename.Contains("stage4/prismA.dds"))
                {
                    _skipTexture = texture;
                    CoreLoggers.Main.Info("Skip texture found");
                }
            }
        }

        private IntPtr GenerateSkip()
        {
            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    //Version 1: skip draw when _drawSkipFlag is set
                    //bw.Write(new byte[]
                    //{
                    //    //cmp byte ptr [xxxxxxxx], 0
                    //    0x80, 0x3D,
                    //});
                    //bw.Write(_drawSkipFlag.ToInt32());
                    //bw.Write(new byte[]
                    //{
                    //    0x00,
                    //
                    //    //jne +5
                    //    0x75, 0x05,
                    //
                    //    //call eax
                    //    0xFF, 0xD0,
                    //
                    //    //sub esp, 0x14
                    //    0x83, 0xEC, 0x14,
                    //
                    //    //add esp, 0x14 (empty call)
                    //    0x83, 0xC4, 0x14,
                    //
                    //    //original function epilogue
                    //    0x5E,
                    //    0x5D,
                    //    0xC2, 0x1C, 0x00,
                    //});

                    //Version 2: skip when x<0||x>800||y<0||y>600
                    /*
                        mov esi, dword ptr [ebp+0x10]

                        cmp dword ptr[esi],0
                        jge l1
                        cmp dword ptr[esi+0x1C],0
                        jge l1
                        cmp dword ptr[esi+0x38],0
                        jge l1
                        cmp dword ptr[esi+0x54],0
                        jge l1
                        add esp, 0x14
                        pop esi
                        pop ebp
                        ret 0x1C
                        l1:

                        cmp dword ptr[esi+4],0
                        jge l2
                        cmp dword ptr[esi+0x20],0
                        jge l2
                        cmp dword ptr[esi+0x3C],0
                        jge l2
                        cmp dword ptr[esi+0x58],0
                        jge l2
                        add esp, 0x14
                        pop esi
                        pop ebp
                        ret 0x1C
                        l2:

                        cmp dword ptr[esi],0x44480000
                        jge l3
                        cmp dword ptr[esi+0x1C],0x44480000
                        jge l3
                        cmp dword ptr[esi+0x38],0x44480000
                        jge l3
                        cmp dword ptr[esi+0x54],0x44480000
                        jge l3
                        add esp, 0x14
                        pop esi
                        pop ebp
                        ret 0x1C
                        l3:

                        cmp dword ptr[esi+4],0x44160000
                        jge l4
                        cmp dword ptr[esi+0x20],0x44160000
                        jge l4
                        cmp dword ptr[esi+0x3C],0x44160000
                        jge l4
                        cmp dword ptr[esi+0x58],0x44160000
                        jge l4
                        add esp, 0x14
                        pop esi
                        pop ebp
                        ret 0x1C
                        l4:

                        call eax
                        pop esi
                        pop ebp
                        ret 0x1C
                     */
                    bw.Write(new byte[]
                    {
                        0x8B, 0x75, 0x10, 0x83, 0x3E, 0x00, 0x7D, 0x1A, 0x83, 0x7E, 0x1C, 0x00, 0x7D, 0x14, 0x83, 0x7E,
                        0x38, 0x00, 0x7D, 0x0E, 0x83, 0x7E, 0x54, 0x00, 0x7D, 0x08, 0x83, 0xC4, 0x14, 0x5E, 0x5D, 0xC2,
                        0x1C, 0x00, 0x83, 0x7E, 0x04, 0x00, 0x7D, 0x1A, 0x83, 0x7E, 0x20, 0x00, 0x7D, 0x14, 0x83, 0x7E,
                        0x3C, 0x00, 0x7D, 0x0E, 0x83, 0x7E, 0x58, 0x00, 0x7D, 0x08, 0x83, 0xC4, 0x14, 0x5E, 0x5D, 0xC2,
                        0x1C, 0x00, 0x81, 0x3E, 0x00, 0x00, 0x48, 0x44, 0x7D, 0x23, 0x81, 0x7E, 0x1C, 0x00, 0x00, 0x48,
                        0x44, 0x7D, 0x1A, 0x81, 0x7E, 0x38, 0x00, 0x00, 0x48, 0x44, 0x7D, 0x11, 0x81, 0x7E, 0x54, 0x00,
                        0x00, 0x48, 0x44, 0x7D, 0x08, 0x83, 0xC4, 0x14, 0x5E, 0x5D, 0xC2, 0x1C, 0x00, 0x81, 0x7E, 0x04,
                        0x00, 0x00, 0x16, 0x44, 0x7D, 0x23, 0x81, 0x7E, 0x20, 0x00, 0x00, 0x16, 0x44, 0x7D, 0x1A, 0x81,
                        0x7E, 0x3C, 0x00, 0x00, 0x16, 0x44, 0x7D, 0x11, 0x81, 0x7E, 0x58, 0x00, 0x00, 0x16, 0x44, 0x7D,
                        0x08, 0x83, 0xC4, 0x14, 0x5E, 0x5D, 0xC2, 0x1C, 0x00,
                        0xFF, 0xD0, 0x5E, 0x5D, 0xC2, 0x1C, 0x00,
                    });

                    return AssemblyCodeStorage.WriteCode(ms.ToArray());
                }
            }
        }

        private void ModifySkip()
        {
            var i = AssemblyCodeStorage.AllocateIndirect();
            AssemblyCodeStorage.WriteIndirect(i, GenerateSkip());
            CodeModification.Modify(0xC18E1, new byte[]
            {
                0xFF, 0x25,
            });
            CodeModification.Modify(0xC18E3, BitConverter.GetBytes(i.ToInt32()));
        }

        private IntPtr CreateInjectedFunction()
        {
            var jmpBack = AssemblyCodeStorage.AllocateIndirect();
            AssemblyCodeStorage.WriteIndirect(jmpBack, AddressHelper.Code(0xC18D9));

            using (var ms = new MemoryStream())
            {
                using (var bw = new BinaryWriter(ms))
                {
                    bw.Write(new byte[]
                    {
                    //    //push eax
                    //    0x50,
                    //    //push ecx (clr may modify this)
                    //    0x51,
                    //
                    //    //push edx
                    //    0x52,
                    //
                    //    //mov eax, xxxxxxxx
                    //    0xB8,
                    //});
                    //bw.Write(Marshal.GetFunctionPointerForDelegate(_handleDrawCall).ToInt32());
                    //bw.Write(new byte[]
                    //{
                    //    //call eax
                    //    0xFF, 0xD0,
                    //    //0x59,
                    //
                    //    //pop ecx
                    //    0x59,
                    //    //pop eax
                    //    0x58,

                    //    //mov dword ptr[xxxxxxxx], edx
                    //    0x89, 0x15,
                    //});
                    //bw.Write(_bufferPtrStorage.ToInt32());
                    //bw.Write(new byte[]
                    //{
                        //Before start, pointer of buffer is in EDX.
                        //ESI is not used.
                        //buffer format is { float4:position, uint:color, float2:texcoord }.
                        //We pick [0] [1] [4] (x, y, color).

                        //Update: prefilter
                        //skip when x<0||x>800||y<0||y>600
                        /*
                            mov esi, dword ptr [ebp+0x10]

                            cmp dword ptr[esi],0
                            jge l1
                            cmp dword ptr[esi+0x1C],0
                            jge l1
                            cmp dword ptr[esi+0x38],0
                            jge l1
                            cmp dword ptr[esi+0x54],0
                            jge l1
                            add esp, 0x14
                            pop esi
                            pop ebp
                            ret 0x1C
                            l1:

                            cmp dword ptr[esi+4],0
                            jge l2
                            cmp dword ptr[esi+0x20],0
                            jge l2
                            cmp dword ptr[esi+0x3C],0
                            jge l2
                            cmp dword ptr[esi+0x58],0
                            jge l2
                            add esp, 0x14
                            pop esi
                            pop ebp
                            ret 0x1C
                            l2:

                            cmp dword ptr[esi],0x44480000
                            jle l3
                            cmp dword ptr[esi+0x1C],0x44480000
                            jle l3
                            cmp dword ptr[esi+0x38],0x44480000
                            jle l3
                            cmp dword ptr[esi+0x54],0x44480000
                            jle l3
                            add esp, 0x14
                            pop esi
                            pop ebp
                            ret 0x1C
                            l3:

                            cmp dword ptr[esi+4],0x44160000
                            jle l4
                            cmp dword ptr[esi+0x20],0x44160000
                            jle l4
                            cmp dword ptr[esi+0x3C],0x44160000
                            jle l4
                            cmp dword ptr[esi+0x58],0x44160000
                            jle l4
                            add esp, 0x14
                            pop esi
                            pop ebp
                            ret 0x1C
                            l4:
                         */
                        0x8B, 0x75, 0x10, 0x83, 0x3E, 0x00, 0x7D, 0x1A, 0x83, 0x7E, 0x1C, 0x00, 0x7D, 0x14, 0x83, 0x7E,
                        0x38, 0x00, 0x7D, 0x0E, 0x83, 0x7E, 0x54, 0x00, 0x7D, 0x08, 0x83, 0xC4, 0x14, 0x5E, 0x5D, 0xC2,
                        0x1C, 0x00, 0x83, 0x7E, 0x04, 0x00, 0x7D, 0x1A, 0x83, 0x7E, 0x20, 0x00, 0x7D, 0x14, 0x83, 0x7E,
                        0x3C, 0x00, 0x7D, 0x0E, 0x83, 0x7E, 0x58, 0x00, 0x7D, 0x08, 0x83, 0xC4, 0x14, 0x5E, 0x5D, 0xC2,
                        0x1C, 0x00, 0x81, 0x3E, 0x00, 0x00, 0x48, 0x44, 0x7E, 0x23, 0x81, 0x7E, 0x1C, 0x00, 0x00, 0x48,
                        0x44, 0x7E, 0x1A, 0x81, 0x7E, 0x38, 0x00, 0x00, 0x48, 0x44, 0x7E, 0x11, 0x81, 0x7E, 0x54, 0x00,
                        0x00, 0x48, 0x44, 0x7E, 0x08, 0x83, 0xC4, 0x14, 0x5E, 0x5D, 0xC2, 0x1C, 0x00, 0x81, 0x7E, 0x04,
                        0x00, 0x00, 0x16, 0x44, 0x7E, 0x23, 0x81, 0x7E, 0x20, 0x00, 0x00, 0x16, 0x44, 0x7E, 0x1A, 0x81,
                        0x7E, 0x3C, 0x00, 0x00, 0x16, 0x44, 0x7E, 0x11, 0x81, 0x7E, 0x58, 0x00, 0x00, 0x16, 0x44, 0x7E,
                        0x08, 0x83, 0xC4, 0x14, 0x5E, 0x5D, 0xC2, 0x1C, 0x00,
                        
                        //Update: we moved to before call eax. We need to get edx again.
                        //mov edx, dword ptr [ebp+0x10]
                        0x8B, 0x55, 0x10,


                        ////mov esi, dword ptr [edx]
                        0x8B, 0x32,
                        //mov si, word ptr [edx + 2]
                        //0x66, 0x8B, 0x72, 0x02,

                        //rol esi, 3
                        //0xC1, 0xC6, 0x03,
                        
                        ////xor esi, dword ptr [edx + 4]
                        0x33, 0x72, 0x04,
                        //xor si, word ptr [edx + 6]
                        //0x66, 0x33, 0x72, 0x06,

                        //rol esi, 3
                        //0xC1, 0xC6, 0x03,

                        ////xor esi, dword ptr [edx + 0x10]
                        0x33, 0x72, 0x10,
                        //xor si, word ptr [edx + 0x12]
                        //0x66, 0x33, 0x72, 0x12,

                        //cmp byte ptr[xxxxxxxx], 0
                        0x80, 0x3D,
                    });
                    bw.Write(_disableFlag.ToInt32());

                    bw.Write(new byte[]
                    {
                        0x00,

                        //jne +6
                        0x75, 0x06,

                        //xor dword ptr[xxxxxxxx], esi
                        0x31, 0x35,
                    });
                    bw.Write(_renderUpdateInt.ToInt32());
                    bw.Write(new byte[]
                    {
                        //original code
                        0xFF, 0xD0,
                        0x5E,
                        0x5D,
                        0xC2, 0x1C, 0x00,
                    });

                    //bw.Write(new byte[]
                    //{
                    //    //Move original code (overwritten by jmp) here.
                    //    0x52,
                    //    0x8B, 0x55, 0x0C,
                    //    0x52,
                    //    0x8B, 0x55, 0x08,
                    //
                    //
                    //    //Jump back.
                    //    0xFF, 0x25,
                    //});
                    //bw.Write(jmpBack.ToInt32());
                    //});

                    return AssemblyCodeStorage.WriteCode(ms.ToArray());
                }
            }
        }

        private void ModifyRenderFunction()
        {
            var jmp = AssemblyCodeStorage.AllocateIndirect();
            AssemblyCodeStorage.WriteIndirect(jmp, CreateInjectedFunction());
            CodeModification.Modify(0xC18E1, 0xFF, 0x25);
            CodeModification.Modify(0xC18E3, BitConverter.GetBytes(jmp.ToInt32()));
        }

        private static int CountSetBits(int i)
        {
            i = i - ((i >> 1) & 0x55555555);
            i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
            return (((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        private int _checkFrame = 0;
        private long _count = 0;

        private void HandleSkip()
        {
            var val = Marshal.ReadInt32(_renderUpdateInt);
            Marshal.WriteInt32(_renderUpdateInt, 0);

            using (SquirrelHelper.PushMemberChainThis("guage"))
            {
                var gsize = SquirrelFunctions.getsize(SquirrelHelper.SquirrelVM, -1);
                if (gsize <= 0)
                {
                    _count = 0;
                    return;
                }
            }

            _count += 1;
            //_log.Write((byte)1);
            //_log.Write((int)_count);
            //_log.Flush();

            if (_checkFrame > 0)
            {
                _checkFrame -= 1;
                if (_checkFrame == 0)
                {
                    CoreLoggers.Main.Info("{0} Skip val: ==== {1} ====", _count, val.ToString("X8"));
                    if ((CountSetBits(val) & 1) != 0)
                    {
                        var rnd = _rand();
                        CoreLoggers.Main.Info("Skipped rand: {0}", rnd.ToString("X8"));
                    }
                }
                else
                {
                    CoreLoggers.Main.Info("{0} Check count down, {1}", _count, val.ToString("X8"));
                    SkipRenderer.PreventSkip();
                }
            }
            else
            {
                var rnd = _rand();
                CoreLoggers.Main.Info("{0} Frame rand {1}, {2}", _count, rnd.ToString("X8"), val.ToString("X8"));
                if ((rnd % 11) == 0)
                {
                    _checkFrame = 2;
                    SkipRenderer.PreventSkip();
                    CoreLoggers.Main.Info("Check start");
                }
            }
        }
    }
}
