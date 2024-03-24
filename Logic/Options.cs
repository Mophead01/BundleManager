using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.Interfaces;
using FrostySdk.IO;
using FrostySdk.Managers;
using MeshSetPlugin.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AutoBundleManagerPlugin
{
    public static class AutoBundleManagerOptions
    {
        public static bool CompleteMeshVariations { get { return Config.Get<bool>("ABM_CompleteMeshVariationDbs", true, ConfigScope.Game); } set { Config.Add("ABM_CompleteMeshVariationDbs", value, ConfigScope.Game); } }
        public static Dictionary<string, List<string>> ForcedBundleEdits { 
            get {
                string strList = Config.Get<string>("ABM_ForcedBundleEdits", null, ConfigScope.Game);
                Dictionary<string, List<string>> returnList = new Dictionary<string, List<string>>();
                if (strList == null)
                {
                    returnList = new Dictionary<string, List<string>>()
                    {
                        { "animations/antanimations/levels/frontend/frontend_win32_antstate", new List<string>()},
                        { "animations/antanimations/levels/frontend/collection_win32_antstate", new List<string>()},
                    };
                    foreach(EbxAssetEntry levEntry in App.AssetManager.EnumerateEbx(type: "LevelData"))
                    {
                        if (!levEntry.IsAdded && levEntry.Name != "Levels/Frontend/Frontend")
                            returnList.Keys.ToList().ForEach(key => returnList[key].Add(App.AssetManager.GetBundleEntry(levEntry.Bundles[0]).Name));
                    }
                    return returnList;
                }
                foreach (string subStr in strList.Split('$'))
                {
                    string assetName = subStr.Split(':')[0];
                    returnList.Add(assetName, new List<string>());
                    foreach (string subStr2 in subStr.Split(':')[1].Split('£'))
                        returnList[assetName].Add(subStr2);
                }
                return returnList;
            }
            set
            {
                Config.Add("ABM_ForcedBundleEdits", string.Join("$", value.ToList().Select(pair => $"{pair.Key}:{string.Join("£", pair.Value)}")), ConfigScope.Game);                
            }
        }
    }
    [EbxClassMeta(EbxFieldType.Struct)]
    public class ForcedBundleEditsViewer
    {
        [DisplayName("Asset Name")]
        [Description("Name of the asset to add to bundles")]
        public CString assetName { get; set; }

        [DisplayName("Bundles")]
        [Description("Bundles to add the asset to")]
        public List<CString> bundleNames { get; set; }
        public ForcedBundleEditsViewer(string assetName, List<string> bundleNames)
        {
            this.assetName = assetName;
            this.bundleNames = bundleNames.Select(str => new CString(str)).ToList();
        }
        public ForcedBundleEditsViewer()
        {
            this.bundleNames = new List<CString>();
        }
    }
}
