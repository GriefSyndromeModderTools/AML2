using AMLCore.Injection.Engine.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static AMLCore.Injection.Game.Scene.Caocao.CaocaoStageHelper;

namespace AMLCore.Injection.Game.Scene.Caocao
{
    internal class Stage2
    {
        private static ReferencedScriptObject _initFunction;
        private static ReferencedScriptObject _beginFunction;

        public static void Inject()
        {
            SquirrelHelper.InjectCompileFileMain("data/stage/stage2.nut").AddAfter(InjectMain);
            SquirrelHelper.InjectCompileFile("data/stage/stage2.nut", "ChipObjectSet").AddBefore(BeforeEvent);
            _initFunction = SquirrelHelper.GetNewClosure(StageInit);
            _beginFunction = SquirrelHelper.GetNewClosure(StageBegin);
        }

        private static void InjectMain(IntPtr vm)
        {
            SquirrelFunctions.pushstring(vm, "Stage2AInit", -1);
            SquirrelFunctions.pushobject(vm, _initFunction.SQObject);
            SquirrelFunctions.set(vm, 1);

            SquirrelFunctions.pushstring(vm, "Stage2A_Begin", -1);
            SquirrelFunctions.pushobject(vm, _beginFunction.SQObject);
            SquirrelFunctions.newslot(vm, 1, 0);
        }

        private static void BeforeEvent(IntPtr vm)
        {
            SquirrelFunctions.getinteger(vm, 2, out var id);
            if (id == 80 || id == 81 || id == 82)
            {
                SquirrelFunctions.getfloat(vm, 3, out var l);
                SquirrelFunctions.getfloat(vm, 4, out var t);
                SquirrelFunctions.getfloat(vm, 5, out var r);
                SquirrelFunctions.getfloat(vm, 6, out var b);
                if (SquirrelFunctions.gettop(vm) == 6)
                {
                    SquirrelFunctions.pop(vm, 4);
                    SquirrelFunctions.pushfloat(vm, l + CaocaoPlayerLocation.X);
                    SquirrelFunctions.pushfloat(vm, t + CaocaoPlayerLocation.Y);
                    SquirrelFunctions.pushfloat(vm, r + CaocaoPlayerLocation.X);
                    SquirrelFunctions.pushfloat(vm, b + CaocaoPlayerLocation.Y);
                }
            }
        }

        private static int StageInit(IntPtr vm)
        {
            switch (CaocaoPlayerLocation.Name)
            {
                case 211:
                    CreateMap("stage2A", "map0");
                    StageSetting("StageBaseSetting", "map0", "2_1A");
                    CreateEventFromMap("map0", "start");
                    CreateEventFromMap("map0", "warpPoint1");
                    CreateEventFromMap("map0", "stopPoint1");
                    StartFadeIn(16777215);
                    SetScrollLock(true, false, true, true);
                    break;
                case 212:
                    CreateMap("stage2B", "map1");
                    StageSetting("StageBaseSetting", "map1", "2_2A");
                    CreateEventFromMap("map1", "start");
                    CreateEventFromMap("map1", "warpPoint1");
                    CreateEventFromMap("map1", "stopPoint1");
                    StartFadeIn(16777215);
                    SetScrollLock(false, false, true, true);
                    break;
                case 213:
                    CreateMap("stage2C", "map2");
                    StageSetting("StageBaseSetting", "map2", "2_2B");
                    CreateEventFromMap("map2", "start");
                    CreateEventFromMap("map2", "warpPoint1");
                    CreateEventFromMap("map2", "stopPoint1");
                    StartFadeIn(16777215);
                    SetScrollLock(false, false, true, true);
                    break;
                case 214:
                    CreateMap("stage2D", "map3");
                    StageSetting("StageBaseSetting", "map3", "2_2C");
                    CreateEventFromMap("map3", "start");
                    CreateEventFromMap("map3", "warpPoint1");
                    CreateEventFromMap("map3", "stopPoint1");
                    StartFadeIn(16777215);
                    SetScrollLock(false, false, true, true);
                    break;
                case 221:
                    CreateMap("stage2I", "map8");
                    StageSetting("StageBaseSetting", "map8", "2_3A");
                    CreateEventFromMap("map3", "start");
                    CreateEventFromMap("map3", "stopPoint1");
                    CreateEventFromMap("map3", "freeEnemy1");
                    StartFadeIn(16777215);
                    SetScrollLock(false, false, true, true);
                    break;
                case 222:
                    CreateMap("stage2F", "map5");
                    StageSetting("StageBaseSetting", "map5", "2_4B");
                    CreateEventFromMap("map5", "start");
                    CreateEventFromMap("map5", "warpPoint1");
                    CreateEventFromMap("map5", "stopPoint1");
                    StartFadeIn(16777215);
                    SetScrollLock(false, false, true, true);
                    break;
                case 223:
                    CreateMap("stage2E", "map4");
                    StageSetting("StageBaseSetting", "map4", "2_4A");
                    CreateEventFromMap("map4", "start");
                    CreateEventFromMap("map4", "warpPoint1");
                    CreateEventFromMap("map4", "stopPoint1");
                    StartFadeIn(16777215);
                    SetScrollLock(false, false, true, true);
                    break;
                case 224:
                    CreateMap("stage2G", "map6");
                    StageSetting("StageBaseSetting", "map6", "2_4C");
                    CreateEventFromMap("map6", "start");
                    CreateEventFromMap("map6", "warpPoint1");
                    CreateEventFromMap("map6", "stopPoint1");
                    StartFadeIn(16777215);
                    SetScrollLock(false, false, true, true);
                    break;
                case 225:
                    CreateMap("stage2H", "map7");
                    StageSetting("StageBaseSetting", "map6", "2_4D");
                    CreateEventFromMap("map7", "start");
                    CreateEventFromMap("map7", "warpPoint1");
                    CreateEventFromMap("map7", "stopPoint1");
                    StartFadeIn(16777215);
                    SetScrollLock(false, false, true, true);
                    break;
                case 231:
                    CreateMap("stage2L", "map11");
                    StageSetting("StageBaseSetting_2stL", "map11", "2_6A");
                    CreateEventFromMap("map11", "start");
                    CreateEventFromMap("map11", "stopPoint1");
                    CreateEventFromMap("map11", "freeEnemy1");
                    StartFadeIn(16777215);
                    SetScrollLock(false, false, true, true);
                    break;
                case 232:
                    CreateMap("stage2J", "map9");
                    StageSetting("StageBaseSetting", "map9", "2_5A");
                    CreateEventFromMap("map9", "start");
                    CreateEventFromMap("map9", "warpPoint1");
                    StartFadeIn(0);
                    SetScrollLock(false, false, true, true);
                    break;
                case 241:
                    CreateMap("stage2K", "map10");
                    CreateMap("stage2M", "map12");
                    StageSetting("StageBaseSetting_2stK", "map10", "2_Boss");
                    CreateEventFromMap("map10", "start");
                    CreateEventFromMap("map10", "stopPoint1");
                    StartFadeIn();
                    SetScrollLock(false, false, true, true);
                    break;
                default:
                    throw new Exception("Invalid stage location.");
            }
            if (CaocaoPlayerLocation.Name != 241) PlayBgm("mdk_s2.ogg");
            SetState();
            CaocaoPlayerLocation.ResetLocation();
            return 0;
        }

        private static int StageBegin(IntPtr vm)
        {
            if (CaocaoPlayerLocation.Name == 232)
            {
                MapIn("Fall");
            }
            else
            {
                MapIn("WalkRight");
            }
            return 0;
        }
    }
}
