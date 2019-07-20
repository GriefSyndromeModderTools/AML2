using AMLCore.Injection.Native;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.Script
{
    class SquirrelAPINewFunctions
    {
        private static IntPtr MakeIndirect(uint offset)
        {
            var ret = AssemblyCodeStorage.AllocateIndirect();
            AssemblyCodeStorage.WriteIndirect(ret, AddressHelper.Code(offset));
            return ret;
        }

        private static void WriteCall(BinaryWriter w, IntPtr indirect)
        {
            w.Write((byte)0xFF);
            w.Write((byte)0x15);
            w.Write(indirect.ToInt32());
        }

        public static void Write_set()
        {
            var vmGetUp = MakeIndirect(0x132B00);
            var vmGetAt = MakeIndirect(0x132B20);
            var vmSet = MakeIndirect(0x134990);
            var VMRaise_IdxError = MakeIndirect(0x13AF10);
            var vmPop = MakeIndirect(0x132A30);
            var jmpForward = AssemblyCodeStorage.AllocateIndirect();

            var dest = AddressHelper.Code(SquirrelFunctions.Addr_sq_set);
            using (new ReadWriteProtect(dest, 6))
            {
                Marshal.WriteByte(dest, 0, 0xFF);
                Marshal.WriteByte(dest, 1, 0x25);
                Marshal.WriteInt32(dest, 2, jmpForward.ToInt32());
            }

            using (var ms = new MemoryStream())
            {
                using (var w = new BinaryWriter(ms))
                {
                    w.Write(new byte[]
                    {
                            //push ebp
                            0x55,
                            //mov ebp, esp
                            0x8B, 0xEC,
                            //push esi
                            0x56,
                            //push edi
                            0x57,

                            //mov eax, dword [ebp+0xC]
                            0x8B, 0x45, 0x0C,
                            //mov esi, dword [ebp+8]
                            0x8B, 0x75, 0x08,

                            //test eax, eax
                            0x85, 0xC0,
                            //js label_0 (+18)
                            0x78, 0x12,
                            //mov ecx, dword [esi+0x34]
                            0x8B, 0x4E, 0x34,
                            //lea edx, [ecx+eax-1]
                            0x8D, 0x54, 0x01, 0xFF,
                            //push edx
                            0x52,
                            //mov ecx, esi
                            0x8B, 0xCE,
                    });
                    WriteCall(w, vmGetAt);
                    w.Write(new byte[]
                    {
                            //jmp label_1
                            0xEB, 0x09,
                            //label_0:
                            //push eax
                            0x50,
                            //mov ecx, esi
                            0x8B, 0xCE,
                    });
                    WriteCall(w, vmGetUp);
                    w.Write(new byte[]
                    {
                            //label_1:
                            //mov edi, eax
                            0x8B, 0xF8,
                            //push 0
                            0x6A, 0x00,
                            //push -1
                            0x6A, 0xFF,
                            //mov ecx, esi
                            0x8B, 0xCE,
                    });
                    WriteCall(w, vmGetUp);
                    w.Write(new byte[]
                    {
                            //push eax
                            0x50,
                            //push -2
                            0x6A, 0xFE,
                            //mov ecx, esi
                            0x8B, 0xCE,
                    });
                    WriteCall(w, vmGetUp);
                    w.Write(new byte[]
                    {
                            //push eax
                            0x50,
                            //push edi
                            0x57,
                            //mov ecx, esi
                            0x8B, 0xCE,
                    });
                    WriteCall(w, vmSet);
                    w.Write(new byte[]
                    {
                            //test al, al
                            0x84, 0xC0,
                            //jz label_2
                            0x74, 0x10,
                            //push 2
                            0x6A, 0x02,
                            //mov ecx, esi
                            0x8B, 0xCE,
                    });
                    WriteCall(w, vmPop);
                    w.Write(new byte[]
                    {
                            //xor eax, eax
                            0x33, 0xC0,
                            //pop edi
                            0x5F,
                            //pop esi
                            0x5E,
                            //pop ebp
                            0x5D,
                            //ret
                            0xC3,
                            //label_2:
                            //push -2
                            0x6A, 0xFE,
                            //mov ecx, esi
                            0x8B, 0xCE,
                    });
                    WriteCall(w, vmGetUp);
                    w.Write(new byte[]
                    {
                            //push eax
                            0x50,
                            //mov ecx, esi
                            0x8B, 0xCE,
                    });
                    WriteCall(w, VMRaise_IdxError);
                    w.Write(new byte[]
                    {
                            //or eax, 0xFFFFFFFF
                            0x83, 0xC8, 0xFF,
                            //pop edi
                            0x5F,
                            //pop esi
                            0x5E,
                            //pop ebp
                            0x5D,
                            //ret
                            0xC3,
                    });

                    var assemlyCode = ms.ToArray();
                    var pCode = AssemblyCodeStorage.WriteCode(assemlyCode);
                    AssemblyCodeStorage.WriteIndirect(jmpForward, pCode);
                }
            }
        }

        public static void Write_rset()
        {
            var vmGetUp = MakeIndirect(0x132B00);
            var vmGetAt = MakeIndirect(0x132B20);
            var vmSet = MakeIndirect(0x134990);
            var VMRaise_IdxError = MakeIndirect(0x13AF10);
            var vmPop = MakeIndirect(0x132A30);
            var jmpForward = AssemblyCodeStorage.AllocateIndirect();

            var dest = AddressHelper.Code(SquirrelFunctions.Addr_sq_rset);
            using (new ReadWriteProtect(dest, 6))
            {
                Marshal.WriteByte(dest, 0, 0xFF);
                Marshal.WriteByte(dest, 1, 0x25);
                Marshal.WriteInt32(dest, 2, jmpForward.ToInt32());
            }

            using (var ms = new MemoryStream())
            {
                using (var w = new BinaryWriter(ms))
                {
                    w.Write(new byte[]
                    {
                            //push ebp
                            0x55,
                            //mov ebp, esp
                            0x8B, 0xEC,
                            //push esi
                            0x56,
                            //push edi
                            0x57,

                            //mov eax, dword [ebp+0xC]
                            0x8B, 0x45, 0x0C,
                            //mov esi, dword [ebp+8]
                            0x8B, 0x75, 0x08,

                            //test eax, eax
                            0x85, 0xC0,
                            //js label_0 (+18)
                            0x78, 0x12,
                            //mov ecx, dword [esi+0x34]
                            0x8B, 0x4E, 0x34,
                            //lea edx, [ecx+eax-1]
                            0x8D, 0x54, 0x01, 0xFF,
                            //push edx
                            0x52,
                            //mov ecx, esi
                            0x8B, 0xCE,
                    });
                    WriteCall(w, vmGetAt);
                    w.Write(new byte[]
                    {
                            //jmp label_1
                            0xEB, 0x09,
                            //label_0:
                            //push eax
                            0x50,
                            //mov ecx, esi
                            0x8B, 0xCE,
                    });
                    WriteCall(w, vmGetUp);
                    w.Write(new byte[]
                    {
                            //label_1:
                            //mov edi, eax
                            0x8B, 0xF8,
                            //push -1
                            0x6A, 0xFF,
                            //push -1
                            0x6A, 0xFF,
                            //mov ecx, esi
                            0x8B, 0xCE,
                    });
                    WriteCall(w, vmGetUp);
                    w.Write(new byte[]
                    {
                            //push eax
                            0x50,
                            //push -2
                            0x6A, 0xFE,
                            //mov ecx, esi
                            0x8B, 0xCE,
                    });
                    WriteCall(w, vmGetUp);
                    w.Write(new byte[]
                    {
                            //push eax
                            0x50,
                            //push edi
                            0x57,
                            //mov ecx, esi
                            0x8B, 0xCE,
                    });
                    WriteCall(w, vmSet);
                    w.Write(new byte[]
                    {
                            //test al, al
                            0x84, 0xC0,
                            //jz label_2
                            0x74, 0x10,
                            //push 2
                            0x6A, 0x02,
                            //mov ecx, esi
                            0x8B, 0xCE,
                    });
                    WriteCall(w, vmPop);
                    w.Write(new byte[]
                    {
                            //xor eax, eax
                            0x33, 0xC0,
                            //pop edi
                            0x5F,
                            //pop esi
                            0x5E,
                            //pop ebp
                            0x5D,
                            //ret
                            0xC3,
                            //label_2:
                            //push -2
                            0x6A, 0xFE,
                            //mov ecx, esi
                            0x8B, 0xCE,
                    });
                    WriteCall(w, vmGetUp);
                    w.Write(new byte[]
                    {
                            //push eax
                            0x50,
                            //mov ecx, esi
                            0x8B, 0xCE,
                    });
                    WriteCall(w, VMRaise_IdxError);
                    w.Write(new byte[]
                    {
                            //or eax, 0xFFFFFFFF
                            0x83, 0xC8, 0xFF,
                            //pop edi
                            0x5F,
                            //pop esi
                            0x5E,
                            //pop ebp
                            0x5D,
                            //ret
                            0xC3,
                    });

                    var assemlyCode = ms.ToArray();
                    var pCode = AssemblyCodeStorage.WriteCode(assemlyCode);
                    AssemblyCodeStorage.WriteIndirect(jmpForward, pCode);
                }
            }
        }

        public static void Write_rget()
        {
            var vmGetUp = MakeIndirect(0x132B00);
            var vmGetAt = MakeIndirect(0x132B20);
            var vmGet = MakeIndirect(0x135B20);
            var throwerror = MakeIndirect(0x12BE60);
            var vmPop = MakeIndirect(0x132A30);
            var jmpForward = AssemblyCodeStorage.AllocateIndirect();
            var stringPtr = Marshal.StringToHGlobalAnsi("the index doesn't exist");

            var dest = AddressHelper.Code(SquirrelFunctions.Addr_sq_rget);
            using (new ReadWriteProtect(dest, 6))
            {
                Marshal.WriteByte(dest, 0, 0xFF);
                Marshal.WriteByte(dest, 1, 0x25);
                Marshal.WriteInt32(dest, 2, jmpForward.ToInt32());
            }

            using (var ms = new MemoryStream())
            {
                using (var w = new BinaryWriter(ms))
                {
                    w.Write(new byte[]
                    {
                        //push ebp
                        0x55,
                        //mov ebp, esp
                        0x8B, 0xEC,
                        //mov eax, dword [ebp+Ch]
                        0x8B, 0x45, 0x0C,
                        //push esi
                        0x56,
                        //mov esi, dword [ebp+8]
                        0x8B, 0x75, 0x08,
                        //push edi
                        0x57,
                        //test eax, eax
                        0x85, 0xC0,
                        //js +0x12
                        0x78, 0x12,
                        //mov ecx, dword [esi+34]
                        0x8B, 0x4E, 0x34,
                        //lea edx, [eax+ecx-1]
                        0x8D, 0x54, 0x01, 0xFF,
                        //push edx
                        0x52,
                        //mov ecx, esi
                        0x8B, 0xCE,
                        //call 2b20
                    });
                    WriteCall(w, vmGetAt);
                    w.Write(new byte[]
                    {
                        //jmp +9
                        0xEB, 0x09,
                        //push eax
                        0x50,
                        //mov ecx, esi
                        0x8B, 0xCE,
                        //call 2b00
                    });
                    WriteCall(w, vmGetUp);
                    w.Write(new byte[]
                    {
                        //push -1
                        0x6A, 0xFF,
                        //push 0
                        0x6A, 0x00,
                        //push -1
                        0x6A, 0xFF,
                        //mov ecx, esi
                        0x8B, 0xCE,
                        //mov edi, eax
                        0x8B, 0xF8,
                        //call 2b00
                    });
                    WriteCall(w, vmGetUp);
                    w.Write(new byte[]
                    {
                        //push eax,
                        0x50,
                        //push -1
                        0x6A, 0xFF,
                        //mov ecx, esi
                        0x8B, 0xCE,
                        //call 2b00
                    });
                    WriteCall(w, vmGetUp);
                    w.Write(new byte[]
                    {
                        //push eax
                        0x50,
                        //push edi
                        0x57,
                        //mov ecx, esi
                        0x8B, 0xCE,
                        //call 5B20
                    });
                    WriteCall(w, vmGet);
                    w.Write(new byte[]
                    {
                        //test al, al
                        0x84, 0xC0,
                        //jz +6
                        0x74, 0x06,
                        //pop edi
                        0x5F,
                        //xor eax, eax
                        0x33, 0xC0,
                        //pop esi
                        0x5E,
                        //pop ebp
                        0x5D,
                        //ret
                        0xC3,
                        //push 1
                        0x6A, 0x01,
                        //mov ecx, esi
                        0x8B, 0xCE,
                        //call 2A30
                    });
                    WriteCall(w, vmPop);
                    w.Write(new byte[]
                    {
                        //push string "the index ..."
                        0x68,
                    });
                    w.Write(stringPtr.ToInt32());
                    w.Write(new byte[]
                    {
                        //push esi
                        0x56,
                        //call BE60
                    });
                    WriteCall(w, throwerror);
                    w.Write(new byte[]
                    {
                        //add esp, 8
                        0x83, 0xC4, 0x08,
                        //pop edi
                        0x5F,
                        //pop esi
                        0x5E,
                        //pop ebp
                        0x5D,
                        //ret
                        0xC3,
                    });

                    var assemlyCode = ms.ToArray();
                    var pCode = AssemblyCodeStorage.WriteCode(assemlyCode);
                    AssemblyCodeStorage.WriteIndirect(jmpForward, pCode);
                }
            }
        }
    }
}
