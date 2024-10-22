using AMLCore.Injection.GSO;
using AMLCore.Internal;
using AMLCore.Misc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AMLCore.Plugins
{
    public sealed class GSOModSyncMode
    {
        public bool Check => GSOLoadingInjection.ModCheck;
        public bool Sync => GSOLoadingInjection.ModCheckSync;
        public bool AllowModSelection => !Sync || !GSOConnectionStatus.IsClient;
    }

    public static class DeferredModLoading
    {
        //This class will contain API related with deferred loading of mods.
        //This special loading mode happens when user uses an online tool.
        //The only online tool we have today is GSO, but we should allow supporting others in the future.
        //Currently when AML is loaded in GSO, mod loading is automatically deferred.
        //For other online tools, AML cannot automatically detect it, so it has to request AML to do so.
        //Before all these happen, we have the mod selection API in this class.

        public static GSOModSyncMode GSOModSyncMode { get; } = new GSOModSyncMode();

        public static bool ShowModSelectionDialog()
        {
            if (!GSOLoadingInjection.IsGSO ||
                GSOLoadingInjection.IsGameStarted ||
                !AllowModSelectionInternal())
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

        private static bool ShowModSelectionDialogInternal(CommonArguments read, CommonArguments write)
        {
            var containers = PluginLoader.InitializeAllInGame();
            read.SetPluginOptions(containers);
            var dialog = new LauncherOptionForm(containers, false);
            dialog.SelectLauncherMode = false;
            dialog.LoadArgPresetOptions(read.PresetSelection);
            dialog.DisablePresetEdit();
            dialog.DisableNonFunctional();
            dialog.DisableGSO();
            dialog.FormClosing += (sender, e) =>
            {
                if (e.CloseReason == System.Windows.Forms.CloseReason.UserClosing &&
                    dialog.DialogResult == System.Windows.Forms.DialogResult.OK)
                {
                    if (!AllowModSelectionInternal())
                    {
                        WindowsHelper.MessageBox("当前连结状态下无法修改Mod列表，请点击取消。");
                        e.Cancel = true;
                    }
                }
            };
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!AllowModSelectionInternal())
                {
                    return false;
                }
                write.GetPluginOptions(dialog.Options, dialog.GetPresetSelection());
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool AllowModSelectionInternal()
        {
            return GSOModSyncMode.AllowModSelection;
        }
    }
}
