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
                BundleTest bunTest = new BundleTest();
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
                BundleTest bunTest = new BundleTest();
                bunTest.SublevelBundleTest();
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
