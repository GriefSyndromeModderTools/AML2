using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Injection.Engine.Script
{
    //We don't mark this as IDisposable because we ignore it if it is release
    //at exiting, when the sqvm is possibly released already.
    //TODO better idea is to inject and release right before vm released.
    public class ReferencedScriptObject
    {
        public SquirrelFunctions.SQObject SQObject = SquirrelFunctions.SQObject.Null;

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
            SQObject = SquirrelFunctions.SQObject.Null;
        }
    }
}
