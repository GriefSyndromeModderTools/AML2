using AMLCore.Injection.Engine.Input;
using AMLCore.Injection.Engine.Script;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Game.Scene.StageSelect
{
    internal class NewStageSelect : ISceneEventHandler
    {
        private class Panel
        {
            public float Left, Top, OffsetX, OffsetY, VX, VY;
        }

        private class WallPanel : Panel
        {
            public WallPanel()
            {
                VX = (float)(_random.NextDouble() - 0.5) * (_random.Next(4) + 2);
                VY = (float)(_random.NextDouble() - 0.5) * (_random.Next(4) + 2);
                OffsetX = (float)(_random.NextDouble() - 0.5) * (_random.Next(15) + 0);
                OffsetY = (float)(_random.NextDouble() - 0.5) * (_random.Next(15) + 0);
            }

            public WallPanel(float x, float y, float vyRatio = 0.75f)
                : this()
            {
                Left = x;
                Top = y;
                OffsetY *= vyRatio;
                VY *= vyRatio;
            }
        }

        private class SelectorRange
        {
            private int[] _selectedCount;
            private bool _exclusive;
            private bool _wrap;
            private bool _singleMove;
            public bool SingleMove => _singleMove;
            public int[] MaxSelection;

            public SelectorRange(int count, bool exclusive, bool wrap, bool singleMove)
            {
                _selectedCount = new int[count];
                _exclusive = exclusive;
                _wrap = wrap;
                _singleMove = singleMove;
            }

            public bool Update(int dir, ref int value)
            {
                if (dir == 0) return false;

                bool wrap = _wrap;
                int newValue = value + dir;
                if (newValue < 0)
                {
                    if (!wrap) return false;
                    newValue = _selectedCount.Length - 1;
                    wrap = false;
                }
                if (newValue >= _selectedCount.Length)
                {
                    if (!wrap) return false;
                    newValue = 0;
                    wrap = false;
                }
                while (newValue != value)
                {
                    if (_selectedCount[newValue] < GetMaxSelection(newValue))
                    {
                        _selectedCount[newValue] += 1;
                        _selectedCount[value] -= 1;
                        value = newValue;
                        return true;
                    }
                    newValue += dir;
                    if (newValue < 0)
                    {
                        if (!wrap)
                        {
                            return false;
                        }
                        newValue = _selectedCount.Length - 1;
                        wrap = false;
                    }
                    if (newValue >= _selectedCount.Length)
                    {
                        if (!wrap)
                        {
                            return false;
                        }
                        newValue = 0;
                        wrap = false;
                    }
                }
                return false;
            }

            public int GetInitial(int val)
            {
                _selectedCount[val] += 1;
                if (_selectedCount[val] <= GetMaxSelection(val))
                {
                    return val;
                }
                if (!Update(1, ref val))
                {
                    Update(-1, ref val);
                }
                return val;
            }

            private int GetMaxSelection(int index)
            {
                if (!_exclusive) return int.MaxValue;
                if (MaxSelection != null) return MaxSelection[index];
                return 1;
            }

            public void RemoveOne(int val)
            {
                _selectedCount[val] -= 1;
            }
        }

        private class Selector
        {
            private int _current;
            private readonly SelectorRange _range;

            public int Current => _current;

            public Selector(SelectorRange range, int selected)
            {
                _range = range;
                _current = range.GetInitial(selected);
            }

            public bool Update(int dir)
            {
                if (dir == 0) return false;
                var abs = Math.Abs(dir);
                if (abs == 1 || !_range.SingleMove && abs >= 25 && (abs % 7) == 0)
                {
                    return _range.Update(Math.Sign(dir), ref _current);
                }
                return false;
            }

            public void Clear()
            {
                _range.RemoveOne(_current);
                _current = -1;
            }

            public void TryMoveTo(int index)
            {
                _range.RemoveOne(_current);
                _current = _range.GetInitial(index);
            }
        }

        private class StagePanalGroup
        {
            public Resource ImageA, ImageB;
            public SceneElement Element;

            public StagePanalGroup(SceneEnvironment env, int i)
            {
                ImageA = env.GetResource($"stage{i}A");
                ImageB = env.GetResource($"stage{i}B");
                Element = env.GetElement($"stage{i}B");
                Element.Visible = false;
            }
        }

        private class CharacterPanel : Panel
        {
            public int Progress;

            public CharacterPanel(int x, int initialProgress)
            {
                Left = x;
                Top = 30;
                Progress = initialProgress;
                OffsetY = -600;
            }
        }

        private class CharacterInfo
        {
            public readonly string PlayerDataName;
            public Resource[] Selected, Dead;
            public SelectorRange ConfigRange;
            public Selector[] PlayerSelectors;
            public bool Available = true;

            public CharacterInfo(SceneEnvironment env, string resName, string playerDataName = null)
            {
                PlayerDataName = playerDataName ?? resName;
                Selected = new[] { GetResource(env, resName + "_B") };
                Dead = new[] { GetResource(env, resName + "_C") };
                ConfigRange = new SelectorRange(1, false, true, true);
                PlayerSelectors = new[] {
                    new Selector(ConfigRange, 0),
                    new Selector(ConfigRange, 0),
                    new Selector(ConfigRange, 0),
                };
            }

            public CharacterInfo(SceneEnvironment env, string[] resNames, string playerDataName)
            {
                PlayerDataName = playerDataName;
                Selected = resNames.Select(nn => GetResource(env, nn + "_B")).ToArray();
                Dead = resNames.Select(nn => GetResource(env, nn + "_C")).ToArray();
                ConfigRange = new SelectorRange(resNames.Length, false, true, true);
                PlayerSelectors = new[] {
                    new Selector(ConfigRange, 0),
                    new Selector(ConfigRange, 0),
                    new Selector(ConfigRange, 0),
                };
            }

            private static Resource GetResource(SceneEnvironment env, string name)
            {
                if (name[0] == '!')
                {
                    return env.CreateResource(name.Substring(1));
                }
                return env.GetResource(name);
            }
        }

        private class TransitionList
        {
            public class Entry
            {
                public int Index;
                public int Start;
            }
            public readonly List<Entry> Entries = new List<Entry>();
            public const int Max = 128;
            private Selector _selector;
            private int _position;

            public TransitionList(Selector selector)
            {
                Reset(selector);
            }

            public void Reset(Selector selector)
            {
                _selector = selector;
                Entries.Clear();
                _position = -1;
                if (_selector != null)
                {
                    Entries.Add(new Entry { Index = selector.Current, Start = 0 });
                    Entries.Add(new Entry { Index = -1, Start = Max });
                    _position = 0;
                }
            }

            public void Update()
            {
                if (_selector == null) return;

                var index = _selector.Current;
                var position = -1;
                for (int i = 0; i < Entries.Count - 1; ++i)
                {
                    if (Entries[i].Index == index)
                    {
                        position = i;
                        break;
                    }
                }
                if (position == -1)
                {
                    var start = Entries[_position].Start + (Entries[_position + 1].Start - Entries[_position].Start) / 2;
                    Entries.Insert(_position + 1, new Entry { Index = index, Start = start });
                    Entries.Insert(_position + 2, new Entry { Index = Entries[_position].Index, Start = start });
                    position = _position + 1;
                }
                for (int i = 0; i < position; ++i)
                {
                    Entries[i + 1].Start -= (position - i) * 4;
                    if (Entries[i + 1].Start < 0)
                    {
                        Entries[i + 1].Start = 0;
                    }
                }
                for (int i = Entries.Count - 2; i > position; --i)
                {
                    Entries[i].Start += (i - position) * 4;
                    if (Entries[i].Start > Max)
                    {
                        Entries[i].Start = Max;
                    }
                }
                for (int i = 0; i < Entries.Count - 1; ++i)
                {
                    if (Entries[i + 1].Start == Entries[i].Start)
                    {
                        Entries.RemoveAt(i);
                        i -= 1;
                    }
                }
                _position = position;
            }

            public void Flush()
            {
                Reset(_selector);
            }
        }

        private class PlayerSelection
        {
            public bool Entered
            {
                get => CharacterSelector != null;
            }

            public bool Confirmed;
            public Selector CharacterSelector { get; private set; }
            public TransitionList TransitionList { get; private set; }
            public int SelectedCharacterIndex => Entered ? CharacterSelector.Current : -1;
            public ICharacterSelectionComponent CurrentComponent;

            private SelectorRange _range;
            private int _preferredIndex;

            public PlayerSelection(SelectorRange range, int preferred, int selected)
            {
                _range = range;
                _preferredIndex = preferred;
                if (selected != -1)
                {
                    CharacterSelector = new Selector(_range, selected);
                }
                TransitionList = new TransitionList(null);
            }

            public void Enter()
            {
                if (Entered) return;
                CharacterSelector = new Selector(_range, _preferredIndex);
            }

            public void Leave()
            {
                if (!Entered) return;
                _preferredIndex = CharacterSelector.Current;
                CharacterSelector.Clear();
                CharacterSelector = null;
            }
        }

        private class DataProvider : ICharacterSelectionDataProvider
        {
            public NewStageSelect Parent;

            public SceneEnvironment SceneEnvironment => Parent._env;
            public ReadOnlyInputHandler Input => _input;
            public int PlayerCount => 3;

            public string GetCharacterNameSelected(int p)
            {
                var selIndex = Parent._playerSelection[p].SelectedCharacterIndex;
                if (selIndex == -1) return null;
                return Parent._characterInfo[selIndex].PlayerDataName;
            }

            public int GetConfigIndexSelected(int p)
            {
                var selIndex = Parent._playerSelection[p].SelectedCharacterIndex;
                if (selIndex == -1) return -1;
                return Parent._characterInfo[selIndex].PlayerSelectors[p].Current;
            }

            public float GetCharacterPanelAlpha(int p)
            {
                return 1 + Parent._characterPanel[p].OffsetY / 300.0f;
            }

            public PointF GetCharacterPanelPosition(int p)
            {
                var panel = Parent._characterPanel[p];
                return new PointF(panel.Left, panel.Top + panel.OffsetY);
            }

            public float GetFlashAlpha()
            {
                return 0.8f + 0.2f * (float)Math.Sin(Parent._frame / 120.0 * 6.28);
            }

            public float GetUIAlpha()
            {
                return Parent._characterSelectAlpha;
            }

            public bool IsComponentActive(ICharacterSelectionComponent component, int p)
            {
                return Parent._playerSelection[p].CurrentComponent == component;
            }
        }

        private enum UpdateFunction
        {
            None,
            StageSelect,
            CharacterSelectEnter,
            CharacterSelect,
            CharacterSelectLeave,
            StartGame,
        }

        private static ReadOnlyInputHandler _input;
        private static ReferencedScriptObject _emptyFunc;
        private static ReferencedScriptObject _exitToTitleFunction;
        private static ReferencedScriptObject _prepStartStageFunction;
        private static ReferencedScriptObject _loopStartStageFunction;

        private static bool _handlerRegistered = false;
        internal static bool _alwaysChooseDeadAsQB = false;
        internal static bool _noAvailableChooseDeadAsQB = true;
        internal static bool _allowSameChar = false;
        internal static bool _allowQBOnlyGame = false;

        private static int[] _stageState;

        private static Random _random = new Random();
        private static int[] _randomLines = CreateRandomLines();

        private static int[] _playerTypeForQB = new int[3];

        internal readonly static List<ICharacterSelectionComponent> _componentList = new List<ICharacterSelectionComponent>();

        private DataProvider _provider;
        private SceneEnvironment _env;
        private Resource _blankCharacterImage;

        private UpdateFunction _currentUpdateType;

        private int _frame;
        private bool _focusExitItem;
        private SelectorRange _stageSelectorRange;
        private Selector _stageSelector;

        private float _stageSelectAlpha;
        private float _stageFaderAlpha;
        private float _characterSelectAlpha;

        private int[] _stageIndexMap;

        private SceneElement[] _bgImage;
        private WallPanel[] _bgPanel;

        private StagePanalGroup[] _stageImageAll;
        private StagePanalGroup[] _stageImage;
        private WallPanel[] _stagePanel;
        private CharacterInfo[] _characterInfo;
        private int _totalAvailableChar;
        private int[][] _characterIndexMap;
        private SelectorRange _characterRange;
        private CharacterPanel[] _characterPanel;
        private PlayerSelection[] _playerSelection;
        private Resource[] _playerCursorImage;
        private Resource[] _playerCursorImageG;

        private int[] _startGamePlayerTypes;

        static NewStageSelect()
        {
            _input = Engine.Input.ReadOnlyInputHandler.Get();
            SquirrelHelper.Run(vm =>
            {
                _emptyFunc = SquirrelHelper.GetNewClosure(vm_ => 0);
                _exitToTitleFunction = SquirrelHelper.CompileScriptFunction(@"
                    this.FaderAct.FadeOut(60, 0, function () {
                        this.pl.EndStage();
                        this.TitleAct.pl.BeginStage(0);
                    }.bindenv(this));", "NewStageSelectExitToTitle");
                _prepStartStageFunction = SquirrelHelper.CompileScriptFunction(@"
                    this.FaderAct.FadeOut(60, 0, null);
                    this.StopBgm(3000);", "NewStageSelectPrepStartStage");
                _loopStartStageFunction = SquirrelHelper.CompileScriptFunction(@"
	                if (!this.FaderAct.IsFading())
	                {
		                this.NowLoadingAct.Show(true);
		                if (this.NowLoadingAct.global.stopFlag == true)
		                {
			                this.pl.EndStage();
                            local i0 = this.thisAct.SelectCharactor[0] == -1 ? null : thisAct.SelectCharactor[0];
                            local i1 = this.thisAct.SelectCharactor[1] == -1 ? null : thisAct.SelectCharactor[1];
                            local i2 = this.thisAct.SelectCharactor[2] == -1 ? null : thisAct.SelectCharactor[2];
			                this.StartStage(this.thisAct.lastStage, i0, i1, i2);
                            return true;
		                }
	                }
                    return false;", "NewStageSelectPrepStartStage");
            });
        }

        public static void UseNewStageSelect()
        {
            if (!_handlerRegistered)
            {
                _handlerRegistered = true;
                SceneInjectionManager.RegisterSceneHandler(SystemScene.StageSelect, new NewStageSelect());
            }
        }

        internal NewStageSelect()
        {
            _provider = new DataProvider { Parent = this };
        }

        public void PostInit(SceneEnvironment env)
        {
            ClearOriginalUpdate();

            //TODO allow customize stage availability?
            _env = env;

            _frame = 0;
            _focusExitItem = false;
            _stageSelectAlpha = 1;
            _stageFaderAlpha = 0;
            _characterSelectAlpha = 0;
            _currentUpdateType = UpdateFunction.StageSelect;

            _bgImage = new[]
            {
                _env.GetElement("pictureA1"),
                _env.GetElement("pictureB1"),
                _env.GetElement("pictureC1"),
                _env.GetElement("pictureD1"),
                _env.GetElement("pictureE1"),
                _env.GetElement("pictureA2"),
                _env.GetElement("pictureB2"),
                _env.GetElement("pictureC2"),
                _env.GetElement("pictureD2"),
                _env.GetElement("pictureE2"),
                _env.GetElement("exitB"),
            };
            _bgPanel = _bgImage.Select(ii => new WallPanel()).ToArray();

            _stageImageAll = new[]
            {
                new StagePanalGroup(_env, 1),
                new StagePanalGroup(_env, 2),
                new StagePanalGroup(_env, 3),
                new StagePanalGroup(_env, 4),
                new StagePanalGroup(_env, 5),
                new StagePanalGroup(_env, 6),
            };

            var newStageState = ReadStageResults();
            _stageIndexMap = Enumerable.Range(0, newStageState.Length)
                .Select(ii => new { Index = ii, State = newStageState[ii] })
                .Where(ss => ss.State >= 1 || ss.Index == 0)
                .Select(ss => ss.Index)
                .ToArray();

            _stageImage = _stageIndexMap.Select(ii => _stageImageAll[ii]).ToArray();
            _stagePanel = _stageIndexMap.Select(ii => new WallPanel(_stageImageAll[ii].Element.DestX, _stageImageAll[ii].Element.DestY)).ToArray();
            foreach (var ss in _stageImage)
            {
                ss.Element.Visible = true;
            }

            _stageSelectorRange = new SelectorRange(_stageIndexMap.Length, true, false, false);

            if (_stageState == null)
            {
                _stageSelector = new Selector(_stageSelectorRange, _stageIndexMap.Length - 1);
            }
            else
            {
                var initialSelected = 0;
                for (int i = 1; i < _stageIndexMap.Length; ++i)
                {
                    var index = _stageIndexMap[i];
                    if (_stageState[index] != newStageState[index])
                    {
                        initialSelected = i;
                    }
                }
                _stageSelector = new Selector(_stageSelectorRange, initialSelected);
            }
            _stageState = newStageState;


            _blankCharacterImage = _env.CreateResource("data/system/StageSelect/blank.bmp");

            _characterInfo = new[]
            {
                new CharacterInfo(_env, new[] { "homura", "M_homura" }, "homura"),
                new CharacterInfo(_env, "kyouko"),
                new CharacterInfo(_env, "madoka"),
                new CharacterInfo(_env, "mami"),
                new CharacterInfo(_env, "sayaka"),
            };
            ReadCharacterConditions(_characterInfo);
            _totalAvailableChar = _characterInfo.Count(cc => cc.Available);

            _characterIndexMap = new[]
            {
                new[] { 0, 5 },
                new[] { 1 },
                new[] { 2 },
                new[] { 3 },
                new[] { 4 },
            };
            _characterRange = new SelectorRange(_characterInfo.Length, !_allowSameChar, false, false);
            _characterRange.MaxSelection = Enumerable.Repeat(1, _characterInfo.Length).ToArray();
            _characterPanel = new[]
            {
                new CharacterPanel(136, 0),
                new CharacterPanel(336, -5),
                new CharacterPanel(536, -10),
            };
            ReadDefaultSelectedCharIndexArray(out var csel, out var cdef);
            for (int i = 0; i < csel.Length; ++i)
            {
                if (csel[i] == 6) csel[i] = _playerTypeForQB[i];
            }
            var cselCharIndex = csel
                .Select(ss => Array.FindIndex(_characterIndexMap, mm => mm.Contains(ss)))
                .ToArray();
            for (int i = 0; i < cselCharIndex.Length; ++i)
            {
                if (cselCharIndex[i] != -1)
                {
                    var find = Array.FindIndex(_characterIndexMap[cselCharIndex[i]], ii => ii == csel[i]);
                    //find shouldn't be -1
                    _characterInfo[cselCharIndex[i]].PlayerSelectors[i].TryMoveTo(find);
                }
            }
            _playerSelection = new[]
            {
                new PlayerSelection(_characterRange, cdef[0], cselCharIndex[0]),
                new PlayerSelection(_characterRange, cdef[1], cselCharIndex[1]),
                new PlayerSelection(_characterRange, cdef[2], cselCharIndex[2]),
            };
            _playerCursorImage = new[]
            {
                _env.GetResource("c1p"),
                _env.GetResource("c2p"),
                _env.GetResource("c3p"),
            };
            _playerCursorImageG = new[]
            {
                _env.GetResource("c1pG"),
                _env.GetResource("c2pG"),
                _env.GetResource("c3pG"),
            };

            foreach (var cc in _componentList)
            {
                cc.Init(_provider);
            }
        }

        private void ClearOriginalUpdate()
        {
            var vm = SquirrelHelper.SquirrelVM;

            SquirrelFunctions.pushstring(vm, "funcUpdate", -1);
            SquirrelFunctions.pushobject(vm, _emptyFunc.SQObject);
            SquirrelFunctions.newslot(vm, 1, 0);

            SquirrelFunctions.pushstring(vm, "PreUpdate", -1);
            SquirrelFunctions.pushobject(vm, _emptyFunc.SQObject);
            SquirrelFunctions.newslot(vm, 1, 0);
        }

        public void PreUpdate()
        {
        }

        public void PostUpdate()
        {
            Draw();
            HandleInput();
        }

        public void Exit()
        {
        }

        private void Draw()
        {
            _frame++;
            _env.GetElement("hook_shadow").RollZ = (float)Math.Sin(_frame / 8.0f * 3.1415927f / 180f) * 45f;
            
            for (int i = 0; i < _bgPanel.Length; ++i)
            {
                var panel = _bgPanel[i];

                if (!_focusExitItem || i != _bgPanel.Length - 1)
                {
                    panel.OffsetX += panel.VX / 30;
                    panel.OffsetY += panel.VY / 30;
                    if ((_frame % 30) == 0)
                    {
                        panel.VX += -panel.OffsetX * 0.001f;
                        panel.VY += -panel.OffsetY * 0.003f;
                    }
                }

                var element = _bgImage[i];
                element.DestX = element.OriginX + panel.OffsetX;
                element.DestY = element.OriginY + panel.OffsetY;
            }

            for (int i = 0; i < _stagePanel.Length; ++i)
            {
                if (i == _stageSelector.Current)
                {
                    continue;
                }

                var panel = _stagePanel[i];
                var group = _stageImage[i];
                panel.OffsetX += panel.VX / 10;
                panel.OffsetY += panel.VY / 10;
                if ((_frame % 10) != 0) //This might be a bug in original game
                {
                    panel.VX += -panel.OffsetX * 0.0001f;
                    panel.VY += -panel.OffsetY * 0.0003f;
                }

                var destX = panel.Left + panel.OffsetX / 2;
                var destY = panel.Top + panel.OffsetY / 2;
                if (destY > 278)
                {
                    destY = 278;
                }
                group.Element.DestX = destX;
                group.Element.DestY = destY;
            }

            _env.StretchBlt(_env.GetResource("frontwall"), -150, 0, 1100, 600, 0, 0, 800, 443, Blend.Alpha, 0.15f);
            _env.StretchBlt(_env.GetResource("front_grad"), -150, 0, 1100, 600, 0, 0, 1, 600, Blend.Add, 0.5f);

            if (_focusExitItem)
            {
                var img0 = _env.GetResource("exit");
                var exitElement = _bgImage[_bgImage.Length - 1];
                _env.BitBlt(img0, exitElement.DestX, exitElement.DestY, img0.ImageWidth, img0.ImageHeight, 0, 0, Blend.Alpha, 1);
                var img1 = _env.GetResource("cexit");
                _env.BitBlt(img1, exitElement.DestX + 50, exitElement.DestY + 100, img1.ImageWidth, img1.ImageHeight, 0, 0, Blend.Alpha, 1);
            }
            else
            {
                var img0 = _stageImage[_stageSelector.Current].ImageA;
                var panel = _stagePanel[_stageSelector.Current];
                _env.BitBlt(img0, panel.Left + panel.OffsetX / 3, panel.Top + panel.OffsetY / 3, img0.ImageWidth, img0.ImageHeight, 0, 0, Blend.Alpha, 1);
                var img1 = _env.GetResource("cstage");
                _env.BitBlt(img1, panel.Left + panel.OffsetX / 3 + 220, panel.Top + panel.OffsetY / 3 + 140, img1.ImageWidth, img1.ImageHeight, 0, 0, Blend.Alpha, 1);
            }

            {
                var img = _env.GetResource("Stage_select");
                _env.BitBlt(img, 0.5f * (800 - img.ImageWidth), 536, img.ImageWidth, img.ImageHeight, 0, 0, Blend.Alpha, _stageSelectAlpha);
            }
            {
                var img = _env.GetResource("black_dot");
                _env.StretchBlt(img, -150, -10, 1100, 620, 0, 0, 1, 1, Blend.Alpha, _stageFaderAlpha);
            }

            for (int i = 0; i < _characterPanel.Length; ++i)
            {
                var panel = _characterPanel[i];
                var alpha = 1 + panel.OffsetY / 300.0f;
                var charSel = _playerSelection[i].SelectedCharacterIndex;
                {
                    var img = _blankCharacterImage;
                    _env.BitBlt(img, panel.Left, panel.Top + panel.OffsetY, img.ImageWidth, img.ImageHeight, 0, 0, Blend.Alpha, alpha);
                }
                if (charSel == -1)
                {
                }
                else if (!_characterInfo[charSel].Available)
                {
                    var charInfo = _characterInfo[charSel];
                    var img = charInfo.Dead[charInfo.PlayerSelectors[i].Current];
                    _env.BitBlt(img, panel.Left, panel.Top + panel.OffsetY, img.ImageWidth, img.ImageHeight, 0, 0, Blend.Alpha, alpha);
                }
                else
                {
                    var charInfo = _characterInfo[charSel];
                    var tr = _playerSelection[i].TransitionList.Entries;
                    var panelAlpha = alpha * (_playerSelection[i].CurrentComponent == null ? _provider.GetFlashAlpha() : 1.0f);
                    if (tr.Count == 2)
                    {
                        var img = charInfo.Selected[tr[0].Index];
                        _env.BitBlt(img, panel.Left, panel.Top + panel.OffsetY, img.ImageWidth, img.ImageHeight, 0, 0, Blend.Alpha, panelAlpha);
                    }
                    else
                    {
                        for (int j = 0; j < tr.Count - 1; ++j)
                        {
                            var img = charInfo.Selected[tr[j].Index];
                            var x = tr[j].Start;
                            var w = tr[j + 1].Start - tr[j].Start;
                            for (int k = x; k < x + w; ++k)
                            {
                                var realX = _randomLines[k];
                                _env.BitBlt(img, panel.Left + realX, panel.Top + panel.OffsetY, 1, img.ImageHeight, realX, 0, Blend.Alpha, panelAlpha);
                            }
                        }
                    }
                }

                if (_playerSelection[i].Entered)
                {
                    var imgs = _playerSelection[i].Confirmed ? _playerCursorImage[i] : _playerCursorImageG[i];
                    _env.BitBlt(imgs, panel.Left + 56, panel.Top + panel.OffsetY + 360, imgs.ImageWidth, imgs.ImageHeight, 0, 0, Blend.Alpha, alpha);
                }
            }

            foreach (var cc in _componentList)
            {
                cc.Draw(_provider);
            }

            {
                var img = _env.GetResource("Character_select");
                _env.BitBlt(img, 0.5f * (800 - img.ImageWidth), 536, img.ImageWidth, img.ImageHeight, 0, 0, Blend.Alpha, _characterSelectAlpha);
            }
        }

        private void HandleInput()
        {
            switch (_currentUpdateType)
            {
                case UpdateFunction.StageSelect:
                    HandleStageSelect();
                    break;
                case UpdateFunction.CharacterSelectEnter:
                    HandleCharacterEnter();
                    break;
                case UpdateFunction.CharacterSelect:
                    HandleCharacterSelect();
                    break;
                case UpdateFunction.CharacterSelectLeave:
                    HandleCharacterLeave();
                    break;
                case UpdateFunction.StartGame:
                    HandleStartGame();
                    break;
                default:
                    break;
            }
        }

        private void HandleStageSelect()
        {
            if (_stageFaderAlpha > 0)
            {
                _stageFaderAlpha -= 0.025f;
            }
            if (_focusExitItem)
            {
                if (_input.InputAll.Y < 0)
                {
                    _env.PlaySE(0);
                    _focusExitItem = false;
                }
                else if (_input.InputAll.B0 == 1)
                {
                    _env.PlaySE(1);
                    ExitToTitle();
                    _currentUpdateType = UpdateFunction.None;
                }
                return;
            }
            if (_input.InputAll.Y > 0)
            {
                _env.PlaySE(0);
                _focusExitItem = true;
                return;
            }

            if (_stageSelector.Update(_input.InputAll.X))
            {
                _env.PlaySE(0);
            }

            for (int i = 0; i < _playerSelection.Length; ++i)
            {
                _playerSelection[i].Confirmed = false;
            }
            for (int i = 0; i < _playerSelection.Length; ++i)
            {
                if (_input.Input[i].B0 > 0)
                {
                    _env.PlaySE(1);
                    _playerSelection[i].Enter();
                    _currentUpdateType = UpdateFunction.CharacterSelectEnter;

                    for (int j = 0; j < _playerSelection.Length; ++j)
                    {
                        if (_playerSelection[j].Entered)
                        {
                            _playerSelection[j].TransitionList.Reset(_characterInfo[_playerSelection[j].SelectedCharacterIndex].PlayerSelectors[j]);
                        }
                        _playerSelection[j].CurrentComponent = null;
                    }
                    break;
                }
            }

            if (_stageSelectAlpha < 1)
            {
                _stageSelectAlpha += 0.02f;
            }
            if (_characterSelectAlpha > 0)
            {
                _characterSelectAlpha -= 0.02f;
            }
        }

        private void HandleCharacterEnter()
        {
            if (_stageFaderAlpha < 0.5f) _stageFaderAlpha += 0.025f;

            var finished = 0;
            for (int i = 0; i < _characterPanel.Length; ++i)
            {
                var panel = _characterPanel[i];
                if (panel.Progress >= 0 && panel.Progress <= 45)
                {
                    var per = 1 - panel.Progress / 45.0f;
                    panel.OffsetY = (float)Math.Cos(panel.Progress * 2 * 3.1415927 / 180) * -500f * per;
                    if (panel.OffsetY > 0)
                    {
                        panel.OffsetY *= 0.1f;
                    }
                }
                if (++panel.Progress > 45)
                {
                    finished += 1;
                }
            }
            if (finished == _characterPanel.Length)
            {
                _currentUpdateType = UpdateFunction.CharacterSelect;
            }
            if (_stageSelectAlpha > 0) _stageSelectAlpha -= 0.02f;
            if (_characterSelectAlpha < 1) _characterSelectAlpha += 0.02f;
        }

        private void HandleCharacterSelect()
        {
            var enteredCount = 0;
            var confirmedCount = 0;
            for (int i = 0; i < _playerSelection.Length; ++i)
            {
                if (_playerSelection[i].Entered)
                {
                    enteredCount += 1;
                    if (_playerSelection[i].Confirmed)
                    {
                        confirmedCount += 1;
                    }
                }
            }

            foreach (var cc in _componentList)
            {
                cc.UpdateAll(_provider);
            }

            int seId = -1;
            bool selectorMoved = false;
            bool specialMoved = false;
            for (int i = 0; i < _playerSelection.Length; ++i)
            {
                var sel = _playerSelection[i];
                var input = _input.Input[i];
                if (sel.Confirmed)
                {
                    if (input.B1 == 1)
                    {
                        sel.Confirmed = false;
                        seId = 2;
                        confirmedCount -= 1;
                        sel.CurrentComponent = null;
                    }
                }
                else if (sel.Entered)
                {
                    if (sel.CharacterSelector.Update(input.Y))
                    {
                        selectorMoved = true;
                        sel.TransitionList.Reset(_characterInfo[sel.SelectedCharacterIndex].PlayerSelectors[i]);
                    }
                    if (input.B0 == 1)
                    {
                        if (_characterInfo[sel.SelectedCharacterIndex].Available ||
                            _alwaysChooseDeadAsQB ||
                            _noAvailableChooseDeadAsQB && enteredCount > _totalAvailableChar)
                        {
                            sel.Confirmed = true;
                            seId = 1;
                            confirmedCount += 1;
                        }
                        else
                        {
                            seId = 7;
                        }
                    }
                    else if (input.B1 == 1)
                    {
                        sel.Leave();
                        seId = 2;
                        enteredCount -= 1;
                    }
                    else
                    {
                        bool switchComponent = false;
                        if (sel.CurrentComponent == null)
                        {
                            if (_characterInfo[sel.SelectedCharacterIndex].Available)
                            {
                                if (_characterInfo[sel.SelectedCharacterIndex].PlayerSelectors[i].Update(input.X))
                                {
                                    selectorMoved = true;
                                    specialMoved = true;
                                }
                                else if (input.X == 1 || input.X == -1)
                                {
                                    seId = 61;
                                }
                            }
                        }
                        else
                        {
                            switchComponent = !sel.CurrentComponent.IsAvailableForPlayer(_provider, i);
                            if (!switchComponent)
                            {
                                sel.CurrentComponent.UpdatePlayer(_provider, i);
                            }
                        }
                        if (input.B3 == 1 || switchComponent)
                        {
                            var index = _componentList.IndexOf(sel.CurrentComponent) + 1;
                            if (index >= _componentList.Count) index = -1;
                            sel.CurrentComponent = index == -1 ? null : _componentList[index];
                            seId = 0;
                        }
                    }
                }
                else
                {
                    if (input.B0 > 0)
                    {
                        sel.Enter();
                        sel.TransitionList.Reset(_characterInfo[sel.SelectedCharacterIndex].PlayerSelectors[i]);
                        seId = 1;
                        enteredCount += 1;
                        sel.CurrentComponent = null;
                    }
                }
                sel.TransitionList.Update();
            }

            if (enteredCount == confirmedCount)
            {
                if (confirmedCount == 0)
                {
                    seId = 2;
                    _currentUpdateType = UpdateFunction.CharacterSelectLeave;
                }
                else
                {
                    var types = new[] { GetCharacterGameType(0), GetCharacterGameType(1), GetCharacterGameType(2) };
                    _startGamePlayerTypes = types;
                    foreach (var cc in _componentList)
                    {
                        cc.ModifyPlayerType(_provider, types);
                    }

                    var qbCount = (types[0] == 6 ? 1 : 0) + (types[1] == 6 ? 1 : 0) + (types[2] == 6 ? 1 : 0);
                    if (_allowQBOnlyGame || qbCount < confirmedCount)
                    {
                        seId = 52;
                        _currentUpdateType = UpdateFunction.StartGame;

                        WriteSelectedIndexArray(types[0], types[1], types[2], _stageIndexMap[_stageSelector.Current] + 1);

                        var vm = SquirrelHelper.SquirrelVM;
                        SquirrelFunctions.pushobject(vm, _prepStartStageFunction.SQObject);
                        SquirrelFunctions.push(vm, 1);
                        SquirrelFunctions.call(vm, 1, 0, 0);
                        SquirrelFunctions.pop(vm, 1);
                    }
                }
            }

            if (seId != -1)
            {
                _env.PlaySE(seId);
            }
            else if (selectorMoved)
            {
                _env.PlaySE(specialMoved ? 51 : 0);
            }
        }

        private void HandleCharacterLeave()
        {
            if (_stageFaderAlpha > 0) _stageFaderAlpha -= 0.01f;

            int finished = 0;
            for (int i = 0; i < _characterPanel.Length; ++i)
            {
                var panel = _characterPanel[i];
                if (panel.Progress >= 0 && panel.Progress <= 45)
                {
                    var per = 1 - panel.Progress / 45.0f;
                    panel.OffsetY = (float)Math.Cos(panel.Progress * 2 * 3.1415927 / 180) * -500 * per;
                    if (panel.OffsetY > 0)
                    {
                        panel.OffsetY *= 0.1f;
                    }
                }
                if (--panel.Progress < 0)
                {
                    finished += 1;
                }
            }
            if (finished == _characterPanel.Length)
            {
                _currentUpdateType = UpdateFunction.StageSelect;
            }

            if (_stageSelectAlpha < 1) _stageSelectAlpha += 0.02f;
            if (_characterSelectAlpha > 0) _characterSelectAlpha -= 0.02f;
        }

        private void HandleStartGame()
        {
            if (_stageFaderAlpha < 1) _stageFaderAlpha += 0.025f;

            var vm = SquirrelHelper.SquirrelVM;

            SquirrelFunctions.pushobject(vm, _loopStartStageFunction.SQObject);
            SquirrelFunctions.push(vm, 1);
            SquirrelFunctions.call(vm, 1, 1, 0);
            SquirrelFunctions.getbool(vm, -1, out var allFinished);
            if (allFinished != 0)
            {
                foreach (var cc in _componentList)
                {
                    cc.ModifyPlayerActor(_provider, _startGamePlayerTypes);
                }
            }

            SquirrelFunctions.pop(vm, 2);
        }

        private void ExitToTitle()
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushobject(vm, _exitToTitleFunction.SQObject);
            SquirrelFunctions.push(vm, 1);
            SquirrelFunctions.call(vm, 1, 0, 0);
            SquirrelFunctions.pop(vm, 1);
            //Because title will reset selected character index, we don't need to save anything.
        }

        private int GetCharacterGameType(int player)
        {
            var charSel = _playerSelection[player].SelectedCharacterIndex;
            if (charSel == -1) return -1;
            var ret = _characterIndexMap[charSel][_characterInfo[charSel].PlayerSelectors[player].Current];
            if (!_characterInfo[charSel].Available)
            {
                _playerTypeForQB[player] = ret;
                return 6;
            }
            return ret;
        }

        private void ReadDefaultSelectedCharIndexArray(out int[] sel, out int[] def)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushstring(vm, "thisAct", -1);
            SquirrelFunctions.get(vm, 1);

            SquirrelFunctions.pushstring(vm, "SelectCharactor", -1);
            SquirrelFunctions.get(vm, -2);
            sel = ReadIntArray3();

            SquirrelFunctions.pushstring(vm, "DefaultCharactor", -1);
            SquirrelFunctions.get(vm, -2);
            def = ReadIntArray3();

            SquirrelFunctions.pop(vm, 1);
        }

        private int[] ReadIntArray3()
        {
            var ret = new int[3];
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushinteger(vm, 0);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.getinteger(vm, -1, out ret[0]);
            SquirrelFunctions.pop(vm, 1);
            SquirrelFunctions.pushinteger(vm, 1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.getinteger(vm, -1, out ret[1]);
            SquirrelFunctions.pop(vm, 1);
            SquirrelFunctions.pushinteger(vm, 2);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.getinteger(vm, -1, out ret[2]);
            SquirrelFunctions.pop(vm, 2); //also pop the array
            return ret;
        }

        private void WriteSelectedIndexArray(int i0, int i1, int i2, int selectedStage)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushstring(vm, "thisAct", -1);
            SquirrelFunctions.get(vm, 1);

            var entryCount = (i0 != -1 ? 1 : 0) + (i1 != -1 ? 1 : 0) + (i2 != -1 ? 1 : 0);
            SquirrelFunctions.pushstring(vm, "entryCount", -1);
            SquirrelFunctions.pushinteger(vm, entryCount);
            SquirrelFunctions.set(vm, -3);

            SquirrelFunctions.pushstring(vm, "lastStage", -1);
            SquirrelFunctions.pushinteger(vm, selectedStage);
            SquirrelFunctions.set(vm, -3);

            SquirrelFunctions.pushstring(vm, "SelectCharactor", -1);
            SquirrelFunctions.get(vm, -2);

            SquirrelFunctions.pushinteger(vm, 0);
            SquirrelFunctions.pushinteger(vm, i0);
            SquirrelFunctions.set(vm, -3);

            SquirrelFunctions.pushinteger(vm, 1);
            SquirrelFunctions.pushinteger(vm, i1);
            SquirrelFunctions.set(vm, -3);

            SquirrelFunctions.pushinteger(vm, 2);
            SquirrelFunctions.pushinteger(vm, i2);
            SquirrelFunctions.set(vm, -3);

            SquirrelFunctions.pop(vm, 2);
        }

        private int[] ReadStageResults()
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushroottable(vm);
            SquirrelFunctions.pushstring(vm, "gameData", -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.remove(vm, -2);

            SquirrelFunctions.pushstring(vm, "stageResult", -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.remove(vm, -2);

            var len = SquirrelFunctions.getsize(vm, -1);
            var ret = new int[len];
            for (int i = 0; i < len; ++i)
            {
                SquirrelFunctions.pushinteger(vm, i);
                SquirrelFunctions.get(vm, -2); //array result
                SquirrelFunctions.pushstring(vm, "state", -1);
                SquirrelFunctions.get(vm, -2); //array result state
                SquirrelFunctions.getinteger(vm, -1, out ret[i]);
                SquirrelFunctions.pop(vm, 2); //array
            }

            SquirrelFunctions.pop(vm, 1);
            return ret;
        }

        private void ReadCharacterConditions(CharacterInfo[] infoList)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushroottable(vm);
            SquirrelFunctions.pushstring(vm, "playerData", -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.remove(vm, -2);

            for (int i = 0; i < infoList.Length; ++i)
            {
                SquirrelFunctions.pushstring(vm, infoList[i].PlayerDataName, -1);
                SquirrelFunctions.get(vm, -2);
                SquirrelFunctions.pushstring(vm, "condition", -1);
                SquirrelFunctions.get(vm, -2);
                SquirrelFunctions.getinteger(vm, -1, out var r);
                SquirrelFunctions.pop(vm, 2);
                infoList[i].Available = r == 0;
            }

            SquirrelFunctions.pop(vm, 1);
        }

        private static int[] CreateRandomLines()
        {
            return Enumerable.Range(0, TransitionList.Max)
                .Select(ii => new { Index = ii, Rand = _random.NextDouble() })
                .OrderBy(gg => gg.Rand)
                .Select(gg => gg.Index)
                .ToArray();
        }
    }
}
