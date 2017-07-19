using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Internal
{
    public static class DebugPoint
    {
        public static bool Enabled = false;

        public static void Trigger(string name)
        {
            if (Enabled)
            {
                System.Windows.Forms.MessageBox.Show("Debug point: " + name);
            }
        }
    }
}
