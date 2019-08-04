using AMLCore.Injection.Engine.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static AMLCore.Injection.Game.Scene.Caocao.CaocaoStageHelper;

namespace AMLCore.Injection.Game.Scene.Caocao
{
    internal class Stage4
    {
        private static ReferencedScriptObject _initFunction;
        private static ReferencedScriptObject _beginFunction;

        public static void Inject()
        {
            SquirrelHelper.InjectCompileFileMain("data/stage/stage4.nut").AddAfter(InjectMain);
            SquirrelHelper.InjectCompileFile("data/stage/stage4.nut", "ChipObjectSet").AddBefore(BeforeEvent);
            _initFunction = SquirrelHelper.GetNewClosure(StageInit);
            _beginFunction = SquirrelHelper.GetNewClosure(StageBegin);
        }

        private static void InjectMain(IntPtr vm)
        {
            SquirrelFunctions.pushstring(vm, "Stage4AInit", -1);
            SquirrelFunctions.pushobject(vm, _initFunction.SQObject);
            SquirrelFunctions.set(vm, 1);

            SquirrelFunctions.pushstring(vm, "Stage4A_Begin", -1);
            SquirrelFunctions.pushobject(vm, _beginFunction.SQObject);
            SquirrelFunctions.newslot(vm, 1, 0);
        }

        private static void BeforeEvent(IntPtr vm)
        {
            SquirrelFunctions.getinteger(vm, 2, out var id);
            if (id == 11 || id == 12 || id == 13)
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
                case 411:
                    CreateMap("stage4A", "map0");
                    StageSetting("StageBaseSetting", "map0", "4_1A");
                    CreateEventFromMap("map0", "start");
                    CreateEventFromMap("map0", "stopPoint1");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 412:
                    CreateMap("stage4A", "map0");
                    StageSetting("StageBaseSetting", "map0", "4_1A");
                    CreateEventFromMap("map0", "start");
                    CreateEventFromMap("map0", "stopPoint2");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 413:
                    CreateMap("stage4A", "map0");
                    StageSetting("StageBaseSetting", "map0", "4_1A");
                    CreateEventFromMap("map0", "start");
                    CreateEventFromMap("map0", "stopPoint3");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 421:
                    CreateMap("stage4B", "map1");
                    StageSetting("StageBaseSetting", "map1", "4_2A");
                    CreateEventFromMap("map1", "start");
                    CreateEventFromMap("map1", "stopPoint1");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 422:
                    CreateMap("stage4B", "map1");
                    StageSetting("StageBaseSetting", "map1", "4_2A");
                    CreateEventFromMap("map1", "start");
                    CreateEventFromMap("map1", "stopPoint2");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 423:
                    CreateMap("stage4B", "map1");
                    StageSetting("StageBaseSetting", "map1", "4_2A");
                    CreateEventFromMap("map1", "start");
                    CreateEventFromMap("map1", "stopPoint3");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 424:
                    CreateMap("stage4B", "map1");
                    StageSetting("StageBaseSetting", "map1", "4_2A");
                    CreateEventFromMap("map1", "start");
                    CreateEventFromMap("map1", "stopPoint4");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 425:
                    CreateMap("stage4B", "map1");
                    StageSetting("StageBaseSetting", "map1", "4_2A");
                    CreateEventFromMap("map1", "start");
                    CreateEventFromMap("map1", "stopPoint5");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 431:
                    CreateMap("stage4C", "map2");
                    StageSetting("StageBaseSetting", "map2", "4_3A");
                    CreateEventFromMap("map2", "start");
                    CreateEventFromMap("map2", "stopPoint1");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 441:
                    CreateMap("stage4D", "map3");
                    CreateMap("stage4E", "map4");
                    StageSetting("StageBaseSetting", "map3", "4_Boss");
                    CreateEventFromMap("map3", "start");
                    CreateEventFromMap("map3", "stopPoint1");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                default:
                    CaocaoPlayerLocation.ResetLocation();
                    goto case 411;
            }
            if (CaocaoPlayerLocation.Name != 441) PlayBgm("mdk_s4.ogg");
            SetState();
            CaocaoPlayerLocation.ResetLocation();
            return 0;
        }

        private static int StageBegin(IntPtr vm)
        {
            if ((CaocaoPlayerLocation.Name % 10) == 1)
            {
                MapIn("WalkRight");
            }
            else
            {
                MapIn("Fall");
            }
            return 0;
        }
    }
}
