using AMLCore.Injection.Engine.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.CharacterInfo
{
    public static class NewCharacterInitFunctionHelper
    {
        private static readonly Dictionary<int, string> _patFiles = new Dictionary<int, string>();
        private static readonly Dictionary<int, ReferencedScriptObject> _initFunctions = new Dictionary<int, ReferencedScriptObject>();

        public static void RegisterCharacterInitFunction(int type, string patFile, ReferencedScriptObject func)
        {
            _patFiles.Add(type, patFile);
            _initFunctions.Add(type, func);
        }

        internal static void CreateCharacter(int playerID, int type, string palette, float x, float y)
        {
            if (!_patFiles.ContainsKey(type)) return;

            var vm = SquirrelHelper.SquirrelVM;

            //local t = this.ActorPlayer();
            SquirrelHelper.GetMemberChainRoot("ActorPlayer");
            SquirrelFunctions.pushroottable(vm);
            SquirrelFunctions.call(vm, 1, 1, 0);
            SquirrelFunctions.remove(vm, -2);

            var tId = SquirrelFunctions.gettop(vm);

            //t.playerID = playerID;
            SquirrelFunctions.pushstring(vm, "playerID", -1);
            SquirrelFunctions.pushinteger(vm, playerID);
            SquirrelFunctions.set(vm, tId);

            var patFile = _patFiles[type];
            var colPath = System.IO.Path.GetDirectoryName(patFile);

            //CreateAnimationDataWithPalette ...
            SquirrelHelper.GetMemberChainRoot("world2d", "CreateAnimationDataWithPalette");
            SquirrelHelper.GetMemberChainRoot("world2d");
            SquirrelFunctions.pushstring(vm, patFile, -1);
            SquirrelFunctions.pushstring(vm, colPath + "/" + palette, -1);
            SquirrelFunctions.call(vm, 3, 0, 0);
            SquirrelFunctions.pop(vm, 1);

            //SetActor ...
            SquirrelHelper.GetMemberChainRoot("SetActor");
            SquirrelFunctions.push(vm, 1);
            SquirrelFunctions.pushnull(vm);
            SquirrelFunctions.pushfloat(vm, x);
            SquirrelFunctions.pushfloat(vm, y);
            SquirrelFunctions.pushfloat(vm, 1);
            SquirrelFunctions.pushobject(vm, _initFunctions[type].SQObject);
            SquirrelFunctions.push(vm, tId);
            SquirrelFunctions.call(vm, 7, 0, 0);
            SquirrelFunctions.pop(vm, 1);

            SquirrelFunctions.pop(vm, 1); //table
        }
    }
}
