using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Engine.Script
{
    public class ReferencedScriptObject : IDisposable
    {
        public SQObject SQObject = SQObject.Null;

        public void Dispose()
        {
            if (SQObject.Type != SQObject.SQObjectType.OT_NULL)
            {
                ReleaseRef();
            }
        }

        public void GetFromStack(int index)
        {
            SquirrelFunctions.getstackobj(SquirrelHelper.SquirrelVM, index, out SQObject);
            SquirrelFunctions.addref_(SquirrelHelper.SquirrelVM, ref SQObject);
        }

        public void PopFromStack()
        {
            SquirrelFunctions.getstackobj(SquirrelHelper.SquirrelVM, -1, out SQObject);
            SquirrelFunctions.addref_(SquirrelHelper.SquirrelVM, ref SQObject);
            SquirrelFunctions.pop(SquirrelHelper.SquirrelVM, 1);
        }

        public void ReleaseRef()
        {
            SquirrelFunctions.release_(SquirrelHelper.SquirrelVM, ref SQObject);
            SQObject = SQObject.Null;
        }
    }
}
