using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AMLCore.Plugins
{
    public interface IPluginOption
    {
        void GetOptions(Action<string, string> list);
        void AddOption(string key, string value);
        object GetPropertyWindowObject();
        Control GetConfigControl();
    }
}
