using AMLCore.Injection.Engine.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static AMLCore.Injection.Game.Scene.Caocao.CaocaoStageHelper;

namespace AMLCore.Injection.Game.Scene.Caocao
{
    internal class Stage6
    {
        private static ReferencedScriptObject _initFunction;
        private static ReferencedScriptObject _beginFunction;

        public static void Inject()
        {
            SquirrelHelper.InjectCompileFileMain("data/stage/stage6.nut").AddAfter(InjectMain);
            SquirrelHelper.InjectCompileFile("data/stage/stage6.nut", "ChipObjectSet").AddBefore(BeforeEvent);
            _initFunction = SquirrelHelper.GetNewClosure(StageInit);
            _beginFunction = SquirrelHelper.GetNewClosure(StageBegin);
        }

        private static void InjectMain(IntPtr vm)
        {
            SquirrelFunctions.pushstring(vm, "Stage6AInit", -1);
            SquirrelFunctions.pushobject(vm, _initFunction.SQObject);
            SquirrelFunctions.set(vm, 1);

            SquirrelFunctions.pushstring(vm, "Stage6A_Begin", -1);
            SquirrelFunctions.pushobject(vm, _beginFunction.SQObject);
            SquirrelFunctions.newslot(vm, 1, 0);
        }

        private static void BeforeEvent(IntPtr vm)
        {
            SquirrelFunctions.getinteger(vm, 2, out var id);
            if (id == 29 || id == 30 || id == 31)
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
                case 611:
                    CreateMap("stage6A", "map0");
                    StageSetting("StageBaseSetting_6st", "map0", "6_1A");
                    CreateEventFromMap("map0", "start");
                    CreateEventFromMap("map0", "stopPoint1");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 612:
                    CreateMap("stage6A", "map0");
                    StageSetting("StageBaseSetting_6st", "map0", "6_1A");
                    CreateEventFromMap("map0", "start");
                    CreateEventFromMap("map0", "stopPoint2");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 613:
                    CreateMap("stage6A", "map0");
                    StageSetting("StageBaseSetting_6st", "map0", "6_1A");
                    CreateEventFromMap("map0", "start");
                    CreateEventFromMap("map0", "stopPoint3");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 621:
                    CreateMap("stage6B", "map1");
                    StageSetting("StageBaseSetting_6stB", "map1", "6_2A");
                    CreateEventFromMap("map1", "start");
                    CreateEventFromMap("map1", "stopPoint1");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 622:
                    CreateMap("stage6B", "map1");
                    StageSetting("StageBaseSetting_6stB", "map1", "6_2A");
                    CreateEventFromMap("map1", "start");
                    CreateEventFromMap("map1", "stopPoint2");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 623:
                    CreateMap("stage6B", "map1");
                    StageSetting("StageBaseSetting_6stB", "map1", "6_2A");
                    CreateEventFromMap("map1", "start");
                    CreateEventFromMap("map1", "stopPoint3");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 631:
                    CreateMap("stage6C", "map2");
                    StageSetting("StageBaseSetting_6stC", "map2", "6_3A");
                    CreateEventFromMap("map2", "start");
                    CreateEventFromMap("map2", "stopPoint1");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 632:
                    CreateMap("stage6C", "map2");
                    StageSetting("StageBaseSetting_6stC", "map2", "6_3A");
                    CreateEventFromMap("map2", "start");
                    CreateEventFromMap("map2", "stopPoint2");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 633:
                    CreateMap("stage6C", "map2");
                    StageSetting("StageBaseSetting_6stC", "map2", "6_3A");
                    CreateEventFromMap("map2", "start");
                    CreateEventFromMap("map2", "stopPoint3");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 634:
                    CreateMap("stage6C", "map2");
                    StageSetting("StageBaseSetting_6stC", "map2", "6_3A");
                    CreateEventFromMap("map2", "start");
                    CreateEventFromMap("map2", "stopPoint4");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 641:
                    CreateMap("stage6D", "map3");
                    CreateMap("stage6E", "map4");
                    StageSetting("StageBaseSetting_6stB", "map3", "6_Boss");
                    CreateEventFromMap("map3", "start");
                    CreateEventFromMap("map3", "stopPoint1");
                    StartFadeIn(0, true);
                    SetScrollLock(false, false, true, true);
                    break;
                default:
                    throw new Exception("Invalid stage location.");
            }
            if (CaocaoPlayerLocation.Name != 641) PlayBgm("mdk_s6.ogg");
            SetState();
            CaocaoPlayerLocation.ResetLocation();
            return 0;
        }

        private static int StageBegin(IntPtr vm)
        {
            if (CaocaoPlayerLocation.Name == 641)
            {
            }
            else
            {
                MapIn("WalkRight");
            }
            return 0;
        }
    }
}
