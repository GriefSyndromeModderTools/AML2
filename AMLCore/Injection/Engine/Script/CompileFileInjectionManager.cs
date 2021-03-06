﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace AMLCore.Injection.Engine.Script
{
    internal class CompileFileInjectionManager
    {
        private static Dictionary<string, Dictionary<string, int>> _FunctionDict =
            new Dictionary<string, Dictionary<string, int>>();
        private static List<InjectedScriptFunction> _FunctionList = new List<InjectedScriptFunction>();
        private static Dictionary<string, int> _FunctionMainDict = new Dictionary<string, int>();

        //called by plugin, before calling the compiled script
        public static void BeforeCompileFile(string file)
        {
            var vm = SquirrelInjectEntry.SquirrelVM;
            int index;
            if (_FunctionMainDict.TryGetValue(file, out index))
            {
                ReplaceFunction(vm, index);
            }
        }

        //called by plugin, after calling the compiled script
        public static void AfterCompileFile(string file, ref SQObject table)
        {
            var vm = SquirrelInjectEntry.SquirrelVM;
            Dictionary<string, int> script;
            if (_FunctionDict.TryGetValue(file, out script))
            {
                SquirrelFunctions.pushobject_(vm, table.Type, table.Value);
                ProcessTable(vm, script);
                SquirrelFunctions.pop(vm, 1);
            }
        }

        //called by plugin, before the sq_call call during CreateAct
        //need to replace this.CompileFile with ::CompileFile
        public static void CreateActBeforeRun()
        {
            var vm = SquirrelInjectEntry.SquirrelVM;
            SquirrelFunctions.push(vm, -1);
            SquirrelFunctions.pushstring(vm, "CompileFile", -1);

            SquirrelFunctions.pushroottable(vm);
            SquirrelFunctions.pushstring(vm, "CompileFile", -1);
            SquirrelFunctions.rawget(vm, -2);
            SquirrelFunctions.remove(vm, -2); //remove root table

            SquirrelFunctions.newslot(vm, -3, 0);
            SquirrelFunctions.pop(vm, 1);
        }

        private static void ProcessTable(IntPtr vm, Dictionary<string, int> functions)
        {
            foreach (var entry in functions)
            {
                if (CheckKey(vm, entry.Key))
                {
                    ProcessFunction(vm, entry.Key, entry.Value);
                }
            }
        }

        private static bool CheckKey(IntPtr vm, string key)
        {
            SquirrelFunctions.pushstring(vm, key, -1);
            return SquirrelFunctions.rawget(vm, -2) == 0;
            //TODO should check if the value is a function, or ProcessFunction may fail
        }

        private static void ProcessFunction(IntPtr vm, string key, int index)
        {
            //table func

            ReplaceFunction(vm, index);//table func_new

            //save the function into table
            SquirrelFunctions.pushstring(vm, key, -1);//table func_new key
            SquirrelFunctions.push(vm, -2);//table func_new key func_new
            SquirrelFunctions.newslot(vm, -4, 0);//table func_new
            SquirrelFunctions.pop(vm, 1);//table
        }

        private static void ReplaceFunction(IntPtr vm, int index)
        {
            SquirrelFunctions.pushinteger(vm, index);
            SquirrelFunctions.newclosure(vm, Marshal.GetFunctionPointerForDelegate(_InjectedEntrance), 2);
        }

        //save the entrance
        private static SquirrelFuncDelegate _InjectedEntrance = SquirrelHelper.Wrap(InjectedEntrance);

        public static InjectedScriptFunction InjectCompileFile(string script, string func)
        {
            Dictionary<string, int> s;
            if (!_FunctionDict.TryGetValue(script, out s))
            {
                s = new Dictionary<string, int>();
                _FunctionDict.Add(script, s);
            }
            int index;
            if (!s.TryGetValue(func, out index))
            {
                index = _FunctionList.Count;
                s.Add(func, index);
                var ret = new InjectedScriptFunction(script, func);
                _FunctionList.Add(ret);
                return ret;
            }
            return _FunctionList[index];
        }

        public static InjectedScriptFunction InjectCompileFileMain(string script)
        {
            int index;
            if (!_FunctionMainDict.TryGetValue(script, out index))
            {
                index = _FunctionList.Count;
                _FunctionMainDict.Add(script, index);
                var ret = new InjectedScriptFunction(script, "<main>");
                _FunctionList.Add(ret);
                return ret;
            }
            return _FunctionList[index];
        }

        //2 free variables: original, index
        private static int InjectedEntrance(IntPtr vm)
        {
            int index;
            if (SquirrelFunctions.getinteger(vm, -2, out index) != 0)
            {
                return -1;
            }
            if (index < 0 || index >= _FunctionList.Count)
            {
                return -1;
            }

            SquirrelFunctions.getstackobj(vm, -1, out var obj);

            //pop the two free vars
            SquirrelFunctions.pop(vm, 2);

            var f = _FunctionList[index];
            return f.Invoke(vm, SquirrelFunctions.gettop(vm), obj.Type, obj.Value);
        }
    }
}
