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

            SquirrelHelper.GetMemberChainRoot("stage", "StartPoint");
            SquirrelFunctions.pushinteger(vm, playerID);
            if (SquirrelFunctions.get(vm, -2) != 0)
            {
                SquirrelFunctions.pop(vm, 1);
                //TODO log
                return;
            }

            float x = 0;
            SquirrelFunctions.pushstring(vm, "x", -1);
            if (SquirrelFunctions.get(vm, -2) == 0)
            {
                SquirrelFunctions.getfloat(vm, -1, out x);
                SquirrelFunctions.pop(vm, 1);
            }

            float y = 0;
            SquirrelFunctions.pushstring(vm, "y", -1);
            if (SquirrelFunctions.get(vm, -2) == 0)
            {
                SquirrelFunctions.getfloat(vm, -1, out y);
                SquirrelFunctions.pop(vm, 1);
            }

            SquirrelFunctions.pop(vm, 2);

            NewCharacterInitFunctionHelper.CreateCharacter(playerID, type, pallette, x, y);

            //this.camera2d.Reset();
            SquirrelHelper.GetMemberChainRoot("camera2d", "Reset");
            SquirrelHelper.GetMemberChainRoot("camera2d");
            SquirrelFunctions.call(vm, 1, 0, 0);
            SquirrelFunctions.pop(vm, 1);
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

            //::playerData[name_].condition = 2;
            var name = CharacterRegistry.GetCharacterConfigInfo(type).Character.PlayerDataName;
            SquirrelHelper.GetMemberChainRoot("playerData", name);
            SquirrelFunctions.pushstring(vm, "condition", -1);
            SquirrelFunctions.pushinteger(vm, 2);
            SquirrelFunctions.set(vm, -3);
            SquirrelFunctions.pop(vm, 1);

            //this.Game_PlayerDown(type_);
            SquirrelHelper.GetMemberChainRoot("Game_PlayerDown");
            SquirrelFunctions.pushroottable(vm);
            SquirrelFunctions.pushinteger(vm, type);
            SquirrelFunctions.call(vm, 2, 0, 0);
            SquirrelFunctions.pop(vm, 1);

            //We are ignoring save data for now (as int type is actually changing).
        }

        private void WriteGameData_Post(IntPtr vm)
        {
            var allConfig = CharacterRegistry.GetAllConfigs();
            foreach (var i in allConfig)
            {
                if (i.Type == 6) continue;

                SquirrelFunctions.pushobject(vm, _checkPlayerStateFunction.SQObject);
                SquirrelFunctions.pushroottable(vm);
                SquirrelFunctions.pushinteger(vm, i.Type);
                SquirrelFunctions.call(vm, 2, 1, 0);
                SquirrelFunctions.getbool(vm, -1, out var zeroSoul);
                SquirrelFunctions.pop(vm, 2);
                if (zeroSoul != 0)
                {
                    SquirrelHelper.GetMemberChainRoot("playerData", i.Character.PlayerDataName);
                    SquirrelFunctions.pushstring(vm, "soul", -1);
                    SquirrelFunctions.pushinteger(vm, 0);
                    SquirrelFunctions.set(vm, -3);
                    SquirrelFunctions.pop(vm, 1);
                }
            }
        }

        private void Game_Post(IntPtr vm)
        {
            SquirrelHelper.GetMemberChainThis("Game_Over");
            _oldGameOverFunction = new ReferencedScriptObject();
            _oldGameOverFunction.PopFromStack();

            SquirrelFunctions.pushstring(vm, "Game_Over", -1);
            SquirrelFunctions.pushobject(vm, _newGameOverFunction.SQObject);
            SquirrelFunctions.set(vm, 1);
        }

        private int Game_Over(IntPtr vm)
        {
            var allChars = CharacterRegistry.GetAllCharacters();

            SquirrelHelper.GetMemberChainRoot("playerData");

            bool notGameOver = false;
            foreach (var ch in allChars)
            {
                if (ch.PlayerDataName == "QB") continue;

                SquirrelFunctions.pushstring(vm, ch.PlayerDataName, -1);
                SquirrelFunctions.get(vm, -2);
                SquirrelFunctions.pushstring(vm, "condition", -1);
                SquirrelFunctions.get(vm, -2);
                SquirrelFunctions.getinteger(vm, -1, out var r);
                SquirrelFunctions.pop(vm, 2);
                if (r != 2)
                {
                    notGameOver = true;
                    break;
                }
            }

            SquirrelFunctions.pop(vm, 1);

            if (!notGameOver)
            {
                SquirrelFunctions.pushobject(vm, _oldGameOverFunction.SQObject);
                SquirrelFunctions.push(vm, 1);
                SquirrelFunctions.call(vm, 1, 0, 0);
            }
            return 0;
        }
    }
}
