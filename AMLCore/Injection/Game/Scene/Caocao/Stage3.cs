using AMLCore.Injection.Engine.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static AMLCore.Injection.Game.Scene.Caocao.CaocaoStageHelper;

namespace AMLCore.Injection.Game.Scene.Caocao
{
    internal class Stage3
    {
        private static ReferencedScriptObject _initFunction;
        private static ReferencedScriptObject _beginFunction;

        public static void Inject()
        {
            SquirrelHelper.InjectCompileFileMain("data/stage/stage3.nut").AddAfter(InjectMain);
            SquirrelHelper.InjectCompileFile("data/stage/stage3.nut", "ChipObjectSet").AddBefore(BeforeEvent);
            _initFunction = SquirrelHelper.GetNewClosure(StageInit);
            _beginFunction = SquirrelHelper.GetNewClosure(StageBegin);
        }

        private static void InjectMain(IntPtr vm)
        {
            SquirrelFunctions.pushstring(vm, "Stage3AInit", -1);
            SquirrelFunctions.pushobject(vm, _initFunction.SQObject);
            SquirrelFunctions.set(vm, 1);

            SquirrelFunctions.pushstring(vm, "Stage3A_Begin", -1);
            SquirrelFunctions.pushobject(vm, _beginFunction.SQObject);
            SquirrelFunctions.newslot(vm, 1, 0);
        }

        private static void BeforeEvent(IntPtr vm)
        {
            SquirrelFunctions.getinteger(vm, 2, out var id);
            if (id == 14 || id == 15 || id == 16)
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
                case 311:
                    CreateMap("stage3A", "map0");
                    StageSetting("StageBaseSetting", "map0", "3_1A");
                    CreateEventFromMap("map0", "start");
                    CreateEventFromMap("map0", "stopPoint1");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 312:
                    CreateMap("stage3A", "map0");
                    StageSetting("StageBaseSetting", "map0", "3_1A");
                    CreateEventFromMap("map0", "start");
                    CreateEventFromMap("map0", "stopPoint2");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 321:
                    CreateMap("stage3B", "map1");
                    StageSetting("StageBaseSetting_3stB", "map1", "3_2A");
                    CreateEventFromMap("map1", "start");
                    CreateEventFromMap("map1", "stopPoint1");
                    StartFadeIn();
                    SetScrollLock(false, false, true, true);
                    break;
                case 322:
                    CreateMap("stage3B", "map1");
                    StageSetting("StageBaseSetting_3stB", "map1", "3_2A");
                    CreateEventFromMap("map1", "start");
                    CreateEventFromMap("map1", "stopPoint2");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 323:
                    CreateMap("stage3B", "map1");
                    StageSetting("StageBaseSetting_3stB", "map1", "3_2A");
                    CreateEventFromMap("map1", "start");
                    CreateEventFromMap("map1", "stopPoint3");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 331:
                    CreateMap("stage3C", "map2");
                    StageSetting("StageBaseSetting_3stC", "map2", "3_3A");
                    CreateEventFromMap("map2", "start");
                    CreateEventFromMap("map2", "stopPoint1");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 332:
                    CreateMap("stage3C", "map2");
                    StageSetting("StageBaseSetting_3stC", "map2", "3_3A");
                    CreateEventFromMap("map2", "start");
                    CreateEventFromMap("map2", "stopPoint2");
                    CreateEventFromMap("map2", "freeEnemy1");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 333:
                    CreateMap("stage3C", "map2");
                    StageSetting("StageBaseSetting_3stC", "map2", "3_3A");
                    CreateEventFromMap("map2", "start");
                    CreateEventFromMap("map2", "stopPoint3");
                    CreateEventFromMap("map2", "freeEnemy1");
                    StartFadeIn();
                    SetScrollLock(true, false, true, true);
                    break;
                case 341:
                    CreateMap("stage3D", "map3");
                    CreateMap("stage3E", "map4");
                    StageSetting("StageBaseSetting", "map3", "3_Boss");
                    CreateEventFromMap("map3", "start");
                    CreateEventFromMap("map3", "stopPoint1");
                    StartFadeIn(0, true);
                    {
                        SquirrelHelper.GetMemberChainRoot("world2d", "CreateActor");
                        SquirrelHelper.GetMemberChainRoot("world2d");
                        SquirrelFunctions.pushinteger(vm, 0);
                        SquirrelFunctions.pushinteger(vm, 0);
                        SquirrelFunctions.pushfloat(vm, -1);
                        SquirrelHelper.GetMemberChainRoot("stage", "BossMapLoop");
                        SquirrelFunctions.pushnull(vm);
                        SquirrelFunctions.call(vm, 6, 0, 0);
                        SquirrelFunctions.pop(vm, 1);

                        SquirrelHelper.GetMemberChainRoot("StopBgm");
                        SquirrelFunctions.pushroottable(vm);
                        SquirrelFunctions.pushinteger(vm, 2000);
                        SquirrelFunctions.call(vm, 2, 0, 0);
                        SquirrelFunctions.pop(vm, 1);
                    }
                    SetScrollLock(true, true, true, true);
                    break;
                default:
                    throw new Exception("Invalid stage location.");
            }
            if (CaocaoPlayerLocation.Name != 341) PlayBgm("mdk_s3.ogg");
            SetState();
            CaocaoPlayerLocation.ResetLocation();
            return 0;
        }

        private static int StageBegin(IntPtr vm)
        {
            if (CaocaoPlayerLocation.Name == 312 ||
                CaocaoPlayerLocation.Name == 331)
            {
                MapIn("Fall");
            }
            else if (CaocaoPlayerLocation.Name == 341)
            {
                MapIn(null);
            }
            else
            {
                MapIn("WalkRight");
            }
            return 0;
        }
    }
}
