using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AMLInjected
{
    class Helper
    {
        public static uint LoadCore(IntPtr data)
        {
            try
            {
                Startup.InitializeInjected(data);
                return 0;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return 1;
            }
        }
    }
}
