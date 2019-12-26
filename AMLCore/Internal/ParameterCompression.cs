using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace AMLCore.Internal
{
    //Currently not used. We prefer automatic checking.
    class ParameterCompression
    {
        public static string Compress(string str)
        {
            var data = Encoding.UTF8.GetBytes(str);
            using (var ms = new MemoryStream())
            {
                using (var compress = new DeflateStream(ms, CompressionMode.Compress))
                {
                    using (var ms2 = new MemoryStream(data))
                    {
                        ms2.CopyTo(compress);
                        compress.Close();
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }

        public static string Decompress(string str)
        {
            return null;
        }
    }
}
