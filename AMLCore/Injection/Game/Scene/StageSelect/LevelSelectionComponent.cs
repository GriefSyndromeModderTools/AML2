using AMLCore.Injection.Engine.Script;
using AMLCore.Injection.Game.CharacterInfo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene.StageSelect
{
    public class LevelSelectionComponent : ICharacterSelectionComponent
    {
        private int[] _level;
        private static IntPtr _findPlayerFunction;

        static LevelSelectionComponent()
        {
            //TODO in LevelInjectEntry, replace ADD_Exp to consider new characters
            //TODO in LevelInjectEntry, replace ADD_Level with an implementation that level up
            //     the actor and gameData separately (work both with and without LevelSelection).
            _findPlayerFunction = SquirrelHelper.CompileScriptFunction(@"
                return function (playerID, dataName, requiredLevel) {
                    try
                    {
                    foreach( idx, aa in ::actor )
                    {
                        if (""playerID"" in aa.u)
                        {
                            if (aa.u.playerID != playerID) continue;
                            if (::playerData[dataName].level != 1) return;
                            while (aa.level < requiredLevel)
                            {
                                aa.Add_Level();
                            }
                        }
                    }
                    }
                    catch (ex_) { ::MessageBox(ex_); }
                };
            ", "LevelSelectionFindPlayers");
        }

        public LevelSelectionComponent()
        {
        }

        public void Draw(ICharacterSelectionDataProvider p)
        {
            var env = p.SceneEnvironment;
            var img1 = env.CreateResource("data/system/StageMain/boss_name.bmp");
            var img2 = env.CreateResource("data/system/num14x19.bmp");
            for (int i = 0; i < p.PlayerCount; ++i)
            {
                if (p.GetConfigIndexSelected(i) == -1) continue;
                var panelPos = p.GetCharacterPanelPosition(i);
                var flash = p.IsComponentActive(this, i) ? p.GetFlashAlpha() : 1;
                var alpha = flash * p.GetCharacterPanelAlpha(i);
                if (alpha > 0)
                {
                    env.BitBlt(img1, panelPos.X - 1, panelPos.Y + 390, 25, 32, 167, 32, Blend.Alpha, alpha);
                    var dig1 = _level[i] / 10;
                    var dig2 = _level[i] % 10;
                    env.BitBlt(img2, panelPos.X + 21, panelPos.Y + 396, 14, 19, dig1 * 14, 0, Blend.Alpha, alpha);
                    env.BitBlt(img2, panelPos.X + 31, panelPos.Y + 396, 14, 19, dig2 * 14, 0, Blend.Alpha, alpha);
                }
            }
        }

        public void Init(ICharacterSelectionDataProvider p)
        {
            if (_level == null)
            {
                _level = new int[p.PlayerCount];
                for (int i = 0; i < _level.Length; ++i)
                {
                    _level[i] = 99;
                }
            }
        }

        public bool IsAvailableForPlayer(ICharacterSelectionDataProvider p, int playerId)
        {
            return true;
        }

        public void ModifyPlayerActor(ICharacterSelectionDataProvider p, int[] types)
        {

            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushobject(vm, _findPlayerFunction);
            SquirrelFunctions.pushroottable(vm);
            SquirrelFunctions.call(vm, 1, 1, 0);

            for (int i = 0; i < types.Length; ++i)
            {
                SquirrelFunctions.pushroottable(vm);
                SquirrelFunctions.pushinteger(vm, i);
                SquirrelFunctions.pushstring(vm, CharacterRegistry.GetCharacterConfigInfo(types[i]).Character.PlayerDataName, -1);
                SquirrelFunctions.pushinteger(vm, _level[i]);
                SquirrelFunctions.call(vm, 4, 0, 0);

                //TODO force reset level in playerData here (get playerData back to level 1 to process next player)
            }

            SquirrelFunctions.pop(vm, 2);
        }
        /*
        private void ModifyPlayerActor(int index, int type, string name)
        {
            //TODO need to be careful here. better check for errors

            var dataName = CharacterRegistry.GetCharacterConfigInfo(type).Character.PlayerDataName;

            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushroottable(vm);
            SquirrelFunctions.pushstring(vm, "playerData", -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.remove(vm, -2);
            SquirrelFunctions.pushstring(vm, dataName, -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.remove(vm, -2);

            SquirrelFunctions.pushstring(vm, "level", -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.getinteger(vm, -1, out int level);
            SquirrelFunctions.pop(vm, 1);

            SquirrelFunctions.pushstring(vm, "lifeMax", -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.getinteger(vm, -1, out int life);
            SquirrelFunctions.pop(vm, 1);

            SquirrelFunctions.pushstring(vm, "soulMax", -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.getinteger(vm, -1, out int soul);
            SquirrelFunctions.pop(vm, 1);

            SquirrelFunctions.pushstring(vm, "baseAtk", -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.getinteger(vm, -1, out int atk);
            SquirrelFunctions.pop(vm, 1);

            SquirrelFunctions.pushstring(vm, "lifeUp", -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.getinteger(vm, -1, out int lifeUp);
            SquirrelFunctions.pop(vm, 1);

            SquirrelFunctions.pushstring(vm, "soulUp", -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.getinteger(vm, -1, out int soulUp);
            SquirrelFunctions.pop(vm, 1);

            SquirrelFunctions.pushstring(vm, "atkUp", -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.getinteger(vm, -1, out int atkUp);
            SquirrelFunctions.pop(vm, 1);

            SquirrelFunctions.pop(vm, 1);

            if (level != 1)
            {
                //Only handle level 1 data
                return;
            }
            CalcPlayerData(_level[index], ref life, ref soul, ref atk, lifeUp, soulUp, atkUp);

            SquirrelFunctions.pushroottable(vm);
            SquirrelFunctions.pushstring(vm, "actor", -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.remove(vm, -2);

            SquirrelFunctions.pushstring(vm, name, -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.remove(vm, -2);


        }

        private void CalcPlayerData(int levelTarget, ref int life, ref int soul, ref int atk, int lifeUp, int soulUp, int atkUp)
        {
            for (int i = 0; i < levelTarget - 1; ++i)
            {
                var newLevel = i + 2;
                var la = (int)Math.Floor(lifeUp - newLevel * 0.15);
                if (la < 1) la = 1;
                life += la;
                soul += soulUp;
                atk += atkUp;
            }
        }
        */
        public void ModifyPlayerType(ICharacterSelectionDataProvider p, int[] types)
        {
            //TODO force reset level in playerData here (ensure all actors created as level 1)
        }

        public void UpdateAll(ICharacterSelectionDataProvider p)
        {
        }

        public void UpdatePlayer(ICharacterSelectionDataProvider p, int playerId)
        {
            var input = p.Input.Input[playerId];
            var abs = Math.Abs(input.X);
            if (abs == 1 || abs > 13 && (abs & 2) == 0)
            {
                int se;
                _level[playerId] += Math.Sign(input.X);
                se = 60;
                if (_level[playerId] > 99)
                {
                    _level[playerId] = 99;
                    se = 61;
                }
                if (_level[playerId] < 1)
                {
                    _level[playerId] = 1;
                    se = 61;
                }
                p.SceneEnvironment.PlaySE(se);
            }
        }
    }
}
