using AutoBundleManagerPlugin;
using Frosty.Core;
using Frosty.Core.Windows;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using static AutoBundleManager.Logic.AbmTestModule;

namespace AutoBundleManager.Logic
{
    public class AbmMenuExtensions
    {
        public static string BMMenuName => "AutoBundleManager";
        public static string SubBMMenuName => null;
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
                bunTest.TestFirstMipH32Accuracy();
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
}
