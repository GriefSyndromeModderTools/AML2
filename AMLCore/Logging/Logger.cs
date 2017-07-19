using AMLCore.Internal;
using AMLCore.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AMLCore.Logging
{
    public class Logger
    {
        private static readonly Dictionary<string, TextWriter> _WriterCache =
            new Dictionary<string, TextWriter>();

        private static string GetFilenameFromProcess()
        {
            return Startup.IsLauncher ? "Launcher" : "Game";
        }

        private static TextWriter OpenWriter(string path)
        {
            Stream file;
            try
            {
                file = File.Open(path, FileMode.Append, FileAccess.Write, FileShare.Read);
            }
            catch (IOException)
            {
                var newPath = Path.ChangeExtension(path, null) + "_" + DateTime.Now.ToString() + ".log";
                try
                {
                    file = File.Open(newPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                }
                catch
                {
                    file = new MemoryStream();
                }
            }
            return TextWriter.Synchronized(new StreamWriter(file, Encoding.UTF8));
        }

        public Logger(string loggerName)
        {
            var filename = GetFilenameFromProcess();
            TextWriter w;
            lock (_WriterCache)
            {
                var fullPath = PathHelper.GetPath("aml/log/" + filename);
                fullPath = Path.ChangeExtension(fullPath, ".log");
                if (!_WriterCache.TryGetValue(fullPath, out w))
                {
                    w = OpenWriter(fullPath);
                    _WriterCache.Add(fullPath, w);

                    w.WriteLine();
                    w.WriteLine("{0} [I][Core]{1}",
                        DateTime.Now.ToString(),
                        "process start");
                    w.Flush();
                }
            }
            Writer = w;
            Name = loggerName;
        }

        public TextWriter Writer { get; private set; }
        public string Name { get; private set; }

        public void Info(string text)
        {
            Writer.WriteLine("{0} [I][{1}]{2}",
                DateTime.Now.ToString(),
                Name,
                text);
            Writer.Flush();
        }

        public void Info(string fmt, params object[] args)
        {
            Info(String.Format(fmt, args));
        }

        public void Error(string text)
        {
            Writer.WriteLine("{0} [E][{1}]{2}",
                DateTime.Now.ToString(),
                Name,
                text);
            Writer.Flush();
        }

        public void Error(string fmt, params object[] args)
        {
            Error(String.Format(fmt, args));
        }
    }
}
