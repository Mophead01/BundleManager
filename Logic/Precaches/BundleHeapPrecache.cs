using Frosty.Core;
using FrostySdk.Managers;
using FrostySdk;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Frosty.Core.Windows;
using FrostySdk.IO;
using System.IO;
using System.Xml.Linq;
using Frosty.Hash;
using System.Reflection;
using FrostySdk.Ebx;
using FrostySdk.Attributes;

namespace AutoBundleManagerPlugin
{
    public class BundleHeapEntry
    {
        public int BundleId { get; private set; }
        public bool IsCustomBundle { get; private set; } = false;
        public List<int> ParentIds { get; private set; } = new List<int>();
        public List<int> CustomParentIds { get; private set; } = new List<int>();

        #region Parent Bundle Id Functions

        /// <summary>
        /// Enumerates the parent IDs of the current object, including custom parent IDs, and optionally grandparent IDs. Ordering is not important
        /// </summary>
        /// <param name="enumerateGrandparents">Specifies whether to include grandparent IDs in the enumeration. Default is true.</param>
        /// <returns>An IEnumerable<int> containing the parent IDs of the current object.</returns>
        public IEnumerable<int> EnumerateParentBundleIds(bool enumerateGrandparents = true)
        {
            // Combine the default and custom parent IDs into a single list
            List<int> fullParentIds = new List<int>();

            // If specified, enumerate grandparent IDs by recursively appending parents to the list
            if (enumerateGrandparents)
            {
                // Iterate through direct parent IDs and append their parents to the list
                foreach (int parentId in ParentIds.Concat(CustomParentIds))
                    AppendParentBundleIdsToList(ref fullParentIds, parentId);
            }
            else
                fullParentIds = ParentIds.Concat(CustomParentIds).ToList();

            // Return the final list of parent IDs
            return fullParentIds;
        }

        /// <summary>
        /// Recursively appends parent bundle IDs to the provided list based on the specified bundle ID.
        /// </summary>
        /// <param name="parentIdList">The list to which parent bundle IDs are appended.</param>
        /// <param name="bunId">The bundle ID for which parents are to be appended.</param>
        private void AppendParentBundleIdsToList(ref List<int> parentIdList, int bunId)
        {

            // Check if the bundle is present in the BundleHeap
            if (AbmBundleHeap.Bundles.ContainsKey(bunId))
            {
                // Iterate through the parents of the current bundle and recursively append their parents
                foreach (int parentBunId in AbmBundleHeap.Bundles[bunId].EnumerateParentBundleIds(false))
                {
                    // Check if the parent bundle ID is not already in the list before appending
                    if (!parentIdList.Contains(parentBunId))
                        AppendParentBundleIdsToList(ref parentIdList, parentBunId);
                }
            }
            else
            {
                // Throw an exception if the bundle is not found in the BundleHeap. If the BM script works this should never happen.
                throw new Exception($"Logic Error: Bundle not logged by bundle heap \"{App.AssetManager.GetBundleEntry(bunId).Name}\"");
            }

            // Add the current bundle ID to the list
            parentIdList.Insert(0, bunId);
        }

        /// <summary>
        /// Adds a parent bundle ID to the current object's parent lists based on the specified bundle ID and custom parent flag.
        /// </summary>
        /// <param name="bunId">The bundle ID to be added as a parent.</param>
        /// <param name="customParent">A flag indicating whether the parent is a custom parent. If false, it is considered a default parent.</param>
        public void AddParentBundleId(int bunId, bool customParent)
        {
            // Check if the specified bundle ID is already among the parents
            if (EnumerateParentBundleIds(false).Contains(bunId))
                return;

            // Determine whether to add the parent ID to default or custom parents based on the flag
            if (!customParent)
                ParentIds.Add(bunId);
            else
                CustomParentIds.Add(bunId);
        }

        /// <summary>
        /// Clears the list of custom parent bundle IDs associated with the current object.
        /// </summary>
        public void ClearCustomParentBundleIds()
        {
            // Clears the list of custom parent bundle IDs
            CustomParentIds.Clear();
        }

        /// <summary>
        /// Initializes a new instance of the BundleHeapEntry class with the specified parameters.
        /// </summary>
        /// <param name="bundleId">The unique identifier of the bundle.</param>
        /// <param name="isCustomBundle">A flag indicating whether the bundle is a custom bundle.</param>
        /// <param name="parentsToAdd">A list of parent bundle IDs to be associated with the new entry.</param>
        /// 

        #endregion
        public BundleHeapEntry(int bundleId, bool isCustomBundle, List<int> parentsToAdd)
        {
            BundleId = bundleId;
            IsCustomBundle = isCustomBundle;
            // Initialize parent lists based on the custom bundle flag
            if (!isCustomBundle)
                // If it's a custom bundle, set ParentIds to the provided list
                ParentIds = parentsToAdd;
            else
                // If it's not a custom bundle, set CustomParentIds to the provided list
                CustomParentIds = parentsToAdd;

            // Add the new entry to the BundleHeap.Bundles dictionary using the bundle ID as the key
            AbmBundleHeap.Bundles.Add(bundleId, this);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class BundleHeapEntryViewer
    {
        [DisplayName("Name")]
        [Description("Bundle Name")]
        [IsReadOnly]
        public CString BundleName { get; set; }

        [DisplayName("Name")]
        [Description("Bundle Parent Names")]
        [IsReadOnly]
        public List<CString> BundleParents { get; set; }
        public BundleHeapEntryViewer(BundleHeapEntry heapEntry)
        {
            BundleName = App.AssetManager.GetBundleEntry(heapEntry.BundleId).Name;
            BundleParents = heapEntry.EnumerateParentBundleIds().Select(parId => new CString(App.AssetManager.GetBundleEntry(parId).Name)).ToList();
        }
        public BundleHeapEntryViewer()
        {

        }
    }
    public static class AbmBundleHeap
    {
        public static Dictionary<int, BundleHeapEntry> Bundles = new Dictionary<int, BundleHeapEntry>();
        public static void ClearCustomBundles()
        {
            Dictionary<int, BundleHeapEntry> bundlesTempCopy = new Dictionary<int, BundleHeapEntry>(Bundles);
            foreach (KeyValuePair<int, BundleHeapEntry> pair in bundlesTempCopy)
            {
                pair.Value.ClearCustomParentBundleIds();
                if (pair.Value.IsCustomBundle)
                    Bundles.Remove(pair.Key);
            }
        }
        //Verifies there's no bundle parent-child loop collisions. Expensive so use only when needed
        public static bool VerifyHeapIntegrity(bool checkCustomOnly)
        {
            bool integrityCompromised = false;
            List<List<int>> foundLoops = new List<List<int>>();
            void VerifyParents(int bunId, List<int> checkedChildrenBunIds)
            {
                if (AbmBundleHeap.Bundles.ContainsKey(bunId))
                {
                    foreach (int parentBunId in AbmBundleHeap.Bundles[bunId].EnumerateParentBundleIds(false))
                    {
                        if (checkedChildrenBunIds.Contains(parentBunId))
                        {
                            integrityCompromised = true;
                            foundLoops.Add(new List<int>(checkedChildrenBunIds) { parentBunId });
                        }
                        else
                            VerifyParents(parentBunId, new List<int>(checkedChildrenBunIds) { parentBunId });
                    }
                }
                else
                    throw new Exception($"Logic Error: Bundle not logged by bundle heap \"{App.AssetManager.GetBundleEntry(bunId).Name}\"");
            }

            foreach (KeyValuePair<int, BundleHeapEntry> bundlePair in Bundles)
            {
                if (checkCustomOnly && (bundlePair.Value.IsCustomBundle || bundlePair.Value.CustomParentIds.Count > 0))
                    continue;
                VerifyParents(bundlePair.Key, new List<int> { bundlePair.Key });
            }

            if (integrityCompromised)
            {
                App.Logger.LogError("MAJOR ERROR");
                App.Logger.LogError("The Bundle Manager has detected an infinite bundle loop caused by your project file.");
                App.Logger.LogError("You must fix your project file before the Bundle Manager can execute.");
                App.Logger.LogError("Detected Bundle Loops:");
                int loopIdx = 1;
                foreach (List<int> loopList in foundLoops)
                {
                    App.Logger.LogError($"Loop {loopIdx++}");
                    int bundleIdx = 1;
                    foreach (int bundleId in loopList)
                    {
                        App.Logger.LogError($"\tBundle: {bundleIdx++} \"{App.AssetManager.GetBundleEntry(bundleId).Name}\"");
                    }
                    App.Logger.Log("\n");

                }
            }

            return integrityCompromised;
        }
        static AbmBundleHeap()
        {
            using (NativeReader reader = new NativeReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("AutoBundleManager.Data.Swbf2_BundleHeap.cache")))
            {
                if (reader.ReadNullTerminatedString() != "MopMagicBundleHi" || reader.ReadInt() != 0)
                    return;
                int bunCount = reader.ReadInt();
                for (int i = 0; i < bunCount; i++)
                    new BundleHeapEntry(reader.ReadInt(), false, reader.ReadIntList());
            }
        }
    }
    public static class AbmBundleHeapCacheCreator
    {
        private static Dictionary<int, List<int>> bundleTree = new Dictionary<int, List<int>>();
        public static void EnumerateSharedBundles(FrostyTaskWindow task)
        {
            object forLock = new object();
            task.ParallelForeach("Caching Shared Bundle Inheritence", App.AssetManager.EnumerateBundles(type: BundleType.SharedBundle), (bEntry, index) =>
            {
                int bunId = App.AssetManager.GetBundleId(bEntry);
                List<int> parents = FindSharedBundleParents(bunId);
                lock (forLock)
                {
                    bundleTree.Add(bunId, parents);
                }
            });
        }
        public static void EnumerateSublevelBundles(FrostyTaskWindow task)
        {
            List<string> constParents = new List<string>()
            {
                "win32/gameplay/wrgameconfiguration",
                "win32/default_settings",
                "win32/loadingscreens_bundle",
                "win32/ui/static"
            };
            foreach (BundleEntry bEntry in App.AssetManager.EnumerateBundles().Where(bEntry => bEntry.Name.StartsWith("win32/ui/resources/fonts/wsuiimfontconfiguration_languageformat")))
                constParents.Add(bEntry.Name);
            object forLock = new object();
            HashSet<int> descriptionBunIds = new HashSet<int>();
            task.ParallelForeach("Caching Sublevel Bundle Inheritence", App.AssetManager.EnumerateEbx(type: "LevelDescriptionAsset"), (descEntry, index) =>
            {
                if (descEntry.IsAdded)
                    return;
                dynamic refRoot = App.AssetManager.GetEbx(descEntry, true).RootObject;
                string levelName = BunNameCorrection(refRoot.LevelName);
                List<int> parents = constParents.Select(bunName => App.AssetManager.GetBundleId(bunName)).ToList();
                foreach (dynamic obj in refRoot.Bundles)
                {
                    string BunName = "win32/" + BunNameCorrection(obj.Name);
                    int bunId = App.AssetManager.GetBundleId(BunName);
                    if (bunId == -1)
                    {
                        bunId = App.AssetManager.GetBundleId(((uint)Utils.HashString(BunName)).ToString("x"));
                        if (bunId == -1)
                        {
                            App.Logger.Log("Error: Could not find bundle " + BunName + " or " + ((uint)Utils.HashString(BunName)).ToString("x"));
                            continue;
                        }
                    }
                    if (obj.Name != refRoot.LevelName)
                    {
                        lock (forLock)
                        {
                            descriptionBunIds.Add(bunId);
                        }
                    }
                    if (!parents.Contains(bunId))
                        parents.Add(bunId);
                }
                SearchLevelData(levelName, parents, forLock);
            });
            foreach (int bunId in descriptionBunIds)
            {
                if (!bundleTree.ContainsKey(bunId))
                    continue;
                List<int> newParents = new List<int>(bunId);
                foreach (int parId in descriptionBunIds)
                {
                    if (newParents.Contains(parId))
                        newParents.Remove(parId);
                }
                bundleTree[bunId] = newParents;
            }
        }
        private static void SearchLevelData(string levelName, List<int> parents, object forLock)
        {
            lock (forLock)
            {
                bundleTree.Add(App.AssetManager.GetBundleId("win32/" + levelName), parents);
            }
            List<int> newParents = new List<int> { App.AssetManager.GetBundleId("win32/" + levelName) };
            EbxAssetEntry refEntry = App.AssetManager.GetEbxEntry(levelName);
            if (refEntry == null)
            {
                App.Logger.Log("Could not find LevelData: " + refEntry);
                return;
            }
            EbxAsset refAsset = App.AssetManager.GetEbx(refEntry);
            foreach (dynamic obj in refAsset.Objects)
            {
                if (obj.GetType().Name == "SubWorldReferenceObjectData")
                {
                    SearchLevelData(BunNameCorrection(obj.BundleName), newParents, forLock);
                }
            }
        }
        public static void EnumerateDetachedSubworlds(FrostyTaskWindow task)
        {
            object forLock = new object();
            task.ParallelForeach("Caching Detached Bundle Inheritence", App.AssetManager.EnumerateEbx(type: "DetachedSetBlueprint"), (parEntry, index) =>
            {
                EbxAsset parAsset = App.AssetManager.GetEbx(parEntry);
                Parallel.ForEach(parAsset.Objects, obj =>
                {
                    if (obj.GetType().Name == "SubWorldReferenceObjectData")
                        SearchLevelData(BunNameCorrection(((dynamic)obj).BundleName), parEntry.Bundles.ToList(), forLock);
                });
            });
        }
        public static void EnumerateBlueprintBundles(FrostyTaskWindow task)
        {
            List<int> mpvurParentBundles = new List<string> { "win32/default_settings",
                "win32/gameplay/bundles/sharedbundles/common/weapons/sharedbundleweapons_common",
                "win32/gameplay/wrgameconfiguration",
                "win32/gameplay/bundles/sharedbundles/frontend+mp/characters/sharedbundlecharacters_frontend+mp",
                "win32/gameplay/bundles/sharedbundles/common/vehicles/sharedbundlevehiclescockpits"
            }.Select(bunName => App.AssetManager.GetBundleId(bunName)).ToList();

            foreach (BundleEntry bEntry in App.AssetManager.EnumerateBundles(BundleType.BlueprintBundle))
            {
                if (bEntry.Name.StartsWith("Win32/weapons/"))
                    bundleTree.Add(App.AssetManager.GetBundleId(bEntry), new List<int>() { App.AssetManager.GetBundleId("win32/gameplay/bundles/sharedbundles/common/weapons/sharedbundleweapons_common") });
                else if (bEntry.Name.Contains("Gameplay/Bundles/SP/"))
                    bundleTree.Add(App.AssetManager.GetBundleId(bEntry), new List<int>() { App.AssetManager.GetBundleId("win32/Levels/SP/RootLevel/RootLevel") });
                else
                    bundleTree.Add(App.AssetManager.GetBundleId(bEntry), new List<int>(mpvurParentBundles) { });
            }

            object forLock = new object();
            task.ParallelForeach("Caching Blueprint Bundle Inheritence", App.AssetManager.EnumerateEbx(type: "VisualUnlockRootAsset"), (parEntry, index) =>
            {
                if (parEntry.IsAdded)
                    return;
                dynamic refRoot = App.AssetManager.GetEbx(parEntry, true).RootObject;
                lock (forLock)
                {
                    foreach (dynamic obj in refRoot.SkinInfos)
                    {
                        if (obj.ThirdPersonBundle.Parents.Count > 0)
                            bundleTree[App.AssetManager.GetBundleId("win32/" + ((string)obj.ThirdPersonBundle.Name))].Add(App.AssetManager.GetBundleId("win32/" + (string)obj.ThirdPersonBundle.Parents[0].Name));
                        if (obj.FirstPersonBundle.Parents.Count > 0)
                            bundleTree[App.AssetManager.GetBundleId("win32/" + ((string)obj.FirstPersonBundle.Name))].Add(App.AssetManager.GetBundleId("win32/" + (string)obj.FirstPersonBundle.Parents[0].Name));
                    }
                }
            });


        }
        public static string BunNameCorrection(string Name)
        {
            if (ProfilesLibrary.DataVersion == (int)ProfileVersion.StarWarsBattlefront || ProfilesLibrary.DataVersion == (int)ProfileVersion.Battlefield1)
                return Name.ToLower();
            else
                return Name;
        }
        public static List<int> FindSharedBundleParents(int bunId)
        {
            List<BundleEntry> ParallelBundles = new List<BundleEntry>();
            List<BundleEntry> ParentBundles = new List<BundleEntry>();
            foreach (EbxAssetEntry parEntry in App.AssetManager.EnumerateEbx().Where(o => o.IsInBundle(bunId)))
            {
                foreach (Guid refGuid in parEntry.DependentAssets)
                {
                    EbxAssetEntry refEntry = App.AssetManager.GetEbxEntry(refGuid);

                    if (refEntry.IsInBundle(bunId))
                    {
                        foreach (int otherBundleId in refEntry.Bundles)
                        {
                            BundleEntry otherBundleEntry = App.AssetManager.GetBundleEntry(otherBundleId);
                            if (!ParallelBundles.Contains(otherBundleEntry) && otherBundleEntry.Type == BundleType.SharedBundle)
                                ParallelBundles.Add(otherBundleEntry);
                        }
                    }
                    else
                    {
                        foreach (int otherBundleId in refEntry.Bundles)
                        {
                            BundleEntry otherBundleEntry = App.AssetManager.GetBundleEntry(otherBundleId);
                            if (!ParentBundles.Contains(otherBundleEntry) && otherBundleEntry.Type == BundleType.SharedBundle)
                                ParentBundles.Add(otherBundleEntry);
                        }
                    }
                }
            }
            foreach (BundleEntry otherBundleEntry in ParallelBundles)
            {
                if (ParentBundles.Contains(otherBundleEntry))
                    ParentBundles.Remove(otherBundleEntry);
            }
            if (ParentBundles.Count > 0)
            {
                List<int> bunParents = new List<int>();
                foreach (BundleEntry otherBundleEntry in ParentBundles)
                    bunParents.Add(App.AssetManager.GetBundleId(otherBundleEntry));
                return bunParents;
            }
            return new List<int>();
        }

        public static void UpdateCache()
        {
            string cacheFileName = $"{App.FileSystem.CacheName}/AutoBundleManager/BundleHierarchyCache.cache";
            if (!Directory.Exists(Path.GetDirectoryName(cacheFileName)))
                Directory.CreateDirectory(Path.GetDirectoryName(cacheFileName));

            void RemoveGrandParents(int bunId, ref List<int> parentList)
            {
                if (!bundleTree.ContainsKey(bunId))
                    return;
                List<int> granParentIds = bundleTree[bunId];

                foreach (int granId in granParentIds)
                {
                    if (parentList.Contains(granId))
                        parentList.Remove(granId);
                    //if (granId == bunId)
                    //    return;
                    RemoveGrandParents(granId, ref parentList);
                }
            }
            foreach (KeyValuePair<int, List<int>> pair in new Dictionary<int, List<int>>(bundleTree))
            {
                List<int> newParents = new List<int>(pair.Value);
                if (newParents.Contains(pair.Key))
                    newParents.Remove(pair.Key);
                bundleTree[pair.Key] = newParents;
            }
            foreach (KeyValuePair<int, List<int>> pair in new Dictionary<int, List<int>>(bundleTree))
            {
                List<int> newParents = new List<int>(pair.Value);
                pair.Value.ForEach(parId => RemoveGrandParents(parId, ref newParents));
                bundleTree[pair.Key] = newParents;
            }
            foreach (int bunId in App.AssetManager.EnumerateBundles().Select(bEntry => App.AssetManager.GetBundleId(bEntry)))
            {
                if (!bundleTree.ContainsKey(bunId))
                {
                    App.Logger.Log($"No data logged for {App.AssetManager.GetBundleEntry(bunId).Name}");
                    bundleTree.Add(bunId, new List<int>());
                }
            }

            using (NativeWriter txtWriter = new NativeWriter(new FileStream(cacheFileName.Replace(".cache", ".txt"), FileMode.Create)))
            {
                using (NativeWriter writer = new NativeWriter(new FileStream(cacheFileName, FileMode.Create)))
                {
                    writer.WriteNullTerminatedString("MopMagicBundleHi"); //Magic
                    writer.Write(0);
                    writer.Write(bundleTree.Count);
                    foreach (KeyValuePair<int, List<int>> pair in bundleTree)
                    {
                        writer.Write(pair.Key);
                        writer.Write(pair.Value);
                        if (pair.Value.Contains(pair.Key))
                            App.Logger.Log(App.AssetManager.GetBundleEntry(pair.Key).Name);
                        txtWriter.WriteLine(App.AssetManager.GetBundleEntry(pair.Key).Name + ":");
                        pair.Value.ForEach(bunId => txtWriter.WriteLine("\t" + App.AssetManager.GetBundleEntry(bunId).Name));
                        txtWriter.WriteLine("\r\n");
                    }
                }
            }
            //cacheNeedsUpdating = false;
        }
    }
}
