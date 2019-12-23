using AMLCore.Injection.Engine.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene.StageMain
{
    internal class StageMainHandler : ISceneEventHandler
    {
        public static Dictionary<int, ICharacterStageMainHandler> _handlers = new Dictionary<int, ICharacterStageMainHandler>();

        private SceneEnvironment _env;
        private Dictionary<IntPtr, int> _soulPrev = new Dictionary<IntPtr, int>();
        private Dictionary<IntPtr, int> _soulPrev2 = new Dictionary<IntPtr, int>();
        private int _frame;

        private void PreUpdateSoulPrev()
        {
            var tmp = _soulPrev2;
            _soulPrev2 = _soulPrev;
            _soulPrev = tmp;

            _soulPrev2.Clear();
        }

        private int UpdateSoulPrev(IntPtr info, int soul)
        {
            _soulPrev2[info] = soul;
            return _soulPrev.TryGetValue(info, out var ret) ? ret : 0;
        }

        public void PostInit(SceneEnvironment env)
        {
            _env = env;
            _frame = 0;
        }

        public void PreUpdate()
        {
            PreUpdateSoulPrev();
            _frame += 1;
        }

        public void PostUpdate()
        {
            using (SquirrelHelper.PushMemberChainThis("guage"))
            {
                var gsize = SquirrelFunctions.getsize(SquirrelHelper.SquirrelVM, -1);
                if (gsize <= 0) return;

                var top = 470;
                var left0 = 410 - 130 * gsize;
                for (int i = 0; i < gsize; ++i)
                {
                    var info = SquirrelHelper.PushMemberChainStack(-1, i).PopObject();

                    if (info.Type == SQObject.SQObjectType.OT_NULL)
                    {
                        //This should only happen at first frame we leave game.
                        //Wait for the original script to remove unused gauge.
                        break;
                    }

                    var left = left0 + i * 260;
                    var characterType = GetI(info, "charactorType");
                    if (!_handlers.TryGetValue(characterType, out var handler))
                    {
                        continue;
                    }
                    switch (GetI(info, "state"))
                    {
                        case 1:
                            {
                                _env.BitBlt(_env.GetResource("barK"), left, top + 84, 190, 16, 0, 48, Blend.Alpha, 1);
                                var dx = 190f * GetI(info, "revive") / GetI(info, "reviveMax");
                                _env.BitBlt(_env.GetResource("barR"), left + 190 - dx, top + 84, dx, 16, 190 - dx, 64, Blend.Alpha, 1);
                                _env.BitBlt(_env.GetResource("gauge_cloud"), left + 190 - dx, top + 84, dx, 16, (_frame * 12) % 800, 0, Blend.Multi, 0.5f);
                                SetF(info, "greenBarAlpha", 0);
                                _env.DrawNumber("num14x19", left + 157, top + 59, GetI(info, "level"), -2, 1);
                                DrawGem(info, left + 180, top + 32, handler.GetSoulGem(_env));
                                _env.BitBlt(_env.GetResource("Lv"), left + 53, top + 2, 128, 128, 0, 0, Blend.Alpha, 1);
                                _env.DrawNumber("num20x36R", left + 109, top + 94, GetI(info, "soul"), -5, 1);
                                _env.BitBlt(_env.GetResource("name"), left - 1, top + 52, 256, 32, 0, 5 * 32, Blend.Alpha, 1);
                                break;
                            }
                        case 3:
                            SetI(info, "gemAnimPattern", -1);
                            goto case 2;
                        case 2:
                            _env.BitBlt(_env.GetResource("barK"), left, top + 84, 190, 16, 0, 48, Blend.Alpha, 1);
                            DrawGem(info, left + 180, top + 32, handler.GetSoulGem(_env));
                            handler.DrawName(_env, left - 1, top + 52);
                            break;
                        case 0:
                            {
                                _env.BitBlt(_env.GetResource("barW"), left, top + 84, 190, 16, 0, 32, Blend.Alpha, 1);
                                var dxR = 190f * GetI(info, "regainLife") / GetI(info, "lifeMax");
                                _env.BitBlt(_env.GetResource("barR"), left + 190 - dxR, top + 84, dxR, 16, 190 - dxR, 16, Blend.Alpha, 1);
                                var dx = 190f * GetI(info, "life") / GetI(info, "lifeMax");
                                _env.BitBlt(_env.GetResource("barG"), left + 190 - dx, top + 84, dx, 16, 190 - dx, 64, Blend.Alpha, 1);

                                var gba = GetF(info, "greenBarAlpha");
                                gba += 0.01f;
                                if (gba > 1) gba = 1;
                                SetF(info, "greenBarAlpha", gba);

                                _env.BitBlt(_env.GetResource("barG"), left + 190 - dx, top + 84, dx, 16, 190 - dx, 0, Blend.Alpha, gba);
                                _env.DrawNumber("num14x19", left + 157, top + 59, GetI(info, "level"), -2, 1);
                                DrawGem(info, left + 180, top + 32, handler.GetSoulGem(_env));
                                _env.BitBlt(_env.GetResource("Lv"), left + 53, top + 2, 128, 128, 0, 0, Blend.Alpha, 1);

                                var soul = GetI(info, "soul");
                                var soulPrev = UpdateSoulPrev(info.Value.Pointer, soul);
                                var tba = GetF(info, "textBlackAlpha");
                                if (soul != soulPrev)
                                {
                                    tba = 0;
                                }
                                if (tba < 1) tba += 0.05f;
                                SetF(info, "textBlackAlpha", tba);

                                _env.DrawNumber("num20x36R", left + 109, top + 94, soul, -5, 1);
                                if (soul > 0)
                                {
                                    if (GetI(info, "gemAnimPattern") == 4)
                                    {
                                        _env.DrawNumber("num20x36", left + 109, top + 94, soul, -5, tba);
                                    }
                                    else
                                    {
                                        _env.DrawNumber("num20x36", left + 109, top + 94, soul, -5, gba);
                                    }
                                }

                                handler.DrawName(_env, left - 1, top + 52);
                            }
                            break;
                    }

                    {
                        var faceX = 136 - i * 16 + 108;
                        var faceY = 13 + i * 24 + 2;
                        handler.DrawSmallFace(_env, faceX, faceY);
                    }
                }
            }
        }

        public void Exit()
        {
        }

        private void DrawGem(SQObject info, int x, int y, Resource img)
        {
            //Replace this.gemImage
            using (SquirrelHelper.PushMemberChainThis("gemImage"))
            {
                SquirrelHelper.Set(0, img._obj.SQObject);
            }

            //Replace info.charactorType
            var type = GetI(info, "charactorType");
            SetI(info, "charactorType", 0);

            //Call this.DrawGem
            using (SquirrelHelper.PushMemberChainThis("DrawGem"))
            {
                SquirrelHelper.CallEmpty(ManagedSQObject.Parameter(1), x, y, info);
            }

            //Restore info.charactorType
            SetI(info, "charactorType", type);

            //Restore this.gemImage
            using (SquirrelHelper.PushMemberChainThis("gemImage"))
            {
                SquirrelHelper.Set(0, _env.GetResource("jem_homura")._obj.SQObject);
            }
        }

        private static float GetF(SQObject obj, string name)
        {
            return SquirrelHelper.PushMemberChainObj(obj, name).PopFloat();
        }

        private static void SetF(SQObject obj, string name, float value)
        {
            using (SquirrelHelper.PushMemberChainObj(obj))
            {
                SquirrelHelper.Set(name, value);
            }
        }

        private static int GetI(SQObject obj, string name)
        {
            return SquirrelHelper.PushMemberChainObj(obj, name).PopInt32();
        }

        private static void SetI(SQObject obj, string name, int value)
        {
            using (SquirrelHelper.PushMemberChainObj(obj))
            {
                SquirrelHelper.Set(name, value);
            }
        }
    }
}
