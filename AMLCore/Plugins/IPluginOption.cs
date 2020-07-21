using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace AMLCore.Plugins
{
    public enum PluginType
    {
        //GSO (no) control: whether GSO loader will try to ensure clients load the same plugins and with same arguments
        //Disable/endable by default: whether the (Default) preset will include it when launcher starts
        //(Not) recorded: whether the use of this plugin should be recorded in the replay

        Debug, //GSO no control, disabled by default, not recorded -- this will be default type if unknown
        Optimization, //GSO no control, enabled by default, not recorded
        EffectOnly, //GSO no control, disabled by default, recorded
        Functional, //GSO control, disabled by default, recorded
    }

    public interface IPluginOption
    {
        void ResetOptions();
        void GetOptions(Action<string, string> list);
        void AddOption(string key, string value);
        object GetPropertyWindowObject();
        Control GetConfigControl();
    }

    //Extensions

    //This is an unused (and thus untested) functionality.
    //GS03Loader was designed to use it, but since most gs03 affects AC system, we should
    //not treat any gs03 mod as effect-only.
    public interface IPluginOptionExtSyncArgument
    {
        string[] GetOptionSyncIgnoreList();
    }
}
