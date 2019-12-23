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

            using (SquirrelHelper.PushMemberChainRoot("ActorPlayer"))
            {
                using (var t = SquirrelHelper.CallPush(ManagedSQObject.Root).PopRefObject())
                {
                    //t.playerID = playerID
                    using (SquirrelHelper.PushMemberChainObj(t.SQObject))
                    {
                        SquirrelHelper.Set("playerID", playerID);
                    }

                    var patFile = _patFiles[type];
                    var colPath = System.IO.Path.GetDirectoryName(patFile);

                    //CreateAnimationDataWithPalette ...
                    var world2d = SquirrelHelper.PushMemberChainRoot("world2d").PopObject();
                    using (SquirrelHelper.PushMemberChainObj(world2d, "CreateAnimationDataWithPalette"))
                    {
                        SquirrelHelper.CallEmpty(world2d, patFile, colPath + "/" + palette);
                    }

                    //SetActor ...
                    using (SquirrelHelper.PushMemberChainRoot("SetActor"))
                    {
                        SquirrelHelper.CallEmpty(ManagedSQObject.Parameter(1), ManagedSQObject.Null,
                            x, y, 1f, _initFunctions[type].SQObject, t.SQObject);
                    }
                }
            }
        }
    }
}
