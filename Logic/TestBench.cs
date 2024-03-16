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

namespace AutoBundleManager.Logic
{
    public class AbmTestModule
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
                FrostyTaskWindow.Show("Caching", "", (task) =>
                {
                    AbmBundleHierarchy.EnumerateSharedBundles(task);
                    AbmBundleHierarchy.EnumerateSublevelBundles(task);
                    AbmBundleHierarchy.UpdateCache();
                });
            });

        }

        public class BundleTest
        {
            public BundleTest()
            {
                
            }
            public void BlueprintBundleTest()
            {
                FrostyTaskWindow.Show("Testing bundles", "", (task) =>
                {
                    int length = App.AssetManager.EnumerateBundles(BundleType.BlueprintBundle).Count();
                    int idx = 0;
                    foreach (BundleEntry bEntry in App.AssetManager.EnumerateBundles(BundleType.BlueprintBundle))
                    {
                        if (new List<string> { "_bpb", "_bpb_bundle1p_win32" }.Any(ender => bEntry.Name.EndsWith(ender)))
                        {
                            task.Update(bEntry.DisplayName.Split('/').Last());
                            TestBundle(App.AssetManager.GetBundleId(bEntry));
                        }
                        task.Update(progress: ((double)idx++ / (double)length) * 100.0d);
                        if (idx > 150)
                            break;
                    }
                    AbmDependenciesCache.UpdateCache();
                    App.Logger.Log("Checked all bpbs");
                });
            }

            public void SublevelBundleTest()
            {
                FrostyTaskWindow.Show("Testing bundles", "", (task) =>
                {
                    int length = App.AssetManager.EnumerateBundles(BundleType.SubLevel).Count();
                    int idx = 0;
                    foreach (BundleEntry bEntry in App.AssetManager.EnumerateBundles(BundleType.SubLevel))
                    {
                        if (new List<string> { "S2/Levels/CloudCity_01" }.Any(ender => bEntry.Name.Contains(ender)))
                        {
                            task.Update(bEntry.DisplayName.Split('/').Last());
                            TestBundle(App.AssetManager.GetBundleId(bEntry));
                        }
                        task.Update(progress: ((double)idx++ / (double)length) * 100.0d);
                    }
                    AbmDependenciesCache.UpdateCache();
                    App.Logger.Log("Checked all sublevel bundles");
                });
            }

            void TestBundle(int bunId)
            {
                Dictionary<AssetEntry, bool> loadedAssets = new Dictionary<AssetEntry, bool>();
                App.AssetManager.EnumerateEbx().Where(refEntry => refEntry.IsInBundle(bunId)).ToList().ForEach(refEntry => loadedAssets.Add(refEntry, false));
                App.AssetManager.EnumerateRes().Where(refEntry => refEntry.IsInBundle(bunId)).ToList().ForEach(refEntry => loadedAssets.Add(refEntry, false));
                App.AssetManager.EnumerateChunks().Where(refEntry => refEntry.IsInBundle(bunId)).ToList().ForEach(refEntry => loadedAssets.Add(refEntry, false));

                void DetectDependencies(EbxAssetEntry parEntry)
                {
                    if ((parEntry.Type != "ShaderGraph" && !parEntry.IsInBundle(bunId)) || (loadedAssets.ContainsKey(parEntry) && loadedAssets[parEntry]))
                        return;
                    if (loadedAssets.ContainsKey(parEntry))
                        loadedAssets[parEntry] = true;

                    DependencyData dependencies = AbmDependenciesCache.GetDependencies(parEntry);
                    foreach(EbxAssetEntry ebxEntry in dependencies.ebxRefs)
                        if (ebxEntry != parEntry)
                            DetectDependencies(ebxEntry);
                    foreach (ResAssetEntry resEntry in dependencies.resRefs)
                        if (loadedAssets.ContainsKey(resEntry))
                            loadedAssets[resEntry] = true;
                    foreach (ChunkAssetEntry chkEntry in dependencies.chkRefs)
                        if (loadedAssets.ContainsKey(chkEntry))
                            loadedAssets[chkEntry] = true;
                }
                void AddIfExists(string refName)
                {
                    EbxAssetEntry parEntry = App.AssetManager.GetEbxEntry(refName);
                    if (parEntry == null || !loadedAssets.ContainsKey(parEntry))
                        return;
                    loadedAssets[parEntry] = true;
                }

                string bunName = App.AssetManager.GetBundleEntry(bunId).Name;
                string bunBlueprintName = bunName.Replace("win32/", "");

                DetectDependencies(App.AssetManager.GetEbxEntry(bunBlueprintName));
                AddIfExists(bunBlueprintName + "/MeshVariationDb_Win32");
                AddIfExists(bunBlueprintName + "_networkregistry_Win32");

                //App.Logger.Log($"{bunName}:\tDetected {loadedAssets.Where(pair => pair.Value).Count()}/{loadedAssets.Count()} assets ({(float)((float)loadedAssets.Where(pair => pair.Value).Count()/ (float)loadedAssets.Count())*100}%)");

                //App.Logger.Log("\r\nEbx:\r\n");
                //foreach (EbxAssetEntry refEntry in loadedAssets.Where(refEntry => refEntry.Key.GetType() == typeof(EbxAssetEntry)).Select(refEntry => (EbxAssetEntry)refEntry.Key).ToList())
                //    App.Logger.Log((loadedAssets[refEntry] ? "Detected:\t" : "Missing:\t") + refEntry.Name);

                //App.Logger.Log("\r\nRes:\r\n");
                //foreach (ResAssetEntry refEntry in loadedAssets.Where(refEntry => refEntry.Key.GetType() == typeof(ResAssetEntry)).Select(refEntry => (ResAssetEntry)refEntry.Key).ToList())
                //    App.Logger.Log((loadedAssets[refEntry] ? "Detected:\t" : "Missing:\t") + refEntry.Name);

                //App.Logger.Log("\r\nChunk:\r\n");
                //foreach (ChunkAssetEntry refEntry in loadedAssets.Where(refEntry => refEntry.Key.GetType() == typeof(ChunkAssetEntry)).Select(refEntry => (ChunkAssetEntry)refEntry.Key).ToList())
                //    App.Logger.Log((loadedAssets[refEntry] ? "Detected:\t" : "Missing:\t") + refEntry.Name);

                string filePath = $@"{App.FileSystem.CacheName}/AutoBundleManager/TestLogs/{App.AssetManager.GetBundleEntry(bunId).DisplayName.Split('/').Last()}.csv";

                //loadedAssets[App.AssetManager.GetEbxEntry(bunBlueprintName)] = false;
                if (loadedAssets.Where(pair => pair.Value).Count() == loadedAssets.Count())
                    return;
                App.Logger.Log($"{bunName}:\tDetected {loadedAssets.Where(pair => pair.Value).Count()}/{loadedAssets.Count()} assets ({(float)((float)loadedAssets.Where(pair => pair.Value).Count() / (float)loadedAssets.Count()) * 100}%)");

                // Create a StreamWriter to write to the CSV file
                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("AssetType, Type, AssetName,Detected");
                    // Write Ebx section
                    foreach (EbxAssetEntry refEntry in loadedAssets.Where(refEntry => refEntry.Key.GetType() == typeof(EbxAssetEntry)).Select(refEntry => (EbxAssetEntry)refEntry.Key))
                        writer.WriteLine($"{refEntry.AssetType}, {refEntry.Type}, {refEntry.Name}, {loadedAssets[refEntry]}");

                    // Write Res section
                    foreach (ResAssetEntry refEntry in loadedAssets.Where(refEntry => refEntry.Key.GetType() == typeof(ResAssetEntry)).Select(refEntry => (ResAssetEntry)refEntry.Key))
                        writer.WriteLine($"{refEntry.AssetType}, {refEntry.Type}, {refEntry.Name} ({refEntry.ResRid}), {loadedAssets[refEntry]}");

                    // Write Chunk section
                    foreach (ChunkAssetEntry refEntry in loadedAssets.Where(refEntry => refEntry.Key.GetType() == typeof(ChunkAssetEntry)).Select(refEntry => (ChunkAssetEntry)refEntry.Key))
                        writer.WriteLine($"{refEntry.AssetType}, {refEntry.Type}, {refEntry.Name}, {loadedAssets[refEntry]}");
                }

                // Logging that CSV file has been written
                App.Logger.Log("CSV file has been written to: " + filePath);
            }
        }
    }
}
