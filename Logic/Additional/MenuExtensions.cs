using AutoBundleManagerPlugin;
using Frosty.Core;
using Frosty.Core.Attributes;
using Frosty.Core.Controls;
using Frosty.Core.Mod;
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using static AutoBundleManagerPlugin.AbmTestModule;
using Newtonsoft.Json;
using Formatting = Newtonsoft.Json.Formatting;
using System.Security.Cryptography.X509Certificates;
using AutoBundleManagerPlugin.Logic.Operations;
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

            public override string MenuItemName => "Complete Bundles (Local)";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                string editorModName = "KyberMod.fbmod";
                string basePath = $@"{(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)).Replace("\\", @"/").Replace("/Plugins", "")}/Mods/Kyber";
                List<string> fbmodNames = KyberIntegration.GetLoadOrder(basePath).Select(shortName => $"{basePath}/{shortName}").ToList();
                editorModName = $"{basePath}/{editorModName}";

                //foreach (string fbmodName in fbmodNames)
                //{
                //    if (fbmodName != editorModName)
                //        new FbmodParsing(fbmodName);
                //}
                //AbmDependenciesCache.UpdateCache();
                //AbmDependenciesCache.WriteToXml();

                FrostyTaskWindow.Show("Completing Bundles", "", (task) =>
                {
                    new BundleCompleter().CompleteBundles(task, ExportType.KyberLaunchOnly, editorModName, fbmodNames);
                });
                AbmDependenciesCache.WriteToXml();
                App.EditorWindow.DataExplorer.RefreshAll();
            });

        }
        public class AutoBundleManagerCompleteGlobalBundlesBundlesMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => BMMenuName;

            public override string SubLevelMenuName => SubBMMenuName;

            public override string MenuItemName => "Complete Bundles (Global)";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                string editorModName = "KyberMod.fbmod";
                string basePath = $@"{(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)).Replace("\\", @"/").Replace("/Plugins", "")}/Mods/Kyber";
                List<string> fbmodNames = KyberIntegration.GetLoadOrder(basePath).Select(shortName => $"{basePath}/{shortName}").ToList();
                editorModName = $"{basePath}/{editorModName}";

                //foreach (string fbmodName in fbmodNames)
                //{
                //    if (fbmodName != editorModName)
                //        new FbmodParsing(fbmodName);
                //}
                //AbmDependenciesCache.UpdateCache();
                //AbmDependenciesCache.WriteToXml();

                FrostyTaskWindow.Show("Completing Bundles", "", (task) =>
                {
                    new BundleCompleter().CompleteBundles(task, ExportType.ExportOnly, editorModName, new List<string>());
                });
                AbmDependenciesCache.WriteToXml();
                App.EditorWindow.DataExplorer.RefreshAll();
            });

        }
        public class AutoBundleManagerClearBundlesBundlesMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => BMMenuName;

            public override string SubLevelMenuName => SubBMMenuName;

            public override string MenuItemName => "Clear Bundles";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                FrostyTaskWindow.Show("Clearing Bundles", "", (task) =>
                {
                    new BundleCompleter().ClearBundles();
                });
                App.EditorWindow.DataExplorer.RefreshAll();
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
                AbmTestFunctions bunTest = new AbmTestFunctions();
                FrostyTaskWindow.Show("", "", (task) =>
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
        public class AbmDependenciesToXmlMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => BMMenuName;

            public override string SubLevelMenuName => SubBMMenuName;

            public override string MenuItemName => "Export: Dependencies Cache to Xml";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                FrostyTaskWindow.Show("Writing Txt", "", (task) =>
                {
                    AbmDependenciesCache.WriteToXml();
                    //AbmMeshVariationDatabasePrecache.WriteToCache(task);
                });
            });

        }
        public class AbmCurrentProjectToCsvMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => BMMenuName;

            public override string SubLevelMenuName => SubBMMenuName;

            public override string MenuItemName => "Export: Bundle Edits to CSV";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                FrostySaveFileDialog sfd = new FrostySaveFileDialog("Save Stat Events", "*.csv (Text File)|*.csv", "StatEvents");
                if (sfd.ShowDialog())
                {
                    AbmTestFunctions bunTest = new AbmTestFunctions();
                    bunTest.ExportBundleEdits(sfd.FileName);
                };
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

        //
        //  Base Options
        //

        [Category("Options")]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        [DisplayName("Enable on KMM Launch")]
        [Description("Enabling will allow the BM to automatically run when launching through KMM.")]
        public bool LaunchKmm { get { return AutoBundleManagerOptions.LaunchKmm; } set { AutoBundleManagerOptions.LaunchKmm = value; } }

        [Category("Options")]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        [DisplayName("Enable on Frosty Launch")]
        [Description("Enabling will allow the BM to automatically run when launching through Frosty.")]
        public bool ExportKmm { get { return AutoBundleManagerOptions.LaunchFrosty; } set { AutoBundleManagerOptions.LaunchFrosty = value; } }

        [Category("Options")]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        [DisplayName("Enable on Module Export")]
        [Description("Enabling will allow the BM to automatically run when exporting the module.")]
        public bool ModExport { get { return AutoBundleManagerOptions.ModExport; } set { AutoBundleManagerOptions.ModExport = value; } }

        [Category("Options")]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        [DisplayName("Is Cosmetic Module")]
        [Description("Enabling prevents the Bundle Manager from making bundle changes to server networked assets, which would cause desynchronisation issues when run online. Please do not have this option enabled if you are making a gameplay modification.")]
        public bool IsCosmeticModule { get { return !AutoBundleManagerOptions.CompleteNetworkRegistries; } set { AutoBundleManagerOptions.CompleteNetworkRegistries = !value; } }

        [Category("Options")]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        [DisplayName("KMM Launch - Process Load Order")]
        [Description("Enabling will allow the BM to process then mod load order when running through KMM, automatically making the mods compatible.")]
        public bool ProcessLoadOrder { get { return AutoBundleManagerOptions.ProcessLoadOrder; } set { AutoBundleManagerOptions.ProcessLoadOrder = value; } }

        [Category("Options")]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        [DisplayName("Export Logs")]
        [Description("Enabling allows the BM to export a log of all the changes made to your project file while running, this can help identify problems with the operation but does slow the BM down")]
        public bool ExportLogs { get { return AutoBundleManagerOptions.ExportLogs; } set { AutoBundleManagerOptions.ExportLogs = value; } }

        // 
        // Advanced Options
        //

        [Category("Advanced Options")]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        [DisplayName("KMM Launch - Optimise Bundles")]
        [Description("Enabling will make it so that when launching through KMM, the BM will only complete bundles for Frontend and the level you are going to go into.")]
        public bool OptimiseBundles { get { return AutoBundleManagerOptions.OptimiseBundles; } set { AutoBundleManagerOptions.OptimiseBundles = value; } }

        [Category("Advanced Options")]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        [DisplayName("Complete MeshVariationDBs")]
        [Description("Disabling prevents the Bundle Manager from making bundle changes to mesh variation databases")]
        public bool CompleteMeshVariations { get { return AutoBundleManagerOptions.CompleteMeshVariations; } set { AutoBundleManagerOptions.CompleteMeshVariations = value; } }

        [Category("Advanced Options")]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        [DisplayName("Enable Forced Bundle Edits")]
        [Description("Disabling prevents the Bundle Manager from forcing certain assets to be loaded on assigned bundles.")]
        public bool EnableForcedBundleEdits { get { return AutoBundleManagerOptions.EnableForcedBundleEdits; } set { AutoBundleManagerOptions.EnableForcedBundleEdits = value; } }

        [Category("Advanced Options")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Forced Bundle Edits")]
        [Description("A list of ebx/res assets to forcibly add to a bundle")]
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

        [Category("Advanced Options")]
        [EbxFieldMeta(EbxFieldType.Boolean)]
        [DisplayName("Enable Forced Bundle Transfers")]
        [Description("Disabling prevents the Bundle Manager from forcibly transferring assets from one bundle to another.")]
        public bool EnableForcedBundleTransfers { get { return AutoBundleManagerOptions.EnableForcedBundleTransfers; } set { AutoBundleManagerOptions.EnableForcedBundleTransfers = value; } }

        [Category("Advanced Options")]
        [EbxFieldMeta(EbxFieldType.Array)]
        [DisplayName("Forced Bundle Transfers")]
        [Description("A list of bundles which the bundle manager should forcibly copy assets between.")]
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
