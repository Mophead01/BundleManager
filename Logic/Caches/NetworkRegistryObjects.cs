using AutoBundleManager.Logic.Additional;
using Frosty.Core;
using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AutoBundleManagerPlugin
{
    public static class AbmNetworkRegistryCache
    {
        public static Dictionary<Guid, List<Guid>> NetworkRegistryReferences = new Dictionary<Guid, List<Guid>>();
        public static HashSet<string> NetworkRegistryTypes = new HashSet<string>();

        static AbmNetworkRegistryCache()
        {
            using (NativeReader reader = new NativeReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("AutoBundleManager.Data.Swbf2_NetworkRegistryObjects.cache")))
            {
                NetworkRegistryTypes = reader.ReadHashSetStrings();
                int dictLength = reader.ReadInt();
                for (int i = 0; i < dictLength; i++)
                    NetworkRegistryReferences.Add(reader.ReadGuid(), reader.ReadListGuids());
            }
        }

        public static void WriteToCache(FrostyTaskWindow task)
        {
            NetworkRegistryTypes.Clear();
            NetworkRegistryReferences.Clear();

            dynamic forLock = new object();
            task.ParallelForeach("Caching Network Registry References", App.AssetManager.EnumerateEbx(type: "NetworkRegistryAsset"), (parEntry, index) =>
            {
                dynamic parRoot = App.AssetManager.GetEbx(parEntry, true).RootObject;

                lock (forLock)
                {
                    foreach (dynamic pr in parRoot.Objects)
                    {
                        if (pr.Type != PointerRefType.External)
                            continue;
                        EbxAssetEntry refEntry = App.AssetManager.GetEbxEntry(pr.External.FileGuid);
                        if (refEntry == null)
                            continue;
                        if (!NetworkRegistryReferences.ContainsKey(refEntry.Guid))
                            NetworkRegistryReferences.Add(refEntry.Guid, new List<Guid> { });
                        if (!NetworkRegistryReferences[refEntry.Guid].Contains(pr.External.ClassGuid))
                            NetworkRegistryReferences[refEntry.Guid].Add(pr.External.ClassGuid);
                    }
                }
            });
            task.ParallelForeach("Caching Network Registry Types", NetworkRegistryReferences.Keys.ToList(), (parGuid, index) =>
            {
                dynamic parAsset = App.AssetManager.GetEbx(App.AssetManager.GetEbxEntry(parGuid), true);

                lock (forLock)
                {
                    foreach(Guid guid in NetworkRegistryReferences[parGuid])
                    {
                        dynamic obj = parAsset.GetObject(guid);
                        NetworkRegistryTypes.Add(obj.GetType().Name);
                    }
                }
            });



            FileInfo fi = new FileInfo($"{App.FileSystem.CacheName}/Swbf2_NetworkRegistryObjects.cache");
            if (!Directory.Exists(fi.DirectoryName))
                Directory.CreateDirectory(fi.DirectoryName);

            task.Update("Caching data");

            using (NativeWriter writer = new NativeWriter(new FileStream(fi.FullName, FileMode.Create)))
            {
                writer.Write(NetworkRegistryTypes);
                writer.Write(NetworkRegistryReferences);
            }
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class NetworkRegistryReferencesViewer
    {
        [DisplayName("Ebx Name")]
        [IsReadOnly]
        public CString EbxName { get; set; }

        [DisplayName("Network Registry Reference Guids")]
        [IsReadOnly]
        public List<Guid> NetworkRegistryExportedGuids { get; set; }

        public NetworkRegistryReferencesViewer(KeyValuePair<Guid, List<Guid>> pair)
        {
            EbxName = App.AssetManager.GetEbxEntry(pair.Key) == null ? "null ref" : App.AssetManager.GetEbxEntry(pair.Key).Name;
            NetworkRegistryExportedGuids = pair.Value;
        }
        public NetworkRegistryReferencesViewer()
        {

        }
    }
}
