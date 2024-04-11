using AutoBundleManagerPlugin.Logic.Operations;
using AutoBundleManagerPlugin.Logic.Precaches;
using Frosty.Core;
using Frosty.Core.Attributes;
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
using static DuplicationPlugin.DuplicationTool;

namespace AutoBundleManagerPlugin
{
    /// <summary>
    /// Class for completing a project file's bundles, filling out network registries and mvdbs.
    /// </summary>
    public class BundleCompleter
    {
        /// <summary>
        /// Used to keep track of how the time spent enuemrating
        /// </summary>
        private Stopwatch stopWatch = new Stopwatch();

        /// <summary>
        /// Used by the ABM log to find the time between the current log and the last log
        /// </summary>
        private long lastTimestamp = 0;

        /// <summary>
        /// String builder used to store the ABM log, which will be exported as a csv once the enumeration process is complete
        /// </summary>
        private StringBuilder LogList = new StringBuilder();

        /// <summary>
        /// Adds string lines to the ABM log, so that they may be added to the generated csv fi;e
        /// </summary>
        /// <param name="type">High level categorisation of the action being taken.</param>
        /// <param name="description">More detailed explanation of the action being taken.</param>
        /// <param name="parent">Used to denote the primary asset(s) involved in the operation.</param>
        /// <param name="child">Used to denote secondary asset(s) or other forms of data involved in the operation.</param>
        private void AddToLog(string type, string description, string parent, string child)
        {
            LogList.AppendLine(type + ", \t" + description + ", \t" + parent + ", \t" + child + ", \t" + (stopWatch.ElapsedMilliseconds - lastTimestamp).ToString());
            lastTimestamp = stopWatch.ElapsedMilliseconds;
        }

        /// <summary>
        /// List of ebx asset types which are known to store SubWorld Bundle References. We use this to figure out how the bundle heap has been modified by duplicated subworlds.
        /// E.g. If the subworld Levels/MP/CustomLevel/Gamemodes has a bundle reference to Levels/MP/CustomLevel/Mode1, we now know that the Mode1 bundle is a child of the Gamemodes bundle
        /// </summary>
        private static List<string> sublevelEbxTypes = new List<string> { "SubWorldData", "LevelData", "DetachedSubWorldData" };

        /// <summary>
        /// The primary operator for enumerating bundles
        /// </summary>
        /// <param name="task">TaskWindow for showing active progress to the user.</param>
        /// <param name="exportType">The type of Frosty export.</param>
        /// <param name="fbmodName">Name of the main project file's fbmod, this is used to figure out the project file's index within the load order.</param>
        /// <param name="loadOrder">List of fbmods included within the load order, these fbmods will be read, cachced, and factored into the enumeration process to ensure module compatibility.</param>
        public void CompleteBundles(FrostyTaskWindow task, ExportType exportType, string fbmodName, List<string> loadOrder)
        {
            #region Preparing Bundle Manager
            stopWatch.Start();
            //Making sure there are no lingering custom bundles edits with the static bundle heap. Custom bundles will be recalculated during this process.
            AbmBundleHeap.ClearCustomBundles();
            AbmDependenciesCache.ClearSha1Overrides();

            //Appends the project file's fbmod to the end of the load order if it is absent.
            if (!loadOrder.Contains(fbmodName))
                loadOrder.Add(fbmodName);

            //Print load order to log.
            for (int i = 0; i < loadOrder.Count(); i++)
                AddToLog("Preparation", $"Fbmod {i.ToString().PadLeft(((i - 1).ToString().Length), '0')}", loadOrder[i], loadOrder[i] == fbmodName ? "Is Loaded Fbmod" : "Is Target Project");

            #endregion 

            #region TO BE DONE (or not) - Clear Existing Bundle Edits From Project
            //To be done - Not sure if I should be doing this though, the old bundle manager did but I'm not convinced it is necessary any longer.

            #endregion

            #region Getting Dependency Active Data From Load Order & Current Project
            /// <summary>
            /// This dictionary stores the active dependency data being used to figure out what ebx, chunk, res, bundle refs, mvdbs, net reg objects, etc... which an asset is using.
            /// We store this, rather than figuring it through App.AssetManager, because other fbmods in the load order may have asset edits which override the changes made by the project file, changes we should respect.
            /// </summary>
            Dictionary<EbxAssetEntry, DependencyActiveData> loggedDependencies = new Dictionary<EbxAssetEntry, DependencyActiveData>();
            List<EbxAssetEntry> mergedAssets = new List<EbxAssetEntry>();

            void UpdatedLoggedDependencies(EbxAssetEntry parEntry, DependencyActiveData dependencies, bool isHandlerAsset = false)
            {
                if (isHandlerAsset && !mergedAssets.Contains(parEntry))
                    mergedAssets.Add(parEntry);

                string dependencyTxt = $" \t{dependencies.ebxRefs.Count()} Ebx Refs$ \t{dependencies.resRefs.Count()} Res Refs$ \t{dependencies.chkRefs.Count()} Chunks Refs$ \t{dependencies.bundleReferences.Count()} Bundle Refs$ \t{dependencies.networkRegistryRefGuids.Count()} Net Reg Refs$ \t{(dependencies.meshVariEntry == null ? "No MeshVariation" : "Includes MeshVariation")}";
                if (loggedDependencies.ContainsKey(parEntry))
                {
                    if (mergedAssets.Contains(parEntry))
                    {
                        loggedDependencies[parEntry].AppendData(dependencies);
                        AddToLog("Caching", "Overriding Data", parEntry.Name, dependencyTxt);
                    }
                    else
                    {
                        loggedDependencies[parEntry] = dependencies;
                        AddToLog("Caching", "Overriding Data", parEntry.Name, dependencyTxt);
                    }
                }
                else
                {
                    loggedDependencies.Add(parEntry, dependencies);
                    AddToLog("Caching", "Logging Data", parEntry.Name, dependencyTxt);
                }
            }

            task.SequentialForeach("ABM: Parsing Load Order", loadOrder, (loadOrderFbmod, index) =>
            {
                bool isTargetProject = loadOrderFbmod == fbmodName;
                if (!isTargetProject)
                {
                    FbmodParsing parsedFbmod = new FbmodParsing(loadOrderFbmod, task);
                    AbmDependenciesCache.AddSha1Overrides(parsedFbmod.GetModifiedSha1s());
                    foreach (FbmodParsing.ParsedModifiedEbx modifiedEbx in parsedFbmod.ParsedEbx)
                    {
                        if (!modifiedEbx.ContainsModifiedData)
                            continue;
                        AddToLog("Caching", "Getting Dependencies", modifiedEbx.Name, modifiedEbx.Hash.ToString());
                        EbxAssetEntry parEntry = App.AssetManager.GetEbxEntry(modifiedEbx.Name);

                        DependencyActiveData dependencies = AbmDependenciesCache.GetDependencies(parEntry, modifiedEbx.Hash);
                        UpdatedLoggedDependencies(parEntry, dependencies, modifiedEbx.HasHandler);
                    }
                }
                else
                {
                    task.Update("ABM: Caching Project");
                    foreach (EbxAssetEntry parEntry in App.AssetManager.EnumerateEbx())
                    {

                        if (!parEntry.HasModifiedData || parEntry.IsImaginary)
                            continue;
                        AddToLog("Caching", "Getting Dependencies", parEntry.Name, parEntry.GetSha1().ToString());
                        DependencyActiveData dependencies = AbmDependenciesCache.GetDependencies(parEntry);

                        UpdatedLoggedDependencies(parEntry, dependencies, App.PluginManager.GetCustomHandler((uint)Utils.HashString(parEntry.Type, true)) != null);
                    }
                }
            });
            task.Update("ABM: Completing Bundles", progress:0);

            #endregion

            #region Adding Custom Bundles To Bundle Heap 
            // Now that we have all bundles from each fbmod loaded, we need to add them the bundle hierarchy. Later on we will figure out parent/child relationships between specific bundles as determined by ebx.

            // List of shared bundle parents which all mpvurs in walrus seem to contain.
            List<int> mpvurParentBundles = new List<string> { "win32/default_settings",
                "win32/gameplay/bundles/sharedbundles/common/weapons/sharedbundleweapons_common",
                "win32/gameplay/wrgameconfiguration",
                "win32/gameplay/bundles/sharedbundles/frontend+mp/characters/sharedbundlecharacters_frontend+mp",
                "win32/gameplay/bundles/sharedbundles/common/vehicles/sharedbundlevehiclescockpits"
            }.Select(bunName => App.AssetManager.GetBundleId(bunName)).ToList();

            // Enumerating over all added bundles
            foreach (BundleEntry bEntry in App.AssetManager.EnumerateBundles().Where(bEntry => bEntry.Added))
            {
                // Some older project files used a dupe plugin which accidently duped bpbs into sublevels, so we need to check the name of the bundle to determine if it is really a sublevel or a bpb.
                bool isBpb = bEntry.Type == BundleType.BlueprintBundle || bEntry.Name.ToLower().EndsWith("_bpb");

                if (isBpb)
                {
                    if (bEntry.Name.StartsWith("Win32/weapons/"))
                        new BundleHeapEntry(App.AssetManager.GetBundleId(bEntry.Name), true, new List<int>() { App.AssetManager.GetBundleId("win32/gameplay/bundles/sharedbundles/common/weapons/sharedbundleweapons_common") });
                    else if (bEntry.Name.Contains("Gameplay/Bundles/SP/"))
                        new BundleHeapEntry(App.AssetManager.GetBundleId(bEntry.Name), true, new List<int>() { App.AssetManager.GetBundleId("win32/Levels/SP/RootLevel/RootLeveln") });
                    else
                        new BundleHeapEntry(App.AssetManager.GetBundleId(bEntry.Name), true, mpvurParentBundles);
                }
                else //For none bpbs (sublevels) we don't make any assumptions about what their parents are, description edits and subworld refs should let us figure that out later.
                    new BundleHeapEntry(App.AssetManager.GetBundleId(bEntry.Name), true, new List<int>());


                AddToLog("Preparation", $"Adding Custom Bundle To Heap", bEntry.Name, $"Parents:\t {string.Join("$", AbmBundleHeap.Bundles[App.AssetManager.GetBundleId(bEntry.Name)].EnumerateParentBundleIds(false).ToList())}");
            }

            #endregion

            #region Forcibly Transferring Edits Between Different Bundles
            /*
                This is an inelegant hacky solution which is necessary to get certain sublevel duplications working.
                To summarise: The bundle manager isn't accurate enough in its methods of determining ebx/chunk/res references to properly map out all dependency relationships across the game. 
                This can cause crashes in game since some required assets will not have been told to load by the bundle manager.
                This workaround involves forcibly moving all bundle edits from a source sublevel to a target sublevel.
                This near guarantees every asset will be loaded if done correctly, but can also be memory inefficient as it means that some unused assets will also be loaded in memory.
                This WILL NOT work for transferring edits between two added bundles, as this process occurs too early.
            */

            //Enumerating over the forced bundle transfers, as determined in the user's Frosty Options. This cannot be derived through the project file alone.
            foreach (KeyValuePair<string, List<string>> forcedTransfers in AutoBundleManagerOptions.ForcedBundleTransfers)
            {
                //Get the target bundle and make sure it actually exists in this project file, else it may be intended for a different project file
                string targBunName = forcedTransfers.Key;
                int targBunId = App.AssetManager.GetBundleId(targBunName);
                if (targBunId != -1)
                {
                    foreach (string sourceBunName in forcedTransfers.Value)
                    {
                        //Get the source bundle and make sure it actually exists in this project file, else it may be intended for a different project file
                        int sourceBunId = App.AssetManager.GetBundleId(sourceBunName);
                        if (sourceBunId != -1)
                        {
                            //Add each ebx and res from the target bundle to the source one, chunks need to be done separately cause of FirstMip/H32
                            List<AssetEntry> assetsToAdd = new List<AssetEntry>(App.AssetManager.EnumerateEbx().Where(o => o.IsInBundle(sourceBunId)));
                            assetsToAdd.AddRange(App.AssetManager.EnumerateRes().Where(o => o.IsInBundle(targBunId)));

                            AddToLog("Bundle Transfer", $"Moving assets to bundle", $"{assetsToAdd.Count() + App.AssetManager.EnumerateChunks().Where(o => o.IsInBundle(sourceBunId)).Count()} assets", $"{sourceBunName} - {targBunName}");

                            foreach (AssetEntry refEntry in assetsToAdd)
                            {
                                //Ignore the asset if it is the primary asset/blueprint of the bundle being transferred
                                if (refEntry != App.AssetManager.GetEbxEntry(sourceBunName.Substring(6, 0)))
                                {
                                    switch (refEntry.Type)
                                    {
                                        case "NetworkRegistryAsset": //TO BE DONE - FIND A WAY TO MERGE NET REGS IF MORE THAN ONE SOURCE BUNDLE IS BEING TRANSFERRED --- ALSO FIGURE OUT A WAY TO ACKNOWLEDGE EDITS MADE TO THE NET REG REFERENCES FOR EBX ASSETS
                                            //if (!BlockNetworkRegistries)
                                            //{
                                            //    //EbxAssetEntry netEntry = new DuplicateAssetExtension().DuplicateAsset((EbxAssetEntry)refEntry, bEntry.Name.ToLower().Substring(6) + "_networkregistry_Win32", false, null);
                                            //    //netEntry.AddedBundles.Clear();
                                            //    //netEntry.AddToBundle(newBunId);
                                            //    //LogString(netEntry.AssetType, "Duplicating original network registry", netEntry.Name, App.AssetManager.GetBundleEntry(newBunId).Name);
                                            //}
                                            break;
                                        case "MeshVariationDatabase": //TO BE DONE - FIND A WAY TO MERGE MVDBS IF MORE THAN ONE SOURCE BUNDLE IS BEING TRANSFERRED --- ALSO FIGURE OUT A WAY TO ACKNOWLEDGE EDITS MADE TO THE MVDB FOR EBX ASSETS
                                            //EbxAssetEntry mvEntry = new DuplicateAssetExtension().DuplicateAsset((EbxAssetEntry)refEntry, bEntry.Name.Replace("win32/", "") + "/MeshVariationDb_Win32", false, null);
                                            //mvEntry.AddedBundles.Clear();
                                            //mvEntry.AddToBundle(newBunId);
                                            //LogString(mvEntry.AssetType, "Duplicating original meshvariationdb", mvEntry.Name, App.AssetManager.GetBundleEntry(newBunId).Name);
                                            break;
                                        default:
                                            if (refEntry.IsInBundle(targBunId))
                                                continue;
                                            refEntry.AddToBundle(targBunId);
                                            AddToLog("Bundle Transfer", $"Transfer {refEntry.AssetType} to bundle", refEntry.Name, $"{sourceBunName} - {targBunName}");
                                            break;
                                    }
                                }
                            }
                            foreach (ChunkAssetEntry chunkEntry in App.AssetManager.EnumerateChunks().Where(o => o.IsInBundle(sourceBunId)))
                            {
                                if (chunkEntry.IsInBundle(targBunId))
                                    continue;
                                if (ChunkH32Precache.chunkH32Cached.ContainsKey(chunkEntry) && chunkEntry.FirstMip == -1)
                                {
                                    //Assign FirstMip/H32 of the chunk, this should not be important to Kyber but Frosty still needs this info
                                    chunkEntry.FirstMip = ChunkH32Precache.chunkH32Cached[chunkEntry].Item1;
                                    chunkEntry.H32 = ChunkH32Precache.chunkH32Cached[chunkEntry].Item2;
                                    AddToLog(chunkEntry.AssetType, "Setting H32&Firstmip (h32 cache)", ChunkH32Precache.chunkH32Cached[chunkEntry].Item1.ToString(), ChunkH32Precache.chunkH32Cached[chunkEntry].Item2.ToString());
                                }
                                chunkEntry.AddToBundle(targBunId);
                                AddToLog("Bundle Transfer", $"Transfer {chunkEntry.AssetType} to bundle", chunkEntry.Name, $"{sourceBunName} - {targBunName}");
                            }
                        }
                    }
                }
            }

            #endregion

            #region List Modified Assets Per Bundle For Enumerating Over Later
            /*
                Using a dictionary, we store a list of every bundle which contains modified assets. This includes assets which have previously been added to the bundle.
                While doing this, we also check to see if any of our modified assets have been manipulating the bundle heap by adding new parent/child bundle relationships
            */

            //The dictionary for storing the modified assets of each bundle.
            Dictionary<int, List<EbxAssetEntry>> bundlesModifiedAssets = new Dictionary<int, List<EbxAssetEntry>>();

            foreach (KeyValuePair<EbxAssetEntry, DependencyActiveData> pairDependancy in loggedDependencies)
            {
                //Ignore if res - we don't care about checking res files since they should all be referenced by an ebx anyway
                if (pairDependancy.Key.AssetType != "ebx")
                    continue;
                EbxAssetEntry parEntry = pairDependancy.Key;

                //Enumerate over the ebx's bundles and add the ebx entry to the dictionary entry of the bundle id.
                foreach (int bunId in parEntry.EnumerateBundles())
                {
                    AddToLog("Bundle Discovery", $"Detected Modified Asset", App.AssetManager.GetBundleEntry(bunId).Name, parEntry.Name);

                    if (bundlesModifiedAssets.ContainsKey(bunId))
                        bundlesModifiedAssets[bunId].Add(parEntry);
                    else
                        bundlesModifiedAssets.Add(bunId, new List<EbxAssetEntry> { parEntry });
                }

                //Enumerate over any text-based bundle references made by the ebx asset (typically done by vurs, subworlds, description assets.)
                foreach (KeyValuePair<string, HashSet<string>> bunParents in pairDependancy.Value.bundleReferences)
                {
                    //Check bundle actually exists
                    int bunId = App.AssetManager.GetBundleId("win32/" + bunParents.Key);
                    if (bunId != -1)
                    {
                        //Bundle heap entry should already exist - If it doesn't then something has gone horribly wrong.
                        BundleHeapEntry heapEntry = AbmBundleHeap.Bundles[bunId];
                        BundleEntry bEntry = App.AssetManager.GetBundleEntry(bunId);

                        //Some older project files used a dupe plugin which accidently duped bpbs into sublevels, so we need to check the name of the bundle to determine if it is really a sublevel or a bpb.
                        bool isBpb = bEntry.Type == BundleType.BlueprintBundle || bEntry.Name.ToLower().EndsWith("_bpb");

                        //Function used for adding a bundle parent to the target bundle heap entry, assuming it is not already a parent and actually exists.
                        void TryAddParent(string bunName)
                        {
                            int bunParId = App.AssetManager.GetBundleId(bunName);
                            if (bunParId != -1 && !heapEntry.EnumerateParentBundleIds(false).Contains(bunParId))
                            {
                                heapEntry.CustomParentIds.Add(bunParId);
                                AddToLog("Bundle Discovery", "Detected Bundle Parent", bunName, bEntry.Name);
                            }
                        }

                        //Some ebx (vurs) will actually declare a parent within the ebx itself 
                        foreach (string bunParStr in bunParents.Value)
                            TryAddParent("win32/" + bunParStr);

                        //Sublevels do not declare their parents through the ebx, but it can be derived by simply looking at the bundles used by the asset which makes the reference to a child bundle
                        if (!isBpb && sublevelEbxTypes.Contains(parEntry.Type))
                        {
                            TryAddParent("win32/" + parEntry.Name);
                            foreach (int bunParId in parEntry.EnumerateBundles())
                                TryAddParent(App.AssetManager.GetBundleEntry(bunParId).Name);
                        }

                        //AddToLog("Caching", "Modifying Bundle Hierarchy", bunParents.Key, string.Join(", ", bunParents.Value));
                    }
                }
            }

            #endregion 

            #region Verify Heap Integrity & Plan Bundle Enumeration Order
            /*
                It is possible (and is known to happen) for a modder to cause an infinite bundle loop.
                This is caused by having one bundle be a parent to a bundle, which it is simultaneously a child of.
                This will cause stack overflow errors with the bundle manager, as the bundle manager will endlessly loop over the parents.
                To prevent this I have created a function to verify the integrity of the heap and cancel the bundle manager if there are infinite loops, with an appropriate warning to the user.
            */
            int addedBundlesCount = AbmBundleHeap.Bundles.Select(pair => pair.Value).Where(heapEntry => heapEntry.IsCustomBundle).ToList().Count();
            int modifiedBundlesCount = AbmBundleHeap.Bundles.Select(pair => pair.Value).Where(heapEntry => !heapEntry.IsCustomBundle && heapEntry.CustomParentIds.Count() > 0).ToList().Count();
            AddToLog("Bundle Planning", "Verifying Heap Integrity", $"{addedBundlesCount} Added Bundles", $"{modifiedBundlesCount} Modified Bundles");
            if (AbmBundleHeap.VerifyHeapIntegrity(true))
                return;
            AddToLog("Bundle Planning", "Verified Heap Integrity", $"{addedBundlesCount} Added Bundles", $"{modifiedBundlesCount} Modified Bundles");

            /*
                With the heap integrity verified, the next step is to determine the enumeration order of the bundle manager on a per bundle basis.
                The reason we do this in an order, is because parent bundles should be filled out before child bundles.
                If this is not done a child & parent bundle may end up sharing assets, which though not catastrophic, is memory inefficient and so best to be avoided.
            */

            //Using a list to store the order of bundles, since a list has structure (unlikely something like a hashset).
            List<int> bundleOrder = new List<int>();
            //Enumerate over all the bundles which we know there are modified assets within, since those are the ones we know we will be enumerating over later.
            foreach (int bunId in bundlesModifiedAssets.Keys)
            {
                //Make sure a bundle hasn't already been added to the order, else it will be pointless to check.
                if (bundleOrder.Contains(bunId))
                    continue;

                BundleHeapEntry heapEntry = AbmBundleHeap.Bundles[bunId];
                //Enumerate over all parents (including parents of those parents) of a bundle - Using .Reverse() so that the first bundles which appear in this list are the highest parents (the first to be loaded into game memory)
                foreach (int parBunId in heapEntry.EnumerateParentBundleIds().Reverse())
                {
                    if (!bundleOrder.Contains(parBunId))
                    {
                        bundleOrder.Add(parBunId);
                        AddToLog("Bundle Planning", "Adding Bundle To Order", App.AssetManager.GetBundleEntry(parBunId).Name, App.AssetManager.GetBundleEntry(bunId).Name);
                    }
                }
                bundleOrder.Add(bunId);
            }

            #endregion

            #region Kyber Launch Only - Remove Sublevels From Bundle Order Which We're Not Launching To

            if (exportType == ExportType.KyberLaunchOnly)
            {
                AddToLog("Bundle Planning", "Limiting Bundle Order", KyberSettings.Level, "Levels/Frontend/Frontend");
                if (App.AssetManager.GetEbxEntry(KyberSettings.Level) == null || App.AssetManager.GetEbxEntry(KyberSettings.Level).EnumerateBundles().ToList().Count() == 0)
                {
                    App.Logger.LogError($"Cannot find level {KyberSettings.Level}");
                    return;
                }
                HashSet<int> levelBundles = new HashSet<int>(new HashSet<string>() { "Levels/Frontend/Frontend", KyberSettings.Level }.Select(x => App.AssetManager.GetEbxEntry(x).EnumerateBundles().ToList()[0]));
                foreach(int bunId in new List<int> (bundleOrder))
                {
                    if (App.AssetManager.GetBundleEntry(bunId).Type != BundleType.SubLevel)
                        continue;
                    BundleHeapEntry heapEntry = AbmBundleHeap.Bundles[bunId];

                    if (!levelBundles.Any(levBunId => levBunId == bunId || heapEntry.EnumerateParentBundleIds().Contains(levBunId)))
                    {
                        bundleOrder.Remove(bunId);
                        AddToLog("Bundle Planning", "Removing Bundle From Enumeration", App.AssetManager.GetBundleEntry(bunId).Name, App.AssetManager.GetBundleEntry(bunId).Imaginary ? "Is Real" : "Is Imaginary");
                    }
                }
            }


            #endregion

            #region Bundle Enumeration
            /*
                Finally we are ready to enumerate across all of our bundles and add asset dependencies to them.
                This process involves three stages for each bundle:
                    1) Enumerate across all modified assets already loaded in the bundle, add any unloaded dependencies to the bundle, then do the same to their dependencies.
                    2) Fill out the bundle's MeshVariationDatabase (or create one if it doesn't have it).
                    3) Fill out the bundle's NetworkRegistry (or create one if it doesn't have it). WARNING - This should not be done for visual only (cosmetic) blueprint bundles (vurs) as they should be client side only, not networked.
            */

            foreach (int bunId in bundleOrder)
            {
                BundleEntry bEntry = App.AssetManager.GetBundleEntry(bunId);
                string bunName = bEntry.Name;
                AddToLog("Completing Bundle", "Starting Enumeration Of Bundle", bunName, $"{(bundlesModifiedAssets.ContainsKey(bunId) ? bundlesModifiedAssets[bunId].Count() : 0)} Assets");

                BundleHeapEntry heapEntry = AbmBundleHeap.Bundles[bunId];
                //Get parents of the target bundle, this will be used to check that dependencies aren't already loaded into memory, and therefore don't need to be added to the target bundle.
                List<int> parentBunIds = heapEntry.EnumerateParentBundleIds().ToList();

                //List of MeshVariationEntries to add to the bundles' MVDB.
                List<AbmMeshVariationDatabaseEntry> meshVariationsToAdd = new List<AbmMeshVariationDatabaseEntry>();
                //List of PointerRefs to add to the Network Registry.
                List<PointerRef> netRegPointerRefsToAdd = new List<PointerRef>();

                /// <summary>
                /// Adds dependencies of the target ebx asset to the bundle, if they are detected as not already loaded.
                /// If the target ebx has just beeen added to the bundle, then it will also append it's MVDB/NetReg References to the aforementioned list.
                /// </summary>
                /// <param name="parEntry">Target Ebx to investigate dependencies of.</param>
                /// <param name="addSelfToRegistries">Determines if the ebx has just been added to the bundle, and therefore it should be added to MVDBs/NetRegs</param>
                void AddDependenciesToBundle(EbxAssetEntry parEntry, bool addSelfToRegistries = true)
                {
                    //Get the dependency active data, checking to see if the asset is within our loggedDependencies dictionary, since that will mean that load order fbmod overrides are prioritised over what App.AssetManager says the asset's Sha1 is.
                    DependencyActiveData dependencies = loggedDependencies.ContainsKey(parEntry) ? loggedDependencies[parEntry] : AbmDependenciesCache.GetDependencies(parEntry);
                    AddToLog("Completing Bundle", "Checking Dependencies Of Ebx", parEntry.Name, loggedDependencies.ContainsKey(parEntry) ? "IsModified" : "IsUnmodified");

                    //Enumerate Ebx dependencies
                    foreach (EbxAssetEntry ebxEntry in dependencies.ebxRefs)
                    {
                        //Check the ebx is loaded in this bundle or it's parents, add it to the bundle if it isn't.
                        bool addToBundle = !ebxEntry.IsInBundleHeap(bunId, parentBunIds);
                        if (addToBundle)
                        {
                            ebxEntry.AddToBundle(bunId);
                            AddToLog("Completing Bundle", "Adding Ebx To Bundle", parEntry.Name, bunName);
                        }

                        //Enumerate over dependencies of ths ebx if we have just added it to the bundle.
                        //There is a super rare case in Walrus where a ShaderGraph's ebx is loaded prior to the res file (for some reason), as a result I've made this rare exemption case to check the dependencies of a ShaderGraph even if the ebx is already loaded.
                        if (addToBundle || ebxEntry.Type == "ShaderGraph")
                            AddDependenciesToBundle(ebxEntry, addToBundle);
                    }

                    //Enumerate Res dependencies
                    foreach (ResAssetEntry resEntry in dependencies.resRefs)
                    {
                        //Check the res is loaded in this bundle or it's parents, add it to the bundle if it isn't.
                        if (!resEntry.IsInBundleHeap(bunId, parentBunIds))
                        {
                            resEntry.AddToBundle(bunId);
                            AddToLog("Completing Bundle", "Adding Res To Bundle", resEntry.Name, bunName);

                            //Check to see if the res is a ShaderBlockDepot for a mesh's ObjectVariation, if it is and has just been added to this bundle then make sure the MeshVariationDatabaseEntry is also added. 
                            //WARNING - This will not work for duplicated ObjectVariations, which are deliberately not supported by the bundle manager.
                            if (AbmMeshVariationDatabasePrecache.VariationMvdbDatabase.ContainsKey(resEntry.Name))
                            {
                                meshVariationsToAdd.Add(AbmMeshVariationDatabasePrecache.VariationMvdbDatabase[resEntry.Name]);
                                AddToLog("Completing Bundle", "Adding ObjectVariation MVDB Entry", resEntry.Name, bunName);
                            }
                        }
                    }

                    //Enumerate Chunk dependencies
                    foreach (KeyValuePair<ChunkAssetEntry, int> chkPair in dependencies.chkRefs)
                    {
                        //Check the res is loaded in this bundle or it's parents, add it to the bundle if it isn't.
                        ChunkAssetEntry chkEntry = chkPair.Key;
                        if (!chkEntry.IsInBundleHeap(bunId, parentBunIds))
                        {
                            chkEntry.AddToBundle(bunId);
                            AddToLog("Completing Bundle", "Adding Chunk To Bundle", chkEntry.Name, bunName);

                            //Add the firstmip and h32 of the chunk, this is not necessary for Kyber but Frosty launches need this information.
                            chkEntry.FirstMip = chkPair.Value;
                            chkEntry.H32 = (int)Utils.HashString(parEntry.Name, true);
                        }
                    }

                    //If this ebx has just been added to the bundle then we need to add any mvdb entries it has, or net reg refs to the appropriate lists.
                    if (addSelfToRegistries)
                    {
                        //Add MVDB Entry to list if it exists
                        if (dependencies.meshVariEntry != null)
                        {
                            meshVariationsToAdd.Add(dependencies.meshVariEntry);
                            AddToLog("Completing Bundle", "Adding MeshAsset MVDB Entry", parEntry.Name, bunName);
                        }

                        //Create a PointerRef for all the possible net reg export guids, then add the PointerRefs to the list.
                        foreach (Guid classGuid in dependencies.networkRegistryRefGuids)
                            netRegPointerRefsToAdd.Add(new PointerRef(new EbxImportReference() { FileGuid = dependencies.srcGuid, ClassGuid = classGuid }));
                        if (dependencies.networkRegistryRefGuids.Count() > 0)
                            AddToLog("Completing Bundle", "Adding Network Registry Objects", $"{parEntry.Name} ({dependencies.networkRegistryRefGuids.Count()})", bunName);
                    }
                }

                //Enumerate Modified Assets and add  dependencies to the bundles
                if (bundlesModifiedAssets.ContainsKey(bunId))
                {
                    //While enumerating make sure to set addSelfToRegistries to false, otherwise these already added ebx assets will get readded to MVDBs and Network Registries.
                    foreach (EbxAssetEntry parEntry in bundlesModifiedAssets[bunId])
                        AddDependenciesToBundle(parEntry, false);
                }

                //Add MeshVariationDatabase Entries
                if (meshVariationsToAdd.Count() > 0)
                {

                    string mvdbName = bEntry.Name.ToLower().Substring(6) + "/MeshVariationDb_Win32";
                    EbxAssetEntry mvdbEntry = App.AssetManager.GetEbxEntry(mvdbName);
                    EbxAsset mvdbAsset;

                    //Check if the MVDB already exists and is not imaginary - if it doesn't then create a new MVDB for this bundle.
                    if (mvdbEntry == null || mvdbEntry.IsImaginary)
                    {
                        //Revert if there is an imaginary asset
                        if (mvdbEntry != null)
                            App.AssetManager.RevertAsset(mvdbEntry);

                        EbxAsset newAsset = new EbxAsset(TypeLibrary.CreateObject("MeshVariationDatabase"));
                        newAsset.SetFileGuid(Guid.NewGuid());

                        dynamic obj = newAsset.RootObject;
                        obj.Name = mvdbName;

                        AssetClassGuid guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(newAsset.Objects, (Type)obj.GetType(), newAsset.FileGuid), -1);
                        obj.SetInstanceGuid(guid);

                        EbxAssetEntry newEntry = App.AssetManager.AddEbx(mvdbName, newAsset);
                        if (newEntry.ModifiedEntry == null) //Super rare issue with the old bundle manager, can't remember the cause but probably had something to do with handlers
                            throw new Exception(string.Format("App.AssetManager.AddEbx() failed to create new MeshVariationDatabase EbxAsset for bundle: {0}\nUnknown cause and how to fix", App.AssetManager.GetBundleEntry(bunId).Name));

                        newEntry.AddedBundles.Add(bunId);
                        newEntry.ModifiedEntry.DependentAssets.AddRange(newAsset.Dependencies);
                        mvdbAsset = App.AssetManager.GetEbx(newEntry);
                        AddToLog("Completing Bundle", "Creating MeshVariationDatabase", mvdbName, bunName);
                    }
                    else
                        mvdbAsset = App.AssetManager.GetEbx(mvdbEntry);


                    dynamic mvdbRoot = mvdbAsset.RootObject;
                    //Enumerate across every MeshVariation Entry and then write them to the game version
                    foreach (AbmMeshVariationDatabaseEntry mvEntryAbm in meshVariationsToAdd)
                        mvdbRoot.Entries.Add(mvEntryAbm.WriteToGameEntry());

                    AddToLog("Completing Bundle", "Adding MeshVariationDatabase Entries", mvdbName, meshVariationsToAdd.Count().ToString());

                    App.AssetManager.ModifyEbx(mvdbName, mvdbAsset);
                }

                //Add Network Registry Objects
                if (netRegPointerRefsToAdd.Count > 0)
                {

                    string netregName = App.AssetManager.GetBundleEntry(bunId).Name.ToLower().Substring(6) + "_networkregistry_Win32";
                    EbxAssetEntry netregEntry = App.AssetManager.GetEbxEntry(netregName);
                    EbxAsset netregAsset;

                    //Check if the Network Registry already exists and is not imaginary - if it doesn't then create a new MVDB for this bundle.
                    if (netregEntry == null || netregEntry.IsImaginary)
                    {
                        //Revert if there is an imaginary asset
                        if (netregEntry != null)
                            App.AssetManager.RevertAsset(netregEntry);

                        EbxAsset newAsset = new EbxAsset(TypeLibrary.CreateObject("NetworkRegistryAsset"));
                        newAsset.SetFileGuid(Guid.NewGuid());

                        dynamic obj = newAsset.RootObject;
                        obj.Name = netregName;

                        AssetClassGuid guid = new AssetClassGuid(Utils.GenerateDeterministicGuid(newAsset.Objects, (Type)obj.GetType(), newAsset.FileGuid), -1);
                        obj.SetInstanceGuid(guid);

                        EbxAssetEntry newEntry = App.AssetManager.AddEbx(netregName, newAsset);
                        if (newEntry.ModifiedEntry == null)
                            throw new Exception(string.Format("App.AssetManager.AddEbx() failed to create new NetworkRegistryAsset EbxAsset for bundle: {0}\nUnknown cause and how to fix", App.AssetManager.GetBundleEntry(bunId).Name));

                        newEntry.AddedBundles.Add(bunId);
                        newEntry.ModifiedEntry.DependentAssets.AddRange(newAsset.Dependencies);
                        netregAsset = App.AssetManager.GetEbx(newEntry);
                        AddToLog("Completing Bundle", "Creating NetworkRegistryAsset", netregName, bunName);
                        //LogString("Bundle", "Adding NetworkRegistryAsset", newMeshVariName, NetworkRegistryAsset.Count.ToString() + " entries added");
                    }
                    else
                        netregAsset = App.AssetManager.GetEbx(netregEntry);

                    dynamic netregRoot = netregAsset.RootObject;
                    foreach (PointerRef pr in netRegPointerRefsToAdd)
                        netregRoot.Objects.Add(pr);

                    AddToLog("Completing Bundle", "Adding NetworkRegistry PointerRefs Entries", netregName, netRegPointerRefsToAdd.Count().ToString());
                    App.AssetManager.ModifyEbx(netregName, netregAsset);
                }

            }

            #endregion

            #region Forcibly Adding Assets To Bundles
            /*
                Another inelegant hacky solution which is specifically designed for force some animation files to be loaded across each level.
                This function allows users to guarantee an asset's ebx and/or res file is loaded into a target bundle.
                This does not take into account dependencies nor mvdbs or net regs, as they are not important for asset banks. 
                Nonetheless, they probably should be added at some point.
                We do this after completing the bundle enumeration to give the bundle manager a chance to add the assets "properly"
            */
            foreach (KeyValuePair<string, List<string>> forcedBundleEdit in AutoBundleManagerOptions.ForcedBundleEdits)
            {
                List<int> bunIds = forcedBundleEdit.Value.Where(bunName => App.AssetManager.GetBundleId(bunName) != -1).Select(bunName => App.AssetManager.GetBundleId((string)bunName)).ToList();
                foreach (AssetEntry targEntry in new List<AssetEntry> { App.AssetManager.GetEbxEntry(forcedBundleEdit.Key), App.AssetManager.GetResEntry(forcedBundleEdit.Key.ToLower()) })
                {
                    //Check asset actually exists in project file
                    if (targEntry != null)
                    {
                        //Add to bundle heap
                        foreach (int bunId in bunIds)
                        {
                            BundleHeapEntry heapEntry = AbmBundleHeap.Bundles[bunId];
                            if (!targEntry.IsInBundleHeap(bunId, heapEntry.EnumerateParentBundleIds().ToList()))
                                targEntry.AddToBundle(bunId);
                        }
                    }
                }
            }

            #endregion

            #region Post Completion Actions

            //Write Log out to csv - Try Catch because common csv readers (Excel) stupidly lock the file while opened, causing a crash if I tried to write to it through this.
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

            AbmDependenciesCache.ClearSha1Overrides();
            //During this process we have probably discovered several new asset dependencies so we should cache that (the cache class will make sure it actually does need to be updated before writing to disk.)
            AbmDependenciesCache.UpdateCache();
            //Reset the bundle heap, clearing any custom bundles or bundle parent changes made during this process.
            AbmBundleHeap.ClearCustomBundles();

            //Stop the stopwatch and print to logger
            stopWatch.Stop();
            App.Logger.Log(string.Format("Bundle Manager Completed in {0} seconds.", stopWatch.Elapsed));

            #endregion
        }

        public void ClearBundles()
        {
            foreach (BundleEntry newBunEntry in App.AssetManager.EnumerateBundles().Where(blueEntry => blueEntry != null && blueEntry.Added && blueEntry.Type != BundleType.SharedBundle && blueEntry.Blueprint == null))
                newBunEntry.Blueprint = App.AssetManager.GetEbxEntry(newBunEntry.Name.Replace("win32/", ""));

            List<EbxAssetEntry> bundleBlueprints = App.AssetManager.EnumerateBundles().Select(bEntry => bEntry.Blueprint).Where(blueEntry => blueEntry != null && blueEntry.IsAdded).ToList();
            List<EbxAssetEntry> ebxEntries = App.AssetManager.EnumerateEbx(modifiedOnly: true).ToList();
            foreach (EbxAssetEntry refEntry in App.AssetManager.EnumerateEbx().ToList())
            {
                if ((!refEntry.IsModified && !refEntry.IsImaginary) || (bundleBlueprints.Contains(refEntry) && !refEntry.IsImaginary))
                    continue;
                if (refEntry.Type == null || refEntry.Type == "NetworkRegistryAsset" || refEntry.Type == "MeshVariationDatabase" || refEntry.IsImaginary)
                    App.AssetManager.RevertAsset(refEntry);
                else
                {
                    refEntry.AddedBundles.Clear();
                    if (!refEntry.HasModifiedData || !refEntry.ModifiedEntry.IsDirty)
                        refEntry.IsDirty = false;
                }
            }
            foreach (ChunkAssetEntry chkEntry in App.AssetManager.EnumerateChunks(modifiedOnly: true).ToList())
            {
                if (chkEntry.IsImaginary)
                    App.AssetManager.RevertAsset(chkEntry);
                else
                {
                    chkEntry.AddedBundles.Clear();
                    if (!chkEntry.IsAdded)
                        chkEntry.FirstMip = -1;
                    if (!chkEntry.HasModifiedData || !chkEntry.ModifiedEntry.IsDirty)
                        chkEntry.IsDirty = false;
                }
            }
            foreach (ResAssetEntry resEntry in App.AssetManager.EnumerateRes(modifiedOnly: true).ToList())
            {
                if (resEntry.IsImaginary)
                    App.AssetManager.RevertAsset(resEntry);
                else
                {
                    resEntry.AddedBundles.Clear();
                    if (!resEntry.HasModifiedData || !resEntry.ModifiedEntry.IsDirty)
                        resEntry.IsDirty = false;
                }
            }

            foreach(BundleEntry bEntry in App.AssetManager.EnumerateBundles().ToList())
            {
                if (bEntry.Imaginary)
                    App.AssetManager.RevertBundle(bEntry);
            }
        }

    }
}
