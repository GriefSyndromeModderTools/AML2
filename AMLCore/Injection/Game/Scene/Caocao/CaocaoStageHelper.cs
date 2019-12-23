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
            using (var world2d = SquirrelHelper.PushMemberChainRoot("world2d").PopRefObject())
            {
                using (SquirrelHelper.PushMemberChainObj(world2d.SQObject, "CreateMap"))
                {
                    SquirrelHelper.CallEmpty(world2d.SQObject, $"data/stage/{file}.act", name);
                }
            }
        }

        public static void StageSetting(string method, string map, string stageName)
        {
            using (SquirrelHelper.PushMemberChainRoot(method))
            {
                using (var mapObj = SquirrelHelper.PushMemberChainRoot(map).PopRefObject())
                {
                    SquirrelHelper.CallEmpty(ManagedSQObject.Root, mapObj.SQObject);
                }
            }
            using (SquirrelHelper.PushMemberChainRoot("SetStageFlag"))
            {
                SquirrelHelper.CallEmpty(ManagedSQObject.Root);
            }
            using (SquirrelHelper.PushMemberChainRoot("stage"))
            {
                SquirrelHelper.NewSlot("name", stageName);
                SquirrelHelper.NewSlot("beginScript", ManagedSQObject.Null);
            }
        }

        public static void CreateEventFromMap(string map, string group)
        {
            var world2d = SquirrelHelper.PushMemberChainRoot("world2d").PopObject();
            using (SquirrelHelper.PushMemberChainObj(world2d, "CreateEventFromMap"))
            {
                SquirrelHelper.CallEmpty(world2d,
                    SquirrelHelper.PushMemberChainRoot(map, group, "layout").PopObject(),
                    SquirrelHelper.PushMemberChainRoot("stage", "ChipObjectSet").PopObject(),
                    ManagedSQObject.Root);
            }
        }

        public static void StartFadeIn(int color = 0, bool noCallback = false)
        {
            var faderAct = SquirrelHelper.PushMemberChainRoot("FaderAct").PopObject();
            using (SquirrelHelper.PushMemberChainObj(faderAct, "FadeIn"))
            {
                SquirrelHelper.CallEmpty(faderAct, 60, color,
                    noCallback ? ManagedSQObject.Null : SquirrelHelper.PushMemberChainRoot("Game_CountStart").PopObject());
            }
        }

        public static void SetScrollLock(bool l, bool r, bool u, bool d)
        {
            using (SquirrelHelper.PushMemberChainRoot("stage"))
            {
                SquirrelHelper.Set("scrollLeftLock", l);
                SquirrelHelper.Set("scrollRightLock", r);
                SquirrelHelper.Set("scrollUpLock", u);
                SquirrelHelper.Set("scrollDownLock", d);
            }
        }

        public static void PlayBgm(string name)
        {
            using (SquirrelHelper.PushMemberChainRoot("PlayBgm"))
            {
                SquirrelHelper.CallEmpty(ManagedSQObject.Root, $"data/bgm/{name}", 0, 100);
            }
        }

        public static void SetState()
        {
            var world2d = SquirrelHelper.PushMemberChainRoot("world2d").PopObject();
            using (SquirrelHelper.PushMemberChainObj(world2d, "SetState"))
            {
                SquirrelHelper.CallEmpty(world2d, 0);
            }
        }

        public static void MapIn(string method)
        {
            if (method == null)
            {
                using (SquirrelHelper.PushMemberChainRoot("Game_CountStop"))
                {
                    SquirrelHelper.CallEmpty(ManagedSQObject.Root);
                }
            }
            else
            {
                var stage = SquirrelHelper.PushMemberChainRoot("stage").PopObject();
                using (SquirrelHelper.PushMemberChainObj(stage, $"MapIn_{method}"))
                {
                    SquirrelHelper.CallEmpty(stage);
                }
            }
        }

        public static void ResetGs03CaocaoPos()
        {
            using (SquirrelHelper.PushMemberChainRoot())
            {
                SquirrelHelper.NewSlot("stage_x_poi", 0);
                SquirrelHelper.NewSlot("stage_y_poi", 0);
            }
        }
    }
}
