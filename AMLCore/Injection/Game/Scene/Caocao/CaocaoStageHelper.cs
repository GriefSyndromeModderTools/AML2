using AMLCore.Injection.Engine.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene.Caocao
{
    internal class CaocaoStageHelper
    {
        //public static readonly string BeginFunction = "AMLCaocao_BeginScript";

        public static void CreateMap(string file, string name)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelHelper.GetMemberChainRoot("world2d", "CreateMap");
            SquirrelHelper.GetMemberChainRoot("world2d");
            SquirrelFunctions.pushstring(vm, $"data/stage/{file}.act", -1);
            SquirrelFunctions.pushstring(vm, name, -1);
            SquirrelFunctions.call(vm, 3, 0, 0);
            SquirrelFunctions.pop(vm, 1);
        }

        public static void StageSetting(string method, string map, string stageName)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelHelper.GetMemberChainRoot(method);
            SquirrelFunctions.pushroottable(vm);
            SquirrelHelper.GetMemberChainRoot(map);
            SquirrelFunctions.call(vm, 2, 0, 0);
            SquirrelFunctions.pop(vm, 1);

            SquirrelHelper.GetMemberChainRoot("SetStageFlag");
            SquirrelFunctions.pushroottable(vm);
            SquirrelFunctions.call(vm, 1, 0, 0);
            SquirrelFunctions.pop(vm, 1);

            SquirrelHelper.GetMemberChainRoot("stage");
            SquirrelFunctions.pushstring(vm, "name", -1);
            SquirrelFunctions.pushstring(vm, stageName, -1);
            SquirrelFunctions.set(vm, -3);
            SquirrelFunctions.pushstring(vm, "beginScript", -1);
            //SquirrelFunctions.pushstring(vm, "Stage1A_Begin", -1);
            SquirrelFunctions.pushnull(vm); //This is actually never used.
            SquirrelFunctions.set(vm, -3);
            SquirrelFunctions.pop(vm, 1);
        }

        public static void CreateEventFromMap(string map, string group)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelHelper.GetMemberChainRoot("world2d", "CreateEventFromMap");
            SquirrelHelper.GetMemberChainRoot("world2d");
            SquirrelHelper.GetMemberChainRoot(map, group, "layout");
            SquirrelHelper.GetMemberChainRoot("stage", "ChipObjectSet");
            SquirrelFunctions.pushroottable(vm);
            SquirrelFunctions.call(vm, 4, 0, 0);
            SquirrelFunctions.pop(vm, 1);
        }

        public static void StartFadeIn(int color = 0, bool noCallback = false)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelHelper.GetMemberChainRoot("FaderAct", "FadeIn");
            SquirrelHelper.GetMemberChainRoot("FaderAct");
            SquirrelFunctions.pushinteger(vm, 60);
            SquirrelFunctions.pushinteger(vm, color);
            if (noCallback)
            {
                SquirrelFunctions.pushnull(vm);
            }
            else
            {
                SquirrelHelper.GetMemberChainRoot("Game_CountStart");
            }
            SquirrelFunctions.call(vm, 4, 0, 0);
            SquirrelFunctions.pop(vm, 1);
        }

        public static void SetScrollLock(bool l, bool r, bool u, bool d)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelHelper.GetMemberChainRoot("stage");

            SquirrelFunctions.pushstring(vm, "scrollLeftLock", -1);
            SquirrelFunctions.pushbool(vm, l ? 1 : 0);
            SquirrelFunctions.set(vm, -3);
            SquirrelFunctions.pushstring(vm, "scrollRightLock", -1);
            SquirrelFunctions.pushbool(vm, r ? 1 : 0);
            SquirrelFunctions.set(vm, -3);
            SquirrelFunctions.pushstring(vm, "scrollUpLock", -1);
            SquirrelFunctions.pushbool(vm, u ? 1 : 0);
            SquirrelFunctions.set(vm, -3);
            SquirrelFunctions.pushstring(vm, "scrollDownLock", -1);
            SquirrelFunctions.pushbool(vm, d ? 1 : 0);
            SquirrelFunctions.set(vm, -3);

            SquirrelFunctions.pop(vm, 1);
        }

        public static void PlayBgm(string name)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelHelper.GetMemberChainRoot("PlayBgm");
            SquirrelFunctions.pushroottable(vm);
            SquirrelFunctions.pushstring(vm, $"data/bgm/{name}", -1);
            SquirrelFunctions.pushinteger(vm, 0);
            SquirrelFunctions.pushinteger(vm, 100);
            SquirrelFunctions.call(vm, 4, 0, 0);
            SquirrelFunctions.pop(vm, 1);
        }

        public static void SetState()
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelHelper.GetMemberChainRoot("world2d", "SetState");
            SquirrelHelper.GetMemberChainRoot("world2d");
            SquirrelFunctions.pushinteger(vm, 0);
            SquirrelFunctions.call(vm, 2, 0, 0);
            SquirrelFunctions.pop(vm, 1);
        }

        public static void MapIn(string method)
        {
            if (method == null)
            {
                var vm = SquirrelHelper.SquirrelVM;
                SquirrelHelper.GetMemberChainRoot("Game_CountStop");
                SquirrelFunctions.pushroottable(vm);
                SquirrelFunctions.call(vm, 1, 0, 0);
                SquirrelFunctions.pop(vm, 1);
            }
            else
            {
                var vm = SquirrelHelper.SquirrelVM;
                SquirrelHelper.GetMemberChainRoot("stage", $"MapIn_{method}");
                SquirrelHelper.GetMemberChainRoot("stage");
                SquirrelFunctions.call(vm, 1, 0, 0);
                SquirrelFunctions.pop(vm, 1);
            }
        }
    }
}
