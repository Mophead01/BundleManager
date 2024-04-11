using Frosty.Core.Attributes;
using Frosty.Core.Mod;
using Frosty.Core.Windows;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBundleManagerPlugin.Logic.Operations
{
    public class BundleManagerLaunchExportOverride : ExportActionOverride
    {
        BundleCompleter BundleManager = new BundleCompleter();
        public override void PreExport(FrostyTaskWindow task, ExportType export, string fbmodName, List<string> loadOrder)
        {
            BundleManager.CompleteBundles(task, export, fbmodName, loadOrder);
        }
        public override void PostExport(FrostyTaskWindow task, ExportType exportType, string fbmodName, List<string> loadOrder)
        {
            BundleManager.ClearBundles();
        }
    }
}
