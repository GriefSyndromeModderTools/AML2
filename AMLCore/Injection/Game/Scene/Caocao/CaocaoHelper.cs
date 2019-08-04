using AMLCore.Injection.Engine.Script;
using AMLCore.Injection.Game.ResourcePack;
using AMLCore.Injection.Game.SaveData;
using AMLCore.Injection.Game.Scene.StageSelect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene.Caocao
{
    public static class CaocaoHelper
    {
        private static bool _injected;

        public static void Enable()
        {
            if (!_injected)
            {
                _injected = true;

                var res = typeof(CaocaoHelper).Assembly.GetManifestResourceStream("AMLCore.Injection.Game.Scene.Caocao.CaocaoRes.dat");
                ResourceInjection.AddProvider(new SimpleZipArchiveProvider(res));

                var titleHandler = new CaocaoTitleHandler();
                SceneInjectionManager.RegisterSceneHandler(SystemScene.Title, titleHandler);

                var update1 = SquirrelHelper.InjectCompileFile("data/system/Title/Title.global.nut", "Update1");
                update1.AddBefore(vm => titleHandler.PreUpdate1());
                update1.AddAfter(vm => titleHandler.PostUpdate1());

                SaveDataHelper.ModifySaveData += ProcessSaveData;

                Stage1.Inject();
                Stage2.Inject();
                Stage3.Inject();
                Stage4.Inject();
                Stage6.Inject();

                NewStageSelectOptions.EnableNewStageSelect();
                NewStageSelectOptions.UnlockAllStages();
            }
        }

        private static void ProcessSaveData(GSDataFile.CompoundType data)
        {
            var maxLoop = (int)data["loopNum"];
            if (maxLoop == 0)
            {
                data["loopNum"] = 1;
            }
        }
    }
}
