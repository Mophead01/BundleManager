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

        [Category("ReadOnlyCaches")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Blueprint Bundle Heap Cache")]
        [Description("Cached list of bundles and their parents")]
        [IsReadOnly]
        public List<BundleHeapEntryViewer> CachedBpbs { get; set; }

        [Category("ReadOnlyCaches")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Sublevel Bundle Heap Cache")]
        [Description("Cached list of bundles and their parents")]
        [IsReadOnly]
        public List<BundleHeapEntryViewer> CachedSublevels { get; set; }

        [Category("ReadOnlyCaches")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Shared Bundle Heap Cache")]
        [Description("Cached list of bundles and their parents")]
        [IsReadOnly]
        public List<BundleHeapEntryViewer> CachedSharedBundles { get; set; }
        public AutoBundleManagerOptionsGrid()
        {
            CachedEbx = AbmDependenciesCache.GetAllCachedDependencies().Where(pair => !pair.Value.isRes).Select(pair => new AutoBundleManagerDependenciesCacheInterpruter(pair)).ToList();
            CachedRes = AbmDependenciesCache.GetAllCachedDependencies().Where(pair => pair.Value.isRes).Select(pair => new AutoBundleManagerDependenciesCacheInterpruter(pair)).ToList();
            CachedBpbs = AbmBundleHeap.Bundles.Where(bunPair => App.AssetManager.GetBundleEntry(bunPair.Key).Type == BundleType.BlueprintBundle).ToList().Select(bunPair => new BundleHeapEntryViewer(bunPair.Value)).ToList();
            CachedSharedBundles = AbmBundleHeap.Bundles.Where(bunPair => App.AssetManager.GetBundleEntry(bunPair.Key).Type == BundleType.SharedBundle).ToList().Select(bunPair => new BundleHeapEntryViewer(bunPair.Value)).ToList();
            CachedSublevels = AbmBundleHeap.Bundles.Where(bunPair => App.AssetManager.GetBundleEntry(bunPair.Key).Type == BundleType.SubLevel).ToList().Select(bunPair => new BundleHeapEntryViewer(bunPair.Value)).ToList();
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
}
