using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AMLInjected
{
    class Helper
    {
        public static uint LoadCore(IntPtr data)
        {
            try
            {
                Startup.Initialize(data);
                return 0;
            }
            catch
            {
                return 1;
            }
        }
    }
}
