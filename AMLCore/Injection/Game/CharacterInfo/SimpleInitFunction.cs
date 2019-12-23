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

                using (SquirrelHelper.PushMemberChainStack(1))
                {
                    SquirrelHelper.Set("type", type);
                    SquirrelHelper.Set("fontType", fontType);
                    var playerID = SquirrelHelper.PushMemberChainStack(2, "playerID").PopInt32();
                    using (SquirrelHelper.PushMemberChainThis("PlayerInit"))
                    {
                        SquirrelHelper.CallEmpty(ManagedSQObject.Parameter(1), name, playerID);
                    }
                }
                using (var u = SquirrelHelper.PushMemberChainStack(1, "u").PopRefObject())
                {
                    using (SquirrelHelper.PushMemberChainRoot("CompileFile"))
                    {
                        foreach (var file in compileFiles)
                        {
                            SquirrelHelper.CallEmpty(ManagedSQObject.Parameter(1), file, u.SQObject);
                        }
                    }
                }
                using (SquirrelHelper.PushMemberChainStack(1, "u"))
                {
                    SquirrelHelper.Set("CA", ca);
                    SquirrelHelper.Set("regainCycle", ch.CharacterData.RegainCycle);
                    SquirrelHelper.Set("regainRate", ch.CharacterData.RegainRate);
                }
                using (SquirrelHelper.PushMemberChainStack(1))
                {
                    SquirrelHelper.Set("atkOffset", ch.CharacterData.AttackOffset);
                }

                additional?.Invoke(vm);

                using (SquirrelHelper.PushMemberChainObj(_commonInit.SQObject))
                {
                    SquirrelHelper.CallEmpty(ManagedSQObject.Parameter(1));
                }
                return 0;
            });
        }
    }
}
