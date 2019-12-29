using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Replay
{
    class FileNameParser
    {
        private static string GetVariable(string name)
        {
            return name == "AML" ? "AML" : null;
        }

        private static string ReplaceVariable(string name)
        {
            var cb = name.IndexOf(name.First(Char.IsLetterOrDigit));
            var ce = name.LastIndexOf(name.Last(Char.IsLetterOrDigit)) + 1;
            var varName = name.Substring(cb, ce - cb);
            var v = GetVariable(varName);
            if (v == null)
            {
                return "";
            }
            return name.Substring(0, cb) + v + name.Substring(ce);
        }

        public static string Parse(string format)
        {
            var now = DateTime.Now;
            StringBuilder output = new StringBuilder();
            int i = 0;
            while (i < format.Length)
            {
                if (format[i] == '{')
                {
                    var e = format.IndexOf('}', i);
                    if (e == -1)
                    {
                        e = format.Length;
                    }
                    output.Append(ReplaceVariable(format.Substring(i + 1, e - i - 1)));
                    i = e + 1;
                }
                else
                {
                    var e = format.IndexOf('{', i);
                    if (e == -1)
                    {
                        e = format.Length;
                    }
                    output.Append(now.ToString(format.Substring(i, e - i)));
                    i = e;
                }
            }
            return output.ToString();
        }
    }
}
