using AMLCore.Misc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AMLCore.Internal
{
    public static class DebugPoint
    {
        public static void Trigger()
        {
            string msg = null;
            try
            {
                msg = String.Format("Core debug point triggered:\n{0}\n{1}",
                    StackTraceHelper.GetCallerMethodName(),
                    Startup.Mode.ToString());
                var enabled = new IniFile("Core").Read("Debug", "DebugPointEnabled", "false");
                if (enabled != "false" && enabled != "0")
                {
                    MessageBox.Show(msg, "Debug");
                }
            }
            catch
            {
                if (msg != null)
                {
                    MessageBox.Show(msg, "Debug");
                }
            }
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }
        }
    }
}
