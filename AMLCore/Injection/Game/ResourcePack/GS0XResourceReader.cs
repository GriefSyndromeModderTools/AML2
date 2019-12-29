using AMLCore.Internal;
using AMLCore.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.ResourcePack
{
    public class GS0XResourceReader
    {
        public static readonly GS0XResourceReader Default = new GS0XResourceReader(new[]
        {
            "gs00.dat", "gs01.dat", "gs02.dat", "gs03.dat"
        });

        public GS0XResourceReader(string[] files)
        {
            _files = files;
            FindAllRes();
        }

        private string[] _files;
        private Dictionary<string, Tuple<int, Package.PackageFileInfo>> _resList =
            new Dictionary<string, Tuple<int, Package.PackageFileInfo>>();

        private void FindAllRes()
        {
            for (int i = 0; i < _files.Length; ++i)
            {
                var fn = _files[i];
                CoreLoggers.Resource.Info($"reading package {fn}");
                using (var pack = Package.ReadPackageFile(PathHelper.GetPath(fn)))
                {
                    foreach (var f in pack.FileList)
                    {
                        _resList[f.Key] = new Tuple<int, Package.PackageFileInfo>(i, Package.GetFileInfo(f.Value));
                    }
                }
            }
        }

        public int GetFileLength(string id)
        {
            if (_resList.TryGetValue(id, out var f))
            {
                return f.Item2.Length;
            }
            return -1;
        }

        public void GetFileContent(string id, byte[] buffer)
        {
            if (!_resList.TryGetValue(id, out var f))
            {
                return;
            }
            using (var pack = File.OpenRead(PathHelper.GetPath(_files[f.Item1])))
            {
                var file = Package.RetriveFile(pack, f.Item2);
                file.Read(buffer, 0);
            }
        }
    }
}
