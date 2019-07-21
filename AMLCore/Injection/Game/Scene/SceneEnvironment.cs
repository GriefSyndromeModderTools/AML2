using AMLCore.Injection.Engine.Script;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Game.Scene
{
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

        private void PushLayout()
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushobject(vm, _obj.SQObject);
            SquirrelFunctions.pushstring(vm, "layout", -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.remove(vm, -2);
        }

        private float ReadFloat(string name)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushobject(vm, _obj.SQObject);
            SquirrelFunctions.pushstring(vm, name, -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.getfloat(vm, -1, out var ret);
            SquirrelFunctions.pop(vm, 2);
            return ret;
        }

        public void WriteFloat(string name, float val)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushobject(vm, _obj.SQObject);
            SquirrelFunctions.pushstring(vm, name, -1);
            SquirrelFunctions.pushfloat(vm, val);
            SquirrelFunctions.set(vm, -3);
            SquirrelFunctions.pop(vm, 1);
        }

        private bool ReadBool(string name)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushobject(vm, _obj.SQObject);
            SquirrelFunctions.pushstring(vm, name, -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.getbool(vm, -1, out var ret);
            SquirrelFunctions.pop(vm, 2);
            return ret != 0;
        }

        public void WriteBool(string name, bool val)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushobject(vm, _obj.SQObject);
            SquirrelFunctions.pushstring(vm, name, -1);
            SquirrelFunctions.pushbool(vm, val ? 1 : 0);
            SquirrelFunctions.set(vm, -3);
            SquirrelFunctions.pop(vm, 1);
        }

        public float RollZ
        {
            get
            {
                var vm = SquirrelHelper.SquirrelVM;
                PushLayout();
                SquirrelFunctions.pushstring(vm, "roll_z", -1);
                SquirrelFunctions.get(vm, -2);
                SquirrelFunctions.getfloat(vm, -1, out var ret);
                SquirrelFunctions.pop(vm, 2);
                return ret;
            }
            set
            {
                var vm = SquirrelHelper.SquirrelVM;
                PushLayout();
                SquirrelFunctions.pushstring(vm, "roll_z", -1);
                SquirrelFunctions.pushfloat(vm, value);
                SquirrelFunctions.set(vm, -3);
                SquirrelFunctions.pop(vm, 1);
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

        private float ReadFloat(string name)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushobject(vm, _obj.SQObject);
            SquirrelFunctions.pushstring(vm, name, -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.getfloat(vm, -1, out var ret);
            SquirrelFunctions.pop(vm, 2);
            return ret;
        }

        public float ImageWidth => ReadFloat("image_width");
        public float ImageHeight => ReadFloat("image_height");
    }

    public class SceneEnvironment
    {
        //TODO Resources can be shared across Act. We may need to keep the created resource cache.
        private readonly Dictionary<string, SceneElement> _cachedElement = new Dictionary<string, SceneElement>();
        private readonly Dictionary<string, Resource> _cachedResource = new Dictionary<string, Resource>();
        private readonly Dictionary<string, Resource> _createdResource = new Dictionary<string, Resource>();

        private ReferencedScriptObject _plObject = new ReferencedScriptObject();
        private ReferencedScriptObject _setBlend = new ReferencedScriptObject();
        private ReferencedScriptObject _stretchBlt = new ReferencedScriptObject();
        private ReferencedScriptObject _bitBlt = new ReferencedScriptObject();
        private ReferencedScriptObject _playSE = new ReferencedScriptObject();
        private ReferencedScriptObject _createResource2D = new ReferencedScriptObject();

        public SceneEnvironment()
        {
            var vm = SquirrelHelper.SquirrelVM;

            SquirrelHelper.GetMemberChainThis("pl");
            _plObject.GetFromStack(-1);

            SquirrelHelper.GetMemberChainStack(-1, "SetBlend");
            _setBlend.PopFromStack();
            SquirrelHelper.GetMemberChainStack(-1, "StretchBlt");
            _stretchBlt.PopFromStack();
            SquirrelHelper.GetMemberChainStack(-1, "BitBlt");
            _bitBlt.PopFromStack();
            SquirrelHelper.GetMemberChainStack(-1, "CreateResource2D");
            _createResource2D.PopFromStack();

            SquirrelFunctions.pop(vm, 1); //pop pl

            SquirrelHelper.GetMemberChainRoot("PlaySE");
            _playSE.PopFromStack();
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
                var vm = SquirrelHelper.SquirrelVM;

                SquirrelHelper.GetMemberChainThis(name);
                var obj = new ReferencedScriptObject();
                obj.PopFromStack();

                ret = new SceneElement(obj);
                _cachedElement.Add(name, ret);
            }
            return ret;
        }

        public Resource GetResource(string name)
        {
            if (!_cachedResource.TryGetValue(name, out var ret))
            {
                var vm = SquirrelHelper.SquirrelVM;

                SquirrelHelper.GetMemberChainThis("resource", name);
                var obj = new ReferencedScriptObject();
                obj.PopFromStack();

                ret = new Resource(obj);
                _cachedResource.Add(name, ret);
            }
            return ret;
        }

        private void DisposeCachedElements()
        {
            var vm = SquirrelHelper.SquirrelVM;
            foreach (var e in _cachedElement)
            {
                e.Value.Release();
            }
            _cachedElement.Clear();
        }

        private void DisposeResource()
        {
            var vm = SquirrelHelper.SquirrelVM;
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
            var vm = SquirrelHelper.SquirrelVM;

            SquirrelFunctions.pushobject(vm, _setBlend.SQObject);
            SquirrelFunctions.pushobject(vm, _plObject.SQObject);
            SquirrelFunctions.pushinteger(vm, (int)blend);
            SquirrelFunctions.pushfloat(vm, alpha);
            SquirrelFunctions.call(vm, 3, 0, 0);
            SquirrelFunctions.pop(vm, 1);

            SquirrelFunctions.pushobject(vm, _stretchBlt.SQObject);
            SquirrelFunctions.pushobject(vm, _plObject.SQObject);
            SquirrelFunctions.pushfloat(vm, destX);
            SquirrelFunctions.pushfloat(vm, destY);
            SquirrelFunctions.pushfloat(vm, destW);
            SquirrelFunctions.pushfloat(vm, destH);
            SquirrelFunctions.pushobject(vm, res._obj.SQObject);
            SquirrelFunctions.pushfloat(vm, srcX);
            SquirrelFunctions.pushfloat(vm, srcY);
            SquirrelFunctions.pushfloat(vm, srcW);
            SquirrelFunctions.pushfloat(vm, srcH);
            SquirrelFunctions.call(vm, 10, 0, 0);
            SquirrelFunctions.pop(vm, 1);
        }

        public void BitBlt(Resource res, float destX, float destY, float destW, float destH,
            float srcX, float srcY, Blend blend, float alpha)
        {
            var vm = SquirrelHelper.SquirrelVM;

            SquirrelFunctions.pushobject(vm, _bitBlt.SQObject);
            SquirrelFunctions.pushobject(vm, _plObject.SQObject);
            SquirrelFunctions.pushfloat(vm, destX);
            SquirrelFunctions.pushfloat(vm, destY);
            SquirrelFunctions.pushfloat(vm, destW);
            SquirrelFunctions.pushfloat(vm, destH);
            SquirrelFunctions.pushobject(vm, res._obj.SQObject);
            SquirrelFunctions.pushfloat(vm, srcX);
            SquirrelFunctions.pushfloat(vm, srcY);
            SquirrelFunctions.pushinteger(vm, (int)blend);
            SquirrelFunctions.pushfloat(vm, alpha);
            SquirrelFunctions.call(vm, 10, 0, 0);
            SquirrelFunctions.pop(vm, 1);
        }

        public void PlaySE(int id)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushobject(vm, _playSE.SQObject);
            SquirrelFunctions.push(vm, 1);
            SquirrelFunctions.pushinteger(vm, id);
            SquirrelFunctions.call(vm, 2, 0, 0);
            SquirrelFunctions.pop(vm, 1);
        }

        public Resource CreateResource(string path)
        {
            if (!_createdResource.TryGetValue(path, out var ret))
            {
                var vm = SquirrelHelper.SquirrelVM;

                SquirrelFunctions.pushobject(vm, _createResource2D.SQObject);
                SquirrelFunctions.pushobject(vm, _plObject.SQObject);
                SquirrelFunctions.pushstring(vm, path, -1);
                SquirrelFunctions.call(vm, 2, 1, 0);

                var obj = new ReferencedScriptObject();
                obj.PopFromStack();
                SquirrelFunctions.pop(vm, 1);

                ret = new Resource(obj);
                _createdResource.Add(path, ret);
            }
            return ret;
        }

        public bool CompareActInstance()
        {
            var vm = SquirrelHelper.SquirrelVM;
            //We don't have sq_cmp. Have to get and compare.
            SquirrelFunctions.getstackobj(vm, 1, out var obj);
            if (obj.Value.Pointer == _plObject.SQObject.Value.Pointer)
            {
                return true;
            }
            return false;
        }
    }
}
