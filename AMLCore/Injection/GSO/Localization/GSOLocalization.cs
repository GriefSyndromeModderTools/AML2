using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.GSO.Localization
{
    internal class GSOLocalization
    {
        public static string ModListReplied { get; private set; }
        public static string ModListReceived { get; private set; }
        public static string ModListReceivedCorrupted { get; private set; }
        public static string ModListReplaced { get; private set; }
        public static string ModListVersionCheckFailure { get; private set; }
        public static string ModListArgCheckSuccess { get; private set; }
        public static string ModListArgCheckFailure { get; private set; }

        static GSOLocalization()
        {
            Zh();
        }

        private static void Zh()
        {
            ModListReplied = "AML启动参数同步请求已回复。";
            ModListReceived = "服务器端同步的AML启动参数已接收。";
            ModListReceivedCorrupted = "服务器端同步的AML启动参数无法识别，已忽略。";
            ModListReplaced = "检查通过，将使用同步的启动参数启动游戏。";
            ModListVersionCheckFailure = "检查失败，无法使用相同的Mod版本，可能导致游戏不同步。";
            ModListArgCheckSuccess = "检查通过，服务器端使用了兼容的启动参数。";
            ModListArgCheckFailure = "检查失败，服务器端使用了不兼容的启动参数，可能导致游戏不同步。";
        }
    }
}
