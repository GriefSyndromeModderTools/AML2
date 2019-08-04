using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.ResourcePack
{
    public interface IResourceContentProvider
    {
        byte[] GetResourceContent(string path);
    }

    public class DebugFolderContentProvider : IResourceContentProvider
    {
        public readonly string Path;

        public DebugFolderContentProvider(string path)
        {
            Path = path;
        }

        public byte[] GetResourceContent(string path)
        {
            var fullPath = System.IO.Path.Combine(Path, path);
            if (File.Exists(fullPath))
            {
                return File.ReadAllBytes(fullPath);
            }
            return null;
        }
    }

    public class SimpleZipArchiveProvider : IResourceContentProvider
    {
        public readonly Stream ArchiveStream;
        private readonly int _offset;
        private readonly Dictionary<string, Tuple<int, int>> _fileTable = new Dictionary<string, Tuple<int, int>>();
        private readonly MemoryStream _destStream = new MemoryStream();
        private readonly MemoryStream _srcStream = new MemoryStream();
        private readonly byte[] _copyBuffer = new byte[1024];

        public SimpleZipArchiveProvider(Stream archiveStream)
        {
            ArchiveStream = archiveStream;
            byte[] buffer = _copyBuffer;

            archiveStream.Seek(0, SeekOrigin.Begin);
            archiveStream.Read(buffer, 0, 4);
            int tableSize = BitConverter.ToInt32(buffer, 0);
            int totalLen = 0;
            for (int i = 0; i < tableSize; ++i)
            {
                archiveStream.Read(buffer, 0, 5);
                int len = BitConverter.ToInt32(buffer, 0);
                int nameLen = buffer[4];
                archiveStream.Read(buffer, 0, nameLen);
                var name = Encoding.ASCII.GetString(buffer, 0, nameLen);
                _fileTable[name] = new Tuple<int, int>(totalLen, len);
                totalLen += len;
            }

            _offset = (int)archiveStream.Position;
        }

        private void CopySection(int start, int len)
        {
            ArchiveStream.Seek(start, SeekOrigin.Begin);
            _srcStream.Seek(0, SeekOrigin.Begin);
            _srcStream.SetLength(0);
            int toCopy = len;
            while (toCopy > 0)
            {
                int copy = Math.Min(toCopy, _copyBuffer.Length);
                ArchiveStream.Read(_copyBuffer, 0, copy);
                _srcStream.Write(_copyBuffer, 0, copy);
                toCopy -= copy;
            }
            _srcStream.Seek(0, SeekOrigin.Begin);
        }

        public byte[] GetResourceContent(string path)
        {
            if (path.Contains('\\'))
            {
                path = path.Replace('\\', '/');
            }
            if (path.StartsWith("./"))
            {
                path = path.Substring(2);
            }
            if (!_fileTable.TryGetValue(path, out var pos))
            {
                return null;
            }
            CopySection(pos.Item1 + _offset, pos.Item2);

            _destStream.Seek(0, SeekOrigin.Begin);
            _destStream.SetLength(0);
            using (var d = new DeflateStream(_srcStream, CompressionMode.Decompress, true))
            {
                d.CopyTo(_destStream);
            }
            return _destStream.ToArray();
        }
    }
}
