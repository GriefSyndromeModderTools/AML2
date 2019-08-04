using AMLCore.Injection.Engine.Script;
using AMLCore.Injection.Game.ResourcePack;
using AMLCore.Injection.Game.Scene;
using AMLCore.Injection.Game.Scene.StageMain;
using AMLCore.Injection.Game.Scene.StagePause;
using AMLCore.Injection.Game.Scene.StageSelect;
using AMLCore.Misc;
using AMLCore.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.CharacterInfo
{
    internal class TestPatchouliMod : IEntryPointLoad
    {
        private static int _type5, _type2, _type1;

        private static readonly string[] _elementLeft = new[]
        {
                "data/ui/patchouliL0.bmp",
                "data/ui/patchouliL1.bmp",
                "data/ui/patchouliL2.bmp",
                "data/ui/patchouliL3.bmp",
                "data/ui/patchouliL4.bmp",
            };
        private static readonly string[] _elementRight = new[]
        {
                "data/ui/patchouliR0.bmp",
                "data/ui/patchouliR1.bmp",
                "data/ui/patchouliR2.bmp",
                "data/ui/patchouliR3.bmp",
                "data/ui/patchouliR4.bmp",
            };
        private static readonly string[] _elementSingle = new[]
        {
                "data/ui/patchouliS0.bmp",
                "data/ui/patchouliS1.bmp",
                "data/ui/patchouliS2.bmp",
                "data/ui/patchouliS3.bmp",
                "data/ui/patchouliS4.bmp",
            };

        public void Run()
        {
            CharacterRegistry.ReplaceLevelUpFunction();

            //ResourceInjection.AddProvider(new DebugFolderContentProvider(@"E:\Library\Projects\PatchouliMod\pack_aml"));
            //ResourceInjection.AddProvider(new DebugFolderContentProvider(@"E:\Library\Projects\PatchouliMod\pack_aml_static"));
            ResourceInjection.AddProvider(new SimpleZipArchiveProvider(System.IO.File.OpenRead(PathHelper.GetPath("patchouli/1.dat"))));
            ResourceInjection.AddProvider(new SimpleZipArchiveProvider(System.IO.File.OpenRead(PathHelper.GetPath("patchouli/0.dat"))));

            SquirrelHelper.InjectCompileFileMain("data/script/boot.nut").AddAfter(vm =>
            {
                SquirrelHelper.GetMemberChainRoot("CreateSE");
                SquirrelFunctions.pushroottable(vm);
                SquirrelFunctions.pushstring(vm, "data/se/se_patchouli.csv", -1);
                SquirrelFunctions.call(vm, 2, 0, 0);
                SquirrelFunctions.pop(vm, 1);
            });

            _type1 = CharacterRegistry.GetNextFreeType();
            _type2 = CharacterRegistry.GetNextFreeType();
            _type5 = CharacterRegistry.GetNextFreeType();
            CharacterRegistry.RegisterCharacter("patchouli", 10, new CharacterData
            {
                Life = 150,
                LifeUp = 4,
                Soul = 9127 * 3,
                SoulUp = 92,
                AttackOffset = 0.02f,
                Attack = 10,
                AttackUp = 1,
                RegainRate = 6,
                RegainCycle = 2,
            });
            CharacterRegistry.RegisterCharacterConfig("patchouli", _type1);
            CharacterRegistry.RegisterCharacterConfig("patchouli", _type2);
            CharacterRegistry.RegisterCharacterConfig("patchouli", _type5);

            NewStageSelectOptions.EnableNewStageSelect();
            NewStageSelectOptions.AddCharacterRenderer(_type1,
                new ResourceCharacterPictureRenderer("data/ui/patchouli1B.bmp", "data/ui/patchouli1C.bmp"));
            NewStageSelectOptions.AddCharacterRenderer(_type2,
                new ResourceCharacterPictureRenderer("data/ui/patchouli2B.bmp", "data/ui/patchouli2C.bmp"));
            NewStageSelectOptions.AddCharacterRenderer(_type5,
                new ResourceCharacterPictureRenderer("data/ui/patchouli5B.bmp", "data/ui/patchouli5C.bmp"));
            NewStageSelectOptions.AddComponent(new ElementSelectionComponent());

            NewCharacterStagePauseRenderer.EnableCharacter(_type1,
                new PatchouliStagePauseHandler(1, "data/ui/patchouliPauseName.bmp", "data/ui/patchouli1B.bmp"));
            NewCharacterStagePauseRenderer.EnableCharacter(_type2,
                new PatchouliStagePauseHandler(2, "data/ui/patchouliPauseName.bmp", "data/ui/patchouli2B.bmp"));
            NewCharacterStagePauseRenderer.EnableCharacter(_type5,
                new PatchouliStagePauseHandler(5, "data/ui/patchouliPauseName.bmp", "data/ui/patchouli5B.bmp"));

            var sm = new PatchouliStageMainHandler("data/ui/patchouliName.bmp", "data/ui/patchouliGem.bmp", "data/ui/patchouliFaceS.bmp");
            NewCharacterStageMainRenderer.EnableCharacter(_type1, sm);
            NewCharacterStageMainRenderer.EnableCharacter(_type2, sm);
            NewCharacterStageMainRenderer.EnableCharacter(_type5, sm);

            var compileFileList = new[]
            {
                "data/actor/patchouli.nut",
                "data/actor/patchouli4466.nut",
                "data/actor/patchouliDash.nut",
                "data/actor/patchouliDoX.nut",
                "data/actor/patchouliDoNX.nut",
                "data/actor/patchouliDoZ.nut",
                "data/actor/patchouliKaA.nut",
                "data/actor/patchouliKinX.nut",
                "data/actor/patchouliSuiX.nut",
                "data/actor/patchouliSwitch.nut",
            };

            SquirrelHelper.Run(vm =>
            {
                SquirrelFunctions.pushroottable(vm);
                SquirrelFunctions.pushstring(vm, "patchouli_initial_config", -1);
                SquirrelFunctions.pushinteger(vm, 0);
                SquirrelFunctions.set(vm, -3);
                SquirrelFunctions.pop(vm, 1);
            });
            
            NewCharacterInitFunctionHelper.RegisterCharacterInitFunction(_type1,
                "data/actor/patchouli/patchouli.pat",
                SimpleInitFunction.Create(_type1, 0, compileFileList, 10000, PostInitPatchouli));
            NewCharacterInitFunctionHelper.RegisterCharacterInitFunction(_type2,
                "data/actor/patchouli/patchouli.pat",
                SimpleInitFunction.Create(_type2, 0, compileFileList, 10000, PostInitPatchouli));
            NewCharacterInitFunctionHelper.RegisterCharacterInitFunction(_type5,
                "data/actor/patchouli/patchouli.pat",
                SimpleInitFunction.Create(_type5, 0, compileFileList, 10000, PostInitPatchouli));

            //Temporary solution for drawing StageMain things
            //SquirrelHelper.InjectCompileFile("data/system/StageMain/StageMain.global.nut", "CreateGuage")
            //    .AddAfter(CreateGuage_After);

            MetaMethodHelper.RegisterIntSetter("collisionMask", (obj, val) =>
            {
                var vm = SquirrelHelper.SquirrelVM;
                SquirrelFunctions.pushobject(vm, obj);
                SquirrelHelper.GetMemberChainTop("priority");
                SquirrelFunctions.getinteger(vm, -1, out var priority);
                SquirrelFunctions.pop(vm, 1);
                if (priority == 100 || priority == 200)
                {
                    //Player/enemy, not boss.
                    if ((val & 8) != 0) val |= 128;
                }
                return val;
            });
        }

        private void CreateGuage_After(IntPtr vm)
        {
            SquirrelFunctions.pushstring(vm, "charactorType", -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.getinteger(vm, -1, out var type);
            SquirrelFunctions.pop(vm, 1);
            if (type == _type1 || type == _type2 || type == _type5)
            {
                SquirrelFunctions.pushstring(vm, "charactorType", -1);
                SquirrelFunctions.pushinteger(vm, 2);
                SquirrelFunctions.set(vm, -3);
            }
        }

        private static void PostInitPatchouli(IntPtr vm)
        {
            SquirrelHelper.GetMemberChainThis("u");
            SquirrelFunctions.pushstring(vm, "config", -1);
            SquirrelFunctions.pushinteger(vm, -1);
            SquirrelFunctions.newslot(vm, -3, 0);
            SquirrelFunctions.pushstring(vm, "patchouli_guard", -1);
            SquirrelFunctions.pushinteger(vm, 0);
            SquirrelFunctions.newslot(vm, -3, 0);
            SquirrelFunctions.pushstring(vm, "baseDef", -1);
            SquirrelFunctions.pushinteger(vm, 2);
            SquirrelFunctions.newslot(vm, -3, 0);
            SquirrelFunctions.pop(vm, 1);
        }

        private class PatchouliStageMainHandler : ICharacterStageMainHandler
        {
            private readonly string _name, _sg, _face;

            public PatchouliStageMainHandler(string name, string sg, string face)
            {
                _name = name;
                _sg = sg;
                _face = face;
            }

            public void DrawName(SceneEnvironment env, int x, int y)
            {
                var r = env.CreateResource(_name);
                env.BitBlt(r, x - 5, y, r.ImageWidth, r.ImageHeight, 0, 0, Blend.Alpha, 1);
            }

            public Resource GetSoulGem(SceneEnvironment env)
            {
                return env.CreateResource(_sg);
            }

            public void DrawSmallFace(SceneEnvironment env, int x, int y)
            {
                var r = env.CreateResource(_face);
                env.BitBlt(r, x, y, 16, 16, 0, 0, Blend.Alpha, 1);
            }
        }

        public class PatchouliStagePauseHandler : ICharacterStagePauseHandler
        {
            private readonly int _elements;
            private readonly string _name, _img;

            public PatchouliStagePauseHandler(int elements, string name, string img)
            {
                _elements = elements;
                _name = name;
                _img = img;
            }

            public void DrawImage(SceneEnvironment env, int playerIndex, int x, int y)
            {
                var r = env.CreateResource(_img);
                env.BitBlt(r, x, y, r.ImageWidth, r.ImageHeight, 0, 0, Blend.Alpha, 1);

                var vm = SquirrelHelper.SquirrelVM;
                if (_elements == 1)
                {
                    SquirrelHelper.GetMemberChainRoot("actor", $"player{playerIndex + 1}", "u", "elementList");
                    SquirrelFunctions.pushinteger(vm, 0);
                    SquirrelFunctions.get_check(vm, -2);
                    SquirrelFunctions.getinteger(vm, -1, out var e1);
                    SquirrelFunctions.pop(vm, 2);

                    env.BitBlt(env.CreateResource(_elementSingle[e1]), x, y, 128, 384, 0, 0, Blend.Add, 1);
                }
                else if (_elements == 2)
                {
                    SquirrelHelper.GetMemberChainRoot("actor", $"player{playerIndex + 1}", "u", "elementList");
                    SquirrelFunctions.pushinteger(vm, 0);
                    SquirrelFunctions.get_check(vm, -2);
                    SquirrelFunctions.getinteger(vm, -1, out var e1);
                    SquirrelFunctions.pop(vm, 1);
                    SquirrelFunctions.pushinteger(vm, 1);
                    SquirrelFunctions.get_check(vm, -2);
                    SquirrelFunctions.getinteger(vm, -1, out var e2);
                    SquirrelFunctions.pop(vm, 2);

                    env.BitBlt(env.CreateResource(_elementLeft[e1]), x, y, 128, 384, 0, 0, Blend.Add, 1);
                    env.BitBlt(env.CreateResource(_elementRight[e2]), x, y, 128, 384, 0, 0, Blend.Add, 1);
                }
            }

            public void DrawName(SceneEnvironment env, int x, int y)
            {
                var r = env.CreateResource(_name);
                env.BitBlt(r, x, y, r.ImageWidth, r.ImageHeight, 0, 0, Blend.Alpha, 1);
            }
        }

        private class ElementSelectionComponent : ICharacterSelectionComponent
        {
            private static ReferencedScriptObject _modifyPlayerFunction;

            private static readonly int[] _allElementList = new[] { 0, 1, 2, 3, 4 };
            private static readonly int[][] _twoElementList = new[]
            {
                new[] { 0, 1 },
                new[] { 2, 3 },
                new[] { 4, 0 },
                new[] { 1, 2 },
                new[] { 3, 4 },

                new[] { 0, 2 },
                new[] { 1, 3 },
                new[] { 2, 4 },
                new[] { 3, 0 },
                new[] { 4, 1 },
            };
            private static readonly int[][] _oneElementList = new[]
            {
                new[] { 0 }, new[] { 1 }, new[] { 2 }, new[] { 3 }, new[] { 4 },
            };

            private int[] _twoElementPointer = new[] { 0, 0, 0 };
            private int[] _oneElementPointer = new[] { 0, 0, 0 };

            static ElementSelectionComponent()
            {
                _modifyPlayerFunction = SquirrelHelper.CompileScriptChildFunction(@"
                    function PatchouliModifyPlayer(playerID, elementList) {
                        try
                        {
                        foreach( idx, aa in ::actor )
                        {
                            if (""playerID"" in aa.u)
                            {
                                if (aa.u.playerID != playerID) continue;
                                aa.u.elementList <- elementList;
                                aa.u.elementListPointer <- 0;
                                aa.u.config <- elementList[0];
                            }
                        }
                        }
                        catch (ex_) { ::MessageBox(ex_); }
                    }", "PatchouliModifyPlayer");
            }

            public void Init(ICharacterSelectionDataProvider p)
            {
            }

            public void Draw(ICharacterSelectionDataProvider p)
            {
                var env = p.SceneEnvironment;

                for (int i = 0; i < p.PlayerCount; ++i)
                {
                    if (p.GetCharacterNameSelected(i) != "patchouli") continue;
                    if (!p.IsSelectedCharacterAvailable(i)) continue;

                    var pos = p.GetCharacterPanelPosition(i);
                    var alpha = p.GetCharacterPanelAlpha(i);
                    if (p.IsComponentActive(this, i)) alpha *= p.GetFlashAlpha();
                    switch (p.GetConfigIndexSelected(i))
                    {
                        case 0:
                            {
                                var sel = _oneElementPointer[i];
                                env.BitBlt(env.CreateResource(_elementSingle[sel]), pos.X, pos.Y, 128, 384, 0, 0, Blend.Add, alpha);
                            }
                            break;
                        case 1:
                            {
                                var sel = _twoElementPointer[i];
                                var e1 = _twoElementList[sel][0];
                                var e2 = _twoElementList[sel][1];
                                env.BitBlt(env.CreateResource(_elementLeft[e1]), pos.X, pos.Y, 128, 384, 0, 0, Blend.Add, alpha);
                                env.BitBlt(env.CreateResource(_elementRight[e2]), pos.X, pos.Y, 128, 384, 0, 0, Blend.Add, alpha);
                            }
                            break;
                        default:
                            break;
                    }
                }
            }

            public bool IsAvailableForPlayer(ICharacterSelectionDataProvider p, int playerId)
            {
                return p.GetCharacterNameSelected(playerId) == "patchouli" &&
                    p.GetConfigIndexSelected(playerId) != 2;
            }

            public void ModifyPlayerActor(ICharacterSelectionDataProvider p, int[] types)
            {
                for (int playerID = 0; playerID < types.Length; ++playerID)
                {
                    if (types[playerID] == _type5)
                    {
                        ModifySinglePlayerActor(playerID, _allElementList);
                    }
                    else if (types[playerID] == _type2)
                    {
                        ModifySinglePlayerActor(playerID, _twoElementList[_twoElementPointer[playerID]]);
                    }
                    else if (types[playerID] == _type1)
                    {
                        ModifySinglePlayerActor(playerID, _oneElementList[_oneElementPointer[playerID]]);
                    }
                }
            }

            private static void ModifySinglePlayerActor(int playerID, int[] elements)
            {
                var vm = SquirrelHelper.SquirrelVM;
                SquirrelFunctions.pushobject(vm, _modifyPlayerFunction.SQObject);
                SquirrelFunctions.pushroottable(vm);
                SquirrelFunctions.pushinteger(vm, playerID);
                SquirrelFunctions.newarray(vm, 0);
                foreach (var ee in elements)
                {
                    SquirrelFunctions.pushinteger(vm, ee);
                    SquirrelFunctions.arrayappend(vm, -2);
                }
                SquirrelFunctions.call(vm, 3, 0, 0);
                SquirrelFunctions.pop(vm, 1);
            }

            public void ModifyPlayerType(ICharacterSelectionDataProvider p, int[] types)
            {
            }

            public void UpdateAll(ICharacterSelectionDataProvider p)
            {
            }

            public void UpdatePlayer(ICharacterSelectionDataProvider p, int playerId)
            {
                if (p.GetCharacterNameSelected(playerId) != "patchouli") return;
                switch (p.GetConfigIndexSelected(playerId))
                {
                    case 0:
                        {
                            var lastSel = _oneElementPointer[playerId];
                            var input = p.Input.Input[playerId].X;

                            if (input == 1) lastSel += 1;
                            if (input == -1) lastSel -= 1;
                            if (lastSel < 0) lastSel = 4;
                            if (lastSel > 4) lastSel = 0;
                            
                            if (lastSel != _oneElementPointer[playerId])
                            {
                                _oneElementPointer[playerId] = lastSel;
                                p.SceneEnvironment.PlaySE(0);
                            }
                        }
                        break;
                    case 1:
                        {
                            var lastSel = _twoElementPointer[playerId];
                            var input = p.Input.Input[playerId].X;

                            if (input == 1) lastSel += 1;
                            if (input == -1) lastSel -= 1;
                            if (lastSel < 0) lastSel = 9;
                            if (lastSel > 9) lastSel = 0;

                            if (lastSel != _twoElementPointer[playerId])
                            {
                                _twoElementPointer[playerId] = lastSel;
                                p.SceneEnvironment.PlaySE(0);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
