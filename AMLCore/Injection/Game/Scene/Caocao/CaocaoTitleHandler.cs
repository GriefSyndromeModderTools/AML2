using AMLCore.Injection.Engine.Input;
using AMLCore.Injection.Engine.Script;
using AMLCore.Injection.Game.Scene.StageSelect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene.Caocao
{
    internal class CaocaoTitleHandler : ISceneEventHandler
    {
        private SceneEnvironment _env;
        private float _cursorX;
        private int _frame = 0;
        private float _faderAlpha;
        private static ReadOnlyInputHandler _input;

        private int _selectedPoint = 0;
        private int[] _allPoints = new[]
        {
            111, 112, 113,
            121, 122, 123, 124,
            131, 132,
            141,

            211, 212, 213, 214,
            221, 222, 223, 224, 225,
            231, 232,
            241,

            311, 312,
            321, 322, 323,
            331, 332, 333,
            341,

            411, 412, 413,
            421, 422, 423, 424, 425,
            431,
            441,

            611, 612, 613,
            621, 622, 623,
            631, 632, 633, 634,
            641,
        };
        private int[] _allX = new[]
        {
            0, 1600, 4000,
            0, 3600, 200, 3493,
            0, 3500,
            0,
            0, 0, 0, 0,
            0, 0, 0, 0, 0,
            0, 0,
            0,
            0, 1600,
            0, 2200, 3600,
            0, 1800, 2710,
            0,
            0, 1800, 2400,
            0, 800, 2400, 3600, 4600,
            0,
            0,
            0, 1100, 4000,
            0, 1400, 2500,
            0, 1000, 4700, 7700,
            0,
        };
        private int[] _allY = new[]
        {
            0, 0, 0,
            0, -1144, -2276, -3432,
            0, 0,
            0,
            0, 0, 0, 0,
            0, 0, 0, 0, 0,
            0, 0,
            0,
            0, -600,
            0, 0, 0,
            0, 420, 550,
            0,
            0, -600, -600,
            0, -600, -600, -600, -600,
            0,
            0,
            0, 0, -280,
            0, 0, 0,
            0, 0, 0, 0,
            0,
        };

        public void PostInit(SceneEnvironment env)
        {
            _env = env;
            _input = ReadOnlyInputHandler.Get();

            //set itemImage[0][0] to null
            using (SquirrelHelper.PushMemberChainThis("itemImage", 0))
            {
                SquirrelHelper.Set(0, ManagedSQObject.Null);
            }
        }

        public void Exit()
        {
        }

        public void PreUpdate()
        {
            _frame += 1;

            using (SquirrelHelper.PushMemberChainThis())
            {
                _faderAlpha = SquirrelHelper.GetFloat("frontFaderAlpha");
                SquirrelHelper.Set("frontFaderAlpha", 0f);
            }
        }

        public void PostUpdate()
        {
            var cursor = SquirrelHelper.PushMemberChainThis("selector", "cursor").PopInt32();
            if (cursor == 0)
            {
                //Draw arrows
                var dx = Math.Abs((float)Math.Cos(_frame * 0.15) * 5f);
                if (_selectedPoint != 0)
                {
                    _env.BitBlt(_env.GetResource("menu_left32x32"), 500 - 31 - dx, 250, 32, 32, 0, 0, Blend.Alpha, 1);
                }
                if (_selectedPoint != _allPoints.Length - 1)
                {
                    _env.BitBlt(_env.GetResource("menu_right32x32"), 800 - 40 + dx, 250, 32, 32, 0, 0, Blend.Alpha, 1);
                }
            }

            _env.DrawNumber("menu_num22x32", 500 + 158, 250, _allPoints[_selectedPoint] / 100, 0, 1);
            _env.DrawNumber("menu_num22x32", 500 + 193, 250, _allPoints[_selectedPoint] / 10 % 10, 0, 1);
            _env.DrawNumber("menu_num22x32", 500 + 228, 250, _allPoints[_selectedPoint] % 10, 0, 1);
            var dash = _env.CreateResource("data/system/Title/_19x7.bmp");
            _env.BitBlt(dash, 500 + 177, 250 + 12, 16, 5, 0, 1, Blend.Alpha, 1);
            _env.BitBlt(dash, 500 + 212, 250 + 12, 16, 5, 0, 1, Blend.Alpha, 1);

            _faderAlpha -= 0.025f;
            if (_faderAlpha < 0) _faderAlpha = 0;

            using (SquirrelHelper.PushMemberChainThis())
            {
                SquirrelHelper.Set("frontFaderAlpha", _faderAlpha);
            }
            _env.StretchBlt(_env.GetResource("black_dot"), -10, -10, 820, 820, 0, 0, 1, 1, Blend.Alpha, _faderAlpha);

            if (cursor == 0 && _input.InputAll.B0 > 0)
            {
                using (SquirrelHelper.PushMemberChainRoot("EnableInput"))
                {
                    SquirrelHelper.CallEmpty(ManagedSQObject.Root, true);
                }
            }
        }

        public void PreUpdate1()
        {
            _cursorX = SquirrelHelper.PushMemberChainThis("cursor_pos", "x").PopFloat();
        }

        public void PostUpdate1()
        {
            var cursor = SquirrelHelper.PushMemberChainThis("selector", "cursor").PopInt32();
            if (cursor == 0)
            {
                //Fix cursor_pos
                _cursorX = _cursorX * 0.4f + 420 * 0.6f;
                if (Math.Abs(_cursorX - 420) < 2)
                {
                    _cursorX = 420;
                }
                using (SquirrelHelper.PushMemberChainThis("cursor_pos"))
                {
                    SquirrelHelper.Set("x", _cursorX);
                }
            }

            if (cursor == 0)
            {
                //Update selection
                var ix = _input.InputAll.X;
                if (ix == 1 || ix > 13 || ix == -1 || ix < -13)
                {
                    var dx = Math.Sign(ix);
                    _selectedPoint += dx;
                    if (_selectedPoint >= _allPoints.Length)
                    {
                        _selectedPoint = _allPoints.Length - 1;
                        _env.PlaySE(61);
                    }
                    else if (_selectedPoint < 0)
                    {
                        _selectedPoint = 0;
                        _env.PlaySE(61);
                    }
                    else if ((Math.Abs(ix) % 5) == 1)
                    {
                        _env.PlaySE(60);
                    }
                }
            }

            if (cursor == 1 && _input.InputAll.B0 > 0)
            {
                CaocaoPlayerLocation.Name = _allPoints[_selectedPoint];
                CaocaoPlayerLocation.X = _allX[_selectedPoint];
                CaocaoPlayerLocation.Y = _allY[_selectedPoint];
                NewStageSelect._initialSelected = CaocaoPlayerLocation.Name / 100 - 1;
            }
        }
    }
}
