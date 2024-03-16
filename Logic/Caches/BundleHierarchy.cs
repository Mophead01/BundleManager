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

namespace AutoBundleManagerPlugin
{
    public static class AbmBundleHierarchy
    {
        private static int cacheVersion = 5;
        private static Dictionary<int, List<int>> bundleTree = new Dictionary<int, List<int>>();
        public static void EnumerateSharedBundles(FrostyTaskWindow task)
        {
            object forLock = new object();
            task.ParallelForeach("Caching Shared Bundle Inheritence", App.AssetManager.EnumerateBundles(type:BundleType.SharedBundle), (bEntry, index) => 
            {
                int bunId = App.AssetManager.GetBundleId(bEntry);
                List<int> parents = FindSharedBundleParents(bunId);
                lock(forLock)
                {
                    bundleTree.Add(bunId, parents);
                }
            });
        }
        public static void EnumerateSublevelBundles(FrostyTaskWindow task)
        {
            object forLock = new object();
            HashSet<int> descriptionBunIds = new HashSet<int>();
            task.ParallelForeach("Caching Sublevel Bundle Inheritence", App.AssetManager.EnumerateEbx(type: "LevelDescriptionAsset"), (descEntry, index) =>
            {
                if (descEntry.IsAdded)
                    return;
                dynamic refRoot = App.AssetManager.GetEbx(descEntry, true).RootObject;
                string levelName = BunNameCorrection(refRoot.LevelName);
                List<int> parents = new List<int>();
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

                foreach(int granId in granParentIds)
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
