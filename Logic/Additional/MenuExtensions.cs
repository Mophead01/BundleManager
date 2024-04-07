using AutoBundleManagerPlugin;
using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Core.Windows;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static AutoBundleManagerPlugin.AbmTestModule;

namespace AutoBundleManagerPlugin
{
    public class AbmMenuExtensions
    {
        public static string BMMenuName => "AutoBundleManager";
        public static string SubBMMenuName => null;
        public class AutoBundleManagerCompleteBundlesBundlesMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => BMMenuName;

            public override string SubLevelMenuName => SubBMMenuName;

            public override string MenuItemName => "Temp: Complete Bundles";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                //App.AssetManager.AddImaginaryBundle("A0_Mophead/ImaginaryBundleTest", BundleType.BlueprintBundle, 0);
                //App.AssetManager.AddImaginaryEbx("A0_Mophead/ImaginaryEbxTest", Guid.NewGuid(), FrostySdk.Sha1.Zero, "VisualUnlockRootAsset");
                //App.AssetManager.AddImaginaryRes("A0_Mophead/ImaginaryResTest".ToLower(), resType:ResourceType.IesResource, 10, FrostySdk.Sha1.Zero);
                //App.AssetManager.AddImaginaryChunk(10, FrostySdk.Sha1.Zero, Guid.NewGuid());
                //App.EditorWindow.DataExplorer.RefreshAll();
                FrostyTaskWindow.Show("Caching Bundles", "", (task) =>
                {
                    new BundleCompleter(task, App.AssetManager, "EditorMod", new List<string>());
                });
            });

        }
        public class AutoBundleManagerTestBlueprintBundlesMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => BMMenuName;

            public override string SubLevelMenuName => SubBMMenuName;

            public override string MenuItemName => "TEST: Reproduce BPBs";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                AbmTestFunctions bunTest = new AbmTestFunctions();
                bunTest.BlueprintBundleTest();
            });

        }
        public class AutoBundleManagerTestSublevelsMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => BMMenuName;

            public override string SubLevelMenuName => SubBMMenuName;

            public override string MenuItemName => "TEST: Reproduce Sublevels";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                AbmTestFunctions bunTest = new AbmTestFunctions();
                bunTest.SublevelTest();
            });

        }
        public class AutoBundleManagerTestBlueprintBundlesParentsMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => BMMenuName;

            public override string SubLevelMenuName => SubBMMenuName;

            public override string MenuItemName => "TEST: Parent BPBs";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                AbmTestFunctions bunTest = new AbmTestFunctions();
                bunTest.BlueprintBundleTest(false);
            });

        }
        public class AutoBundleManagerTestSublevelsParentMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => BMMenuName;

            public override string SubLevelMenuName => SubBMMenuName;

            public override string MenuItemName => "TEST: Parent Sublevels";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                AbmTestFunctions bunTest = new AbmTestFunctions();
                bunTest.SublevelTest(false);
            });

        }
        public class AutoBundleManagerTestChunkFirstMipH32MenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => BMMenuName;

            public override string SubLevelMenuName => SubBMMenuName;

            public override string MenuItemName => "TEST: H32 FirstMip Report";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                AbmTestFunctions bunTest = new AbmTestFunctions(); FrostyTaskWindow.Show("", "", (task) =>
                {
                    bunTest.TestFirstMipH32Accuracy(task);
                });
            });

        }
        public class AbmCacheBundleHierarchyMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => BMMenuName;

            public override string SubLevelMenuName => SubBMMenuName;

            public override string MenuItemName => "Cache: Bundle Hierarchy";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                FrostyTaskWindow.Show("Caching Bundles", "", (task) =>
                {
                    AbmBundleHeapCacheCreator.EnumerateSharedBundles(task);
                    AbmBundleHeapCacheCreator.EnumerateSublevelBundles(task);
                    AbmBundleHeapCacheCreator.EnumerateDetachedSubworlds(task);
                    AbmBundleHeapCacheCreator.EnumerateBlueprintBundles(task);
                    AbmBundleHeapCacheCreator.UpdateCache();
                });
            });

        }
        public class AbmCacheNetworkRegistriesMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => BMMenuName;

            public override string SubLevelMenuName => SubBMMenuName;

            public override string MenuItemName => "Cache: Network Registries";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                FrostyTaskWindow.Show("Caching Network Registries", "", (task) =>
                {
                    AbmNetworkRegistryCache.WriteToCache(task);
                });
            });

        }
        public class AbmCacheMeshVariationDatabasesMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => BMMenuName;

            public override string SubLevelMenuName => SubBMMenuName;

            public override string MenuItemName => "Cache: Mesh Variation Databases";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                FrostyTaskWindow.Show("Caching MVDBs", "", (task) =>
                {
                    AbmMeshVariationDatabasePrecache.WriteToCache(task);
                });
            });

        }
        public class AbmOptionsMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => BMMenuName;

            public override string SubLevelMenuName => SubBMMenuName;

            public override string MenuItemName => "Open Window";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                App.EditorWindow.OpenEditor("Bundle Manager", new AutoBundleManagerOptionsViewer());
            });

        }
    }
    public class AutoBundleManagerOptionsGrid
    {
        [Category("Options")]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        [DisplayName("Complete MeshVariationDBs")]
        [Description("Disabling prevents the Bundle Manager from making bundle changes to mesh variation databases")]
        public bool CompleteMeshVariations { get { return AutoBundleManagerOptions.CompleteMeshVariations; } set { AutoBundleManagerOptions.CompleteMeshVariations = value; } }
        [Category("Options")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Forced Bundle Edits")]
        [Description("Disabling prevents the Bundle Manager from making bundle changes to mesh variation databases")]
        public List<ForcedBundleEditsViewer> ForcedBundleEdits
        {
            get
            {
                return AutoBundleManagerOptions.ForcedBundleEdits.Select(pair => new ForcedBundleEditsViewer(pair.Key, pair.Value)).ToList();
            }
            set
            {
            }
        }
        [Category("Options")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Forced Bundle Transfers")]
        [Description("Disabling prevents the Bundle Manager from making bundle changes to mesh variation databases")]
        public List<ForcedBundleTransfersViewer> ForcedBundleTransfers
        {
            get
            {
                return AutoBundleManagerOptions.ForcedBundleTransfers.Select(pair => new ForcedBundleTransfersViewer(pair.Key, pair.Value)).ToList();
            }
            set
            {
            }
        }
        //[Category("ReadOnlyCaches")]
        //[EbxFieldMeta(EbxFieldType.Array)]
        //[DisplayName("MeshVariationDb Cache")]
        //[Description("Cached list of MeshVariationDatabases")]
        //[IsReadOnly]
        //public List<object> CachedMeshVariationDatabase { get; set; }

        [Category("ReadOnlyCaches")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Ebx Dependancy Cache")]
        [Description("Cached list of ebx, chunk and resource depdencies from an ebx asset")]
        [IsReadOnly]
        public List<AutoBundleManagerDependenciesCacheInterpruter> CachedEbx { get; set; }

        [Category("ReadOnlyCaches")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Res Dependancy Cache")]
        [Description("Cached list of ebx, chunk and resource depdencies from a res asset")]
        [IsReadOnly]
        public List<AutoBundleManagerDependenciesCacheInterpruter> CachedRes { get; set; }

#if false

        //
        //  Read Only Caches
        //

        [Category("ReadOnlyCaches")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Ebx Root Instance Cache")]
        [Description("Cached list of ebx root instance Guids")]
        [IsReadOnly]
        public List<RootInstancesViewer> RootInstances { get; set; }

        [Category("ReadOnlyCaches")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Ebx Dependancy Cache")]
        [Description("Cached list of ebx, chunk and resource depdencies from an ebx asset")]
        [IsReadOnly]
        public List<AutoBundleManagerDependenciesCacheInterpruter> CachedEbx { get; set; }

        [Category("ReadOnlyCaches")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Res Dependancy Cache")]
        [Description("Cached list of ebx, chunk and resource depdencies from a res asset")]
        [IsReadOnly]
        public List<AutoBundleManagerDependenciesCacheInterpruter> CachedRes { get; set; }

        [Category("ReadOnlyCaches")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Shared Bundle Heap Cache")]
        [Description("Cached list of bundles and their parents")]
        [IsReadOnly]
        public List<BundleHeapEntryViewer> CachedSharedBundles { get; set; }

        [Category("ReadOnlyCaches")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Sublevel Bundle Heap Cache")]
        [Description("Cached list of bundles and their parents")]
        [IsReadOnly]
        public List<BundleHeapEntryViewer> CachedSublevels { get; set; }

        [Category("ReadOnlyCaches")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Blueprint Bundle Heap Cache")]
        [Description("Cached list of bundles and their parents")]
        [IsReadOnly]
        public List<BundleHeapEntryViewer> CachedBpbs { get; set; }
        [Category("ReadOnlyCaches")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Network Registry Reference Types Cache")]
        [Description("List of object types which network registries are set to reference for in modified/duplicated assets")]
        [IsReadOnly]
        public List<CString> CachedNetworkRegistryReferenceTypes { get; set; }
        [Category("ReadOnlyCaches")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Network Registry Reference Types Cache")]
        [Description("List of object types which network registries are set to reference for in modified/duplicated assets")]
        [IsReadOnly]
        public List<NetworkRegistryReferencesViewer> CachedNetworkRegistryReferenceObjects { get; set; }
        //NetworkRegistryReferencesViewer

#endif
        public AutoBundleManagerOptionsGrid()
        {
            CachedEbx = AbmDependenciesCache.GetAllCachedDependencies().Where(pair => !pair.Value.isRes && pair.Value.networkRegistryRefGuids.Count() > 0).Select(pair => new AutoBundleManagerDependenciesCacheInterpruter(pair)).ToList();
            CachedRes = AbmDependenciesCache.GetAllCachedDependencies().Where(pair => pair.Value.isRes).Select(pair => new AutoBundleManagerDependenciesCacheInterpruter(pair)).ToList();
            //CachedMeshVariationDatabase = AbmMeshVariationDatabasePrecache.MeshVariationDatabase.Select(pair => pair.Value.WriteToGameEntry()).ToList();
#if false
            //RootInstances = new List<RootInstancesViewer>();
            //CachedEbx = new List<AutoBundleManagerDependenciesCacheInterpruter>();
            //CachedRes = new List<AutoBundleManagerDependenciesCacheInterpruter>();
            //CachedBpbs = new List<BundleHeapEntryViewer>();
            //CachedSublevels = new List<BundleHeapEntryViewer>();
            //CachedSharedBundles = new List<BundleHeapEntryViewer>();

            RootInstances = AbmRootInstanceCache.ebxRootInstanceGuidList.Select(pair => new RootInstancesViewer((pair.Key, pair.Value))).ToList();
            CachedEbx = AbmDependenciesCache.GetAllCachedDependencies().Where(pair => !pair.Value.isRes).Select(pair => new AutoBundleManagerDependenciesCacheInterpruter(pair)).ToList();
            CachedRes = AbmDependenciesCache.GetAllCachedDependencies().Where(pair => pair.Value.isRes).Select(pair => new AutoBundleManagerDependenciesCacheInterpruter(pair)).ToList();
            CachedBpbs = AbmBundleHeap.Bundles.Where(bunPair => App.AssetManager.GetBundleEntry(bunPair.Key).Type == BundleType.BlueprintBundle).ToList().Select(bunPair => new BundleHeapEntryViewer(bunPair.Value)).ToList();
            CachedSharedBundles = AbmBundleHeap.Bundles.Where(bunPair => App.AssetManager.GetBundleEntry(bunPair.Key).Type == BundleType.SharedBundle).ToList().Select(bunPair => new BundleHeapEntryViewer(bunPair.Value)).ToList();
            CachedSublevels = AbmBundleHeap.Bundles.Where(bunPair => App.AssetManager.GetBundleEntry(bunPair.Key).Type == BundleType.SubLevel).ToList().Select(bunPair => new BundleHeapEntryViewer(bunPair.Value)).ToList();
            CachedNetworkRegistryReferenceTypes = AbmNetworkRegistryCache.NetworkRegistryTypes.OrderBy(str => str).Select(type => new CString(type)).ToList();
            CachedNetworkRegistryReferenceObjects = AbmNetworkRegistryCache.NetworkRegistryReferences.Select(pair => new NetworkRegistryReferencesViewer(pair)).ToList();
#endif
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
            dynamic topLevel = e.Item;
            while (topLevel.Parent != null)
                topLevel = topLevel.Parent;

            foreach (FrostyPropertyGridItemData child in ((ObservableCollection<FrostyPropertyGridItemData>)topLevel.Children).Where(child => child.DisplayName == "Forced Bundle Edits"))
            {
                Dictionary<string, List<string>> returnDict = new Dictionary<string, List<string>>();
                foreach (ForcedBundleEditsViewer forcedEdits in ((dynamic)child).Value)
                {
                    if (!returnDict.ContainsKey(forcedEdits.assetName))
                        returnDict.Add(forcedEdits.assetName, forcedEdits.bundleNames.Select(name => name.ToString()).ToList());
                }
                AutoBundleManagerOptions.ForcedBundleEdits = returnDict;
            }

            foreach (FrostyPropertyGridItemData child in ((ObservableCollection<FrostyPropertyGridItemData>)topLevel.Children).Where(child => child.DisplayName == "Forced Bundle Transfers"))
            {
                Dictionary<string, List<string>> returnDict = new Dictionary<string, List<string>>();
                foreach (ForcedBundleTransfersViewer forcedEdits in ((dynamic)child).Value)
                {
                    if (!returnDict.ContainsKey(forcedEdits.targetBundleName))
                        returnDict.Add(forcedEdits.targetBundleName, forcedEdits.copyBundleNames.Select(name => name.ToString()).ToList());
                }
                AutoBundleManagerOptions.ForcedBundleTransfers = returnDict;
            }
            Config.Save();
        }

        private void AutoBundleMangaerOptionsViewer_Loaded(object sender, RoutedEventArgs e)
        {
            AutoBundleManagerOptionsGrid optionsGrid = new AutoBundleManagerOptionsGrid();
            //optionsGrid.DependenciesCache = new List<AutoBundleManagerDependenciesCacheInterpruter>() { new AutoBundleManagerDependenciesCacheInterpruter(new KeyValuePair<Sha1, DependencyData>(new Sha1(), null)) };
            AbmOptionsPropertyGrid.SetClass(optionsGrid);
        }

    }
}
