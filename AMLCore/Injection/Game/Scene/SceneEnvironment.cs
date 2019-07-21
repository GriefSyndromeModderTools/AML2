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
        private IntPtr _pointer;

        internal SceneElement(IntPtr ptr)
        {
            _pointer = ptr;
        }

        public void Release()
        {
            SquirrelFunctions.release(SquirrelHelper.SquirrelVM, _pointer);
            Marshal.FreeHGlobal(_pointer);
            _pointer = IntPtr.Zero;
        }

        private void PushLayout()
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushobject(vm, _pointer);
            SquirrelFunctions.pushstring(vm, "layout", -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.remove(vm, -2);
        }

        private float ReadFloat(string name)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushobject(vm, _pointer);
            SquirrelFunctions.pushstring(vm, name, -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.getfloat(vm, -1, out var ret);
            SquirrelFunctions.pop(vm, 2);
            return ret;
        }

        public void WriteFloat(string name, float val)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushobject(vm, _pointer);
            SquirrelFunctions.pushstring(vm, name, -1);
            SquirrelFunctions.pushfloat(vm, val);
            SquirrelFunctions.set(vm, -3);
            SquirrelFunctions.pop(vm, 1);
        }

        private bool ReadBool(string name)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushobject(vm, _pointer);
            SquirrelFunctions.pushstring(vm, name, -1);
            SquirrelFunctions.get(vm, -2);
            SquirrelFunctions.getbool(vm, -1, out var ret);
            SquirrelFunctions.pop(vm, 2);
            return ret != 0;
        }

        public void WriteBool(string name, bool val)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushobject(vm, _pointer);
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
        private IntPtr _pointer;
        internal IntPtr Pointer => _pointer;

        internal Resource(IntPtr ptr)
        {
            _pointer = ptr;
        }

        public void Release()
        {
            SquirrelFunctions.release(SquirrelHelper.SquirrelVM, _pointer);
            Marshal.FreeHGlobal(_pointer);
            _pointer = IntPtr.Zero;
        }

        private float ReadFloat(string name)
        {
            var vm = SquirrelHelper.SquirrelVM;
            SquirrelFunctions.pushobject(vm, _pointer);
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

        private IntPtr _plObject;
        private IntPtr _setBlend;
        private IntPtr _stretchBlt;
        private IntPtr _bitBlt;
        private IntPtr _playSE;
        private IntPtr _createResource2D;

        public SceneEnvironment()
        {
            var vm = SquirrelHelper.SquirrelVM;

            SquirrelFunctions.pushstring(vm, "pl", -1);
            SquirrelFunctions.get(vm, 1);

            _plObject = Marshal.AllocHGlobal(8);
            Marshal.WriteInt32(_plObject, 0x01000001);
            Marshal.WriteInt32(_plObject, 4, 0);
            SquirrelFunctions.getstackobj_(vm, -1, _plObject);
            SquirrelFunctions.addref(vm, _plObject);

            SquirrelFunctions.pushstring(vm, "SetBlend", -1);
            SquirrelFunctions.get(vm, -2);
            _setBlend = Marshal.AllocHGlobal(8);
            Marshal.WriteInt32(_setBlend, 0x01000001);
            Marshal.WriteInt32(_setBlend, 4, 0);
            SquirrelFunctions.getstackobj_(vm, -1, _setBlend);
            SquirrelFunctions.addref(vm, _setBlend);
            SquirrelFunctions.pop(vm, 1);

            SquirrelFunctions.pushstring(vm, "StretchBlt", -1);
            SquirrelFunctions.get(vm, -2);
            _stretchBlt = Marshal.AllocHGlobal(8);
            Marshal.WriteInt32(_stretchBlt, 0x01000001);
            Marshal.WriteInt32(_stretchBlt, 4, 0);
            SquirrelFunctions.getstackobj_(vm, -1, _stretchBlt);
            SquirrelFunctions.addref(vm, _stretchBlt);
            SquirrelFunctions.pop(vm, 1);

            SquirrelFunctions.pushstring(vm, "BitBlt", -1);
            SquirrelFunctions.get(vm, -2);
            _bitBlt = Marshal.AllocHGlobal(8);
            Marshal.WriteInt32(_bitBlt, 0x01000001);
            Marshal.WriteInt32(_bitBlt, 4, 0);
            SquirrelFunctions.getstackobj_(vm, -1, _bitBlt);
            SquirrelFunctions.addref(vm, _bitBlt);
            SquirrelFunctions.pop(vm, 1);

            SquirrelFunctions.pushstring(vm, "CreateResource2D", -1);
            SquirrelFunctions.get(vm, -2);
            _createResource2D = Marshal.AllocHGlobal(8);
            Marshal.WriteInt32(_createResource2D, 0x01000001);
            Marshal.WriteInt32(_createResource2D, 4, 0);
            SquirrelFunctions.getstackobj_(vm, -1, _createResource2D);
            SquirrelFunctions.addref(vm, _createResource2D);
            SquirrelFunctions.pop(vm, 1);

            SquirrelFunctions.pop(vm, 1); //pop pl

            SquirrelFunctions.pushroottable(vm);
            SquirrelFunctions.pushstring(vm, "PlaySE", -1);
            SquirrelFunctions.get(vm, -2);
            _playSE = Marshal.AllocHGlobal(8);
            Marshal.WriteInt32(_playSE, 0x01000001);
            Marshal.WriteInt32(_playSE, 4, 0);
            SquirrelFunctions.getstackobj_(vm, -1, _playSE);
            SquirrelFunctions.addref(vm, _playSE);
            SquirrelFunctions.pop(vm, 2);
        }

        public void DisposeResources()
        {
            DisposeCachedElements();
            DisposeResource();

            SquirrelFunctions.release(SquirrelHelper.SquirrelVM, _plObject);
            Marshal.FreeHGlobal(_plObject);
            _plObject = IntPtr.Zero;

            SquirrelFunctions.release(SquirrelHelper.SquirrelVM, _setBlend);
            Marshal.FreeHGlobal(_setBlend);
            _setBlend = IntPtr.Zero;

            SquirrelFunctions.release(SquirrelHelper.SquirrelVM, _stretchBlt);
            Marshal.FreeHGlobal(_stretchBlt);
            _stretchBlt = IntPtr.Zero;

            SquirrelFunctions.release(SquirrelHelper.SquirrelVM, _bitBlt);
            Marshal.FreeHGlobal(_bitBlt);
            _bitBlt = IntPtr.Zero;

            SquirrelFunctions.release(SquirrelHelper.SquirrelVM, _playSE);
            Marshal.FreeHGlobal(_playSE);
            _playSE = IntPtr.Zero;

            SquirrelFunctions.release(SquirrelHelper.SquirrelVM, _createResource2D);
            Marshal.FreeHGlobal(_createResource2D);
            _createResource2D = IntPtr.Zero;
        }

        public SceneElement GetElement(string name)
        {
            if (!_cachedElement.TryGetValue(name, out var ret))
            {
                var vm = SquirrelHelper.SquirrelVM;
                SquirrelFunctions.pushstring(vm, name, -1);
                SquirrelFunctions.get(vm, 1);

                var ptr = Marshal.AllocHGlobal(8);
                Marshal.WriteInt32(ptr, 0x01000001);
                Marshal.WriteInt32(ptr, 4, 0);

                SquirrelFunctions.getstackobj_(vm, -1, ptr);
                SquirrelFunctions.addref(vm, ptr);
                SquirrelFunctions.pop(vm, 1);

                ret = new SceneElement(ptr);
                _cachedElement.Add(name, ret);
            }
            return ret;
        }

        public Resource GetResource(string name)
        {
            if (!_cachedResource.TryGetValue(name, out var ret))
            {
                var vm = SquirrelHelper.SquirrelVM;

                SquirrelFunctions.pushstring(vm, "resource", -1);
                SquirrelFunctions.get(vm, 1);

                SquirrelFunctions.pushstring(vm, name, -1);
                SquirrelFunctions.get(vm, -2);

                var ptr = Marshal.AllocHGlobal(8);
                Marshal.WriteInt32(ptr, 0x01000001);
                Marshal.WriteInt32(ptr, 4, 0);

                SquirrelFunctions.getstackobj_(vm, -1, ptr);
                SquirrelFunctions.addref(vm, ptr);
                SquirrelFunctions.pop(vm, 2);

                ret = new Resource(ptr);
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

            SquirrelFunctions.pushobject(vm, _setBlend);
            SquirrelFunctions.pushobject(vm, _plObject);
            SquirrelFunctions.pushinteger(vm, (int)blend);
            SquirrelFunctions.pushfloat(vm, alpha);
            SquirrelFunctions.call(vm, 3, 0, 0);
            SquirrelFunctions.pop(vm, 1);

            SquirrelFunctions.pushobject(vm, _stretchBlt);
            SquirrelFunctions.pushobject(vm, _plObject);
            SquirrelFunctions.pushfloat(vm, destX);
            SquirrelFunctions.pushfloat(vm, destY);
            SquirrelFunctions.pushfloat(vm, destW);
            SquirrelFunctions.pushfloat(vm, destH);
            SquirrelFunctions.pushobject(vm, res.Pointer);
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

            SquirrelFunctions.pushobject(vm, _bitBlt);
            SquirrelFunctions.pushobject(vm, _plObject);
            SquirrelFunctions.pushfloat(vm, destX);
            SquirrelFunctions.pushfloat(vm, destY);
            SquirrelFunctions.pushfloat(vm, destW);
            SquirrelFunctions.pushfloat(vm, destH);
            SquirrelFunctions.pushobject(vm, res.Pointer);
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
            SquirrelFunctions.pushobject(vm, _playSE);
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

                SquirrelFunctions.pushobject(vm, _createResource2D);
                SquirrelFunctions.pushobject(vm, _plObject);
                SquirrelFunctions.pushstring(vm, path, -1);
                SquirrelFunctions.call(vm, 2, 1, 0);

                var ptr = Marshal.AllocHGlobal(8);
                Marshal.WriteInt32(ptr, 0x01000001);
                Marshal.WriteInt32(ptr, 4, 0);

                SquirrelFunctions.getstackobj_(vm, -1, ptr);
                SquirrelFunctions.addref(vm, ptr);
                SquirrelFunctions.pop(vm, 2);

                ret = new Resource(ptr);
                _createdResource.Add(path, ret);
            }
            return ret;
        }

        public bool CompareActInstance()
        {
            var vm = SquirrelHelper.SquirrelVM;
            //We don't have sq_cmp. Have to get and compare.
            SquirrelFunctions.getstackobj(vm, 1, out var obj);
            if (obj.Value.Pointer == Marshal.ReadIntPtr(_plObject, 4))
            {
                return true;
            }
            return false;
        }
    }
}
