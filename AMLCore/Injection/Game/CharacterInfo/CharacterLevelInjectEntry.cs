using AMLCore.Injection.Engine.Script;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.CharacterInfo
{
    internal class CharacterLevelInjectEntry : IEntryPointPostload
    {
        private static ReferencedScriptObject _newExpFunc, _newLevelFunc;

        public void Run()
        {
            if (!CharacterRegistry.LevelUpInjected) return;

            var expFunc = SquirrelHelper.InjectCompileFile("data/actor/characterCommon.nut", "ADD_Exp");
            var levelFunc = SquirrelHelper.InjectCompileFile("data/actor/characterCommon.nut", "ADD_Level");
            expFunc.AddBefore(BeforeExp);
            expFunc.AddAfter(AfterExp);
            levelFunc.AddBefore(BeforeLevel);
            levelFunc.AddAfter(AfterLevel);

            _newExpFunc = SquirrelHelper.CompileScriptChildFunction(@"
                function NewAddExp(exp_, strType) {
                    this.exp += exp_;
                    if (this.playerData[strType].level < this.level ||
                            this.playerData[strType].level == this.level && this.playerData[strType].exp <= this.exp) {
                        this.playerData[strType].exp = this.exp;
                    }
                    if (this.exp >= this.expMax) {
                        this.ADD_Level(strType);
                    }
                }", "NewAddExp");
            _newLevelFunc = SquirrelHelper.CompileScriptChildFunction(@"
                function NewAddLevel(strType) {
                    if (this.level >= 99) return;
                    this.level++;
                    local la = (this.playerData[strType].lifeUP - this.level * 0.15).tointeger();
                    if (la < 1) la = 1;
                    this.lifeMax += la;
                    this.soulMax += this.playerData[strType].soulUp;
                    this.baseAtk += this.playerData[strType].atkUP;
                    this.exp = 0;
                    if (this.life > 0 && this.soul > 0) {
                        this.life += la;
                        this.regainLife += la;
                        if (this.life > this.lifeMax) this.life = this.lifeMax;
                        if (this.regainLife > this.lifeMax) this.regainLife = this.lifeMax;
                        this.soul += this.playerData[strType].soulUp;
                        if (this.soul > this.soulMax) this.soul = this.soulMax;
                    }
                    if (this.regainLife < this.life) this.regainLife = this.life; //TODO ?
                    if (this.playerData[strType].level < this.level ||
                            this.playerData[strType].level == this.level && this.playerData[strType].exp <= this.exp) {
                        this.playerData[strType].level = this.level;
                        this.playerData[strType].exp = 0;
                        this.playerData[strType].lifeMax = this.lifeMax;
                        this.playerData[strType].soulMax = this.soulMax;
                        this.playerData[strType].baseAtk = this.baseAtk;
                    }
                    if (!(""LevelUpNoEffect"" in this.u)) {
                        this.PlaySE(1011);
                        this.world2d.CreateActor(this.x, this.y, this.direction, this.LevelUP_Effect, this.DefaultShotTable());
                    }
                }", "NewAddLevel");
        }

        private static void BeforeExp(IntPtr vm)
        {
            if (SquirrelFunctions.gettop(vm) != 3) return;
            SquirrelFunctions.pop(vm, 1);
            SquirrelFunctions.pushinteger(vm, -1);
        }

        private static void AfterExp(IntPtr vm)
        {
            var type = SquirrelHelper.PushMemberChainThis("type").PopInt32();
            var charName = CharacterRegistry.GetCharacterConfigInfo(type).Character.PlayerDataName;

            using (SquirrelHelper.PushMemberChainObj(_newExpFunc.SQObject))
            {
                SquirrelHelper.CallEmpty(ManagedSQObject.Parameter(1), ManagedSQObject.Parameter(2), charName);
            }
        }

        private static void BeforeLevel(IntPtr vm)
        {
            if (SquirrelFunctions.gettop(vm) != 2) return;

            using (SquirrelHelper.PushMemberChainRoot("playerData"))
            {
                SquirrelFunctions.pushstring(vm, "level0_player", -1);

                SquirrelFunctions.newtable(vm);
                SquirrelFunctions.pushstring(vm, "level", -1);
                SquirrelFunctions.pushinteger(vm, 100);
                SquirrelFunctions.newslot(vm, -3, 0);

                SquirrelFunctions.newslot(vm, -3, 0);
            }
            SquirrelFunctions.pop(vm, 1);
            SquirrelFunctions.pushstring(vm, "level0_player", -1);
        }

        private static void AfterLevel(IntPtr vm)
        {
            var type = SquirrelHelper.PushMemberChainThis("type").PopInt32();
            var charName = CharacterRegistry.GetCharacterConfigInfo(type).Character.PlayerDataName;

            using (SquirrelHelper.PushMemberChainObj(_newLevelFunc.SQObject))
            {
                SquirrelHelper.CallEmpty(ManagedSQObject.Parameter(1), charName);
            }
        }
    }
}
