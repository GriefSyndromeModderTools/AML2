using AMLCore.Injection.Engine.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.CharacterInfo
{
    public static class SimpleInitFunction
    {
        private static ReferencedScriptObject _commonInit;

        static SimpleInitFunction()
        {
            _commonInit = SquirrelHelper.CompileScriptFunction(@"
	            this.SetMotion(this.u.CA + 0, 0);
	            this.SetUpdateFunction(this.u.Update_Normal);
	            this.fallLabel = this.u.BeginFall;
	            this.u.dexLV = 5;
	            this.world2d.CreateActor(this.x, this.y, this.direction, this.Player_Shadow, this.DefaultShotTable());
                ", "SimpleInitFunctionCommon");
        }

        public static ReferencedScriptObject Create(int type, int fontType, string[] compileFiles, int ca, Action<IntPtr> additional = null)
        {
            return SquirrelHelper.GetNewClosure(vm =>
            {
                var ch = CharacterRegistry.GetCharacterConfigInfo(type).Character;
                var name = ch.PlayerDataName;

                SquirrelFunctions.pushstring(vm, "type", -1);
                SquirrelFunctions.pushinteger(vm, type);
                SquirrelFunctions.set(vm, 1);

                SquirrelFunctions.pushstring(vm, "fontType", -1);
                SquirrelFunctions.pushinteger(vm, fontType);
                SquirrelFunctions.set(vm, 1);

                SquirrelHelper.GetMemberChainThis("PlayerInit");
                SquirrelFunctions.push(vm, 1);
                SquirrelFunctions.pushstring(vm, name, -1);
                SquirrelFunctions.pushstring(vm, "playerID", -1);
                SquirrelFunctions.get(vm, 2);
                SquirrelFunctions.call(vm, 3, 0, 0);
                SquirrelFunctions.pop(vm, 1);

                SquirrelHelper.GetMemberChainRoot("CompileFile");
                foreach (var file in compileFiles)
                {
                    SquirrelFunctions.push(vm, 1);
                    SquirrelFunctions.pushstring(vm, file, -1);
                    SquirrelFunctions.pushstring(vm, "u", -1);
                    SquirrelFunctions.get(vm, 1);
                    SquirrelFunctions.call(vm, 3, 0, 0);
                }
                SquirrelFunctions.pop(vm, 1);

                SquirrelHelper.GetMemberChainThis("u");

                SquirrelFunctions.pushstring(vm, "CA", -1);
                SquirrelFunctions.pushinteger(vm, ca);
                SquirrelFunctions.set(vm, -3);

                SquirrelFunctions.pushstring(vm, "regainCycle", -1);
                SquirrelFunctions.pushinteger(vm, ch.CharacterData.RegainCycle);
                SquirrelFunctions.set(vm, -3);

                SquirrelFunctions.pushstring(vm, "regainRate", -1);
                SquirrelFunctions.pushinteger(vm, ch.CharacterData.RegainRate);
                SquirrelFunctions.set(vm, -3);

                SquirrelFunctions.pop(vm, 1);

                SquirrelFunctions.pushstring(vm, "atkOffset", -1);
                SquirrelFunctions.pushfloat(vm, ch.CharacterData.AttackOffset);
                SquirrelFunctions.set(vm, 1);

                additional?.Invoke(vm);

                SquirrelFunctions.pushobject(vm, _commonInit.SQObject);
                SquirrelFunctions.push(vm, 1);
                SquirrelFunctions.call(vm, 1, 0, 0);
                SquirrelFunctions.pop(vm, 1);

                return 0;
            });
        }
    }
}
