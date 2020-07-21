using AMLCore.Internal.UpdateCheckGithub;
using AMLCore.Misc;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace AMLCore.Internal
{
    namespace UpdateCheckGithub
    {
        public class Release
        {
            public string name;
            public Asset[] assets;
        }
        public class Asset
        {
            public string name;
            public string browser_download_url;
            public int size;
        }
    }
    internal class DownloadTask
    {
        public string Url;
        public string Destination;
        public int Size;

        public DownloadTask(Asset a)
        {
            if (!Directory.Exists(PathHelper.GetPath("aml/update")))
            {
                Directory.CreateDirectory(PathHelper.GetPath("aml/update"));
            }
            Url = a.browser_download_url;
            Destination = PathHelper.GetPath("aml/update/" + a.name);
            Size = a.size;
        }
    }
    public static class OnlineUpdateCheck
    {
        private static readonly Version _CurrentVersion =
            typeof(OnlineUpdateCheck).Assembly.GetName().Version;
        internal static readonly string RestartArg = "/UpdateRestart";
        
        private static string _Version;
        private static DownloadTask[] _Tasks;

        internal static string LatestVersion { get => _Version; }

        private static void CheckGithub()
        {
            var url = @"https://api.github.com/repos/GriefSyndromeModderTools/AML2/releases/latest";
            using (var client = new WebClient())
            {
                client.Headers.Add("User-Agent", "aml2");
                var ret = client.DownloadString(url);
                var r = JsonSerialization.Deserialize<Release>(ret);
                _Version = r.name;
                if (r.assets.Length != 2)
                {
                    return;
                }
                if (r.assets[0].name == "AMLCore.dll" &&
                    r.assets[1].name == "Launcher.exe")
                {
                    _Tasks = new[]
                    {
                        new DownloadTask(r.assets[0]),
                        new DownloadTask(r.assets[1]),
                    };
                }
                else if (r.assets[1].name == "AMLCore.dll" &&
                    r.assets[0].name == "Launcher.exe")
                {
                    _Tasks = new[]
                    {
                        new DownloadTask(r.assets[0]),
                        new DownloadTask(r.assets[1]),
                    };
                }
            }
        }

        private static bool CompareVersion()
        {
            //TODO skip version in config
            var v = _CurrentVersion;
            var vcheck = new Version(v.Major, v.Minor, v.Build);
            var vconfigstr = new IniFile("Core").Read("Update", "LatestVersion", _CurrentVersion.ToString());
            var vconfig = new Version(vconfigstr);
            try
            {
                var vnewest = new Version(_Version);
                return vnewest.CompareTo(vcheck) > 0 && vnewest.CompareTo(vconfig) > 0;
            }
            catch
            {
                CoreLoggers.Update.Error("version check failed, ignoring");
                return false;
            }
        }

        public static bool CheckOnly()
        {
            if (Startup.Mode != StartupMode.Launcher)
            {
                CoreLoggers.Update.Error("cannot check update in game");
                return false;
            }
            return CheckAll();
        }

        public static void Check()
        {
            if (Startup.Mode != StartupMode.Launcher)
            {
                CoreLoggers.Update.Error("cannot check update in game");
                return;
            }
            if (CheckAll())
            {
                //show dialog to ask
                CoreLoggers.Update.Info("update from {0} to {1}", _CurrentVersion, _Version);
                StartUpdate();
            }
        }

        private static bool CheckAll()
        {
            try
            {
                CheckGithub();
            }
            catch (Exception e)
            {
                CoreLoggers.Update.Error("cannot check update from Github api: {0}",
                    e.ToString());
            }
            return CompareVersion();
        }

        private static void StartUpdate()
        {
            if (DownloadUpdate())
            {
                Restart();
            }
        }
        
        private static bool DownloadUpdate()
        {
            if (_Tasks == null)
            {
                CoreLoggers.Update.Error("cannot download update: invalid url");
                WindowsHelper.MessageBox("Update failed. Please download the program again.");
                return false;
            }
            var dialog = new UpdateWaitingWindow(_Tasks);
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                CoreLoggers.Update.Error("download cancelled");
                return false;
            }
            return true;
        }

        private static void Restart()
        {
            CoreLoggers.Update.Info("writing update config");
            new IniFile("Core").Write("Update", "LatestVersion", _CurrentVersion.ToString());
            CoreLoggers.Update.Info("trying to restart");
            try
            {
                var process = new ProcessStartInfo
                {
                    FileName = PathHelper.GetPath("aml/update/Launcher.exe"),
                    WorkingDirectory = PathHelper.GetPath(""),
                    Arguments = RestartArg,
                };
                Process.Start(process);
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                CoreLoggers.Update.Error("cannot start process: ", e.ToString());
            }
        }

        internal static void DoRestart()
        {
            Thread.Sleep(500);
            try
            {
                File.Copy(PathHelper.GetPath("aml/update/Launcher.exe"),
                    PathHelper.GetPath("Launcher.exe"), true);
                File.Copy(PathHelper.GetPath("aml/update/AMLCore.dll"),
                    PathHelper.GetPath("aml/core/AMLCore.dll"), true);
            }
            catch
            {
                WindowsHelper.MessageBox("Cannot update executables. Please retry.");
            }
            Process.Start(PathHelper.GetPath("Launcher.exe"));
            Environment.Exit(0);
        }
    }
}
