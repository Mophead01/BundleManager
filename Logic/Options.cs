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
        public static bool CompleteMeshVariations { get { return Config.Get<bool>("ABM_CompleteMeshVariationDbs", true); } set { Config.Add("ABM_CompleteMeshVariationDbs", value); } }
    }
    public class AutoBundleManagerOptionsGrid
    {
        [Category("Options")]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        [DisplayName("Complete MeshVariationDBs")]
        [Description("Disabling prevents the Bundle Manager from making bundle changes to mesh variation databases")]
        public bool CompleteMeshVariations { get { return AutoBundleManagerOptions.CompleteMeshVariations; } set { AutoBundleManagerOptions.CompleteMeshVariations = value; } }

        [Category("ReadOnlyCaches")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Ebx Dependancy Cache")]
        [Description("Cached list of ebx, chunk and resource depdencies from an ebx asset")]
        [IsReadOnly]
        public List<AutoBundleManagerDependenciesCacheInterpruter> CachedEbx {get; set; }

        [Category("ReadOnlyCaches")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Res Dependancy Cache")]
        [Description("Cached list of ebx, chunk and resource depdencies from a res asset")]
        [IsReadOnly]
        public List<AutoBundleManagerDependenciesCacheInterpruter> CachedRes { get; set; }
        public AutoBundleManagerOptionsGrid()
        {
            CachedEbx = AbmDependenciesCache.GetAllCachedDependencies().Where(pair => !pair.Value.isRes).Select(pair => new AutoBundleManagerDependenciesCacheInterpruter(pair)).ToList();
            CachedRes = AbmDependenciesCache.GetAllCachedDependencies().Where(pair => pair.Value.isRes).Select(pair => new AutoBundleManagerDependenciesCacheInterpruter(pair)).ToList();
        }
    }
    public class AutoBundleManagerOptionsViewer : FrostyBaseEditor
    {
        private const string PART_AbmOptionsViewer = "PART_AbmOptionsViewer";
        private FrostyPropertyGrid AbmOptionsPropertyGrid;
        static AutoBundleManagerOptionsViewer()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AutoBundleManagerOptionsViewer), new FrameworkPropertyMetadata(typeof(AutoBundleManagerOptionsViewer)));
        }
        public AutoBundleManagerOptionsViewer()
        {

        }
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            AbmOptionsPropertyGrid = GetTemplateChild(PART_AbmOptionsViewer) as FrostyPropertyGrid;
            Loaded += AutoBundleMangaerOptionsViewer_Loaded;
            AbmOptionsPropertyGrid.OnModified += AbmOptionsPropertyGrid_OnModified;
        }

        private void AbmOptionsPropertyGrid_OnModified(object sender, ItemModifiedEventArgs e)
        {
            AutoBundleManagerOptionsGrid optionsGrid = (AutoBundleManagerOptionsGrid)AbmOptionsPropertyGrid.Object;
        }

        private void AutoBundleMangaerOptionsViewer_Loaded(object sender, RoutedEventArgs e)
        {
            AutoBundleManagerOptionsGrid optionsGrid = new AutoBundleManagerOptionsGrid();
            //optionsGrid.DependenciesCache = new List<AutoBundleManagerDependenciesCacheInterpruter>() { new AutoBundleManagerDependenciesCacheInterpruter(new KeyValuePair<Sha1, DependencyData>(new Sha1(), null)) };
            AbmOptionsPropertyGrid.SetClass(optionsGrid);
        }

    }
    [EbxClassMeta(EbxFieldType.Struct)]
    public class AutoBundleManagerDependenciesCacheInterpruter
    {
        [DisplayName("Name")]
        [Description("Asset Name")]
        [IsReadOnly]
        public string Name { get; set; }
        [DisplayName("Sha1")]
        [Description("Asset signature")]
        [IsReadOnly]
        public Sha1 Sha1 { get; set; }

        [DisplayName("Ebx Assets")]
        [Description("Ebx assets referenced by this asset")]
        [IsReadOnly]
        public List<CString> EbxAssets { get; set; }

        [DisplayName("Res Assets")]
        [Description("Res assets referenced by this asset")]
        [IsReadOnly]
        public List<CString> ResAssets { get; set; }

        [DisplayName("Chunk Assets")]
        [Description("Chunk assets referenced by this asset")]
        [IsReadOnly]
        public List<Guid> ChunkAssets { get; set; }
        public AutoBundleManagerDependenciesCacheInterpruter()
        {

        }

        public AutoBundleManagerDependenciesCacheInterpruter(KeyValuePair<Sha1, DependencyData> pair)
        {
            Name = pair.Value.srcName;
            Sha1 = pair.Key;
            EbxAssets = pair.Value.ebxRefs.Select(ebxRef => new CString(ebxRef.Name)).ToList();
            ResAssets = pair.Value.resRefs.Select(resRef => new CString(resRef.Name)).ToList();
            ChunkAssets = pair.Value.chkRefs.Select(chkRef => chkRef.Id).ToList();
        }
    }
}
