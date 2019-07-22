using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Engine.Script
{
    //for before, stack is only arguments
    //for after, stack is arguments, returnValue
    //arguments may be modified by other registered function
    public delegate void InjectedScriptDelegate(IntPtr vm);

    public class InjectedScriptFunction
    {
        private List<InjectedScriptDelegate> _Before = new List<InjectedScriptDelegate>();
        private List<InjectedScriptDelegate> _After = new List<InjectedScriptDelegate>();
        private readonly string _scriptName, _funcName;

        internal InjectedScriptFunction(string s, string f)
        {
            _scriptName = s;
            _funcName = f;
        }

        internal int Invoke(IntPtr vm, int nargs, SquirrelHelper.SQObjectType original1, SquirrelFunctions.SQObjectValue original2)
        {
            bool stackTopChanged = false;
            foreach (var d in _Before)
            {
                d(vm);

                //check stack size after every call
                int top;
                while ((top = SquirrelFunctions.gettop(vm)) < nargs)
                {
                    SquirrelFunctions.pushnull(vm);
                    stackTopChanged = true;
                }
                if (top > nargs)
                {
                    SquirrelFunctions.pop(vm, top - nargs);
                    stackTopChanged = true;
                }
            }

            if (stackTopChanged)
            {
                stackTopChanged = false;
                CoreLoggers.Script.Error("stack size changed when calling prerun functions for {0} ({1})", _funcName, _scriptName);
            }

            //push original
            SquirrelFunctions.pushobject_(vm, original1, original2);
            //push n arguments
            for (int i = 1; i <= nargs; ++i)
            {
                SquirrelFunctions.push(vm, i);
            }

            //call
            if (SquirrelFunctions.call(vm, nargs, 1, 0) != 0)
            {
                return -1;
            }
            //remove original
            SquirrelFunctions.remove(vm, -2);

            foreach (var d in _After)
            {
                d(vm);

                //check stack size after every call
                int top;
                while ((top = SquirrelFunctions.gettop(vm)) < nargs + 1)
                {
                    SquirrelFunctions.pushnull(vm);
                    stackTopChanged = true;
                }
                //keep the last as return value
                while ((top = SquirrelFunctions.gettop(vm)) > nargs + 1)
                {
                    SquirrelFunctions.remove(vm, -2);
                    stackTopChanged = true;
                }
            }

            if (stackTopChanged)
            {
                CoreLoggers.Script.Error("stack size changed when calling postrun functions for {0} ({1})", _funcName, _scriptName);
            }
            //always (try to) return a value
            return 1;
        }

        public void AddBefore(InjectedScriptDelegate d)
        {
            _Before.Add(d);
        }

        public void AddAfter(InjectedScriptDelegate d)
        {
            _After.Add(d);
        }
    }
}
