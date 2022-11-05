using AMLCore.Injection.Engine.File;
using AMLCore.Injection.Game.Replay;
using AMLCore.Injection.Game.Replay.FramerateControl;
using AMLCore.Injection.Native;
using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.GSO
{
    class GSOReplayRedirect
    {
        public static void Inject()
        {
            new InjectBeforeOpen();
        }

        private class InjectBeforeOpen : CodeInjection
        {
            public InjectBeforeOpen() : base(AddressHelper.Code("gso", 0xA359), 7)
            {
            }

            protected override void Triggered(NativeEnvironment env)
            {
                ReplayRecorderEntry.DisableRecording = true;
                FramerateControlEntry.Enabled = true;
                GSOReplay.IsReplaying = true;

                var str = Marshal.PtrToStringUni(env.GetParameterP(0));
                if (str.EndsWith(".repx"))
                {
                    FileReplacement.RegisterFile(str, new DecompressRepFile());
                    CoreLoggers.GSO.Info("redirected aml repx file: " + str);
                }
            }
        }

        private class DecompressRepFile : CachedModificationFileProxyFactory
        {
            private static byte[] Decompress(byte[] data)
            {
                byte[] decompressedArray = null;
                try
                {
                    using (MemoryStream decompressedStream = new MemoryStream())
                    {
                        using (MemoryStream compressStream = new MemoryStream(data))
                        {
                            using (DeflateStream deflateStream = new DeflateStream(compressStream, CompressionMode.Decompress))
                            {
                                deflateStream.CopyTo(decompressedStream);
                            }
                        }
                        decompressedArray = decompressedStream.ToArray();
                    }
                }
                catch
                {
                    return null;
                }

                return decompressedArray;
            }

            public override byte[] Modify(byte[] data)
            {
                var rep = new ReplayFile(data);
                if (rep.AMLSections.Count > 0)
                {
                    GSOReplay._readSections = rep.AMLSections;
                }
                if (!rep.AMLSections.TryGetValue("aml.rep.blocks", out var blocks))
                {
                    return null;
                }

                using (var ms = new MemoryStream())
                {
                    using (var blocksStream = new MemoryStream(blocks))
                    {
                        using (var blocksReader = new BinaryReader(blocksStream))
                        {
                            while (blocksStream.Position < blocksStream.Length)
                            {
                                var len = blocksReader.ReadInt32();
                                var d = blocksReader.ReadBytes(len);
                                var decompressed = Decompress(d);
                                if (decompressed == null) return data;

                                ms.Write(decompressed, 0, decompressed.Length);
                            }
                            var raw = rep.AMLSections["aml.rep.raw"];
                            ms.Write(raw, 0, raw.Length);
                        }
                    }
                    var input = ms.ToArray();

                    var conv = new ReplayFile(rep.BaseLap, (int)ms.Length / 2 / 3);
                    Buffer.BlockCopy(input, 0, conv.InputData, 0, input.Length);

                    //Reuse ms.
                    ms.Seek(0, SeekOrigin.Begin);
                    ms.SetLength(0);

                    conv.ChatMessages = rep.ChatMessages;
                    conv.Save(ms);
                    return ms.ToArray();
                }
            }
        }
    }
}
