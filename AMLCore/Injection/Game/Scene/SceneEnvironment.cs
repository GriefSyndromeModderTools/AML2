using AMLCore.Injection.Engine.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Game.Scene
{
    //TODO should print log when resource create/get fails

    public enum Blend
    {
        Normal = 0,
        Alpha = 1,
        Add = 2,
        Sub = 3,
        Multi = 4,
        Invert = 5,
    }

    public class SceneElement
    {
        private ReferencedScriptObject _obj;

        internal SceneElement(ReferencedScriptObject obj)
        {
            _obj = obj;
        }

        public void Release()
        {
            _obj.ReleaseRef();
        }

        private float ReadFloat(string name)
        {
            return SquirrelHelper.PushMemberChainObj(_obj.SQObject, name).PopFloat();
        }

        public void WriteFloat(string name, float val)
        {
            using (SquirrelHelper.PushMemberChainObj(_obj.SQObject))
            {
                SquirrelHelper.Set(name, val);
            }
        }

        private bool ReadBool(string name)
        {
            return SquirrelHelper.PushMemberChainObj(_obj.SQObject, name).PopBool();
        }

        public void WriteBool(string name, bool val)
        {
            using (SquirrelHelper.PushMemberChainObj(_obj.SQObject))
            {
                SquirrelHelper.Set(name, val);
            }
        }

        public float RollZ
        {
            get
            {
                return SquirrelHelper.PushMemberChainObj(_obj.SQObject, "layout", "roll_z").PopFloat();
            }
            set
            {
                using (SquirrelHelper.PushMemberChainObj(_obj.SQObject, "layout"))
                {
                    SquirrelHelper.Set("roll_z", value);
                }
            }
        }

        public float DestX
        {
            get => ReadFloat("dst_x");
            set => WriteFloat("dst_x", value);
        }

        public float DestY
        {
            get => ReadFloat("dst_y");
            set => WriteFloat("dst_y", value);
        }

        public float OriginX
        {
            get => ReadFloat("ox");
            set => WriteFloat("ox", value);
        }

        public float OriginY
        {
            get => ReadFloat("oy");
            set => WriteFloat("oy", value);
        }

        public bool Visible
        {
            get => ReadBool("visible");
            set => WriteBool("visible", value);
        }
    }

    public class Resource
    {
        internal readonly ReferencedScriptObject _obj;

        internal Resource(ReferencedScriptObject obj)
        {
            _obj = obj;
        }

        public void Release()
        {
            _obj.ReleaseRef();
        }

        public float ImageWidth => SquirrelHelper.PushMemberChainObj(_obj.SQObject, "image_width").PopFloat();
        public float ImageHeight => SquirrelHelper.PushMemberChainObj(_obj.SQObject, "image_height").PopFloat();
    }

    public class SceneEnvironment
    {
        //TODO Resources can be shared across Act. We may need to keep the created resource cache.
        private readonly Dictionary<string, SceneElement> _cachedElement = new Dictionary<string, SceneElement>();
        private readonly Dictionary<string, Resource> _cachedResource = new Dictionary<string, Resource>();
        private readonly Dictionary<string, Resource> _createdResource = new Dictionary<string, Resource>();

        private ReferencedScriptObject _plObject;
        private ReferencedScriptObject _setBlend;
        private ReferencedScriptObject _stretchBlt;
        private ReferencedScriptObject _bitBlt;
        private ReferencedScriptObject _playSE;
        private ReferencedScriptObject _createResource2D;

        public SceneEnvironment()
        {
            using (SquirrelHelper.PushMemberChainThis())
            {
                _plObject = SquirrelHelper.PushMemberChainStack(-1, "pl").PopRefObject();
                using (SquirrelHelper.PushMemberChainObj(_plObject.SQObject))
                {
                    _setBlend = SquirrelHelper.PushMemberChainStack(-1, "SetBlend").PopRefObject();
                    _stretchBlt = SquirrelHelper.PushMemberChainStack(-1, "StretchBlt").PopRefObject();
                    _bitBlt = SquirrelHelper.PushMemberChainStack(-1, "BitBlt").PopRefObject();
                    _createResource2D = SquirrelHelper.PushMemberChainStack(-1, "CreateResource2D").PopRefObject();
                }
            }
            _playSE = SquirrelHelper.PushMemberChainRoot("PlaySE").PopRefObject();
        }

        public void DisposeResources()
        {
            DisposeCachedElements();
            DisposeResource();

            _plObject.ReleaseRef();
            _setBlend.ReleaseRef();
            _stretchBlt.ReleaseRef();
            _bitBlt.ReleaseRef();
            _createResource2D.ReleaseRef();
            _playSE.ReleaseRef();
        }

        public SceneElement GetElement(string name)
        {
            if (!_cachedElement.TryGetValue(name, out var ret))
            {
                var obj = SquirrelHelper.PushMemberChainThis(name).PopRefObject();

                ret = new SceneElement(obj);
                _cachedElement.Add(name, ret);
            }
            return ret;
        }

        public Resource GetResource(string name)
        {
            if (!_cachedResource.TryGetValue(name, out var ret))
            {
                var obj = SquirrelHelper.PushMemberChainThis("resource", name).PopRefObject();

                ret = new Resource(obj);
                _cachedResource.Add(name, ret);
            }
            return ret;
        }

        private void DisposeCachedElements()
        {
            foreach (var e in _cachedElement)
            {
                e.Value.Release();
            }
            _cachedElement.Clear();
        }

        private void DisposeResource()
        {
            foreach (var e in _cachedResource)
            {
                e.Value.Release();
            }
            _cachedResource.Clear();
            foreach (var e in _createdResource)
            {
                e.Value.Release();
            }
            _createdResource.Clear();
        }

        public void StretchBlt(Resource res, float destX, float destY, float destW, float destH,
            float srcX, float srcY, float srcW, float srcH, Blend blend, float alpha)
        {
            using (SquirrelHelper.PushMemberChainObj(_setBlend.SQObject))
            {
                SquirrelHelper.CallEmpty(_plObject.SQObject, (int)blend, alpha);
            }
            using (SquirrelHelper.PushMemberChainObj(_stretchBlt.SQObject))
            {
                SquirrelHelper.CallEmpty(_plObject.SQObject, destX, destY, destW, destH,
                    res._obj.SQObject, srcX, srcY, srcW, srcH);
            }
        }

        public void BitBlt(Resource res, float destX, float destY, float destW, float destH,
            float srcX, float srcY, Blend blend, float alpha)
        {
            using (SquirrelHelper.PushMemberChainObj(_bitBlt.SQObject))
            {
                SquirrelHelper.CallEmpty(_plObject.SQObject, destX, destY, destW, destH,
                    res._obj.SQObject, srcX, srcY, (int)blend, alpha);
            }
        }

        public void PlaySE(int id)
        {
            using (SquirrelHelper.PushMemberChainObj(_playSE.SQObject))
            {
                SquirrelHelper.CallEmpty(ManagedSQObject.Root, id);
            }
        }

        public Resource CreateResource(string path)
        {
            if (!_createdResource.TryGetValue(path, out var ret))
            {
                using (SquirrelHelper.PushMemberChainObj(_createResource2D.SQObject))
                {
                    var obj = SquirrelHelper.CallPush(_plObject.SQObject, path).PopRefObject();
                    ret = new Resource(obj);
                    _createdResource.Add(path, ret);
                }
            }
            return ret;
        }

        public bool CompareActInstance()
        {
            //We don't have sq_cmp. Have to get and compare.
            SquirrelFunctions.getstackobj(SquirrelHelper.SquirrelVM, 1, out var obj);
            if (obj.Value.Pointer == _plObject.SQObject.Value.Pointer)
            {
                return true;
            }
            return false;
        }

        public void DrawNumber(string font, int x, int y, int num, int digits, float alpha)
        {
            using (SquirrelHelper.PushMemberChainThis("DrawNumber"))
            {
                SquirrelHelper.CallEmpty(ManagedSQObject.Parameter(1), font, x, y, num, digits, alpha);
            }
        }
    }
}
