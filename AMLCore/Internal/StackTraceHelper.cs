using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace AMLCore.Internal
{
    internal static class StackTraceHelper
    {
        public static string GetCallerMethodName()
        {
            var m = new StackTrace(2).GetFrame(0).GetMethod();
            if (m == null)
            {
                return "<unknown>";
            }
            return m.DeclaringType != null ?
                m.DeclaringType.FullName + "." + m.Name : m.Name;
        }
    }
}
