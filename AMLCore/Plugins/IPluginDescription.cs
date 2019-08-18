using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Plugins
{
    public interface IPluginDescription
    {
        string InternalName { get; }
        string[] Authers { get; }

        string DisplayName { get; }
        string Description { get; }

        int LoadPriority { get; }
        PluginType PluginType { get; }
        string[] Dependencies { get; }
    }
}
