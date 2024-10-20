using AMLCore.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Plugins
{
    public static class DeferredModLoading
    {
        //This class will contain API related with deferred loading of mods.
        //This special loading mode happens when user uses an online tool.
        //The only online tool we have today is GSO, but we should allow supporting others in the future.
        //Currently when AML is loaded in GSO, mod loading is automatically deferred.
        //For other online tools, AML cannot automatically detect it, so it has to request AML to do so.
        //Before all these happen, we have the mod selection API in this class.

        private static bool ShowModSelectionDialogInternal(CommonArguments read, CommonArguments write)
        {
            var containers = PluginLoader.InitializeAllInGame();
            read.SetPluginOptions(containers);
            var dialog = new LauncherOptionForm(containers, false);
            dialog.LoadArgPresetOptions(read.PresetSelection);
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                write.GetPluginOptions(dialog.Options, dialog.GetPresetSelection());
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool ShowModSelectionDialog()
        {
            if (!GSOLoadingInjection.IsGSO || GSOLoadingInjection.IsGameStarted)
            {
                return false;
            }
            var result = new CommonArguments();
            if (!ShowModSelectionDialogInternal(GSOLoadingInjection.GetCurrentArguments(), result))
            {
                return false;
            }
            GSOLoadingInjection.ReplaceArguments(result.Serialize(false, true));
            return true;
        }
    }
}
