using AtlasTexturePlugin;
using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;
using MeshSetPlugin.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;
using System.Xml.Linq;

namespace AutoBundleManagerPlugin
{
    #region Res Detectors
    public class ResDependencyDetector
    {
        public virtual ResourceType resType => ResourceType.Invalid;
        public ulong resRid;
        public HashSet<string> refNames = new HashSet<string>();
        public HashSet<Guid> ebxPointerGuids = new HashSet<Guid>();
        public HashSet<ulong> resRids = new HashSet<ulong>();
        public Dictionary<Guid, int> chunkGuids = new Dictionary<Guid, int>();
        public virtual void ExtractData(ulong resRid)
        {
            this.resRid = resRid;

        }
    }
    public class ShaderBlockDepotDependencyDetector : ResDependencyDetector
    {
        public override ResourceType resType => ResourceType.ShaderBlockDepot;
        public override void ExtractData(ulong resRid)
        {
            ResAssetEntry resBlockEntry = App.AssetManager.GetResEntry(resRid);

            if (resBlockEntry != null)
            {
                using (NativeReader reader = new NativeReader(App.AssetManager.GetRes(resBlockEntry)))
                {
                    for (int idx = 72; idx < Convert.ToInt32(reader.BaseStream.Length - 12); idx = idx + 4)
                    {
                        reader.BaseStream.Position = idx;
                        Guid ReadGuid = reader.ReadGuid();
                        if (AbmRootInstanceCache.GetEbxEntryByRootInstanceGuid(ReadGuid) != null)
                            ebxPointerGuids.Add(AbmRootInstanceCache.GetEbxEntryByRootInstanceGuid(ReadGuid).Guid);
                    }
                }
            }
        }
    }
    public class MeshSetDependencyDetector : ResDependencyDetector
    {
        public override ResourceType resType => ResourceType.MeshSet;
        public override void ExtractData(ulong resRid)
        {
            ResAssetEntry meshSetResEntry = App.AssetManager.GetResEntry(resRid);
            MeshSet meshSet = App.AssetManager.GetResAs<MeshSet>(meshSetResEntry);
            foreach (MeshSetLod lod in meshSet.Lods)
            {
                if (lod.ChunkId != Guid.Empty)
                    chunkGuids.Add(lod.ChunkId, -1);
            }

            ResAssetEntry blocksEntry = App.AssetManager.GetResEntry(meshSetResEntry.Name + "_mesh/blocks");
            if (blocksEntry != null)
                resRids.Add(blocksEntry.ResRid);
        }
    }
    public class TextureDependencyDetector : ResDependencyDetector
    {
        public override ResourceType resType => ResourceType.Texture;
        public override void ExtractData(ulong resRid)
        {
            ResAssetEntry resEntry = App.AssetManager.GetResEntry(resRid);
            Texture texture = App.AssetManager.GetResAs<Texture>(resEntry);
            chunkGuids.Add(texture.ChunkId, texture.FirstMip);
        }
    }
    public class AtlasTextureDependencyDetector : ResDependencyDetector
    {
        public override ResourceType resType => ResourceType.AtlasTexture;
        public override void ExtractData(ulong resRid)
        {
            ResAssetEntry resEntry = App.AssetManager.GetResEntry(resRid);
            AtlasTexture texture = App.AssetManager.GetResAs<AtlasTexture>(resEntry);
            chunkGuids.Add(texture.ChunkId, 0);
        }
    }

    #endregion
    public class EbxDependencyDetector
    {
        public EbxAssetEntry parEntry;
        public EbxAsset parAsset;
        public bool getResDependencies;

        private const BindingFlags PropertyBindingFlags = BindingFlags.Public | BindingFlags.Instance;
        public HashSet<string> refNames = new HashSet<string>();
        public HashSet<Guid> ebxPointerGuids = new HashSet<Guid>();
        public HashSet<ulong> resRids = new HashSet<ulong>();
        public HashSet<Guid> chunkGuids = new HashSet<Guid>();
        public HashSet<Guid> networkRegistryReferenceGuids = new HashSet<Guid>();
        public AbmMeshVariationDatabaseEntry meshVariEntry = null;
        //public HashSet<(Guid, Guid)> meshVariationIds = new HashSet<(Guid, Guid)> ();

        public EbxDependencyDetector(EbxAssetEntry parEntry, EbxAsset parAsset)
        {
            this.parEntry = parEntry;
            this.parAsset = parAsset;

            foreach (object obj in parAsset.Objects)
                ExtractClass(obj);

            refNames.Add(parEntry.Name.ToLower());
            switch (parEntry.Type)
            {
                case "ShaderGraph":
                    refNames.Add(parEntry.Name.ToLower() + "_graph/blocks");
                    break;
                case "SubWorldData":
                    refNames.Add("animations/antanimations/" + parEntry.Name.ToLower() + "_win32_antstate");
                    break;
                case "LevelData":
                    refNames.Add("animations/antanimations/" + parEntry.Name.ToLower() + "_win32_antstate");
                    refNames.Add(parEntry.Name.ToLower() + "/rvmdatabase_dx12pcrvmdatabase");
                    refNames.Add(parEntry.Name.ToLower() + "/rvmdatabase_dx12nvrvmdatabase");
                    refNames.Add(parEntry.Name.ToLower() + "/rvmdatabase_dx11rvmdatabase");
                    refNames.Add(parEntry.Name.ToLower() + "/rvmdatabase_dx11nvrvmdatabase");
                    break;
                case "StaticMeshAsset":
                case "SkinnedMeshAsset":
                case "CompositeMeshAsset":
                    Sha1 meshSha1 = parEntry.GetSha1();
                    //Check if already cached
                    if (!parEntry.HasModifiedData)
                    {
                        if (AbmMeshVariationDatabasePrecache.MeshMvdbDatabase.ContainsKey(parEntry.Guid))
                        {
                            meshVariEntry = AbmMeshVariationDatabasePrecache.MeshMvdbDatabase[parEntry.Guid];
                            break;
                        }
                        else
                            App.Logger.LogError($"{parEntry.Name} is unmodified but not contained within the abm meshvari precache. Please investigate");
                    }

                    dynamic meshRoot = parAsset.RootObject;
                    meshVariEntry = new AbmMeshVariationDatabaseEntry(parEntry, parAsset, meshRoot);

                    //meshVariationIds.Add((parEntry.Guid, Guid.Empty));
                    break;
            }

            if (!parEntry.HasModifiedData)
            {
                if (AbmNetworkRegistryCache.NetworkRegistryReferences.ContainsKey(parEntry.Guid))
                {
                    foreach (Guid networkRegistryReferenceGuid in AbmNetworkRegistryCache.NetworkRegistryReferences[parEntry.Guid])
                        networkRegistryReferenceGuids.Add(networkRegistryReferenceGuid);
                }
            }
            else
            {
                foreach (dynamic obj in parAsset.ExportedObjects)
                {
                    if (AbmNetworkRegistryCache.NetworkRegistryTypes.Contains(obj.GetType().Name))
                        networkRegistryReferenceGuids.Add(((dynamic)obj).GetInstanceGuid().ExportedGuid);
                }
            }
        }

        void ExtractClass(object obj)
        {
            switch(obj.GetType().Name)
            {
                case "MeshAndVariationPair":
                    EbxAssetEntry meshEntry = App.AssetManager.GetEbxEntry(((dynamic)obj).MeshAsset.External.FileGuid);
                    EbxAssetEntry varEntry = App.AssetManager.GetEbxEntry(((dynamic)obj).Variation.External.FileGuid);
                    if (meshEntry != null && varEntry != null)
                    {
                        //meshVariationIds.Add((meshEntry.Guid, varEntry.Guid));
                        refNames.Add($"{varEntry.Name}/{meshEntry.DisplayName}_{(uint)Utils.HashString(meshEntry.Name)}/shaderblocks_variation/blocks".ToLower());
                    }
                    break;
                case "StaticModelGroupEntityData":
                    Dictionary<uint, EbxAssetEntry> hashesToObjectVariations = App.AssetManager.EnumerateEbx(type: "ObjectVariation").ToDictionary(refEntry => (uint)Utils.HashString(refEntry.Name, true), refEntry => refEntry);

                    foreach (dynamic memberData in ((dynamic)obj).MemberDatas)
                    {
                        foreach (uint objVarNameHash in memberData.InstanceObjectVariation)
                        {
                            if (hashesToObjectVariations.ContainsKey(objVarNameHash) && objVarNameHash != 0)
                            {
                                meshEntry = App.AssetManager.GetEbxEntry((memberData).MeshAsset.External.FileGuid);
                                varEntry = hashesToObjectVariations[objVarNameHash];
                                if (varEntry != null)
                                    refNames.Add(varEntry.Name);
                                if (meshEntry != null && varEntry != null)
                                {
                                    //meshVariationIds.Add((meshEntry.Guid, varEntry.Guid));
                                    refNames.Add($"{varEntry.Name}/{meshEntry.DisplayName}_{(uint)Utils.HashString(meshEntry.Name)}/shaderblocks_variation/blocks".ToLower());
                                }
                            }
                        }
                    }
                    break;
                case "PropertyConnection":
                case "EventConnection":
                case "LinkConnection":
                    return;
            }

            PropertyInfo[] Properties = obj.GetType().GetProperties(PropertyBindingFlags);
            Array.Sort(Properties, new PropertyComparer());
            foreach (PropertyInfo PI in Properties)
            {
                //App.Logger.Log($"Class Property:\t{PI.Name}");
                if (PI.Name == "ExportedGuid")
                    continue;
                ExtractField(PI.GetValue(obj));
            }

            //Remove cases where the bundle manager is being overly cautious
            void RemoveIfFound(string assetName)
            {
                if (refNames.Contains(assetName))
                    refNames.Remove(assetName);
            }
            switch (obj.GetType().Name) 
            {
                case "VisualUnlockRootAsset":
                    RemoveIfFound(((dynamic)obj).Name);
                    break;
                case "BlueprintBundleReference":
                    //TODO - Parent BPBs
                    RemoveIfFound(((dynamic)obj).Name);
                    foreach(dynamic parent in ((dynamic)obj).Parents)
                        if(parent.Name != "")
                            RemoveIfFound(parent.Name);
                    break;
                case "SubWorldReferenceObjectData":
                    RemoveIfFound(((dynamic)obj).BundleName);
                    break;
            }

        }
        void ExtractField(object Value)
        {
            Type FieldType = Value.GetType();
            //App.Logger.Log($"Type :\t{FieldType.Name}");

            if (FieldType.Name == "List`1")
            {
                for (int i = 0; i < (int)FieldType.GetMethod("get_Count").Invoke(Value, null); i++)
                {
                    object SubValue = FieldType.GetMethod("get_Item").Invoke(Value, new object[] { i });
                    ExtractField(SubValue);
                }
            }
            else
            {
                switch (FieldType)
                {
                    case Type t when t == typeof(PointerRef):
                        if (((PointerRef)Value).Type == PointerRefType.External)
                            ebxPointerGuids.Add(((PointerRef)Value).External.FileGuid);
                        break;
                    case Type t1 when t1 == typeof(CString):
                    case Type t2 when t2 == typeof(string):
                        string refName = Value.ToString();
                        if (refName == null)
                            return;
                        if (refName != parEntry.Name && (App.AssetManager.GetEbxEntry(refName) != null || refName.Contains("/")))
                            refNames.Add(refName);
                        break;
                    case Type t when t == typeof(ResourceRef):
                        ulong resId = ((ResourceRef)Value).resourceId;
                        if (resId != 0)
                            resRids.Add(resId);
                        break;
                    case Type t when t == typeof(Guid):
                        chunkGuids.Add((Guid)Value);
                        break;
                    case Type t when t == typeof(FileRef):
                        //refNames.Add("FILEREF IN THIS FILE REPLACEME");
                        break;
                    case Type t when t == typeof(TypeRef):
                        refName = ((TypeRef)Value).Name;
                        if (App.AssetManager.GetEbxEntry(refName) != null || (refName.Contains("/") && refName.Split('\n').Length == 1))
                            refNames.Add(refName);
                        break;
                    case Type t2 when t2 == typeof(BoxedValueRef):
                    case Type t3 when t3.Namespace != "FrostySdk.Ebx":
                        return;
                    default:
                        ExtractClass(Value);
                        break;
                }
            }
        }
    }

    public class DependencySavedData
    {
        public string srcName;
        public Guid srcGuid;
        public bool isRes;
        public HashSet<string> refNames = new HashSet<string>();
        public HashSet<Guid> ebxGuids = new HashSet<Guid>();
        public HashSet<ulong> resRids = new HashSet<ulong>();
        public Dictionary<Guid, int> chunkGuids = new Dictionary<Guid, int>();
        public HashSet<Guid> networkRegistryRefGuids = new HashSet<Guid>();
        public AbmMeshVariationDatabaseEntry meshVariEntry = null;
        //public HashSet<(Guid, Guid)> meshVariationIds = new HashSet<(Guid, Guid)>();
        public DependencySavedData(string srcName, Guid srcGuid, bool isRes, HashSet<string> refNames, HashSet<Guid> ebxGuids, HashSet<ulong> resRids, Dictionary<Guid, int> chunkGuids, HashSet<Guid> networkRegistryRefGuids, AbmMeshVariationDatabaseEntry meshVariEntry)
        {
            this.srcName = srcName;
            this.srcGuid = srcGuid;
            this.isRes = isRes;
            this.refNames = refNames;
            this.ebxGuids = ebxGuids;
            this.resRids = resRids;
            this.chunkGuids = chunkGuids;
            this.networkRegistryRefGuids = networkRegistryRefGuids;
            this.meshVariEntry = meshVariEntry;
            //this.meshVariationIds = meshVariationIds;
        }

        public DependencySavedData(DependencySavedData copyFrom)
        {
            srcName = copyFrom.srcName;
            srcGuid = copyFrom.srcGuid;
            isRes = copyFrom.isRes;
            foreach (string refName in copyFrom.refNames)
                refNames.Add(refName);
            foreach (Guid refGuid in copyFrom.ebxGuids)
                ebxGuids.Add(refGuid);
            foreach (ulong resRid in copyFrom.resRids)
                resRids.Add(resRid);
            foreach (KeyValuePair<Guid, int> guidPair in copyFrom.chunkGuids)
                if (!chunkGuids.ContainsKey(guidPair.Key))
                    chunkGuids.Add(guidPair.Key, guidPair.Value);
            foreach (Guid refGuid in copyFrom.networkRegistryRefGuids)
                networkRegistryRefGuids.Add(refGuid);
            if (this.meshVariEntry == null && copyFrom.meshVariEntry != null)
                meshVariEntry = copyFrom.meshVariEntry;
            //foreach ((Guid, Guid) mvPair in copyFrom.meshVariationIds)
            //    meshVariationIds.Add(mvPair);
        }
    }
    public class DependencyActiveData
    {
        public string srcName;
        public Guid srcGuid;
        public bool isRes;
        public HashSet<EbxAssetEntry> ebxRefs = new HashSet<EbxAssetEntry>();
        public HashSet<ResAssetEntry> resRefs = new HashSet<ResAssetEntry>();
        public Dictionary<ChunkAssetEntry, int> chkRefs = new Dictionary<ChunkAssetEntry, int>();
        public HashSet<Guid> networkRegistryRefGuids = new HashSet<Guid>();
        public AbmMeshVariationDatabaseEntry meshVariEntry = null;
        //public HashSet<AbmMeshVariationDatabaseEntry> meshVariEntries = new HashSet<AbmMeshVariationDatabaseEntry>();
        public DependencyActiveData(DependencySavedData rawDependency)
        {
            srcName = rawDependency.srcName;
            srcGuid = rawDependency.srcGuid;
            isRes = rawDependency.isRes;
            foreach (Guid ebxGuid in rawDependency.ebxGuids)
            {
                EbxAssetEntry refEntry = App.AssetManager.GetEbxEntry(ebxGuid);
                if (refEntry != null && !(!isRes && refEntry.Name == srcName))
                    ebxRefs.Add(refEntry);
            }
            foreach (ulong resId in rawDependency.resRids)
            {
                ResAssetEntry resEntry = App.AssetManager.GetResEntry(resId);
                if (resEntry != null && !(isRes && resEntry.Name == srcName))
                    resRefs.Add(resEntry);
            }
            foreach (KeyValuePair<Guid, int> pair in rawDependency.chunkGuids)
            {
                ChunkAssetEntry chkEntry = App.AssetManager.GetChunkEntry(pair.Key);
                if (chkEntry != null)
                    chkRefs.Add(chkEntry, pair.Value);
            }
            foreach (string refName in rawDependency.refNames)
            {
                EbxAssetEntry refEntry = App.AssetManager.GetEbxEntry(refName);
                if (refEntry != null && !(!isRes && refEntry.Name == srcName))
                    ebxRefs.Add(refEntry);

                ResAssetEntry resEntry = App.AssetManager.GetResEntry(refName);
                if (resEntry != null && !(isRes && resEntry.Name == srcName))
                    resRefs.Add(resEntry);
            }
            foreach (Guid networkRegistryReferenceGuid in rawDependency.networkRegistryRefGuids)
                networkRegistryRefGuids.Add(networkRegistryReferenceGuid);
            meshVariEntry = rawDependency.meshVariEntry;

            //foreach((Guid, Guid) meshVariPair in rawDependency.meshVariationIds)
            //{
            //    EbxAssetEntry meshEntry = App.AssetManager.GetEbxEntry(meshVariPair.Item1);
            //    EbxAssetEntry varEntry = App.AssetManager.GetEbxEntry(meshVariPair.Item2);
            //    meshVariEntries.Add(MeshVariationsCache.GetMeshVariationEntry(meshEntry, varEntry));
            //}
        }
    }
    [EbxClassMeta(EbxFieldType.Struct)]
    public class AutoBundleManagerDependenciesCacheInterpruter
    {
        [DisplayName("Name")]
        [Description("Asset Name")]
        [IsReadOnly]
        public string Name { get; set; }
        [DisplayName("Guid")]
        [Description("Asset Guid")]
        [IsReadOnly]
        public Guid FileGuid { get; set; }
        [DisplayName("Sha1")]
        [Description("Asset signature")]
        [IsReadOnly]
        public Sha1 Sha1 { get; set; }

        [DisplayName("Ebx Assets")]
        [Description("Ebx assets referenced by this asset")]
        [IsReadOnly]
        public List<CString> EbxAssets { get; set; }

        [DisplayName("Res Assets")]
        [Description("Res assets referenced by this asset")]
        [IsReadOnly]
        public List<CString> ResAssets { get; set; }

        [DisplayName("Chunk Assets")]
        [Description("Chunk assets referenced by this asset")]
        [IsReadOnly]
        public List<Guid> ChunkAssets { get; set; }

        [DisplayName("Network Registry Reference Guids")]
        [Description("Guids to reference an ebx by in network registries")]
        [IsReadOnly]
        public List<Guid> NetworkRegistryReferenceGuids { get; set; }
        public AutoBundleManagerDependenciesCacheInterpruter()
        {

        }

        public AutoBundleManagerDependenciesCacheInterpruter(KeyValuePair<Sha1, DependencyActiveData> pair)
        {
            Name = pair.Value.srcName;
            FileGuid = pair.Value.srcGuid;
            Sha1 = pair.Key;
            EbxAssets = pair.Value.ebxRefs.Select(ebxRef => new CString(ebxRef.Name)).ToList();
            ResAssets = pair.Value.resRefs.Select(resRef => new CString(resRef.Name)).ToList();
            ChunkAssets = pair.Value.chkRefs.Select(chkRef => chkRef.Key.Id).ToList();
            NetworkRegistryReferenceGuids = pair.Value.networkRegistryRefGuids.ToList();
        }
    }
    public static class AbmDependenciesCache
    {
        private static string cacheFileName = $"{App.FileSystem.CacheName}/AutoBundleManager/DepedenciesCache.cache";
        private static int cacheVersion = 16;
        private static bool cacheNeedsUpdating = false;
        private static Dictionary<Sha1, DependencySavedData> dependencies = new Dictionary<Sha1, DependencySavedData>();
        private static Dictionary<ResourceType, Type> resLoggerExtensions = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(ResDependencyDetector))).ToDictionary(type => ((ResDependencyDetector)Activator.CreateInstance(type)).resType, type => type);
        private static DependencySavedData GetRawDependencies(AssetEntry parEntry, bool getResDependencies = true)
        {
            Sha1 sha1 = parEntry.GetSha1();
            if (!dependencies.ContainsKey(sha1))
            {
                cacheNeedsUpdating = true;
                if (parEntry.GetType() == typeof(EbxAssetEntry))
                {
                    EbxDependencyDetector ebxDependencyDetector = new EbxDependencyDetector((EbxAssetEntry)parEntry, App.AssetManager.GetEbx((EbxAssetEntry)parEntry));
                    dependencies.Add(sha1, new DependencySavedData(parEntry.Name, ((EbxAssetEntry)parEntry).Guid, false, ebxDependencyDetector.refNames, ebxDependencyDetector.ebxPointerGuids, ebxDependencyDetector.resRids, 
                        ebxDependencyDetector.chunkGuids.ToDictionary(chkGuid => chkGuid, chkGuid => -1), ebxDependencyDetector.networkRegistryReferenceGuids, ebxDependencyDetector.meshVariEntry));
                }
                else if (parEntry.GetType() == typeof(ResAssetEntry))
                {
                    ResDependencyDetector extension = (ResDependencyDetector)Activator.CreateInstance(resLoggerExtensions[(ResourceType)((ResAssetEntry)parEntry).ResType]);
                    extension.ExtractData(((ResAssetEntry)parEntry).ResRid);
                    dependencies.Add(sha1, new DependencySavedData(parEntry.Name, new Guid(), true, extension.refNames, extension.ebxPointerGuids, extension.resRids, extension.chunkGuids, new HashSet<Guid>() { }, null));
                }
                else
                    throw new Exception("Unknown AssetEntry Type");
            }
            DependencySavedData dependencyData =  new DependencySavedData(dependencies[sha1]);
            void ExtractRes(ulong resRid)
            {
                ResAssetEntry resEntry = App.AssetManager.GetResEntry(resRid);
                if (resEntry != null && resLoggerExtensions.ContainsKey((ResourceType)resEntry.ResType))
                {
                    DependencySavedData resDependencies = GetRawDependencies(resEntry, true);

                    foreach (Guid ebxGuid in resDependencies.ebxGuids)
                        dependencyData.ebxGuids.Add(ebxGuid);
                    foreach (ulong resourceId in resDependencies.resRids)
                    {
                        if (resourceId != 0 && !dependencyData.resRids.Contains(resourceId))
                        {
                            dependencyData.resRids.Add(resourceId);
                            ExtractRes(resourceId);
                        }
                    }
                    foreach (KeyValuePair<Guid, int> pair in resDependencies.chunkGuids)
                        if (!dependencyData.chunkGuids.ContainsKey(pair.Key))
                            dependencyData.chunkGuids.Add(pair.Key, pair.Value);
                    foreach (string refName in resDependencies.refNames)
                    {
                        if (App.AssetManager.GetEbxEntry(refName) != null || refName.Contains("/"))
                            dependencyData.refNames.Add(refName);
                        ResAssetEntry refResEntry = App.AssetManager.GetResEntry(refName);
                        if (refResEntry != null && refResEntry.ResRid != 0)
                            ExtractRes(refResEntry.ResRid);
                    }
                }
            }

            if (getResDependencies)
            {
                HashSet<ulong> resRidsCopy = new HashSet<ulong>(dependencyData.resRids);
                HashSet<string> refNamesCopy = new HashSet<string>(dependencyData.refNames);
                foreach (ulong resRid in resRidsCopy)
                    ExtractRes(resRid);
                foreach (string refName in refNamesCopy)
                {
                    ResAssetEntry refResEntry = App.AssetManager.GetResEntry(refName);
                    if (refResEntry != null && refResEntry.ResRid != 0)
                        ExtractRes(refResEntry.ResRid);
                }
            }

            return dependencyData;
        }
        public static DependencyActiveData GetDependencies(AssetEntry parEntry)
        {
            return new DependencyActiveData(GetRawDependencies(parEntry));
        }
        public static Dictionary<Sha1, DependencyActiveData> GetAllCachedDependencies()
        {
            return dependencies.ToDictionary(pair => pair.Key, pair => new DependencyActiveData(pair.Value));
        }
        public static void UpdateCache()
        {
            if (!cacheNeedsUpdating)
                return;
            if (!Directory.Exists(Path.GetDirectoryName(cacheFileName)))
                Directory.CreateDirectory(Path.GetDirectoryName(cacheFileName));
            using (NativeWriter writer = new NativeWriter(new FileStream(cacheFileName, FileMode.Create)))
            {
                writer.WriteNullTerminatedString("MopMagicDependen"); //Magic
                writer.Write(cacheVersion);
                writer.Write(dependencies.Count);
                foreach(KeyValuePair<Sha1, DependencySavedData> pair in dependencies)
                {
                    writer.Write(pair.Key);
                    writer.WriteNullTerminatedString(pair.Value.srcName);
                    writer.Write(pair.Value.srcGuid);
                    writer.Write(pair.Value.isRes);
                    writer.Write(pair.Value.refNames);
                    writer.Write(pair.Value.ebxGuids);
                    writer.Write(pair.Value.resRids);
                    writer.Write(pair.Value.chunkGuids);
                    writer.Write(pair.Value.networkRegistryRefGuids);
                    writer.Write(pair.Value.meshVariEntry != null);
                    if (pair.Value.meshVariEntry != null)
                        pair.Value.meshVariEntry.WriteToGameEntry();
                    //writer.Write(pair.Value.meshVariationIds);
                }
            }
            cacheNeedsUpdating = false;
        }

        static AbmDependenciesCache()
        {
            //Read Cache
            if (!File.Exists(cacheFileName))
                return;
            using (NativeReader reader = new NativeReader(new FileStream(cacheFileName, FileMode.Open, FileAccess.Read)))
            {
                if (reader.ReadNullTerminatedString() != "MopMagicDependen" || reader.ReadInt() != cacheVersion)
                    return;
                int dependencyCount = reader.ReadInt();
                for (int i = 0; i < dependencyCount; i++)
                    dependencies.Add(reader.ReadSha1(), new DependencySavedData(reader.ReadNullTerminatedString(), reader.ReadGuid() ,reader.ReadBoolean(), reader.ReadHashSetStrings(), reader.ReadHashSetGuids(), 
                        reader.ReadHashSetULongs(), reader.ReadGuidDictionary(), reader.ReadHashSetGuids(), reader.ReadBoolean() ? new AbmMeshVariationDatabaseEntry(reader) : null));
            }
        }
    }
}
