using Frosty.Core;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBundleManagerPlugin
{
    public class BundleCompleter
    {
        private Stopwatch stopWatch = new Stopwatch();
        private long lastTimestamp = 0;
        private StringBuilder LogList = new StringBuilder();
        private void AddToLog(string type, string description, string parent, string child)
        {
            LogList.AppendLine(type + ", \t" + description + ", \t" + parent + ", \t" + child + ", \t" + (stopWatch.ElapsedMilliseconds - lastTimestamp).ToString());
            lastTimestamp = stopWatch.ElapsedMilliseconds;
        }
        Dictionary<AssetEntry, DependencyActiveData> loggedDependencies = new Dictionary<AssetEntry, DependencyActiveData>();
        public BundleCompleter(FrostyTaskWindow task, AssetManager AM, string fbmodName,List<string> loadOrder) 
        {
            stopWatch.Start();

            if (!loadOrder.Contains(fbmodName))
                loadOrder.Add(fbmodName);

            //
            //  Clear Existing Bundle Edits from primary project
            //

            //
            // Get data from modified files and check to see if they modify the bundle heap, if so make changes
            //
            foreach (EbxAssetEntry parEntry in AM.EnumerateEbx()) 
            {
                if (!parEntry.HasModifiedData && !parEntry.IsImaginary)
                    continue;
                AddToLog("Caching", "Getting Dependencies", parEntry.Name, parEntry.GetSha1().ToString());
                DependencyActiveData dependencies = AbmDependenciesCache.GetDependencies(parEntry);

                AddToLog("Caching", loggedDependencies.ContainsKey(parEntry) ? "Overriding Logged Data" : "Logging Data", parEntry.Name, $" \t{dependencies.ebxRefs.Count()} Ebx Refs$ \t{dependencies.resRefs.Count()} Res Refs$ \t{dependencies.chkRefs.Count()} Chunks Refs$ \t" +
                    $"{dependencies.bundleReferences.Count()} Bundle Refs$ \t{dependencies.networkRegistryRefGuids.Count()} Net Reg Refs$ \t{(dependencies.meshVariEntry == null ? "No MeshVariation" : "Includes MeshVariation")}");
                if (loggedDependencies.ContainsKey(parEntry))
                    loggedDependencies[parEntry] = dependencies;
                else
                    loggedDependencies.Add(parEntry, dependencies);
            }

            //
            //  List of modified assets for each bundle which we will enumerate over later.
            //

            Dictionary<int, List<EbxAssetEntry>> bundlesModifiedAssets = new Dictionary<int, List<EbxAssetEntry>>();

            foreach(KeyValuePair<AssetEntry, DependencyActiveData> pairDependancy in loggedDependencies)
            {
                if (pairDependancy.Key.AssetType != "ebx")
                    continue;
                EbxAssetEntry parEntry = (EbxAssetEntry)pairDependancy.Key;
                foreach(int bunId in parEntry.EnumerateBundles())
                {
                    if (bundlesModifiedAssets.ContainsKey(bunId))
                        bundlesModifiedAssets[bunId].Add(parEntry);
                    else
                        bundlesModifiedAssets.Add(bunId, new List<EbxAssetEntry> { parEntry });
                }
                foreach (KeyValuePair<string, HashSet<string>> bunParents in pairDependancy.Value.bundleReferences)
                {
                    int bunId = AM.GetBundleId("win32/" + bunParents.Key);
                    if (bunId != -1)
                    {
                        BundleHeapEntry heapEntry = AbmBundleHeap.Bundles[bunId];
                        foreach(string bunParStr in bunParents.Value)
                        {
                            int bunParId = AM.GetBundleId("win32/" + bunParStr);
                            if (bunParId != -1 && !heapEntry.EnumerateParentBundleIds(false).Contains(bunParId))
                                heapEntry.CustomParentIds.Add(bunParId);
                        }
                        AddToLog("Caching", "Modifying Bundle Hierarchy", bunParents.Key, string.Join(", ", bunParents.Value));
                    }
                }
            }

            //
            //  Verify modder hasn't fucked up and created an infinite bundle loop (avoiding stack overflows later)
            //
            if (AbmBundleHeap.VerifyHeapIntegrity(true))
                return;

            //
            //  Planning bundle order for enumeration
            //
            List<int> bundleOrder = new List<int>();
            foreach(int bunId in bundlesModifiedAssets.Keys)
            {
                if (bundleOrder.Contains(bunId))
                    continue;
                BundleHeapEntry heapEntry = AbmBundleHeap.Bundles[bunId];
                foreach(int parBunId in heapEntry.EnumerateParentBundleIds().Reverse())
                {
                    if (!bundleOrder.Contains(parBunId))
                    {
                        bundleOrder.Add(parBunId);
                        AddToLog("Planning Enumeration", "Adding Bundle To Order", App.AssetManager.GetBundleEntry(parBunId).Name, App.AssetManager.GetBundleEntry(bunId).Name);
                        //App.Logger.Log(App.AssetManager.GetBundleEntry(parBunId).Name);
                    }
                }
                bundleOrder.Add(bunId);
                //App.Logger.Log(App.AssetManager.GetBundleEntry(bunId).Name);
                //App.Logger.Log("\r\n");
            }

            //
            //  Enumerating Bundles
            //

            foreach (int bunId in bundleOrder)
            {
                BundleEntry bEntry = AM.GetBundleEntry(bunId);
                string bunName = bEntry.Name;
                AddToLog("Completing Bundle", "Starting Enumeration Of Bundle", bunName, $"{(bundlesModifiedAssets.ContainsKey(bunId) ? bundlesModifiedAssets[bunId].Count() : 0)} Assets");
                BundleHeapEntry heapEntry = AbmBundleHeap.Bundles[bunId];
                List<int> parentBunIds = heapEntry.EnumerateParentBundleIds().ToList();

                List<AbmMeshVariationDatabaseEntry> meshVariationsToAdd = new List<AbmMeshVariationDatabaseEntry>();
                List<PointerRef> netRegPointerRefsToAdd = new List<PointerRef>();
                void AddDependenciesToBundle(EbxAssetEntry parEntry, bool addSelfToRegistries = true)
                {
                    DependencyActiveData dependencies = loggedDependencies.ContainsKey(parEntry) ? loggedDependencies[parEntry] : AbmDependenciesCache.GetDependencies(parEntry);
                    foreach (EbxAssetEntry ebxEntry in dependencies.ebxRefs)
                    {
                        bool addToBundle = !ebxEntry.IsInBundleHeap(bunId, parentBunIds);
                        if (addToBundle)
                            ebxEntry.AddToBundle(bunId);
                        if (addToBundle || ebxEntry.Type == "ShaderGraph")
                            AddDependenciesToBundle(ebxEntry, addToBundle);
                    }
                    foreach (ResAssetEntry resEntry in dependencies.resRefs)
                    {
                        if (!resEntry.IsInBundleHeap(bunId, parentBunIds))
                        {
                            resEntry.AddToBundle(bunId);
                            if (AbmMeshVariationDatabasePrecache.VariationMvdbDatabase.ContainsKey(resEntry.Name))
                                meshVariationsToAdd.Add(AbmMeshVariationDatabasePrecache.VariationMvdbDatabase[resEntry.Name]);
                        }
                    }
                    foreach (KeyValuePair<ChunkAssetEntry, int> chkPair in dependencies.chkRefs)
                    {
                        ChunkAssetEntry chkEntry = chkPair.Key;
                        if (!chkEntry.IsInBundleHeap(bunId, parentBunIds))
                        {
                            chkEntry.AddToBundle(bunId);
                            chkEntry.FirstMip = chkPair.Value;
                            chkEntry.H32 = (int)Utils.HashString(parEntry.Name, true);
                        }
                    }
                    if (addSelfToRegistries)
                    {
                        if (dependencies.meshVariEntry != null)
                            meshVariationsToAdd.Add(dependencies.meshVariEntry);
                        foreach (Guid classGuid in dependencies.networkRegistryRefGuids)
                            netRegPointerRefsToAdd.Add(new PointerRef(new EbxImportReference() { FileGuid = dependencies.srcGuid, ClassGuid = classGuid }));
                    }
                }

                //Enumerate Modified Assets and add to bundles
                if (bundlesModifiedAssets.ContainsKey(bunId))
                {
                    foreach (EbxAssetEntry parEntry in bundlesModifiedAssets[bunId])
                        AddDependenciesToBundle(parEntry, false);
                }

                //Add MeshVariationDatabase Entries
                if (meshVariationsToAdd.Count() > 0)
                {
                    string mvdbName = bEntry.Name.ToLower().Substring(6) + "/MeshVariationDb_Win32";
                    EbxAssetEntry mvdbEntry = AM.GetEbxEntry(mvdbName);
                    EbxAsset mvdbAsset;
                    if (mvdbEntry == null || mvdbEntry.IsImaginary)
                    {
                        if (mvdbEntry != null)
                            AM.RevertAsset(mvdbEntry);

                        EbxAsset newAsset = new EbxAsset(TypeLibrary.CreateObject("MeshVariationDatabase"));
                        newAsset.SetFileGuid(Guid.NewGuid());

                        dynamic obj = newAsset.RootObject;
                        obj.Name = mvdbName;

                        AssetClassGuid guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(newAsset.Objects, (Type)obj.GetType(), newAsset.FileGuid), -1);
                        obj.SetInstanceGuid(guid);

                        EbxAssetEntry newEntry = AM.AddEbx(mvdbName, newAsset);
                        if (newEntry.ModifiedEntry == null)
                            throw new Exception(string.Format("App.AssetManager.AddEbx() failed to create new MeshVariationDatabase EbxAsset for bundle: {0}\nUnknown cause and how to fix", App.AssetManager.GetBundleEntry(bunId).Name));

                        newEntry.AddedBundles.Add(bunId);
                        newEntry.ModifiedEntry.DependentAssets.AddRange(newAsset.Dependencies);
                        mvdbAsset = App.AssetManager.GetEbx(newEntry);
                        //LogString("Bundle", "Adding MeshVariationDB", newMeshVariName, NewMeshVariationDbEntries.Count.ToString() + " entries added");
                    }
                    else
                        mvdbAsset = AM.GetEbx(mvdbEntry);

                    dynamic mvdbRoot = mvdbAsset.RootObject;
                    foreach(AbmMeshVariationDatabaseEntry mvEntryAbm in meshVariationsToAdd)
                        mvdbRoot.Entries.Add(mvEntryAbm.WriteToGameEntry());

                    AM.ModifyEbx(mvdbName, mvdbAsset);
                }

                //Add Network Registry Objects
                if (netRegPointerRefsToAdd.Count > 0)
                {

                    string netregName = AM.GetBundleEntry(bunId).Name.ToLower().Substring(6) + "_networkregistry_Win32";
                    EbxAssetEntry netregEntry = AM.GetEbxEntry(netregName);
                    EbxAsset netregAsset;
                    if (netregEntry == null || netregEntry.IsImaginary)
                    {
                        if (netregEntry != null)
                            AM.RevertAsset(netregEntry);

                        EbxAsset newAsset = new EbxAsset(TypeLibrary.CreateObject("NetworkRegistryAsset"));
                        newAsset.SetFileGuid(Guid.NewGuid());

                        dynamic obj = newAsset.RootObject;
                        obj.Name = netregName;

                        AssetClassGuid guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(newAsset.Objects, (Type)obj.GetType(), newAsset.FileGuid), -1);
                        obj.SetInstanceGuid(guid);

                        EbxAssetEntry newEntry = AM.AddEbx(netregName, newAsset);
                        if (newEntry.ModifiedEntry == null)
                            throw new Exception(string.Format("App.AssetManager.AddEbx() failed to create new NetworkRegistryAsset EbxAsset for bundle: {0}\nUnknown cause and how to fix", App.AssetManager.GetBundleEntry(bunId).Name));

                        newEntry.AddedBundles.Add(bunId);
                        newEntry.ModifiedEntry.DependentAssets.AddRange(newAsset.Dependencies);
                        netregAsset = App.AssetManager.GetEbx(newEntry);
                        //LogString("Bundle", "Adding NetworkRegistryAsset", newMeshVariName, NetworkRegistryAsset.Count.ToString() + " entries added");
                    }
                    else
                        netregAsset = AM.GetEbx(netregEntry);

                    dynamic netregRoot = netregAsset.RootObject;
                    foreach (PointerRef pr in netRegPointerRefsToAdd)
                        netregRoot.Objects.Add(pr);

                    AM.ModifyEbx(netregName, netregAsset);
                }

            }

            //
            //  Forced Bundle Edits
            //
            foreach(KeyValuePair<string, List<string>> forcedBundleEdit in AutoBundleManagerOptions.ForcedBundleEdits)
            {
                List<int> bunIds = forcedBundleEdit.Value.Where(bunName => AM.GetBundleId(bunName) != -1).Select(bunName => AM.GetBundleId((string)bunName)).ToList();
                EbxAssetEntry targEbx = AM.GetEbxEntry(forcedBundleEdit.Key);
                foreach(AssetEntry targEntry in new List<AssetEntry> { AM.GetEbxEntry(forcedBundleEdit.Key), AM.GetResEntry(forcedBundleEdit.Key.ToLower()) })
                {
                    if (targEntry != null)
                    {
                        foreach (int bunId in bunIds)
                        {
                            BundleHeapEntry heapEntry = AbmBundleHeap.Bundles[bunId];
                            if (!targEbx.IsInBundleHeap(bunId, heapEntry.EnumerateParentBundleIds().ToList()))
                                targEbx.AddToBundle(bunId);
                        }
                    }
                }
            }

            //
            // Post completion operations
            //

            try
            {
                using (NativeWriter writer = new NativeWriter(new FileStream($"{App.FileSystem.CacheName}/AutoBundleManager/Logger.csv", FileMode.Create)))
                {
                    writer.WriteLine("Type, Description, Parent, Child, Time Elapsed (MS)");
                    writer.WriteLine(LogList.ToString());
                }
            }
            catch
            {
                App.Logger.Log("Could not export file " + App.FileSystem.CacheName + "_BundleManager_LogList.csv");
            }

            AbmDependenciesCache.UpdateCache();
            stopWatch.Stop();
            App.Logger.Log(string.Format("Bundle Manager Completed in {0} seconds.", stopWatch.Elapsed));
        }

    }
}
