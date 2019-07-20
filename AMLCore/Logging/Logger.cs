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
            switch (Startup.Mode)
            {
                case StartupMode.Launcher:
                case StartupMode.LauncherRestart:
                    return "Launcher";
                case StartupMode.Injected:
                    return "Game";
                case StartupMode.Standalone:
                    return "Standalone";
                case StartupMode.Unknown:
                default:
                    return "Unknown";
            }
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
                var newPath = Path.ChangeExtension(path, null) + "_" + DateTime.Now.ToString("yyMMddHHmmssfff") + ".log";
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
            bool printInitial = false;
            lock (_WriterCache)
            {
                var fullPath = PathHelper.GetPath("aml/log/" + filename);
                fullPath = Path.ChangeExtension(fullPath, ".log");
                if (!_WriterCache.TryGetValue(fullPath, out w))
                {
                    w = OpenWriter(fullPath);
                    _WriterCache.Add(fullPath, w);
                    printInitial = true;
                }
            }
            Writer = w;
            Name = loggerName;
            if (printInitial)
            {
                w.WriteLine();
                Print("I", "Core", "process start");
            }
        }

        public TextWriter Writer { get; private set; }
        public string Name { get; private set; }

        private void Print(string type, string module, string text)
        {
            Writer.WriteLine("{0} [{1}|{2}|{3}] {4}",
                DateTime.Now.ToString("yy-MM-dd HH:mm:ss.fff"),
                type,
                ThreadHelper.GetCurrentThreadName(),
                module,
                text);
            Writer.Flush();
        }

        public void Info(string text)
        {
            Print("I", Name, text);
        }

        public void Info(string fmt, params object[] args)
        {
            Info(String.Format(fmt, args));
        }

        public void Error(string text)
        {
            Print("E", Name, text);
        }

        public void Error(string fmt, params object[] args)
        {
            Error(String.Format(fmt, args));
        }
    }
}
