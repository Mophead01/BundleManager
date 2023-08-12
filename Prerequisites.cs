using Frosty.Core;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BundleManager
{
    internal class BundleManagerPrerequisites
    {
        private int Version = 0;
        public Dictionary<EbxAssetEntry, List<BundleEntry>> assetsAddedToBundles = new Dictionary<EbxAssetEntry, List<BundleEntry>>();

        public void FindBundleEdits()
        {
            assetsAddedToBundles.Clear();
            foreach (EbxAssetEntry parEntry in App.AssetManager.EnumerateEbx())
            {
                if (!parEntry.IsModified || parEntry.IsAdded || parEntry.AddedBundles.Count == 0)
                    continue;
                assetsAddedToBundles.Add(parEntry, parEntry.AddedBundles.Select(o => App.AssetManager.GetBundleEntry(o)).ToList());
            }
        }

        public void WriteToFile(string file)
        {
            using (NativeWriter writer = new NativeWriter(new FileStream(file, FileMode.Create)))
            {
                writer.WriteNullTerminatedString("MopMagicMopMagic"); //Magic
                writer.Write(Version);
                writer.Write(assetsAddedToBundles.Count);
                foreach (KeyValuePair<EbxAssetEntry, List<BundleEntry>> assetPairs in assetsAddedToBundles)
                {
                    writer.WriteNullTerminatedString(assetPairs.Key.Name);
                    writer.Write(assetPairs.Value.Count);
                    foreach (BundleEntry entry in assetPairs.Value)
                    {
                        writer.WriteNullTerminatedString(entry.Name);
                    }
                }
            }
        }

        public void ReadFile(string file)
        {
            using (NativeReader reader = new NativeReader(new FileStream(file, FileMode.Open, FileAccess.Read)))
            {
                if (reader.ReadNullTerminatedString() != "MopMagicMopMagic" || reader.ReadInt() > Version)
                {
                    App.Logger.Log(string.Format($"Could not read prerequisite file {file}. Your BM may be out of date"));
                    return;
                }
                int pairsCount = reader.ReadInt();
                for (int i = 0; i < pairsCount; i++)
                {
                    EbxAssetEntry parEntry = App.AssetManager.GetEbxEntry(reader.ReadNullTerminatedString());
                    if (!assetsAddedToBundles.ContainsKey(parEntry))
                        assetsAddedToBundles.Add(parEntry, new List<BundleEntry>());

                    int bundlesCount = reader.ReadInt();
                    for (int y = 0; y < bundlesCount; y++)
                    {
                        int bunId = App.AssetManager.GetBundleId(reader.ReadNullTerminatedString());
                        if (bunId == -1)
                            continue;
                        BundleEntry bEntry = App.AssetManager.GetBundleEntry(bunId);
                        if (bEntry == null)
                            continue;
                        assetsAddedToBundles[parEntry].Add(bEntry);
                    }

                }
            }
        }
    }
    //internal class BundleManagerPrerequisites
    //{
    //    private int Version = 1;
    //    public Dictionary<EbxAssetEntry, List<BundleEntry>> assetsAddedToBundles = new Dictionary<EbxAssetEntry, List<BundleEntry>>();

    //    public void FindBundleEdits()
    //    {
    //        assetsAddedToBundles.Clear();
    //        foreach (EbxAssetEntry parEntry in App.AssetManager.EnumerateEbx())
    //        {
    //            if (!parEntry.IsModified || parEntry.IsAdded || parEntry.AddedBundles.Count == 0)
    //                continue;
    //            assetsAddedToBundles.Add(parEntry, parEntry.AddedBundles.Select(o => App.AssetManager.GetBundleEntry(o)).ToList());
    //        }
    //    }

    //    public void WriteToFile(string file)
    //    {
    //        using (NativeWriter writer = new NativeWriter(new FileStream(file, FileMode.Create)))
    //        {
    //            writer.WriteNullTerminatedString("MopMagicMopMagic"); //Magic
    //            writer.Write(Version);
    //            List<BundleEntry> addedBundles = App.AssetManager.EnumerateBundles().Where(bEntry => bEntry.Added).ToList();
    //            writer.Write(addedBundles.Count);
    //            foreach(BundleEntry bEntry in addedBundles)
    //            {
    //                writer.WriteNullTerminatedString(bEntry.Name);
    //                writer.WriteNullTerminatedString(bEntry.Type.ToString());
    //                writer.Write(bEntry.SuperBundleId);
    //            }
    //            writer.Write(assetsAddedToBundles.Count);
    //            foreach(KeyValuePair<EbxAssetEntry, List<BundleEntry>> assetPairs in assetsAddedToBundles)
    //            {
    //                writer.WriteNullTerminatedString(assetPairs.Key.Name);
    //                writer.Write(assetPairs.Value.Count);
    //                foreach(BundleEntry entry in assetPairs.Value)
    //                {
    //                    writer.WriteNullTerminatedString(entry.Name);
    //                }
    //            }
    //        }
    //    }

    //    public void ReadFile(string file)
    //    {
    //        using (NativeReader reader = new NativeReader(new FileStream(file, FileMode.Open, FileAccess.Read)))
    //        {
    //            if (reader.ReadNullTerminatedString() != "MopMagicMopMagic")
    //            {
    //                App.Logger.Log(string.Format($"Could not read prerequisite file {file}. Your BM may be out of date"));
    //                return;
    //            }
    //            int readVersion = reader.ReadInt();
    //            if (readVersion > Version)
    //            {
    //                App.Logger.Log(string.Format($"Could not read prerequisite file {file}. Your BM may be out of date"));
    //                return;
    //            }
    //            if (readVersion >= 1)
    //            {
    //                int addedBundlesCount = reader.ReadInt();
    //                for (int i = 0; i < addedBundlesCount; i++)
    //                {
    //                    string bunName = reader.ReadNullTerminatedString();
    //                    BundleType bunType = (BundleType)Enum.Parse(typeof(BundleType), reader.ReadNullTerminatedString());
    //                    int supBunId = reader.ReadInt();
    //                    int bunId = App.AssetManager.GetBundleId(bunName);
    //                    if (bunId == -1)
    //                    {
    //                        BundleEntry bEntry =  App.AssetManager.AddBundle(bunName, bunType, supBunId);
    //                        //App.Logger.Log($"Added {bunType.ToString()} \"{bunName}\" from prerequisites \"{Path.GetFileName(file)}\"");
    //                        //GetEmptyChunk().AddToBundle(App.AssetManager.GetBundleId(bEntry));
    //                        App.AssetManager.GetEbxEntry("DataVersion").AddToBundle(App.AssetManager.GetBundleId(bEntry));
    //                    }
    //                }
    //            }
    //            int pairsCount = reader.ReadInt();
    //            for (int i = 0; i < pairsCount; i++)
    //            {
    //                EbxAssetEntry parEntry = App.AssetManager.GetEbxEntry(reader.ReadNullTerminatedString());
    //                if (!assetsAddedToBundles.ContainsKey(parEntry))
    //                    assetsAddedToBundles.Add(parEntry, new List<BundleEntry>());

    //                int bundlesCount = reader.ReadInt();
    //                for (int y = 0; y < bundlesCount; y++)
    //                {
    //                    int bunId = App.AssetManager.GetBundleId(reader.ReadNullTerminatedString());
    //                    if (bunId == -1)
    //                        continue;
    //                    BundleEntry bEntry = App.AssetManager.GetBundleEntry(bunId);
    //                    if (bEntry == null)
    //                        continue;
    //                    assetsAddedToBundles[parEntry].Add(bEntry);
    //                }

    //            }
    //        }
    //    }

    //    public ChunkAssetEntry GetEmptyChunk()
    //    {
    //        Guid dummyGuid = new Guid(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
    //        ChunkAssetEntry dummyChunk = App.AssetManager.GetChunkEntry(dummyGuid);
    //        if (dummyChunk == null)
    //        {
    //            Guid newGuid = App.AssetManager.AddChunk(Encoding.ASCII.GetBytes("BundleManagerAssetDONOTDELETE"), dummyGuid);
    //            dummyChunk = App.AssetManager.GetChunkEntry(dummyGuid);
    //        }
    //        return dummyChunk;
    //    }
    //}
}
