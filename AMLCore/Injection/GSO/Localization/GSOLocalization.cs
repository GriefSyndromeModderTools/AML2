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
        public static string ModListReplacedVersionInconsistent { get; private set; }
        public static string ModListReplaceFailed { get; private set; }
        public static string ModListValidated { get; private set; }
        public static string ModListValidatedVersionInconsistent { get; private set; }
        public static string ModListValidateFailed { get; private set; }

        static GSOLocalization()
        {
            Zh();
        }

        private static void Zh()
        {
            ModListReplied = "AML启动参数同步请求已回复。";
            ModListReceived = "服务器端的AML启动参数已接收。";
            ModListReceivedCorrupted = "服务器端的AML启动参数无法识别，已忽略。";

            ModListReplaced = "检查通过，将使用服务器端的启动参数启动游戏。";
            ModListReplacedVersionInconsistent = "Mod列表检查通过，将使用服务器端的启动参数启动游戏，但Mod版本不匹配，可能导致游戏不同步。";
            ModListReplaceFailed = "Mod列表已接收，但无法使用相同的Mod启动游戏，可能导致游戏不同步。";

            ModListValidated = "检查通过，服务器端使用了兼容的启动参数和Mod版本。";
            ModListValidatedVersionInconsistent = "检查通过，服务器端使用了兼容的启动参数，但Mod版本不匹配，可能导致游戏不同步。";
            ModListValidateFailed = "检查失败，服务器端使用了不兼容的启动参数，可能导致游戏不同步。";
        }
    }
}
