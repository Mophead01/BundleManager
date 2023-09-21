using AtlasTexturePlugin;
using Frosty.Controls;
using Frosty.Core;
using Frosty.Core.Viewport;
using Frosty.Core.Windows;
using Frosty.Hash;
using FrostySdk;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;
using MeshSetPlugin;
using MeshSetPlugin.Resources;
using RootInstanceEntiresPlugin;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;
using DuplicationPlugin;
using System.Windows.Documents;
using static DuplicationPlugin.DuplicationTool;

namespace BundleManager
{
    internal class BundleManager
    {
        #region Base Classes and public voids
        private FrostyTaskWindow task;
        private AssetManager AM
        {
            get { return App.AssetManager; }
        }
        private Random rnd = new Random();
        private Stopwatch stopWatch = new Stopwatch();
        private StringBuilder LogList = new StringBuilder(); //Logs changes so that they can be exported to a csv
        private long lastTimestamp = 0;

        private List<int> BundleOrder = new List<int>(); //The order of bundles that the Bundle Manager completes in (very important to get right)
        private Dictionary<int, BundleParentArrays> BundleParents = new Dictionary<int, BundleParentArrays>();
        private Dictionary<int, BM_BundleData> BundleDataDict = new Dictionary<int, BM_BundleData>();

        private Dictionary<EbxAssetEntry, List<EbxAssetEntry>> assetsToBM = new Dictionary<EbxAssetEntry, List<EbxAssetEntry>>();
        private Dictionary<EbxAssetEntry, List<ChunkAssetEntry>> soundwaveSpecialCase = new Dictionary<EbxAssetEntry, List<ChunkAssetEntry>>();
        private Dictionary<EbxAssetEntry, MeshVariData> meshassetSpecialCase = new Dictionary<EbxAssetEntry, MeshVariData>();
        Dictionary<EbxAssetEntry, Dictionary<EbxAssetEntry, MeshVariData>> ModifiedObjectVariations = new Dictionary<EbxAssetEntry, Dictionary<EbxAssetEntry, MeshVariData>>();

        private Dictionary<EbxAssetEntry, Dictionary<int, EbxAssetEntry>> mvdbsToUpdate = new Dictionary<EbxAssetEntry, Dictionary<int, EbxAssetEntry>>();

        private Dictionary<string, AssetLogger> loggerExtensions = new Dictionary<string, AssetLogger>();
        private Dictionary<EbxAssetEntry, AssetData> LoggedData = new Dictionary<EbxAssetEntry, AssetData>();

        private BundleManagerPrerequisites prerequisites = new BundleManagerPrerequisites();

        public BundleManager(FrostyTaskWindow Task) //Constructor which just loads the cache if it can
        {
            task = Task;
            loggerExtensions.Add("null", new AssetLogger());
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsSubclassOf(typeof(AssetLogger)))
                {
                    var extension = (AssetLogger)Activator.CreateInstance(type);
                    loggerExtensions.Add(extension.AssetType, extension);
                }
            }
            BundleParents = BmCache.BundleParents.ToDictionary(o => o.Key, o => new BundleParentArrays(o.Value, new List<int>()));
        }

        public void CompleteBundleManage(List<int> levelBundles = null)
        {
            stopWatch.Start();
            ClearBundleEdits();
            if (EstablishBundleLoadOrder())
            {
                List<int> AllowedBundles = GetAllowedBundles(levelBundles);
                FindNewDependencies(AllowedBundles);
                BundleEnumeration(AllowedBundles);
                ExportLog();
            }
            stopWatch.Stop();
            App.Logger.Log(string.Format("Bundle Manager Completed in {0} seconds.", stopWatch.Elapsed));
            CompletionMessage();
        }

       

        #endregion

        #region Stage 1 - Preparing Bundle Manager (Clearing existing edits, checking there are no bundle infinite loops and finding swbf2 bpb parent bundles)

        public void ClearBundleEdits() //Removes any existing bundle edits, reverts net reg and mvdb edits, and sets chunk firstmips to -1
        {
            App.WhitelistedBundles.Clear();
            //Make sure any added bundles have the correct .Blueprint assigned.
            foreach (BundleEntry newBunEntry in AM.EnumerateBundles().Where(blueEntry => blueEntry != null && blueEntry.Added && blueEntry.Type != BundleType.SharedBundle && blueEntry.Blueprint == null))
                newBunEntry.Blueprint = AM.GetEbxEntry(newBunEntry.Name.Replace("win32/", ""));

            List<EbxAssetEntry> bundleBlueprints = AM.EnumerateBundles().Select(bEntry => bEntry.Blueprint).Where(blueEntry => blueEntry != null && blueEntry.IsAdded).ToList();
            List<EbxAssetEntry> ebxEntries = AM.EnumerateEbx(modifiedOnly: true).ToList();
            foreach (EbxAssetEntry refEntry in ebxEntries)
            {
                if (bundleBlueprints.Contains(refEntry))
                    continue;
                if (refEntry.Type == null || refEntry.Type == "NetworkRegistryAsset" || refEntry.Type == "MeshVariationDatabase")
                    AM.RevertAsset(refEntry);
                else
                {
                    refEntry.AddedBundles.Clear();
                    if (!refEntry.HasModifiedData || !refEntry.ModifiedEntry.IsDirty)
                        refEntry.IsDirty = false;
                }
            }
            foreach (ChunkAssetEntry chkEntry in AM.EnumerateChunks(modifiedOnly: true))
            {
                chkEntry.AddedBundles.Clear();
                if (!chkEntry.IsAdded)
                    chkEntry.FirstMip = -1;
                if (!chkEntry.HasModifiedData || !chkEntry.ModifiedEntry.IsDirty)
                    chkEntry.IsDirty = false;
            }
            foreach (ResAssetEntry resEntry in AM.EnumerateRes(modifiedOnly: true))
            {
                resEntry.AddedBundles.Clear();
                if (!resEntry.HasModifiedData || !resEntry.ModifiedEntry.IsDirty)
                    resEntry.IsDirty = false;
            }
        }

        private void AddModdedParent(int bunId, int moddedParentBunId)
        {
            if (BundleParents.ContainsKey(bunId))
            {
                if (!BundleParents[bunId].moddedParents.Contains(moddedParentBunId) && !BundleParents[bunId].baseParents.Contains(moddedParentBunId)) 
                    BundleParents[bunId].moddedParents.Add(moddedParentBunId);
            }
            else
                BundleParents.Add(bunId, new BundleParentArrays(new List<int>(), new List<int>() { moddedParentBunId }));
        }
        private bool EstablishBundleLoadOrder(bool loadPrerequisites = true) //Verifies integrity of cached bundle hierarchy and checks if modder has modified the bundle load order (e.g. adding new bpb parent references in swbf2 vurs)
        {
            bool LoopFound = false;

            if (ProfilesLibrary.IsLoaded(ProfileVersion.StarWarsBattlefrontII))
            {
                foreach (EbxAssetEntry refEntry in App.AssetManager.EnumerateEbx(type: "VisualUnlockRootAsset"))
                {
                    if (refEntry.HasModifiedData)
                    {
                        dynamic refRoot = App.AssetManager.GetEbx(refEntry).RootObject;
                        foreach (dynamic BlueprintBundleReference in refRoot.ThirdPersonBundles)
                            CheckSwbf2VurBundle(BlueprintBundleReference, refEntry.Name);
                        foreach (dynamic BlueprintBundleReference in refRoot.FirstPersonBundles)
                            CheckSwbf2VurBundle(BlueprintBundleReference, refEntry.Name);
                        foreach (dynamic SkinInfo in refRoot.SkinInfos)
                        {
                            CheckSwbf2VurBundle(SkinInfo.ThirdPersonBundle, refEntry.Name);
                            CheckSwbf2VurBundle(SkinInfo.FirstPersonBundle, refEntry.Name);
                        }
                    }
                }
                foreach (EbxAssetEntry refEntry in App.AssetManager.EnumerateEbx(type: "SubWorldData"))
                {
                    if (refEntry.HasModifiedData)
                    {
                        dynamic refRoot = App.AssetManager.GetEbx(refEntry).RootObject;
                        foreach (dynamic pr in refRoot.Objects)
                        {
                            if (pr.Type == PointerRefType.Internal && pr.Internal.GetType().Name == "SubWorldReferenceObjectData")
                            {
                                string bunName = "win32/" + pr.Internal.BundleName;
                                int bunId = AM.GetBundleId(bunName);
                                if (bunId == -1)
                                    continue;
                                AddModdedParent(bunId, refEntry.EnumerateBundles().ToList()[0]);
                            }
                        }
                    }
                }
            }

            if (loadPrerequisites)
                LoadPrerequisites();

            foreach (BundleEntry bunEntry in AM.EnumerateBundles())
            {
                int bunID = AM.GetBundleId(bunEntry);
                if (!BundleDataDict.ContainsKey(bunID))
                {
                    if (SeekBundleParents(bunID, new List<int> { }) == false)
                    {
                        LoopFound = true;
                        break;
                    }
                }
            }
            return !LoopFound;
        }

        private void CheckSwbf2VurBundle(dynamic BlueprintBundleReference, string vurName) //Checks swbf2 vur for new bpb parent references
        {
            foreach (dynamic Par in BlueprintBundleReference.Parents)
            {
                if (Par.Name != "")
                {
                    if (CheckSwbf2VurBundleName(Par.Name, vurName) == true & CheckSwbf2VurBundleName(BlueprintBundleReference.Name, vurName) == true)
                        AddModdedParent(AM.GetBundleId("win32/" + BlueprintBundleReference.Name), AM.GetBundleId("win32/" + Par.Name));
                }
            }
        }
        private bool CheckSwbf2VurBundleName(string bunName, string vurName) //I can't be bothered to explain this
        {
            if (AM.GetBundleId("win32/" + bunName) != -1)
                return true;
            App.Logger.Log(string.Format("Warning: Bundle parent {0} in {1} does not exist", "win32/" + bunName, vurName));
            return false;
        }

        private bool SeekBundleParents(int bunID, List<int> prevBunIDs) //Verifies there are no bundle loops which could cause the bundle manager to never finish
        {
            if (!prevBunIDs.Contains(bunID))
            {
                prevBunIDs.Add(bunID);
                if (!BundleParents.ContainsKey(bunID))
                {
                    BundleOrder.Add(bunID);
                    BundleDataDict.Add(bunID, new BM_BundleData { Parents = new List<int>(), ModifiedAssets = new List<EbxAssetEntry>() });
                    if (Config.Get<bool>("BMO_EnableBundleLogExport", false))
                        LogString("Bundle", "Logging Parents", string.Format("{0} ({1})", AM.GetBundleEntry(bunID).Name, bunID), "0 parents (No cache data found)");
                    return true;
                }
                else
                {
                    List<int> ParentsList = new List<int>();
                    foreach (int bunParID in BundleParents[bunID].allParents)
                    {
                        if (bunID != bunParID)
                        {
                            if (!BundleDataDict.ContainsKey(bunParID))
                            {
                                if (SeekBundleParents(bunParID, prevBunIDs) == false)
                                    return false;
                            }
                            if (!ParentsList.Contains(bunParID))
                                ParentsList.Add(bunParID);
                            foreach (int bunGranParID in BundleDataDict[bunParID].Parents)
                            {
                                if (!ParentsList.Contains(bunGranParID))
                                    ParentsList.Add(bunGranParID);
                            }
                        }
                    }
                    if (ProfilesLibrary.IsLoaded(ProfileVersion.StarWarsBattlefrontII)) //Extreme case for swbf2. Not sure if this code is necessary anymore and I can't be bothered to check
                    {
                        if (AM.GetBundleEntry(bunID).Name == @"win32/S9_3/COOP_NT_FOSD/COOP_NT_FOSD" || AM.GetBundleEntry(bunID).Name == @"win32/S9_3/COOP_NT_MC85/COOP_NT_MC85")
                        {
                            foreach (int badBunId in new List<string> { "win32/gameplay/bundles/sharedbundles/common/animation/sharedbundleanimation_common",
                                "win32/gameplay/bundles/sharedbundles/frontend+mp/abilities/sharedbundleabilities_frontend+mp",  }.Select(o => AM.GetBundleId(o)).ToList())
                            {
                                if (ParentsList.Contains(badBunId))
                                    ParentsList.Remove(badBunId);
                            }
                        }
                    }

                    BundleOrder.Add(bunID);
                    BundleDataDict.Add(bunID, new BM_BundleData { Parents = ParentsList, ModifiedAssets = new List<EbxAssetEntry>() });
                    if (Config.Get<bool>("BMO_EnableBundleLogExport", false))
                        LogString("Bundle", "Logging Parents", string.Format("{0} ({1})", AM.GetBundleEntry(bunID).Name, bunID), String.Join(";", BundleDataDict[bunID].Parents.Select(o => AM.GetBundleEntry(o).Name).ToList()));
                    return true;
                }
            }
            else
            {
                App.Logger.Log("ERROR: BUNDLE LOOP DETECTED. BUNDLE MANAGER CANCELLED");
                foreach (int prevBunId in prevBunIDs)
                {
                    App.Logger.Log(AM.GetBundleEntry(prevBunId).Name);
                }
                App.Logger.Log(AM.GetBundleEntry(bunID).Name);
                return false;
            }
        }

        private void LoadPrerequisites()
        {
            string dirName = Path.GetDirectoryName(App.FileSystem.CacheName) + @"/BundleManagerPrerequisites";
            if (!Directory.Exists(dirName) || !Config.Get<bool>("BMO_EnablePrerequisites", true))
                return;
            List<string> prereqFiles = Directory.EnumerateFiles(dirName).Where(file => file.EndsWith(".bmpre")).ToList();
            if (prereqFiles.Count == 0)
                return;

            App.Logger.Log($"Bundle Manager: Using Prerequisites files: {string.Join(", ", prereqFiles.ToList().Select(o => "\"" + Path.GetFileNameWithoutExtension(o) + "\""))}");
            foreach (string prereqFile in prereqFiles)
                prerequisites.ReadFile(prereqFile, ref BundleParents);
        }

        public void ExportPrerequistis(string FileName)
        {
            BundleManagerPrerequisites prerequistes = new BundleManagerPrerequisites();
            prerequistes.FindBundleEdits();
            EstablishBundleLoadOrder(false);
            prerequistes.WriteToFile(FileName, ref BundleParents);
        }

        #endregion

        #region Stage 2 - Finding dependencies of modified base game assets and creating mvdbs for modified/added meshes & objectvariations
        private void FindNewDependencies(List<int> AllowedBundles)
        {
            List<BundleEntry> newBundles = AM.EnumerateBundles().Where(bEntry => bEntry.Added && bEntry.Blueprint != null).ToList();
            foreach (BundleEntry bEntry in newBundles)
            {
                int newBunId = AM.GetBundleId(bEntry);
                EbxAssetEntry blueEntry = bEntry.Blueprint;
                if (!blueEntry.IsInBundle(AM.GetBundleId(bEntry)))
                    blueEntry.AddedBundles.Add(AM.GetBundleId(bEntry));
                DependencyDetector(blueEntry);

                if (!AllowedBundles.Contains(newBunId))
                    continue;


                //Copies over bundle contents of sublevel if the bundles are linked
                if (Config.Get<bool>("BMO_CopyLinkedBundles", true))
                {
                    foreach (EbxAssetEntry linkedEntry in blueEntry.LinkedAssets)
                    {
                        foreach (int oldBunId in linkedEntry.Bundles)
                        {
                            if (AM.GetBundleEntry(oldBunId) != null && AM.GetBundleEntry(oldBunId).Blueprint == linkedEntry)
                            {
                                List<AssetEntry> assetsToAdd = new List<AssetEntry>(AM.EnumerateEbx().Where(o => o.IsInBundle(oldBunId)));
                                assetsToAdd.AddRange(AM.EnumerateRes().Where(o => o.IsInBundle(oldBunId)));
                                foreach (AssetEntry refEntry in assetsToAdd)
                                {
                                    if (refEntry != linkedEntry)
                                    {
                                        switch (refEntry.Type)
                                        {
                                            case "NetworkRegistryAsset":
                                                EbxAssetEntry netEntry = new DuplicateAssetExtension().DuplicateAsset((EbxAssetEntry)refEntry, bEntry.Name.ToLower().Substring(6) + "_networkregistry_Win32", false, null);
                                                netEntry.AddedBundles.Clear();
                                                netEntry.AddToBundle(newBunId);
                                                LogString(netEntry.AssetType, "Duplicating original network registry", netEntry.Name, AM.GetBundleEntry(newBunId).Name);
                                                break;
                                            case "MeshVariationDatabase":
                                                EbxAssetEntry mvEntry = new DuplicateAssetExtension().DuplicateAsset((EbxAssetEntry)refEntry, bEntry.Name.Replace("win32/", "") + "/MeshVariationDb_Win32", false, null);
                                                mvEntry.AddedBundles.Clear();
                                                mvEntry.AddToBundle(newBunId);
                                                LogString(mvEntry.AssetType, "Duplicating original meshvariationdb", mvEntry.Name, AM.GetBundleEntry(newBunId).Name);
                                                break;
                                            default:
                                                if (refEntry != linkedEntry)
                                                {
                                                    refEntry.AddToBundle(newBunId);
                                                    LogString(refEntry.AssetType, "Copying from duplicated bundle", refEntry.Name, AM.GetBundleEntry(newBunId).Name);
                                                }
                                                break;
                                        }
                                    }
                                }
                                foreach (ChunkAssetEntry chunkEntry in AM.EnumerateChunks().Where(o => o.IsInBundle(oldBunId)))
                                {
                                    if (BmH32Cache.IsLoaded)
                                    {
                                        chunkEntry.FirstMip = BmH32Cache.chunkCachedData[chunkEntry].Item1;
                                        chunkEntry.H32 = BmH32Cache.chunkCachedData[chunkEntry].Item2;
                                        LogString(chunkEntry.AssetType, "Setting H32&Firstmip (h32 cache)", BmH32Cache.chunkCachedData[chunkEntry].Item1.ToString(), BmH32Cache.chunkCachedData[chunkEntry].Item2.ToString());
                                    }
                                    chunkEntry.AddToBundle(newBunId);
                                    LogString(chunkEntry.AssetType, "Copying from duplicated bundle", chunkEntry.Name, AM.GetBundleEntry(newBunId).Name);
                                }
                            }
                        }
                    }
                }
            }

            foreach (EbxAssetEntry parEntry in AM.EnumerateEbx())
            {
                if (parEntry.HasModifiedData)
                {
                    if (!parEntry.IsAdded)
                        DependencyDetector(parEntry);

                    string type = "null";
                    foreach (string uniqueTypes in new List<string> { "SoundWaveAsset", "MeshAsset", "ObjectVariation" })
                    {
                        if (TypeLibrary.IsSubClassOf(parEntry.Type, uniqueTypes))
                            type = uniqueTypes;
                    }
                    switch (type)
                    {
                        case "SoundWaveAsset": SoundWaveNewChunksDetector(parEntry); break;
                        case "MeshAsset": MeshAssetDatabaseDetector(parEntry); break;
                        case "ObjectVariation": ObjectVariationDatabaseDetector(parEntry); break;
                    }
                }
            }
        }
        private void DependencyDetector(EbxAssetEntry parEntry) //Finds new ebx references in modified files and orders them to be checked over during the bundle enumeration
        {
            List<EbxAssetEntry> refEntries = new List<EbxAssetEntry>();
            foreach (Guid refGuid in parEntry.EnumerateDependencies())
            {
                if (!parEntry.DependentAssets.Contains(refGuid))
                {
                    EbxAssetEntry refEntry = AM.GetEbxEntry(refGuid);
                    if (refEntry != null)
                    {
                        LogString("Ebx", "Dependency found", parEntry.Name, refEntry.Name);
                        refEntries.Add(refEntry);
                    }
                    else
                        LogString("Ebx", "Dependency found", parEntry.Name, "Null Reference");
                }
            }
            if (refEntries.Count != 0)
            {
                assetsToBM.Add(parEntry, refEntries);
                void AddToBundleEnumeration(int bunId)
                {
                    if (BundleDataDict[bunId].ModifiedAssets.Count == 0)
                        LogString("Bundle", "Preparing to enumerate over bundle", App.AssetManager.GetBundleEntry(bunId).Name, "");
                    BundleDataDict[bunId].ModifiedAssets.Add(parEntry);
                }
                foreach (int bunId in parEntry.EnumerateBundles())
                    AddToBundleEnumeration(bunId);
                if (prerequisites.assetsAddedToBundles.ContainsKey(parEntry))
                {
                    foreach (int bunId in prerequisites.assetsAddedToBundles[parEntry].Select(o => App.AssetManager.GetBundleId(o)))
                        AddToBundleEnumeration(bunId);
                }
            }
        }

        private void SoundWaveNewChunksDetector(EbxAssetEntry parEntry) //Finds cases where modified sound wave assets have new chunks added to them
        {
            if (parEntry.IsAdded)
                return;
            dynamic parRootOrig = AM.GetEbx(parEntry, true).RootObject;
            dynamic parRoot = AM.GetEbx(parEntry).RootObject;
            List<Guid> origChunkGuids = new List<Guid>();
            List<ChunkAssetEntry> newChunkEntries = new List<ChunkAssetEntry>();
            foreach (dynamic chunkData in parRootOrig.Chunks)
                origChunkGuids.Add(chunkData.ChunkId);
            foreach (dynamic chunkData in parRoot.Chunks)
            {
                if (!origChunkGuids.Contains(chunkData.ChunkId))
                {
                    ChunkAssetEntry chkEntry = App.AssetManager.GetChunkEntry(chunkData.ChunkId);
                    if (chkEntry != null)
                    {
                        LogString("Ebx-Chunk", "Dependency found", parEntry.Name, chkEntry.Name);
                        newChunkEntries.Add(chkEntry);
                    }
                    else
                        LogString("Ebx-Chunk", "Dependency found", parEntry.Name, "Null Reference");
                }
            }

            if (newChunkEntries.Count != 0)
                soundwaveSpecialCase.Add(parEntry, newChunkEntries);
        }

        private void MeshAssetDatabaseDetector(EbxAssetEntry parEntry) //Creates a MeshVariationDatabase entry for new meshes and detects if modified meshes require a newMeshVariationDatabase 
        {
            EbxAsset parAsset = AM.GetEbx(parEntry);
            dynamic parRoot = parAsset.RootObject;
            GetModifiedLoggedData(parEntry, parAsset);
            bool needsNewEntry = parEntry.IsAdded || CheckMeshNeedsNewDatabase(parEntry, parAsset, parRoot);

            if (needsNewEntry)
            {
                BM_MeshVariationDatabaseEntry bm_mvEntry = new BM_MeshVariationDatabaseEntry(parEntry, parAsset, parRoot);
                LoggedData[parEntry].meshVari = new MeshVariData() { BM_MeshVariationDatabaseEntry = bm_mvEntry, refGuids = bm_mvEntry.GetReferenceGuids() };

                if (!parEntry.IsAdded && BmCache.MeshVariationEntries.ContainsKey(parEntry) && BmCache.MeshVariationEntries[parEntry].ContainsKey(0) && BmCache.MeshVariationEntries[parEntry][0].BM_MeshVariationDatabaseEntry != null)
                {
                    MeshVariOriginalData unmodifiedData = BmCache.MeshVariationEntries[parEntry][0];
                    foreach (KeyValuePair<EbxAssetEntry, int> pair in unmodifiedData.dbLocations)
                    {
                        if (!mvdbsToUpdate.ContainsKey(pair.Key))
                            mvdbsToUpdate.Add(pair.Key, new Dictionary<int, EbxAssetEntry>());
                        mvdbsToUpdate[pair.Key].Add(pair.Value, parEntry);
                    }
                }
            }
            else
                LoggedData[parEntry].meshVari = new MeshVariData() { BM_MeshVariationDatabaseEntry = BmCache.MeshVariationEntries[parEntry][0].BM_MeshVariationDatabaseEntry, refGuids = BmCache.MeshVariationEntries[parEntry][0].refGuids };
        }

        private bool CheckMeshNeedsNewDatabase(EbxAssetEntry parEntry, EbxAsset parAsset, dynamic parRoot)
        {
            if (!BmCache.MeshVariationEntries.ContainsKey(parEntry) || !BmCache.MeshVariationEntries[parEntry].ContainsKey(0) || BmCache.MeshVariationEntries[parEntry][0].BM_MeshVariationDatabaseEntry == null)
                return true;

            BM_MeshVariationDatabaseEntry bm_mvEntry = BmCache.MeshVariationEntries[parEntry][0].BM_MeshVariationDatabaseEntry;
            return bm_mvEntry.CheckMeshNeedsUpdating(parAsset, parRoot);
        }

        private void ObjectVariationDatabaseDetector(EbxAssetEntry parEntry)
        {
            EbxAsset parAsset = AM.GetEbx(parEntry);
            dynamic parRoot = parAsset.RootObject;
            uint varHash = parRoot.NameHash;
            GetModifiedLoggedData(parEntry, parAsset);

            if (!parEntry.IsAdded)
                App.Logger.LogWarning($"{parEntry.Name}\n The bundle manager does not update existing MeshVariationDatabase entries for none-duplicated ObjectVariations because the gameplay merger cannot properly handle those edits.\n It is recommended to use the duplication plugin to create a new ObjectVariation with the changes you want");


            Dictionary<string, EbxAssetEntry> meshNamesToEntry = new Dictionary<string, EbxAssetEntry>();
            foreach (EbxAssetEntry refEntry in App.AssetManager.EnumerateEbx())
            {
                if (TypeLibrary.IsSubClassOf(refEntry.Type, "MeshAsset"))
                    meshNamesToEntry.Add($"{refEntry.Filename}_{(uint)Utils.HashString(refEntry.Name)}", refEntry);
            }

            //Find the mesh(es) which this variation is attached to
            foreach (ResAssetEntry resEntry in App.AssetManager.EnumerateRes())
            {
                if (resEntry.Name.ToLower().StartsWith(parEntry.Name.ToLower()))
                {
                    string meshName = resEntry.Name.ToLower().Substring(parEntry.Name.Length + 1);
                    meshName = meshName.Substring(0, meshName.IndexOf("/"));
                    if (!meshNamesToEntry.ContainsKey(meshName))
                    {
                        App.Logger.LogWarning($"Bundle Manager: Could not find mesh {meshName.Substring(0, meshName.LastIndexOf("_"))} when trying to create mvdb entry of {parEntry.Name}");
                        continue;
                    }
                    EbxAssetEntry meshEntry = meshNamesToEntry[meshName];
                    EbxAsset meshAsset = App.AssetManager.GetEbx(meshEntry);
                    dynamic meshRoot = meshAsset.RootObject;

                    MeshSet meshSet = App.AssetManager.GetResAs<MeshSet>(App.AssetManager.GetResEntry(meshRoot.MeshSetResource));

                    Dictionary<string, dynamic> meshMaterialsNameToMaterial = new Dictionary<string, dynamic>();
                    foreach (dynamic classObject in parAsset.Objects)
                    {
                        if (classObject.GetType().Name == "MeshMaterialVariation")
                        {
                            //App.Logger.Log(classObject.__Id);
                            if (classObject.__Id == "MeshMaterialVariation" || meshMaterialsNameToMaterial.ContainsKey(classObject.__Id))
                            {
                                App.Logger.LogError($"{parEntry.Name} section \"{classObject.__Id}\"\nYou need to rename your MeshMaterialVariations to match the mesh section names of the original mesh in the materials section and they should each be unique");
                                return;
                            }
                            meshMaterialsNameToMaterial.Add(classObject.__Id, classObject);
                        }
                    }

                    Dictionary<Guid, dynamic> meshSectionToVariationSection = new Dictionary<Guid, dynamic>();
                    foreach (MeshSetLod lod in meshSet.Lods)
                    {
                        foreach (MeshSetSection section in lod.Sections)
                        {
                            if (lod.IsSectionRenderable(section) && section.PrimitiveCount > 0)
                            {
                                dynamic material = meshRoot.Materials[section.MaterialId].Internal;
                                if (!meshSectionToVariationSection.ContainsKey(material.__InstanceGuid.ExportedGuid))
                                {
                                    if (!meshMaterialsNameToMaterial.ContainsKey(section.Name))
                                    {
                                        App.Logger.LogError($"{parEntry.Name} missing mesh material variation \"{section.Name}\"\nYou need to rename your MeshMaterialVariations to match the mesh section names of the original mesh in the materials section and they should each be unique");
                                        return;
                                    }
                                    meshSectionToVariationSection.Add(material.__InstanceGuid.ExportedGuid, meshMaterialsNameToMaterial[section.Name]);
                                }
                            }
                        }
                    }

                    BM_MeshVariationDatabaseEntry bm_mvEntry = new BM_MeshVariationDatabaseEntry(meshEntry, meshAsset, meshRoot, parEntry, parRoot, meshSectionToVariationSection);
                    if (!ModifiedObjectVariations.ContainsKey(parEntry))
                        ModifiedObjectVariations.Add(parEntry, new Dictionary<EbxAssetEntry, MeshVariData>());
                    ModifiedObjectVariations[parEntry].Add(meshEntry, new MeshVariData() { BM_MeshVariationDatabaseEntry = bm_mvEntry, refGuids = bm_mvEntry.GetReferenceGuids() });
                    //LoggedData[parEntry].meshVari = new MeshVariData() { BM_MeshVariationDatabaseEntry = bm_mvEntry, refGuids = bm_mvEntry.GetReferenceGuids() };
                }
            }

            //if (!parEntry.IsAdded && BmCache.ObjectVariationPairs.ContainsKey(parEntry))
            //{
            //    foreach (KeyValuePair<EbxAssetEntry, ResAssetEntry> pair in BmCache.ObjectVariationPairs[parEntry])
            //    {
            //        EbxAsset meshAsset = AM.GetEbx(parEntry);
            //        if (CheckObjectVariationNeedsNewDatabase(parAsset, parRoot, pair.Key, meshAsset, varHash))
            //        {

            //        }
            //    }
            //}
        }

        private bool CheckObjectVariationNeedsNewDatabase(EbxAsset parAsset, dynamic parRoot, EbxAssetEntry meshEntry, EbxAsset meshAsest, uint varHash)
        {
            if (!BmCache.MeshVariationEntries.ContainsKey(meshEntry) || !BmCache.MeshVariationEntries[meshEntry].ContainsKey(varHash) || BmCache.MeshVariationEntries[meshEntry][varHash].BM_MeshVariationDatabaseEntry == null)
                return true;

            BM_MeshVariationDatabaseEntry bm_mvEntry = BmCache.MeshVariationEntries[meshEntry][varHash].BM_MeshVariationDatabaseEntry;
            return bm_mvEntry.CheckVariationNeedsUpdating(parAsset, parRoot, meshAsest);
        }



        #endregion

        #region Stage 3 - Bundle Enumeration



        private void BundleEnumeration(List<int> AllowedBundles)
        {
            LoadSwbf2FrontendAnimations(AllowedBundles);
            int taskIdx = 0;
            int taskCount = BundleOrder.Where(bunID => BundleDataDict[bunID].ModifiedAssets.Count > 0 && AllowedBundles.Contains(bunID)).ToList().Count;
            LogString("BUNDLE MANAGER", "Enumerating Over", string.Format("{0}/{1} bundles", taskCount, BundleOrder.Count), "");
            //task.Update(status: "Enumerating Bundles");
            foreach (int bunID in BundleOrder) //DO NOT USE PARALLEL FOREACH
            {
                if (AllowedBundles.Contains(bunID))
                {
                    if (BundleDataDict[bunID].ModifiedAssets.Count > 0)
                        task.Update(status: String.Format("Completing: {0}", AM.GetBundleEntry(bunID).Name), progress: ((double)taskIdx++ / (double)taskCount) * 100.0d);
                    BundleCompleter(bunID);
                }
            }

            //Make sure all of the new bundles have some asset in them else Frosty will crash if the bundle is empty
            foreach (BundleEntry bEntry in App.AssetManager.EnumerateBundles().Where(bEntry => bEntry.Added).ToList())
                GetEmptyChunk().AddToBundle(App.AssetManager.GetBundleId(bEntry));
        }

        private void BundleCompleter(int bunID)
        {
            string bunName = AM.GetBundleEntry(bunID).Name;
            List<int> parIDs = BundleDataDict[bunID].Parents;

            List<MeshVariData> NewMeshVariationDbEntries = new List<MeshVariData>();
            List<EbxImportReference> NewNetworkRegistryReferences = new List<EbxImportReference>();
            List<EbxAssetEntry> AddedObjectVariations = new List<EbxAssetEntry>();

            EbxAssetEntry mvdbEntry = AM.GetEbxEntry(AM.GetBundleEntry(bunID).Name.ToLower().Substring(6) + "/MeshVariationDb_Win32");

            bool prereqBundle = AM.GetBundleEntry(bunID).Added && AM.GetBundleEntry(bunID).Type == BundleType.SubLevel && AM.GetEbxEntry(AM.GetBundleEntry(bunID).Name) == null;
            List<EbxAssetEntry> ignoreDependencies = !prereqBundle ? new List<EbxAssetEntry>() : prerequisites.assetsAddedToBundles.Where(bunList => bunList.Value.Contains(AM.GetBundleEntry(bunID))).Select(ebxEntry => ebxEntry.Key).ToList();

            //Adding dependencies to bundle
            if (BundleDataDict[bunID].ModifiedAssets.Count > 0)
            {
                LogString("Bundle", "Completing Bundle", AM.GetBundleEntry(bunID).Name, BundleDataDict[bunID].ModifiedAssets.Count + " assets enumerating over");

                parIDs = BundleDataDict[bunID].Parents;
                foreach (EbxAssetEntry parEntry in BundleDataDict[bunID].ModifiedAssets)
                {
                    LogString("Ebx", "Completing Dependencies", parEntry.Name, assetsToBM[parEntry].Count + " assets");
                    foreach (EbxAssetEntry refEntry in assetsToBM[parEntry])
                        CheckAddEbxToBundle(refEntry);
                }
                foreach (EbxAssetEntry varEntry in AddedObjectVariations)
                {
                    if (ModifiedObjectVariations.ContainsKey(varEntry))
                    {
                        foreach (KeyValuePair<EbxAssetEntry, MeshVariData> pair in ModifiedObjectVariations[varEntry])
                        {
                            if (IsLoaded(pair.Key))
                            {
                                NewMeshVariationDbEntries.Add(pair.Value);
                                EbxAssetEntry meshEntry = AM.GetEbxEntry(pair.Value.BM_MeshVariationDatabaseEntry.Mesh.External.FileGuid);
                                ResAssetEntry variationBlocks = AM.GetResEntry($"{varEntry.Name.ToLower()}/{meshEntry.Filename}_{(uint)Utils.HashString(meshEntry.Name)}/shaderblocks_variation/blocks");
                                CheckAddResToBundle(variationBlocks);
                            }
                        }
                    }
                    else
                    {
                        if (varEntry.IsAdded)
                            continue;
                        foreach (KeyValuePair<EbxAssetEntry, ResAssetEntry> pair in BmCache.ObjectVariationPairs[varEntry])
                        {
                            if (IsLoaded(pair.Key) && (mvdbEntry == null || !(mvdbEntry.ContainsDependency(varEntry.Guid) && mvdbEntry.ContainsDependency(pair.Key.Guid))))
                            {
                                MeshVariOriginalData variation = BmCache.MeshVariationEntries[pair.Key][(uint)Utils.HashString(varEntry.Name, true)];
                                CheckAddResToBundle(pair.Value);
                                List<Guid> refGuids = new List<Guid>(variation.refGuids);
                                refGuids.Add(varEntry.Guid);
                                NewMeshVariationDbEntries.Add(new MeshVariData() { BM_MeshVariationDatabaseEntry = variation.BM_MeshVariationDatabaseEntry, refGuids = refGuids });

                                using (NativeReader reader = new NativeReader(AM.GetRes(pair.Value)))
                                {
                                    for (int idx = 72; idx < Convert.ToInt32(reader.BaseStream.Length - 12); idx = idx + 4)
                                    {
                                        reader.BaseStream.Position = idx;
                                        EbxAssetEntry readEntry = RootInstanceEbxEntryDb.GetEbxEntryByRootInstanceGuid(reader.ReadGuid());
                                        if (readEntry != null && readEntry != varEntry && (!BmCache.UnmodifiedAssetData.ContainsKey(varEntry) || !BmCache.UnmodifiedAssetData[varEntry].EbxReferences.Contains(readEntry)) && !IsLoaded(readEntry))
                                        {
                                            CheckAddEbxToBundle(readEntry);
                                            //App.Logger.Log(readEntry.Name);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //MeshVariationDB
            if (Config.Get<bool>("BMO_CompleteMeshVariationDBs", true))
            {
                if (NewMeshVariationDbEntries.Count != 0 || (mvdbEntry != null && mvdbsToUpdate.ContainsKey(mvdbEntry)))
                {
                    int count = NewMeshVariationDbEntries.Count;
                    if (mvdbEntry != null && mvdbsToUpdate.ContainsKey(mvdbEntry))
                        count = count + mvdbsToUpdate[mvdbEntry].Count;
                    LogString("Bundle", "Completing MeshVariationDatabase", AM.GetBundleEntry(bunID).Name, count + " entries enumerating over");
                    if (mvdbEntry != null)
                    {
                        EbxAsset meshvariAsset = AM.GetEbx(mvdbEntry);
                        dynamic meshvariRoot = meshvariAsset.RootObject;
                        if (mvdbsToUpdate.ContainsKey(mvdbEntry))
                        {
                            foreach (KeyValuePair<int, EbxAssetEntry> pair in mvdbsToUpdate[mvdbEntry])
                            {
                                meshvariRoot.Entries[pair.Key] = LoggedData[pair.Value].meshVari.BM_MeshVariationDatabaseEntry.WriteToGameEntry();
                                foreach (Guid texGuid in LoggedData[pair.Value].meshVari.refGuids)
                                {
                                    if (!mvdbEntry.ContainsDependency(texGuid))
                                        meshvariAsset.AddDependency(texGuid);
                                }
                            }
                            LogString("Bundle", "Updated MeshVariationDB Modified Entries", mvdbEntry.Name, mvdbsToUpdate[mvdbEntry].Count.ToString() + " entries edited");
                            mvdbsToUpdate.Remove(mvdbEntry);
                        }

                        foreach (MeshVariData mvEntry in NewMeshVariationDbEntries)
                        {
                            meshvariRoot.Entries.Add(mvEntry.BM_MeshVariationDatabaseEntry.WriteToGameEntry());
                            foreach (Guid refGuid in mvEntry.refGuids)
                                meshvariAsset.AddDependency(refGuid);
                        }
                        if (NewMeshVariationDbEntries.Count > 0)
                            LogString("Bundle", "Added MeshVariationDB New Entries", mvdbEntry.Name, NewMeshVariationDbEntries.Count.ToString() + " entries added");

                        AM.ModifyEbx(mvdbEntry.Name, meshvariAsset);
                    }
                    else
                    {
                        CreateMeshVariationDatabase(AM.GetBundleEntry(bunID).Name.Replace("win32/", "") + "/MeshVariationDb_Win32");
                    }
                }
            }

            //Network Registry
            if (NewNetworkRegistryReferences.Count > 0 & Config.Get<bool>("BMO_CompleteNetworkRegistries", true) == true)
            {
                EbxAssetEntry netregEntry = AM.GetEbxEntry(AM.GetBundleEntry(bunID).Name.ToLower().Substring(6) + "_networkregistry_Win32");

                if (netregEntry != null)
                {
                    if (!netregEntry.IsAdded)
                        netregEntry.ClearModifications();
                    if (Config.Get<bool>("BMO_CreateNetworkRegistries", false) && !netregEntry.IsAdded)
                        CreateNetworkRegistry(netregEntry.Name.Replace("_networkregistry_Win32", "") + "Modded_" + rnd.Next(0, Int32.MaxValue).ToString() + rnd.Next(0, Int32.MaxValue).ToString() + "_networkregistry_Win32");
                    else
                        AddNetRegEntries(netregEntry, true);
                }
                else if (AM.GetBundleEntry(bunID).Type == BundleType.SubLevel)
                    CreateNetworkRegistry(AM.GetBundleEntry(bunID).Name.ToLower().Substring(6) + "_networkregistry_Win32");
                //Config.Get<bool>("BMO_CreateNetworkRegistries", false);
            }

            //Loading Sound Wave Chunks
            foreach (EbxAssetEntry refEntry in soundwaveSpecialCase.Keys)
            {
                if (refEntry.IsInBundle(bunID) || (prerequisites.assetsAddedToBundles.ContainsKey(refEntry) && prerequisites.assetsAddedToBundles[refEntry].Select(o => App.AssetManager.GetBundleId(o)).Contains(bunID)))
                {
                    foreach (ChunkAssetEntry chkEntry in soundwaveSpecialCase[refEntry])
                        CheckAddChkToBundle(chkEntry);
                }
            }

            //Methods
            bool IsLoaded(AssetEntry refEntry)
            {
                if (refEntry == null)
                    return true;
                if (refEntry.IsInBundle(bunID))
                    return true;
                foreach (int parId in parIDs)
                {
                    if (refEntry.IsInBundle(parId))
                        return true;
                }
                if (ignoreDependencies.Contains(refEntry))
                    return true;

                return false;
            }
            void CheckAddEbxToBundle(EbxAssetEntry refEntry)
            {
                if (!IsLoaded(refEntry))
                    AddEbxToBundle(refEntry);
            }
            void AddEbxToBundle(EbxAssetEntry parEntry)
            {
                if (!LoggedData.ContainsKey(parEntry))
                    GetLoggedData(parEntry);

                LogString(parEntry.AssetType, "Adding to bundle", parEntry.Name, AM.GetBundleEntry(bunID).Name);
                parEntry.AddToBundle(bunID);
                AssetData parData = LoggedData[parEntry];

                foreach (ResAssetEntry resEntry in parData.Res)
                    CheckAddResToBundle(resEntry);

                foreach ((ChunkAssetEntry, int, string) chkData in parData.Chunks)
                {
                    //App.Logger.Log((!chkData.Item1.IsAdded && !chkData.Item1.HasModifiedData).ToString());
                    if (!chkData.Item1.IsAdded && !chkData.Item1.HasModifiedData)
                    {
                        if (BmH32Cache.IsLoaded)
                        {
                            chkData.Item1.FirstMip = BmH32Cache.chunkCachedData[chkData.Item1].Item1;
                            chkData.Item1.H32 = BmH32Cache.chunkCachedData[chkData.Item1].Item2;
                            LogString(chkData.Item1.AssetType, "Setting H32&Firstmip (h32 cache)", BmH32Cache.chunkCachedData[chkData.Item1].Item1.ToString(), BmH32Cache.chunkCachedData[chkData.Item1].Item2.ToString());
                        }
                        else
                        {
                            chkData.Item1.FirstMip = chkData.Item2;
                            chkData.Item1.H32 = Utils.HashString(chkData.Item3 != null ? chkData.Item3 : parEntry.Name, true);
                            LogString(chkData.Item1.AssetType, "Setting H32&Firstmip (cache)", chkData.Item2.ToString(), chkData.Item1.H32.ToString());
                        }
                    }
                    CheckAddChkToBundle(chkData.Item1);
                }


                if (parData.Objects != null)
                {
                    foreach (EbxImportReference InstanceObj in parData.Objects)
                        NewNetworkRegistryReferences.Add(InstanceObj);
                }

                if (parData.meshVari != null)
                    NewMeshVariationDbEntries.Add(parData.meshVari);

                if (parEntry.Type == "ObjectVariation")
                    AddedObjectVariations.Add(parEntry);

                foreach (EbxAssetEntry refEntry in parData.EbxReferences)
                    CheckAddEbxToBundle(refEntry);
            }
            void CheckAddResToBundle(ResAssetEntry refEntry)
            {
                if (!IsLoaded(refEntry))
                    AddResToBundle(refEntry);
            }
            void AddResToBundle(ResAssetEntry refEntry)
            {
                LogString(refEntry.AssetType, "Adding to bundle", refEntry.Name, bunName);
                refEntry.AddToBundle(bunID);
            }
            void CheckAddChkToBundle(ChunkAssetEntry refEntry)
            {
                if (!IsLoaded(refEntry))
                    AddChkToBundle(refEntry);
            }
            void AddChkToBundle(ChunkAssetEntry refEntry)
            {
                LogString(refEntry.AssetType, "Adding to bundle", refEntry.Name, bunName);
                refEntry.AddToBundle(bunID);
                //if (refEntry.FirstMip == -1 && BmCache.ChunkFirstMips.ContainsKey(refEntry))
                //    refEntry.FirstMip = BmCache.ChunkFirstMips[refEntry];
            }
            void GetLoggedData(EbxAssetEntry parEntry)
            {
                if (!parEntry.HasModifiedData)
                {
                    if (BmCache.UnmodifiedAssetData.ContainsKey(parEntry))
                    {
                        LogString("Ebx", "Reading Cached Data", parEntry.Name, parEntry.Type.ToString());
                        LoggedData.Add(parEntry, BmCache.UnmodifiedAssetData[parEntry]);
                        if (TypeLibrary.IsSubClassOf(parEntry.Type, "MeshAsset"))
                            LoggedData[parEntry].meshVari = new MeshVariData() { BM_MeshVariationDatabaseEntry = BmCache.MeshVariationEntries[parEntry][0].BM_MeshVariationDatabaseEntry, refGuids = BmCache.MeshVariationEntries[parEntry][0].refGuids };
                    }
                    else
                        LoggedData.Add(parEntry, new AssetData() { EbxReferences = parEntry.EnumerateDependencies().Select(o => App.AssetManager.GetEbxEntry(o)).ToList(), Chunks = new List<(ChunkAssetEntry, int, string)>(), Res = new List<ResAssetEntry>() });
                }
                else
                {
                    EbxAsset parAsset = AM.GetEbx(parEntry);
                    GetModifiedLoggedData(parEntry, parAsset);
                }
            }
            void CreateMeshVariationDatabase(string newMeshVariName)
            {
                EbxAsset newAsset = new EbxAsset(TypeLibrary.CreateObject("MeshVariationDatabase"));
                newAsset.SetFileGuid(Guid.NewGuid());

                dynamic obj = newAsset.RootObject;
                obj.Name = newMeshVariName;

                AssetClassGuid guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(newAsset.Objects, (Type)obj.GetType(), newAsset.FileGuid), -1);
                obj.SetInstanceGuid(guid);

                EbxAssetEntry newEntry = App.AssetManager.AddEbx(newMeshVariName, newAsset);
                if (newEntry.ModifiedEntry == null)
                    throw new Exception(string.Format("App.AssetManager.AddEbx() failed to create new MeshVariationDatabase EbxAsset for bundle: {0}\nUnknown cause and how to fix", App.AssetManager.GetBundleEntry(bunID).Name));

                newEntry.AddedBundles.Add(bunID);
                newEntry.ModifiedEntry.DependentAssets.AddRange(newAsset.Dependencies);
                EbxAsset meshvariAsset = App.AssetManager.GetEbx(newEntry);
                dynamic meshvariRoot = meshvariAsset.RootObject;
                foreach (MeshVariData mvEntry in NewMeshVariationDbEntries)
                {
                    meshvariRoot.Entries.Add(mvEntry.BM_MeshVariationDatabaseEntry.WriteToGameEntry());
                    foreach (Guid refGuid in mvEntry.refGuids)
                        meshvariAsset.AddDependency(refGuid);
                }
                AM.ModifyEbx(newMeshVariName, newAsset);
                LogString("Bundle", "Adding MeshVariationDB", newMeshVariName, NewMeshVariationDbEntries.Count.ToString() + " entries added");
            }
            void CreateNetworkRegistry(string newNetRegName)
            {
                EbxAsset newAsset = new EbxAsset(TypeLibrary.CreateObject("NetworkRegistryAsset"));
                newAsset.SetFileGuid(Guid.NewGuid());

                dynamic obj = newAsset.RootObject;
                obj.Name = newNetRegName;

                AssetClassGuid guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(newAsset.Objects, (Type)obj.GetType(), newAsset.FileGuid), -1);
                obj.SetInstanceGuid(guid);

                EbxAssetEntry newEntry = App.AssetManager.AddEbx(newNetRegName, newAsset);

                newEntry.AddedBundles.Add(bunID);
                newEntry.ModifiedEntry.DependentAssets.AddRange(newAsset.Dependencies);

                AddNetRegEntries(newEntry, false);

                LogString("Bundle", "Adding Network Registry", newNetRegName, NewNetworkRegistryReferences.Count.ToString() + " entries added");
            }
            void AddNetRegEntries(EbxAssetEntry netregEntry, bool logMessage)
            {
                EbxAsset netAsset = App.AssetManager.GetEbx(netregEntry);
                if (netAsset == null)
                    return;
                dynamic netRoot = netAsset.RootObject;
                foreach (EbxImportReference NewReference in NewNetworkRegistryReferences)
                {
                    netRoot.Objects.Add(new PointerRef(NewReference));
                    netAsset.AddDependency(NewReference.FileGuid);
                }
                AM.ModifyEbx(netregEntry.Name, netAsset);
                if (logMessage)
                    LogString("Bundle", "Completing Network Registry", netregEntry.Name, NewNetworkRegistryReferences.Count.ToString() + " references added");
            }
        }

        private List<int> GetAllowedBundles(List<int> levelBunIDs)
        {
            int frontendId = App.AssetManager.GetBundleId("win32/Levels/Frontend/Frontend");
            if (levelBunIDs != null)
                levelBunIDs.Add(frontendId);
            bool BMO_CompleteSharedBundles = Config.Get<bool>("BMO_CompleteSharedBundles", true);
            bool BMO_CompleteSublevelBundles = Config.Get<bool>("BMO_CompleteSublevelBundles", true);
            bool BMO_CompleteBlueprintBundles = Config.Get<bool>("BMO_CompleteBlueprintBundles", true);
            bool BMO_Sublevel_SP = Config.Get<bool>("BMO_Sublevel_SP", false);
            bool BMO_Sublevel_MP = Config.Get<bool>("BMO_Sublevel_MP", true);
            bool BMO_Sublevel_Mode1 = Config.Get<bool>("BMO_Sublevel_Mode1", true);
            bool BMO_Sublevel_Mode9 = Config.Get<bool>("BMO_Sublevel_Mode9", true);
            bool BMO_Sublevel_Skirmish = Config.Get<bool>("BMO_Sublevel_Skirmish", true);
            bool BMO_Sublevel_SpaceArcade = Config.Get<bool>("BMO_Sublevel_SpaceArcade", true);
            bool BMO_Sublevel_Mode3 = Config.Get<bool>("BMO_Sublevel_Mode3", false);
            bool BMO_Sublevel_Mode5 = Config.Get<bool>("BMO_Sublevel_Mode5", false);
            bool BMO_Sublevel_Mode6 = Config.Get<bool>("BMO_Sublevel_Mode6", false);
            bool BMO_Sublevel_Mode7 = Config.Get<bool>("BMO_Sublevel_Mode7", false);
            bool BMO_Sublevel_ModeC = Config.Get<bool>("BMO_Sublevel_ModeC", false);
            bool BMO_Sublevel_DeathMatchOnline = Config.Get<bool>("BMO_Sublevel_DeathMatchOnline", false);
            bool BMO_Sublevel_PlanetaryBattles = Config.Get<bool>("BMO_Sublevel_PlanetaryBattles", false);
            bool BMO_Sublevel_FantasyBattles = Config.Get<bool>("BMO_Sublevel_FantasyBattles", false);
            bool BMO_Sublevel_HvsV = Config.Get<bool>("BMO_Sublevel_HvsV", false);
            bool BMO_Sublevel_SpaceBattles = Config.Get<bool>("BMO_Sublevel_SpaceBattles", false);
            bool BMO_CompleteAddedBundles = Config.Get<bool>("BMO_CompleteAddedBundles", true);
            List<int> AllowedBundles = new List<int>();
            foreach (BundleEntry bEntry in AM.EnumerateBundles())
            {
                //if (cacheBundlesToIgnore.Contains(AM.GetBundleId(bEntry)))
                //    continue;
                if (ProfilesLibrary.IsLoaded(ProfileVersion.StarWarsBattlefrontII))
                {
                    if ((bEntry.Type == BundleType.SharedBundle & BMO_CompleteSharedBundles == true) || (bEntry.Type == BundleType.BlueprintBundle & BMO_CompleteBlueprintBundles == true))
                    {
                        if (bEntry.Name.ToLower().Contains("/sp/") & BMO_Sublevel_SP == false)
                        {
                            continue;
                        }
                        AllowedBundles.Add(AM.GetBundleId(bEntry));
                    }
                    else if (bEntry.Type == BundleType.SubLevel & BMO_CompleteSublevelBundles == true)
                    {
                        if (levelBunIDs != null)
                        {
                            bool tempAllowed = false;
                            int bunId = AM.GetBundleId(bEntry);

                            foreach (int levBundId in levelBunIDs)
                            {
                                if (BundleDataDict[bunId].Parents.Contains(levBundId) || levBundId == bunId)
                                {
                                    tempAllowed = true;
                                    break;
                                }
                            }

                            if (!tempAllowed)
                                continue;
                        }
                        if (bEntry.Added & !BMO_CompleteAddedBundles)
                            continue;

                        if (((bEntry.Name.ToLower().Contains("/sp/") & BMO_Sublevel_SP == false) || (bEntry.Name.ToLower().Contains("/mp/") & BMO_Sublevel_MP == false)))
                        {
                            continue;
                        }
                        if ((BMO_Sublevel_Mode1 == false & bEntry.Name.Contains("Mode1")) ||
                                (BMO_Sublevel_Mode9 == false & bEntry.Name.Contains("Mode9")) ||
                                (BMO_Sublevel_Mode3 == false & bEntry.Name.Contains("Mode3")) ||
                                (BMO_Sublevel_Mode5 == false & (bEntry.Name.Contains("Mode5") || bEntry.Name.Contains("Extraction"))) ||
                                (BMO_Sublevel_Mode6 == false & bEntry.Name.Contains("Mode6")) ||
                                (BMO_Sublevel_Mode7 == false & bEntry.Name.Contains("Mode7")) ||
                                (BMO_Sublevel_ModeC == false & bEntry.Name.Contains("ModeC")) ||
                                (BMO_Sublevel_Skirmish == false & (bEntry.Name.Contains("Skirmish") || bEntry.Name.Contains("Skrimish"))) ||
                                (BMO_Sublevel_SpaceArcade == false & bEntry.Name.Contains("SpaceArcade")) ||
                                (BMO_Sublevel_DeathMatchOnline == false & bEntry.Name.Contains("Deathmatch_Online")) ||
                                (BMO_Sublevel_PlanetaryBattles == false & (bEntry.Name.Contains("PlanetaryMissions") || bEntry.Name.Contains("Domination"))) ||
                                (BMO_Sublevel_FantasyBattles == false & bEntry.Name.Contains("FantasyBattle")) ||
                                (BMO_Sublevel_HvsV == false & (bEntry.Name.Contains("HvsV") || bEntry.Name.Contains("Hero"))) ||
                                (BMO_Sublevel_SpaceBattles == false & bEntry.Name.Contains("SpaceBattle")))
                        {
                            continue;
                        }
                        AllowedBundles.Add(AM.GetBundleId(bEntry));
                    }
                }
                else
                {
                    if ((bEntry.Type == BundleType.SharedBundle & BMO_CompleteSharedBundles == true) || (bEntry.Type == BundleType.SubLevel & BMO_CompleteSublevelBundles == true) || (bEntry.Type == BundleType.BlueprintBundle & BMO_CompleteBlueprintBundles == true))
                    {
                        AllowedBundles.Add(AM.GetBundleId(bEntry));
                    }
                }
            }
            foreach (int bunID in BundleDataDict.Keys.Where(bunID => BundleDataDict[bunID].ModifiedAssets.Count > 0))
            {
                if (AllowedBundles.Contains(bunID))
                    LogString("Bundle", "Allowed to enumerate over", App.AssetManager.GetBundleEntry(bunID).Name, "");
                else
                    LogString("Bundle", "Not allowed to enumerate over", App.AssetManager.GetBundleEntry(bunID).Name, "");
            }

            if (Config.Get<bool>("BMO_WhitelistBundles", true))
            {
                foreach (BundleEntry bunEntry in AM.EnumerateBundles())
                    if (bunEntry.Added || AllowedBundles.Contains(AM.GetBundleId(bunEntry)))
                        App.WhitelistedBundles.Add(HashBundle(bunEntry));

                //foreach (BundleEntry bEntry in App.AssetManager.EnumerateBundles())
                //    App.WhitelistBundles.Add(HashBundle(bEntry));
            }

            return AllowedBundles;
        }

        private int HashBundle(BundleEntry bentry)
        {
            int hash = Fnv1.HashString(bentry.Name.ToLower());

            if (bentry.Name.Length == 8 && int.TryParse(bentry.Name, System.Globalization.NumberStyles.HexNumber, null, out int tmp))
                hash = tmp;

            return hash;
        }

        private void GetModifiedLoggedData(EbxAssetEntry parEntry, EbxAsset parAsset)
        {
            string key = "null";
            foreach (string typekey in loggerExtensions.Keys)
            {
                if (TypeLibrary.IsSubClassOf(parEntry.Type, typekey))
                {
                    key = typekey;
                    break;
                }
            }
            if (key == "null")
                LogString("Ebx", "Logging Data", parEntry.Name, parEntry.EnumerateDependencies().ToList().Count().ToString() + " dependencies.");
            else
                LogString("Ebx", "Logging Special Data", parEntry.Name, parEntry.Type.ToString());
            AssetData parData = loggerExtensions[key].GetAssetData(parEntry, parAsset);

            //if (key != "null")
            //{
            //    foreach (EbxAssetEntry refEntry in parData.EbxReferences)
            //        LogString("Ebx-Ebx", "Linking " + parEntry.Type, parEntry.Name, refEntry.Name);
            //    foreach (ResAssetEntry resEntry in parData.Res)
            //    {
            //        LogString("Ebx-Res", "Linking " + parEntry.Type, parEntry.Name, resEntry.Name);
            //        parEntry.LinkAsset(resEntry);
            //    }
            //    foreach ((ChunkAssetEntry, int, string) chkData in parData.Chunks)
            //    {
            //        LogString("Ebx-Chunk", "Linking " + parEntry.Type, parEntry.Name, chkData.Item3. chkEntry.Name);
            //        parEntry.LinkAsset(chkEntry);
            //    }
            //}
            parData.Objects = LogNetRegData(parEntry, parAsset);
            LoggedData.Add(parEntry, parData);
        }
        private List<EbxImportReference> LogNetRegData(EbxAssetEntry refEntry, EbxAsset refAsset)
        {
            List<EbxImportReference> Objects = new List<EbxImportReference>();
            foreach (dynamic obj in refAsset.ExportedObjects)
            {
                if (BmCache.NetRegReferenceTypes.Contains(obj.GetType().Name))
                {
                    AssetClassGuid ObjGuid = ((dynamic)obj).GetInstanceGuid();
                    Objects.Add(new EbxImportReference() { FileGuid = refAsset.FileGuid, ClassGuid = ObjGuid.ExportedGuid });
                }
            }
            LogString("Ebx", "Searched for Net Reg References", refEntry.Name, String.Format("{0} references found", Objects.Count));
            return Objects;
        }

        private void LoadSwbf2FrontendAnimations(List<int> AllowedBundles)
        {
            if (!ProfilesLibrary.IsLoaded(ProfileVersion.StarWarsBattlefrontII) || !Config.Get<bool>("BMO_LoadFrontendAnimations", false))
                return;
            foreach (BundleEntry bEntry in App.AssetManager.EnumerateBundles(type: BundleType.SubLevel))
            {
                if (AllowedBundles.Contains(AM.GetBundleId(bEntry)) && bEntry.Blueprint != null && bEntry.Blueprint.Type == "LevelData" && bEntry.Name != "win32/Levels/Frontend/Frontend")
                {
                    foreach (string animation in new List<string> { "animations/antanimations/levels/frontend/frontend_win32_antstate",
                    "animations/antanimations/levels/frontend/collection_win32_antstate",})
                    {
                        EbxAssetEntry ebxEntry = AM.GetEbxEntry(animation);
                        ResAssetEntry resEntry = AM.GetResEntry(animation);
                        ebxEntry.AddToBundle(AM.GetBundleId(bEntry));
                        resEntry.AddToBundle(AM.GetBundleId(bEntry));
                    }
                }
            }
        }

        public ChunkAssetEntry GetEmptyChunk()
        {
            Guid dummyGuid = new Guid(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            ChunkAssetEntry dummyChunk = App.AssetManager.GetChunkEntry(dummyGuid);
            if (dummyChunk == null)
            {
                Guid newGuid = App.AssetManager.AddChunk(Encoding.ASCII.GetBytes("BundleManagerAssetDONOTDELETE"), dummyGuid);
                dummyChunk = App.AssetManager.GetChunkEntry(dummyGuid);
            }
            return dummyChunk;
        }

        #endregion

        #region Logging Edits

        private void LogString(string type, string description, string parent, string child) //Logs changes made by the bundle manager so that they can be exported as a csv file
        {
            LogList.AppendLine(type + "," + description + "," + parent + "," + child + "," + (stopWatch.ElapsedMilliseconds - lastTimestamp).ToString());
            lastTimestamp = stopWatch.ElapsedMilliseconds;
        }

        public void ExportLog()
        {
            if (Config.Get<bool>("BMO_EnableLogExport", true) == false)
                return;
            try
            {
                using (NativeWriter writer = new NativeWriter(new FileStream(App.FileSystem.CacheName + "_BundleManager_LogList.csv", FileMode.Create)))
                {
                    writer.WriteLine("Type, Description, Parent, Child, Time Elapsed (MS)");
                    writer.WriteLine(LogList.ToString());
                }
            }
            catch
            {
                App.Logger.Log("Could not export file " + App.FileSystem.CacheName + "_BundleManager_LogList.csv");
            }
        }

        private void CompletionMessage()
        {
            List<(string, int)> messages = new List<(string, int)>()
            {
                ($"", 1000),
                ($"Have a nice day.", 5),
                ($"Have a nice day {Environment.UserName} ;).", 1),
                ($"Did anyone catch the game last night?", 15),
                ($"Your free bundle manager trial has ended. Please subscribe to my patreon.", 3),
                ($"How much wood would a woodchuck chuck if a woodchuck could chuck wood?", 5),
                ($"By the way your mod looks shit.", 5),
                ($"Now go fuck yourself.", 5),
                ($"Does anyone even read these?", 10),
                ($"I love you.", 3),
                ($"It's official. You suck.", 3),
                ($"You're still modding this game?", 3),
                ($"Don't you have anything better to do with your life?", 3),
                ($"Slava Ukraine", 3),
                ($"STOP LOOKING AT THE SOURCE CODE", 0),
                ($"Hello {Environment.UserName}, I want to play a game.", 2),
                ($"The rest of these messages may or may not have been generated by ChatGPT.", 3),
                ($"Whispers of the past echo through the corridors of time.", 5),
                ($"Stars dance in the midnight sky, revealing secrets untold.", 4),
                ($"The river of destiny flows with twists and turns, leading to the unknown.", 3),
                ($"Lost in the labyrinth of thoughts, seeking the key to unlock reality.", 4),
                ($"Shadows whisper tales of forgotten realms, waiting for the curious heart.", 5),
                ($"The mirror reflects not just the image, but the depths within.", 5),
                ($"Beneath the surface, the riddles of existence lie in silence.", 2),
                ($"Eyes closed, yet the journey through the mind's eye begins.", 4),
                ($"A single leaf falls, carrying the weight of a universe in its descent.", 5),
                ($"In the dance of pixels, a story unfolds pixel by pixel.", 6),
                ($"The code of life intertwines with the threads of the digital realm.", 4),
                ($"Echoes of laughter linger in the corridors of forgotten timezones.", 5),
                ($"Beyond the screen, reality blurs into a canvas of imagination.", 5),
                ($"As the moon wanes, secrets of the night wax with intrigue.", 5),
                ($"Binary whispers traverse the digital winds, seeking a listener.", 3),
                ($"A puzzle of thoughts forms a mosaic of the mind's enigma.", 5),
                ($"Pages of virtual reality turn, revealing tales untethered by gravity.", 4),
                ($"Within the algorithms, a symphony of data dances to an unseen tune.", 5),
                ($"Wanderer of codes, you navigate the labyrinth of zeroes and ones.", 5),
                ($"The script of destiny writes itself in the language of serendipity.", 4),
                ($"Have you ever considered exploring new horizons beyond modding?", 5),
                ($"Time is a canvas; are you using yours to the fullest?", 5),
                ($"Life's tapestry is woven with diverse experiences. What's your next thread?", 5),
                ($"The game of life has many levels. Are you stuck on just one?", 5),
                ($"Unleash your creativity in realms beyond virtual landscapes.", 5),
                ($"Perhaps the world outside your monitor holds unexpected adventures.", 5),
                ($"Remember, the clock ticks for us all. How will you spend your moments?", 5),
                ($"Every pixel modded is a moment lost in the grand mosaic of reality.", 5),
                ($"Gaming worlds are vast, but reality's landscape is even vaster.", 5),
                ($"Challenge yourself to grow beyond the confines of virtual worlds.", 5),
                ($"I'M COMING FOR YOU {Environment.UserName.ToUpper()}.", 1),
            };
            int totalWeight = messages.Sum(x => x.Item2);
            int choice = rnd.Next(totalWeight);
            int sum = 0;

            foreach ((string, int) msg in messages)
            {
                sum += msg.Item2;
                if (choice < sum)
                {
                    if (msg.Item1 != "")
                        App.Logger.Log(msg.Item1);
                    return;
                }
            }
        }

        #endregion

    }
}
