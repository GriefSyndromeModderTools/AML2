using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Misc
{
    public class IniFile
    {
        private string _Path;

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string value, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string @default, StringBuilder retVal, int size, string filePath);

        public IniFile(string name)
        {
            _Path = Path.ChangeExtension(PathHelper.GetPath("aml/config/" + name), ".ini");
            if (!File.Exists(_Path))
            {
                File.WriteAllBytes(_Path, new byte[0]);
            }
        }

        public string Read(string section, string key)
        {
            var retVal = new StringBuilder(255);
            GetPrivateProfileString(section, key, "", retVal, 255, _Path);
            return retVal.ToString();
        }

        public void Write(string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, _Path);
        }

        public void DeleteKey(string section, string key)
        {
            Write(section, key, null);
        }

        public void DeleteSection(string section)
        {
            Write(section, null, null);
        }

        public bool KeyExists(string section, string key)
        {
            return Read(section, key).Length > 0;
        }

        public string Read(string section, string key, string @default)
        {
            var ret = Read(section, key);
            if (ret.Length > 0)
            {
                return ret;
            }
            Write(section, key, @default);
            return @default;
        }
    }
}
