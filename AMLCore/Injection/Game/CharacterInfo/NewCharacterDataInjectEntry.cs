using AMLCore.Injection.Engine.Script;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.CharacterInfo
{
    //This is one of two injection required for new characters.
    //The other is the rendering injection in Scene namespace.
    internal class NewCharacterDataInjectEntry : IEntryPointPostload
    {
        private ReferencedScriptObject _checkPlayerStateFunction, _oldGameOverFunction, _newGameOverFunction;

        public void Run()
        {
            if (!CharacterRegistry.NewCharacterInjected) return;

            SquirrelHelper.InjectCompileFile("data/script/stage.nut", "Stage_SetStartPlayer")
                .AddAfter(Stage_SetStartPlayer_Post);

            SquirrelHelper.InjectCompileFile("data/script/game.nut", "SetGameData")
                .AddAfter(SetGameData_Post);
            SquirrelHelper.InjectCompileFile("data/script/game.nut", "ClearGameDataSet")
                .AddAfter(ClearGameDataSet_Post);
            SquirrelHelper.InjectCompileFile("data/script/game.nut", "Game_PlayerDead")
                .AddAfter(Game_PlayerDead_Post);
            SquirrelHelper.InjectCompileFile("data/script/game.nut", "WriteGameData")
                .AddAfter(WriteGameData_Post);
            SquirrelHelper.InjectCompileFileMain("data/script/game.nut")
                .AddAfter(Game_Post);

            _checkPlayerStateFunction = SquirrelHelper.CompileScriptChildFunction(@"
                function NewCharacterDataCheckPlayerState(type)
                {try {
                    foreach( idx, aa in ::actor )
                    {
                        if (""playerID"" in aa.u)
                        {
                            if (aa.type != type) continue;
                            if (aa.soul <= 0) return true;
                        }
                    } } catch (ex_) { ::MessageBox(ex_); }
                    return false;
                }
                ", "NewCharacterDataCheckPlayerState");
            _newGameOverFunction = SquirrelHelper.GetNewClosure(Game_Over);
        }

        private void Stage_SetStartPlayer_Post(IntPtr vm)
        {
            SquirrelFunctions.getinteger(vm, 3, out var type);
            if (type <= 6) return;

            SquirrelFunctions.getinteger(vm, 2, out var playerID);
            SquirrelFunctions.getstring(vm, 4, out var pallette);

            using (SquirrelHelper.PushMemberChainRoot("stage", "StartPoint", playerID))
            {
                var x = SquirrelHelper.GetFloat("x");
                var y = SquirrelHelper.GetFloat("y");
                NewCharacterInitFunctionHelper.CreateCharacter(playerID, type, pallette, x, y);
            }

            //this.camera2d.Reset();
            var camera2d = SquirrelHelper.PushMemberChainRoot("camera2d").PopObject();
            using (SquirrelHelper.PushMemberChainObj(camera2d, "Reset"))
            {
                SquirrelHelper.CallEmpty(camera2d);
            }
        }

        private void SetGameData_Post(IntPtr vm)
        {
            CharacterRegistry.ResetAllPlayerGlobalData();
        }

        private void ClearGameDataSet_Post(IntPtr vm)
        {
            CharacterRegistry.ResetAllPlayerSoul();
        }

        private void Game_PlayerDead_Post(IntPtr vm)
        {
            SquirrelFunctions.getinteger(vm, 2, out var type);
            if (type <= 6) return;

            var name = CharacterRegistry.GetCharacterConfigInfo(type).Character.PlayerDataName;

            //::playerData[name_].condition = 2;
            using (SquirrelHelper.PushMemberChainRoot("playerData", name))
            {
                SquirrelHelper.Set("condition", 2);
            }

            //this.Game_PlayerDown(type_);
            using (SquirrelHelper.PushMemberChainRoot("Game_PlayerDown"))
            {
                SquirrelHelper.CallEmpty(ManagedSQObject.Root, type);
            }

            //We are ignoring save data for now (as int type is actually changing).
        }

        private void WriteGameData_Post(IntPtr vm)
        {
            var allConfig = CharacterRegistry.GetAllConfigs();
            foreach (var i in allConfig)
            {
                if (i.Type == 6) continue;

                using (SquirrelHelper.PushMemberChainObj(_checkPlayerStateFunction.SQObject))
                {
                    var zeroSoul = SquirrelHelper.CallPush(ManagedSQObject.Root, i.Type).PopBool();
                    if (zeroSoul)
                    {
                        using (SquirrelHelper.PushMemberChainRoot("playerData", i.Character.PlayerDataName))
                        {
                            SquirrelHelper.Set("soul", 0);
                        }
                    }
                }
            }
        }

        private void Game_Post(IntPtr vm)
        {
            _oldGameOverFunction = SquirrelHelper.PushMemberChainThis("Game_Over").PopRefObject();
            using (SquirrelHelper.PushMemberChainThis())
            {
                SquirrelHelper.Set("Game_Over", _newGameOverFunction.SQObject);
            }
        }

        private int Game_Over(IntPtr vm)
        {
            var allChars = CharacterRegistry.GetAllCharacters();

            var playerData = SquirrelHelper.PushMemberChainRoot("playerData").PopObject();

            bool notGameOver = false;
            foreach (var ch in allChars)
            {
                if (ch.PlayerDataName == "QB") continue;

                var r = SquirrelHelper.PushMemberChainObj(playerData, ch.PlayerDataName, "condition").PopInt32();
                if (r != 2)
                {
                    notGameOver = true;
                    break;
                }
            }
            if (!notGameOver)
            {
                using (SquirrelHelper.PushMemberChainObj(_oldGameOverFunction.SQObject))
                {
                    SquirrelHelper.CallEmpty(ManagedSQObject.Parameter(1));
                }
            }

            return 0;
        }
    }
}
