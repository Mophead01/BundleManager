using AutoBundleManagerPlugin;
using Frosty.Core;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;

namespace AutoBundleManagerPlugin
{
    public class AbmTestModule
    {
        public class AbmTestFunctions
        {
            public AbmTestFunctions()
            {

            }
            public void BlueprintBundleTest(bool depenTest = true)
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
                            if (depenTest)
                                TestBundleDepdenecyAccuracy(App.AssetManager.GetBundleId(bEntry));
                            else
                                TestBundleParentAccuracy(App.AssetManager.GetBundleId(bEntry));
                        }
                        task.Update(progress: idx++ / (double)length * 100.0d);
                        //if (idx > 150)
                        //    break;
                    }
                    AbmDependenciesCache.UpdateCache();
                    App.Logger.Log("Checked all bpbs");
                });
            }

            public void SublevelTest(bool depenTest = true)
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
                            if (depenTest)
                                TestBundleDepdenecyAccuracy(App.AssetManager.GetBundleId(bEntry));
                            else
                                TestBundleParentAccuracy(App.AssetManager.GetBundleId(bEntry));
                        }
                        task.Update(progress: idx++ / (double)length * 100.0d);
                    }
                    AbmDependenciesCache.UpdateCache();
                    App.Logger.Log("Checked all sublevel bundles");
                });
            }

            //Testing if the bundle manager can find all assets within existing bundles
            void TestBundleDepdenecyAccuracy(int bunId)
            {
                Dictionary<AssetEntry, bool> loadedAssets = new Dictionary<AssetEntry, bool>();
                App.AssetManager.EnumerateEbx().Where(refEntry => refEntry.IsInBundle(bunId)).ToList().ForEach(refEntry => loadedAssets.Add(refEntry, false));
                App.AssetManager.EnumerateRes().Where(refEntry => refEntry.IsInBundle(bunId)).ToList().ForEach(refEntry => loadedAssets.Add(refEntry, false));
                App.AssetManager.EnumerateChunks().Where(refEntry => refEntry.IsInBundle(bunId)).ToList().ForEach(refEntry => loadedAssets.Add(refEntry, false));

                void DetectDependencies(EbxAssetEntry parEntry)
                {
                    if (parEntry.Type != "ShaderGraph" && !parEntry.IsInBundle(bunId) || loadedAssets.ContainsKey(parEntry) && loadedAssets[parEntry])
                        return;
                    if (loadedAssets.ContainsKey(parEntry))
                        loadedAssets[parEntry] = true;

                    DependencyActiveData dependencies = AbmDependenciesCache.GetDependencies(parEntry);
                    foreach (EbxAssetEntry ebxEntry in dependencies.ebxRefs)
                        if (ebxEntry != parEntry)
                            DetectDependencies(ebxEntry);
                    foreach (ResAssetEntry resEntry in dependencies.resRefs)
                        if (loadedAssets.ContainsKey(resEntry))
                            loadedAssets[resEntry] = true;
                    foreach (ChunkAssetEntry chkEntry in dependencies.chkRefs.Select(chkPair => chkPair.Key))
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

                string filePath = $@"{App.FileSystem.CacheName}/AutoBundleManager/DependencyTestLogs/{App.AssetManager.GetBundleEntry(bunId).DisplayName.Split('/').Last()}.csv";

                //loadedAssets[App.AssetManager.GetEbxEntry(bunBlueprintName)] = false;
                if (loadedAssets.Where(pair => pair.Value).Count() == loadedAssets.Count())
                    return;
                App.Logger.Log($"{bunName}:\tDetected {loadedAssets.Where(pair => pair.Value).Count()}/{loadedAssets.Count()} assets ({(float)(loadedAssets.Where(pair => pair.Value).Count() / (float)loadedAssets.Count()) * 100}%)");

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

            //Testing if the bundle heap knows all parents which existing bundles have.
            void TestBundleParentAccuracy(int bunId)
            {
                List<int> bunParIds = AbmBundleHeap.Bundles[bunId].EnumerateParentBundleIds().ToList();

                HashSet<AssetEntry> readAssets = new HashSet<AssetEntry>();
                HashSet<AssetEntry> inaccurateAssets = new HashSet<AssetEntry>();
                void DetectDependencies(EbxAssetEntry parEntry)
                {
                    DependencyActiveData dependencies = AbmDependenciesCache.GetDependencies(parEntry);
                    if (readAssets.Contains(parEntry))
                        return;
                    readAssets.Add(parEntry);

                    if (parEntry.IsInBundle(bunId))
                    {
                        foreach (EbxAssetEntry ebxEntry in dependencies.ebxRefs)
                            DetectDependencies(ebxEntry);
                        foreach (ResAssetEntry resEntry in dependencies.resRefs)
                            if (!parEntry.IsInBundleHeap(bunId, bunParIds))
                            {
                                inaccurateAssets.Add(resEntry);
                                readAssets.Add(resEntry);
                            }
                        foreach (ChunkAssetEntry chkEntry in dependencies.chkRefs.Select(chkPair => chkPair.Key))
                            if (chkEntry.Bundles.Count() != 0 && !chkEntry.IsInBundleHeap(bunId, bunParIds))
                            {
                                inaccurateAssets.Add(chkEntry);
                                readAssets.Add(chkEntry);
                            }
                    }
                    else if (!parEntry.IsInBundleHeap(bunId, bunParIds))
                        inaccurateAssets.Add(parEntry);
                }

                string bunName = App.AssetManager.GetBundleEntry(bunId).Name;
                string bunBlueprintName = bunName.Replace("win32/", "");

                DetectDependencies(App.AssetManager.GetEbxEntry(bunBlueprintName));


                string filePath = $@"{App.FileSystem.CacheName}/AutoBundleManager/ParentTestLogs/{App.AssetManager.GetBundleEntry(bunId).DisplayName.Split('/').Last()}.csv";

                if (inaccurateAssets.Count() == 0)
                    return;
                App.Logger.Log($"{bunName}:\t{inaccurateAssets.Count()} inaccurate asset parents detected.");

                // Create a StreamWriter to write to the CSV file
                if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                using (StreamWriter writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("AssetType, Type, AssetName, Bundles");
                    // Write Ebx section
                    foreach (EbxAssetEntry refEntry in inaccurateAssets.Where(refEntry => refEntry.GetType() == typeof(EbxAssetEntry)).Select(refEntry => (EbxAssetEntry)refEntry))
                        writer.WriteLine($"{refEntry.AssetType}, {refEntry.Type}, {refEntry.Name}, {string.Join("$", refEntry.EnumerateBundles().Select(refBunId => App.AssetManager.GetBundleEntry(refBunId).Name))})");

                    // Write Res section
                    foreach (ResAssetEntry refEntry in inaccurateAssets.Where(refEntry => refEntry.GetType() == typeof(ResAssetEntry)).Select(refEntry => (ResAssetEntry)refEntry))
                        writer.WriteLine($"{refEntry.AssetType}, {refEntry.Type}, {refEntry.Name}, {string.Join("$", refEntry.EnumerateBundles().Select(refBunId => App.AssetManager.GetBundleEntry(refBunId).Name))})");

                    // Write Chunk section
                    foreach (ChunkAssetEntry refEntry in inaccurateAssets.Where(refEntry => refEntry.GetType() == typeof(ChunkAssetEntry)).Select(refEntry => (ChunkAssetEntry)refEntry))
                        writer.WriteLine($"{refEntry.AssetType}, {refEntry.Type}, {refEntry.Name}, {string.Join("$", refEntry.EnumerateBundles().Select(refBunId => App.AssetManager.GetBundleEntry(refBunId).Name))})");
                }

                // Logging that CSV file has been written
                App.Logger.Log("CSV file has been written to: " + filePath);
            }

            //Testing the bundle manager's ability to correctly assign H32/FirstMip to assets by comparing them to a cache generated from 1.0.7. NOTE: Kyber mod loader is supposed to be doing Firstmip/H32 automatically so this is only necessary for Frosty most likely
            public void TestFirstMipH32Accuracy(FrostyTaskWindow task)
            {
                task.Update("Discovering basic info");
                Dictionary<ChunkAssetEntry, (int, int)> chunkH32Cached = new Dictionary<ChunkAssetEntry, (int, int)>();
                using (NativeReader reader = new NativeReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("AutoBundleManager.Data.Swbf2_H32FirstMip.cache")))
                {
                    int chunkCount = reader.ReadInt();
                    for (int i = 0; i < chunkCount; i++)
                        chunkH32Cached.Add(App.AssetManager.GetChunkEntry(reader.ReadGuid()), (reader.ReadInt(), reader.ReadInt()));
                }
                Dictionary<uint, EbxAssetEntry> h32HashesEbx = new Dictionary<uint, EbxAssetEntry>();
                foreach (EbxAssetEntry refEntry in App.AssetManager.EnumerateEbx())
                {
                    if (refEntry.Name.EndsWith("/MeshVariationDb_Win32"))
                        continue;
                    uint hash = (uint)Utils.HashString(refEntry.Name, true);
                    if (h32HashesEbx.ContainsKey(hash))
                        App.Logger.LogWarning($"Duplicate Ebx Hash:\t{hash}\tAsset 1:\t{h32HashesEbx[hash].Name}\tAsset 2:\t{refEntry.Name}");
                    else
                        h32HashesEbx.Add(hash, refEntry);
                }

                Dictionary<uint, ResAssetEntry> h32HashesRes = new Dictionary<uint, ResAssetEntry>();
                foreach (ResAssetEntry resEntry in App.AssetManager.EnumerateRes())
                {
                    uint hash = (uint)Utils.HashString(resEntry.Name, true);
                    if (h32HashesRes.ContainsKey(hash))
                        App.Logger.LogWarning($"Duplicate Res Hash:\t{hash}\tAsset 1:\t{h32HashesRes[hash].Name}\tAsset 2:\t{resEntry.Name}");
                    else
                        h32HashesRes.Add(hash, resEntry);
                }

                Dictionary<string, int> ebxH32Types = new Dictionary<string, int>();
                Dictionary<string, int> resH32Types = new Dictionary<string, int>();
                Dictionary<string, int> ebxFirstMipTypes = new Dictionary<string, int>();
                Dictionary<string, int> resFirstMipTypes = new Dictionary<string, int>();
                void TryAddDictionary(Dictionary<string, int> dict, string key)
                {
                    if (dict.ContainsKey(key))
                        dict[key]++;
                    else
                        dict[key] = 1;
                }
                Dictionary<ChunkAssetEntry, EbxAssetEntry> chkToEbx = new Dictionary<ChunkAssetEntry, EbxAssetEntry>();
                List<(ChunkAssetEntry, string)> missingChunkEbx = new List<(ChunkAssetEntry, string)>();
                foreach (ChunkAssetEntry chkEntry in chunkH32Cached.Keys)
                {
                    uint h32Hash = (uint)chunkH32Cached[chkEntry].Item2;
                    int firstMip = chunkH32Cached[chkEntry].Item1;
                    if (h32Hash == 0)
                        continue;
                    if (!h32HashesEbx.ContainsKey(h32Hash) && !h32HashesRes.ContainsKey(h32Hash))
                        App.Logger.LogError($"Could not find h32 asset for: {chkEntry.Name}\t({h32Hash})");
                    if (h32HashesEbx.ContainsKey(h32Hash))
                    {
                        chkToEbx.Add(chkEntry, h32HashesEbx[h32Hash]);
                        //if (h32HashesEbx[h32Hash].Type == "AntStateAsset")
                        //{
                        //    App.Logger.Log(h32HashesRes.ContainsKey(h32Hash).ToString());
                        //    App.Logger.Log(h32HashesEbx[h32Hash].Name);
                        //    App.Logger.Log((App.AssetManager.GetResEntry(h32HashesEbx[h32Hash].Name) != null).ToString());
                        //}
                        TryAddDictionary(ebxH32Types, h32HashesEbx[h32Hash].Type);
                        if (firstMip != -1)
                            TryAddDictionary(ebxFirstMipTypes, h32HashesEbx[h32Hash].Type);
                    }
                    else
                        missingChunkEbx.Add((chkEntry, h32HashesRes[h32Hash].Type));
                    if (h32HashesRes.ContainsKey(h32Hash))
                    {
                        //string test = h32HashesEbx[h32Hash].Name;
                        TryAddDictionary(resH32Types, h32HashesRes[h32Hash].Type);
                        if (firstMip != -1)
                            TryAddDictionary(resFirstMipTypes, h32HashesRes[h32Hash].Type);
                    }
                }

                string printToLog = "\r\nEbx H32 Types:\r\n";
                foreach (KeyValuePair<string, int> pair in ebxH32Types)
                    printToLog += $"{pair.Key} ({pair.Value})\n";

                printToLog += "\r\nRes H32 Types:\r\n";
                foreach (KeyValuePair<string, int> pair in resH32Types)
                    printToLog += $"{pair.Key} ({pair.Value})\n";

                printToLog += "\r\nEbx FirstMip Types:\r\n";
                foreach (KeyValuePair<string, int> pair in ebxFirstMipTypes)
                    printToLog += $"{pair.Key} ({pair.Value})\n";

                printToLog += "\r\nRes FirstMip Types:\r\n";
                foreach (KeyValuePair<string, int> pair in resFirstMipTypes)
                    printToLog += $"{pair.Key} ({pair.Value})\n";

                //printToLog += "\r\nMissing chunk ebx:\r\n";
                //foreach ((ChunkAssetEntry, string) chkPairs in missingChunkEbx)
                //    printToLog += $"{chkPairs.Item1.Name}\t{chkPairs.Item2}\n";

                //printToLog += "\r\nRes AssetBanks:\r\n";
                //foreach (string assetBank in h32HashesRes.Values.Where(str => str.EndsWith("_antstate")))
                //    printToLog += $"{assetBank} ({App.AssetManager.GetResEntry(assetBank).Type})\t{h32HashesRes.ContainsKey((uint)Utils.HashString(assetBank))}\n";
                App.Logger.Log(printToLog);








                using (StreamWriter writer = new StreamWriter($@"{App.FileSystem.CacheName}/AutoBundleManager/DependencyTestLogs/ChunkH32Detection.csv"))
                {
                    writer.WriteLine("EbxName,EbxType,ChunkId");

                    task.Update("Checking chunk detection");
                    int count = chkToEbx.Count;
                    int idx = 0;
                    foreach (KeyValuePair<ChunkAssetEntry, EbxAssetEntry> chkEbxPair in chkToEbx)
                    {
                        ChunkAssetEntry targChunk = chkEbxPair.Key;
                        EbxAssetEntry targEbx = chkEbxPair.Value;
                        DependencyActiveData dependencies = AbmDependenciesCache.GetDependencies(targEbx);
                        if (!dependencies.chkRefs.ContainsKey(targChunk))
                            writer.WriteLine($"{targEbx.Name},{targEbx.Type},{targChunk.Name}");
                        task.Update(progress: idx++ / (float)count * 100.0f);
                    }
                }
                AbmDependenciesCache.UpdateCache();

                using (StreamWriter writer = new StreamWriter($@"{App.FileSystem.CacheName}/AutoBundleManager/DependencyTestLogs/ChunkFirstMipDetection.csv"))
                {
                    writer.WriteLine("EbxName,EbxType,ChunkId,CacheFirstMip,RealFirstMip");

                    task.Update("Checking chunk detection");
                    int count = chunkH32Cached.Where(pair => pair.Value.Item1 != -1).ToList().Count;
                    int idx = 0;
                    foreach (KeyValuePair<ChunkAssetEntry, (int, int)> chkEbxPair in chunkH32Cached.Where(pair => pair.Value.Item1 != -1).ToList())
                    {
                        ChunkAssetEntry targChunk = chkEbxPair.Key;
                        EbxAssetEntry targEbx = chkToEbx[chkEbxPair.Key];
                        DependencyActiveData dependencies = AbmDependenciesCache.GetDependencies(targEbx);
                        if (dependencies.chkRefs.ContainsKey(targChunk))
                        {
                            if (dependencies.chkRefs[targChunk] != chkEbxPair.Value.Item1)
                                writer.WriteLine($"{targEbx.Name},{targEbx.Type},{targChunk.Name},{dependencies.chkRefs[targChunk]},{chkEbxPair.Value.Item1}");
                        }
                        task.Update(progress: idx++ / (float)count * 100.0f);
                    }
                }
                HashSet<ChunkAssetEntry> undiscoveredChunks = new HashSet<ChunkAssetEntry>(App.AssetManager.EnumerateChunks().ToList());

                HashSet<string> ebxTypesWithChunksThatWerentDiscovered = new HashSet<string>();
                using (StreamWriter writer = new StreamWriter($@"{App.FileSystem.CacheName}/AutoBundleManager/DependencyTestLogs/ChunkGameWideDetection.csv"))
                {
                    writer.WriteLine("ChunkId,Bundles");

                    task.Update("Checking chunk detection");
                    int count = App.AssetManager.EnumerateEbx().ToList().Count;
                    int idx = 0;
                    foreach (EbxAssetEntry parEntry in App.AssetManager.EnumerateEbx())
                    {
                        task.Update(progress: idx++ / (float)count * 100.0f);
                        //if (!ebxH32Types.ContainsKey(parEntry.Type))
                        //    continue;
                        DependencyActiveData dependencies = AbmDependenciesCache.GetDependencies(parEntry);
                        if (!ebxH32Types.ContainsKey(parEntry.Type) && dependencies.chkRefs.Count() > 0)
                            ebxTypesWithChunksThatWerentDiscovered.Add(parEntry.Type);
                        foreach (ChunkAssetEntry chkEntry in dependencies.chkRefs.Keys)
                            if (undiscoveredChunks.Contains(chkEntry))
                                undiscoveredChunks.Remove(chkEntry);
                    }
                    foreach (ChunkAssetEntry undiscoveredChunk in undiscoveredChunks)
                        writer.WriteLine($"{undiscoveredChunk.Name},{string.Join("$", undiscoveredChunk.Bundles.Select(bunId => App.AssetManager.GetBundleEntry(bunId).Name))}");
                }
                AbmDependenciesCache.UpdateCache();
                foreach (string ebxType in ebxTypesWithChunksThatWerentDiscovered)
                    App.Logger.Log(ebxType);
            }
        }
    }
}
