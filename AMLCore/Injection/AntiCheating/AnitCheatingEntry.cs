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
        private static IntPtr _strictMode = Marshal.AllocHGlobal(1);

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
            Marshal.WriteByte(_strictMode, 0);
            Marshal.WriteInt32(_renderUpdateInt, 0);

            if (!PluginLoader.ContainsFunctionalMods())
            {
                CoreLoggers.Main.Info("no AML mods loaded. AntiCheating function disabled.");
                return;
            }
            var configDisabled = new IniFile("Core").Read("AntiCheating", "CompatibleMode", "false");
            if (configDisabled != "false" && configDisabled != "0")
            {
                CoreLoggers.Main.Info("antiCheating disabled by config file.");
                return;
            }
            CoreLoggers.Main.Info("antiCheating function enabled.");

            _rand = (StdlibRandDelegate)Marshal.GetDelegateForFunctionPointer(AddressHelper.Code(0x1B0DEF), typeof(StdlibRandDelegate));
            ModifyRenderFunction();
            SkipRenderer.HandleSkip += HandleSkip;

            //new Draw_GetTexture();
            //new BeforeCreateTextureInjection();
            //new AfterCreateTextureInjection();
            new Branch11Begin();
            new Branch11End();
        }

        private class Branch11Begin : CodeInjection
        {
            public Branch11Begin() : base(0x64E48, 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                Marshal.WriteByte(_strictMode, 1);
            }
        }

        private class Branch11End : CodeInjection
        {
            public Branch11End() : base(0x106227, 6)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                Marshal.WriteByte(_strictMode, 0);
            }
        }


        //private static bool _recordTexture = false;
        //private static BinaryWriter _log = new BinaryWriter(File.OpenWrite(@"E:\draw.log"));

        //private class Draw_GetTexture : CodeInjection
        //{
        //    public Draw_GetTexture() : base(0xC18A3, 6)
        //    {
        //    }
        //
        //    private float[] _modifyBuffer = new float[2];
        //
        //    protected override void Triggered(NativeEnvironment env)
        //    {
        //        var tex = env.GetRegister(Register.EAX);
        //        var buffer = env.GetParameterP(2);
        //        Marshal.Copy(buffer, _modifyBuffer, 0, 2);
        //        _log.Write((byte)2);
        //        _log.Write(Marshal.ReadInt32(buffer, 0));
        //        _log.Write(Marshal.ReadInt32(buffer, 4));
        //        _log.Write(tex.ToInt32());
        //    }
        //}
        //
        //private static IntPtr _pTexture;
        //private static IntPtr _pFileName;
        ////private static IntPtr _skipTexture;
        //
        //private class BeforeCreateTextureInjection : CodeInjection
        //{
        //    public BeforeCreateTextureInjection() : base(0xB2E49, 8)
        //    {
        //    }
        //
        //    protected override void Triggered(NativeEnvironment env)
        //    {
        //        _pTexture = env.GetRegister(Register.EAX);
        //        _pFileName = Marshal.ReadIntPtr(env.GetRegister(Register.EBP) + 0xC);
        //    }
        //}
        //
        //private class AfterCreateTextureInjection : CodeInjection
        //{
        //
        //    public AfterCreateTextureInjection() : base(0xB2F01, 8)
        //    {
        //    }
        //
        //    private static string GamePath = PathHelper.GetPath("").Replace('\\', '/');
        //
        //    protected override void Triggered(NativeEnvironment env)
        //    {
        //        var filename = Marshal.PtrToStringAnsi(_pFileName);
        //        var texture = Marshal.ReadIntPtr(_pTexture);
        //
        //        if (filename.StartsWith(GamePath))
        //        {
        //            filename = filename.Substring(GamePath.Length + 1);
        //        }
        //        if (filename.StartsWith("./"))
        //        {
        //            filename = filename.Substring(2);
        //        }
        //        CoreLoggers.Main.Info("Texture {0} {1}", texture.ToInt32().ToString("X8"), filename);
        //        //if (filename.Contains("stage4/prismA.dds"))
        //        //{
        //        //    _skipTexture = texture;
        //        //    CoreLoggers.Main.Info("Skip texture found");
        //        //}
        //    }
        //}

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
                        //buffer stored at dword ptr [ebp+0x10]
                        //ESI and EDX is not used.
                        //buffer format is { float4:position, uint:color, float2:texcoord }.
                        //We pick [0] [1] [4] (x, y, color).

                        //If sprite is out of region (100, 100, 700, 500), set edx to 1. Otherwise 0.
                        //Here we use signed int comparison to compare float to be faster.
                        /*
                            xor edx, edx
                            mov esi, dword ptr [ebp+0x10]

                            cmp dword ptr[esi],0x42c80000
                            jge l1
                            cmp dword ptr[esi+0x1C],0x42c80000
                            jge l1
                            cmp dword ptr[esi+0x38],0x42c80000
                            jge l1
                            cmp dword ptr[esi+0x54],0x42c80000
                            jge l1
                            inc edx
                            jmp l4
                            l1:

                            cmp dword ptr[esi+4],0x42c80000
                            jge l2
                            cmp dword ptr[esi+0x20],0x42c80000
                            jge l2
                            cmp dword ptr[esi+0x3C],0x42c80000
                            jge l2
                            cmp dword ptr[esi+0x58],0x42c80000
                            jge l2
                            inc edx
                            jmp l4
                            l2:

                            cmp dword ptr[esi],0x442f0000
                            jle l3
                            cmp dword ptr[esi+0x1C],0x442f0000
                            jle l3
                            cmp dword ptr[esi+0x38],0x442f0000
                            jle l3
                            cmp dword ptr[esi+0x54],0x442f0000
                            jle l3
                            inc edx
                            jmp l4
                            l3:

                            cmp dword ptr[esi+4],0x43fa0000
                            jle l4
                            cmp dword ptr[esi+0x20],0x43fa0000
                            jle l4
                            cmp dword ptr[esi+0x3C],0x43fa0000
                            jle l4
                            cmp dword ptr[esi+0x58],0x43fa0000
                            jle l4
                            inc edx
                            l4:
                         */
                        0x31, 0xD2, 0x8B, 0x75, 0x10, 0x81, 0x3E, 0x00, 0x00, 0xC8, 0x42, 0x7D, 0x1E, 0x81, 0x7E, 0x1C,
                        0x00, 0x00, 0xC8, 0x42, 0x7D, 0x15, 0x81, 0x7E, 0x38, 0x00, 0x00, 0xC8, 0x42, 0x7D, 0x0C, 0x81,
                        0x7E, 0x54, 0x00, 0x00, 0xC8, 0x42, 0x7D, 0x03, 0x42, 0xEB, 0x72, 0x81, 0x7E, 0x04, 0x00, 0x00,
                        0xC8, 0x42, 0x7D, 0x1E, 0x81, 0x7E, 0x20, 0x00, 0x00, 0xC8, 0x42, 0x7D, 0x15, 0x81, 0x7E, 0x3C,
                        0x00, 0x00, 0xC8, 0x42, 0x7D, 0x0C, 0x81, 0x7E, 0x58, 0x00, 0x00, 0xC8, 0x42, 0x7D, 0x03, 0x42,
                        0xEB, 0x4B, 0x81, 0x3E, 0x00, 0x00, 0x2F, 0x44, 0x7E, 0x1E, 0x81, 0x7E, 0x1C, 0x00, 0x00, 0x2F,
                        0x44, 0x7E, 0x15, 0x81, 0x7E, 0x38, 0x00, 0x00, 0x2F, 0x44, 0x7E, 0x0C, 0x81, 0x7E, 0x54, 0x00,
                        0x00, 0x2F, 0x44, 0x7E, 0x03, 0x42, 0xEB, 0x25, 0x81, 0x7E, 0x04, 0x00, 0x00, 0xFA, 0x43, 0x7E,
                        0x1C, 0x81, 0x7E, 0x20, 0x00, 0x00, 0xFA, 0x43, 0x7E, 0x13, 0x81, 0x7E, 0x3C, 0x00, 0x00, 0xFA,
                        0x43, 0x7E, 0x0A, 0x81, 0x7E, 0x58, 0x00, 0x00, 0xFA, 0x43, 0x7E, 0x01, 0x42,
                        
                        //If edx is not 0, and we are in strict mode (branch 11), we skip the value calculation.
                        //and dl, byte ptr[xxxxxxxx]
                        0x22, 0x15,
                    });
                    bw.Write(_strictMode.ToInt32());
                    bw.Write(new byte[] {

                        //test edx, edx
                        0x85, 0xD2,

                        //jne "call eax"
                        0x75, 0x20,

                        //Move buffer into edx
                        //mov edx, dword ptr [ebp+0x10]
                        0x8B, 0x55, 0x10,

                        //mov esi, dword ptr [edx]
                        0x8B, 0x32,

                        //rol esi, 3
                        0xC1, 0xC6, 0x03,
                        
                        //xor esi, dword ptr [edx + 4]
                        0x33, 0x72, 0x04,

                        //rol esi, 3
                        0xC1, 0xC6, 0x03,

                        //xor esi, dword ptr [edx + 0x10]
                        0x33, 0x72, 0x10,

                        //Some draw call check is disabled (by API).
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
                        //call eax
                        0xFF, 0xD0,
                        //pop esi
                        0x5E,
                        //pop ebp
                        0x5D,
                        //ret 1C
                        0xC2, 0x1C, 0x00,
                    });

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
            //if (_count > 6441)
            //{
            //    //_log.Write((byte)1);
            //    //_log.Write(_count);
            //    //_log.Flush();
            //    //if (_recordTexture)
            //    //{
            //        //WindowsHelper.MessageBox("finished");
            //    //}
            //    //_recordTexture = true;
            //}
            //if (_count > 6482)
            //{
            //    //Environment.Exit(0);
            //}

            if (_checkFrame > 0)
            {
                _checkFrame -= 1;
                if (_checkFrame == 0)
                {
                    //CoreLoggers.Main.Info("{0} Skip val: ==== {1} ====", _count, val.ToString("X8"));
                    if ((CountSetBits(val) & 1) != 0)
                    {
                        //Skip
                        var rnd = _rand();
                        //CoreLoggers.Main.Info("Skipped rand: {0}", rnd.ToString("X8"));
                    }
                }
                else
                {
                    //CoreLoggers.Main.Info("{0} Check count down, {1}", _count, val.ToString("X8"));
                    SkipRenderer.PreventSkip();
                }
            }
            else
            {
                var rnd = _rand();
                //CoreLoggers.Main.Info("{0} Frame rand {1}, {2}", _count, rnd.ToString("X8"), val.ToString("X8"));
                if ((rnd % 60) == 0)
                {
                    _checkFrame = 2;
                    SkipRenderer.PreventSkip();
                    //CoreLoggers.Main.Info("Check start");
                }
            }
        }
    }
}
