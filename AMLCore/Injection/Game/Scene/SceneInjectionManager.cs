using AMLCore.Injection.Game.Scene.StageSelect;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Game.Scene
{
    public enum SystemScene
    {
        Title,
        StageSelect,
        StagePause,
        StageMain,
    }

    public interface ISceneEventHandler
    {
        void PostInit(SceneEnvironment env);
        void PreUpdate();
        void PostUpdate();
        void Exit();
    }

    public static class SceneInjectionManager
    {
        internal static readonly Dictionary<SystemScene, string> SystemSceneNames = new Dictionary<SystemScene, string>
        {
            { SystemScene.Title, "Title" },
            { SystemScene.StageSelect, "StageSelect" },
            { SystemScene.StagePause, "StagePause" },
            { SystemScene.StageMain, "StageMain" },
        };

        private static Dictionary<SystemScene, SceneEnvironment> _runningScene = new Dictionary<SystemScene, SceneEnvironment>();
        private static Dictionary<SystemScene, List<ISceneEventHandler>> _handlerLists = new Dictionary<SystemScene, List<ISceneEventHandler>>
        {
            { SystemScene.Title, new List<ISceneEventHandler>() },
            { SystemScene.StageSelect, new List<ISceneEventHandler>() },
            { SystemScene.StagePause, new List<ISceneEventHandler>() },
            { SystemScene.StageMain, new List<ISceneEventHandler>() },
        };

        internal static void PostInitCallback(SystemScene scene)
        {
            if (!_runningScene.ContainsKey(scene))
            {
                _runningScene[scene] = new SceneEnvironment();
            }
            var env = _runningScene[scene];
            foreach (var h in _handlerLists[scene])
            {
                h.PostInit(env);
            }
        }

        internal static void PreUpdateCallback(SystemScene scene)
        {
            if (_runningScene.ContainsKey(scene))
            {
                foreach (var h in _handlerLists[scene])
                {
                    h.PreUpdate();
                }
            }
        }

        internal static void PostUpdateCallback(SystemScene scene)
        {
            if (_runningScene.ContainsKey(scene))
            {
                foreach (var h in _handlerLists[scene])
                {
                    h.PostUpdate();
                }
            }
        }

        internal static void EndStageCallback()
        {
            foreach (var e in _runningScene)
            {
                if (e.Value.CompareActInstance())
                {
                    foreach (var h in _handlerLists[e.Key])
                    {
                        h.Exit();
                    }
                    e.Value.DisposeResources();
                    _runningScene.Remove(e.Key);
                    break;
                }
            }
        }

        public static void RegisterSceneHandler(SystemScene scene, ISceneEventHandler handler)
        {
            _handlerLists[scene].Add(handler);
        }
    }
}
