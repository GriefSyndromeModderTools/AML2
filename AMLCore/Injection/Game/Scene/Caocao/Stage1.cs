using AMLCore.Injection.Engine.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static AMLCore.Injection.Game.Scene.Caocao.CaocaoStageHelper;

namespace AMLCore.Injection.Game.Scene.Caocao
{
    internal class Stage1
    {
        private static ReferencedScriptObject _initFunction;
        private static ReferencedScriptObject _beginFunction;

        public static void Inject()
        {
            SquirrelHelper.InjectCompileFileMain("data/stage/stage1.nut").AddAfter(InjectMain);
            SquirrelHelper.InjectCompileFile("data/stage/stage1.nut", "ChipObjectSet").AddBefore(BeforeEvent);
            SquirrelHelper.InjectCompileFile("data/stage/stage1.nut", "StopPoint2Actor_1_2A").AddAfter(AfterStopPoint122);
            SquirrelHelper.InjectCompileFile("data/stage/stage1.nut", "StopPoint3Actor_1_2A").AddAfter(AfterStopPoint123);
            _initFunction = SquirrelHelper.GetNewClosure(StageInit);
            _beginFunction = SquirrelHelper.GetNewClosure(StageBegin);
        }

        private static void InjectMain(IntPtr vm)
        {
            SquirrelFunctions.pushstring(vm, "Stage1AInit", -1);
            SquirrelFunctions.pushobject(vm, _initFunction.SQObject);
            SquirrelFunctions.set(vm, 1);

            SquirrelFunctions.pushstring(vm, "Stage1A_Begin", -1);
            SquirrelFunctions.pushobject(vm, _beginFunction.SQObject);
            SquirrelFunctions.newslot(vm, 1, 0);
        }

        private static void AfterStopPoint122(IntPtr vm)
        {
            if (CaocaoPlayerLocation.Name == 122)
            {
                SquirrelHelper.GetMemberChainRoot("stage");
                SquirrelFunctions.pushstring(vm, "scrollBottom", -1);
                SquirrelFunctions.pushinteger(vm, 4152);
                SquirrelFunctions.set(vm, -3);
                SquirrelFunctions.pop(vm, 1);
            }
        }

        private static void AfterStopPoint123(IntPtr vm)
        {
            if (CaocaoPlayerLocation.Name == 123)
            {
                SquirrelHelper.GetMemberChainRoot("stage");
                SquirrelFunctions.pushstring(vm, "scrollBottom", -1);
                SquirrelFunctions.pushinteger(vm, 3064);
                SquirrelFunctions.set(vm, -3);
                SquirrelFunctions.pop(vm, 1);
            }
        }

        private static void BeforeEvent(IntPtr vm)
        {
            SquirrelFunctions.getinteger(vm, 2, out var id);
            if (id == 60 || id == 61 || id == 62)
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
                case 111:
                    CreateMap("stage1A", "map0");
                    StageSetting("StageBaseSetting", "map0", "1_1A");
                    CreateEventFromMap("map0", "start");
                    CreateEventFromMap("map0", "stopPoint1");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 112:
                    CreateMap("stage1A", "map0");
                    StageSetting("StageBaseSetting", "map0", "1_1A");
                    CreateEventFromMap("map0", "start");
                    CreateEventFromMap("map0", "stopPoint2");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 113:
                    CreateMap("stage1A", "map0");
                    StageSetting("StageBaseSetting", "map0", "1_1A");
                    CreateEventFromMap("map0", "start");
                    CreateEventFromMap("map0", "stopPoint3");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 121:
                    CreateMap("stage1B", "map1");
                    StageSetting("StageBaseSetting_1stB", "map1", "1_2A");
                    CreateEventFromMap("map1", "start");
                    CreateEventFromMap("map1", "stopPoint1");
                    StartFadeIn();
                    SetScrollLock(true, false, false, false);
                    break;
                case 122:
                    CreateMap("stage1B", "map1");
                    StageSetting("StageBaseSetting_1stB", "map1", "1_2A");
                    CreateEventFromMap("map1", "start");
                    CreateEventFromMap("map1", "stopPoint2");
                    StartFadeIn();
                    SetScrollLock(false, false, false, false);
                    break;
                case 123:
                    CreateMap("stage1B", "map1");
                    StageSetting("StageBaseSetting_1stB", "map1", "1_2A");
                    CreateEventFromMap("map1", "start");
                    CreateEventFromMap("map1", "stopPoint3");
                    StartFadeIn();
                    SetScrollLock(false, false, false, false);
                    break;
                case 124:
                    CreateMap("stage1B", "map1");
                    StageSetting("StageBaseSetting_1stB", "map1", "1_2A");
                    CreateEventFromMap("map1", "start");
                    CreateEventFromMap("map1", "stopPoint4");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 131:
                    CreateMap("stage1C", "map2");
                    StageSetting("StageBaseSetting", "map2", "1_3A");
                    CreateEventFromMap("map2", "start");
                    CreateEventFromMap("map2", "stopPoint1");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 132:
                    CreateMap("stage1C", "map2");
                    StageSetting("StageBaseSetting", "map2", "1_3A");
                    CreateEventFromMap("map2", "start");
                    CreateEventFromMap("map2", "stopPoint2");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 141:
                    CreateMap("stage1E", "map4");
                    CreateMap("stage1F", "map5");
                    StageSetting("StageBaseSetting", "map4", "1_Boss");
                    CreateEventFromMap("map4", "start");
                    CreateEventFromMap("map4", "stopPoint1");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                default:
                    CaocaoPlayerLocation.ResetLocation();
                    goto case 111;
            }
            if (CaocaoPlayerLocation.Name != 141) PlayBgm("mdk_s1.ogg");
            SetState();
            CaocaoPlayerLocation.ResetLocation();
            return 0;
        }

        private static int StageBegin(IntPtr vm)
        {
            if (CaocaoPlayerLocation.Name == 122)
            {
                MapIn("WalkLeft");
                SetScrollLock(false, true, false, false);
            }
            else if (CaocaoPlayerLocation.Name == 123)
            {
                MapIn("WalkRight");
                SetScrollLock(true, false, false, false);
            }
            else
            {
                MapIn("WalkRight");
            }
            return 0;
        }
    }
}
