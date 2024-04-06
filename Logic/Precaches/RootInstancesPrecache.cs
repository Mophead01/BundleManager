using Frosty.Core.Windows;
using Frosty.Core;
using Frosty.Hash;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using FrostySdk.Ebx;
using FrostySdk.Attributes;

namespace AutoBundleManagerPlugin
{
    public static class AbmRootInstanceCache
    {
        public static bool IsLoaded { get; private set; }

        private const uint cacheVersion = 1;
        public static Dictionary<Guid, Guid> ebxRootInstanceGuidList = new Dictionary<Guid, Guid>();

        //public static void LoadEbxRootInstanceEntries(FrostyTaskWindow task)
        //{
        //    ebxRootInstanceGuidList.Clear();

        //    if (!ReadCache(task))
        //    {
        //        uint totalCount = App.AssetManager.GetEbxCount();
        //        uint index = 0;

        //        task.Update("Collecting ebx root instance guids");

        //        foreach (EbxAssetEntry entry in App.AssetManager.EnumerateEbx())
        //        {
        //            uint progress = (uint)((index / (float)totalCount) * 100);
        //            task.Update(progress: progress);

        //            EbxAsset asset = App.AssetManager.GetEbx(entry);
        //            ebxRootInstanceGuidList.Add(asset.RootInstanceGuid, entry.Guid);

        //            index++;
        //        }

        //        WriteToCache(task);
        //    }
        //    IsLoaded = true;
        //}

        public static EbxAssetEntry GetEbxEntryByRootInstanceGuid(Guid guid)
        {
            return ebxRootInstanceGuidList.ContainsKey(guid) ? App.AssetManager.GetEbxEntry(ebxRootInstanceGuidList[guid]) : null;
        }

        static AbmRootInstanceCache()
        {
            using (NativeReader reader = new NativeReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("AutoBundleManagerPlugin.Data.Swbf2_RootInstances.cache")))
            {
                uint version = reader.ReadUInt();
                if (version != cacheVersion)
                    throw new Exception("Out of date ABM Cache");

                int profileHash = reader.ReadInt();
                if (profileHash != Fnv1.HashString(ProfilesLibrary.ProfileName))
                    return;

                int count = reader.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    Guid rootInstanceGuid = reader.ReadGuid();
                    Guid fileGuid = reader.ReadGuid();

                    ebxRootInstanceGuidList.Add(rootInstanceGuid, fileGuid);
                }
            }
            IsLoaded = true;
        }

        //public static void WriteToCache(FrostyTaskWindow task)
        //{
        //    FileInfo fi = new FileInfo($"{App.FileSystem.CacheName}_rootinstances.cache");
        //    if (!Directory.Exists(fi.DirectoryName))
        //        Directory.CreateDirectory(fi.DirectoryName);

        //    task.Update("Caching data");

        //    using (NativeWriter writer = new NativeWriter(new FileStream(fi.FullName, FileMode.Create)))
        //    {
        //        writer.Write(cacheVersion);
        //        writer.Write(Fnv1.HashString(ProfilesLibrary.ProfileName));

        //        writer.Write(ebxRootInstanceGuidList.Count);
        //        foreach (KeyValuePair<Guid, Guid> kv in ebxRootInstanceGuidList)
        //        {
        //            writer.Write(kv.Key); // Root Instance Guid
        //            writer.Write(kv.Value); // File Guid
        //        }
        //    }
        //}
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RootInstancesViewer
    {
        [DisplayName("Ebx Name")]
        [IsReadOnly]
        public CString EbxName { get; set; }

        [DisplayName("Asset Guid")]
        [IsReadOnly]
        public Guid AssetGuid { get; set; }

        [DisplayName("Root Instance Guid")]
        [IsReadOnly]
        public Guid RootInstanceGuid { get; set; }
        public RootInstancesViewer((Guid, Guid) pair)
        {
            EbxName = App.AssetManager.GetEbxEntry(pair.Item2) == null ? "null ref" : App.AssetManager.GetEbxEntry(pair.Item2).Name;
            AssetGuid = pair.Item2;
            RootInstanceGuid = pair.Item1;
        }
        public RootInstancesViewer()
        {

        }
    }
}
