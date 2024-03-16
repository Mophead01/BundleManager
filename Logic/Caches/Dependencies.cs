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
        public HashSet<Guid> chunkGuids = new HashSet<Guid>();
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
                        if (AbmRootInstancePlugin.GetEbxEntryByRootInstanceGuid(ReadGuid) != null)
                            ebxPointerGuids.Add(AbmRootInstancePlugin.GetEbxEntryByRootInstanceGuid(ReadGuid).Guid);
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
                    chunkGuids.Add(lod.ChunkId);
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
            chunkGuids.Add(texture.ChunkId);
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
                        refNames.Add($"{varEntry.Name}/{meshEntry.DisplayName}_{(uint)Utils.HashString(meshEntry.Name)}/shaderblocks_variation/blocks".ToLower());
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
                                    refNames.Add($"{varEntry.Name}/{meshEntry.DisplayName}_{(uint)Utils.HashString(meshEntry.Name)}/shaderblocks_variation/blocks".ToLower());
                            }
                        }
                    }
                    break;
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
                        refNames.Add("FILEREF IN THIS FILE REPLACEME");
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

    public class DependencyRaw
    {
        public HashSet<string> refNames = new HashSet<string>();
        public HashSet<Guid> ebxGuids = new HashSet<Guid>();
        public HashSet<ulong> resRids = new HashSet<ulong>();
        public HashSet<Guid> chunkGuids = new HashSet<Guid>();
        public DependencyRaw(HashSet<string> refNames, HashSet<Guid> ebxGuids, HashSet<ulong> resRids, HashSet<Guid> chunkGuids)
        {
            this.refNames = refNames;
            this.ebxGuids = ebxGuids;
            this.resRids = resRids;
            this.chunkGuids = chunkGuids;
        }

        public DependencyRaw(DependencyRaw copyFrom)
        {
            foreach (string refName in copyFrom.refNames)
                refNames.Add(refName);
            foreach (Guid refGuid in copyFrom.ebxGuids)
                ebxGuids.Add(refGuid);
            foreach (ulong resRid in copyFrom.resRids)
                resRids.Add(resRid);
            foreach (Guid refName in copyFrom.chunkGuids)
                chunkGuids.Add(refName);
        }
    }
    public class DependencyData
    {
        public HashSet<EbxAssetEntry> ebxRefs = new HashSet<EbxAssetEntry>();
        public HashSet<ResAssetEntry> resRefs = new HashSet<ResAssetEntry>();
        public HashSet<ChunkAssetEntry> chkRefs = new HashSet<ChunkAssetEntry>();
        public DependencyData(DependencyRaw rawDependency)
        {
            foreach (Guid ebxGuid in rawDependency.ebxGuids)
            {
                EbxAssetEntry refEntry = App.AssetManager.GetEbxEntry(ebxGuid);
                if (refEntry != null)
                    ebxRefs.Add(refEntry);
            }
            foreach (ulong resId in rawDependency.resRids)
            {
                ResAssetEntry resEntry = App.AssetManager.GetResEntry(resId);
                if (resEntry != null)
                    resRefs.Add(resEntry);
            }
            foreach (Guid chkGuid in rawDependency.chunkGuids)
            {
                ChunkAssetEntry chkEntry = App.AssetManager.GetChunkEntry(chkGuid);
                if (chkEntry != null)
                    chkRefs.Add(chkEntry);
            }
            foreach (string refName in rawDependency.refNames)
            {
                EbxAssetEntry refEntry = App.AssetManager.GetEbxEntry(refName);
                if (refEntry != null)
                    ebxRefs.Add(refEntry);

                ResAssetEntry resEntry = App.AssetManager.GetResEntry(refName);
                if (resEntry != null)
                    resRefs.Add(resEntry);
            }
        }
    }
    public static class DependenciesCache
    {
        private static string cacheFileName = $"{App.FileSystem.CacheName}/AutoBundleManager/DepedenciesCache.cache";
        private static int cacheVersion = 5;
        private static bool cacheNeedsUpdating = false;
        private static Dictionary<Sha1, DependencyRaw> dependencies = new Dictionary<Sha1, DependencyRaw>();
        private static Dictionary<ResourceType, Type> resLoggerExtensions = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(ResDependencyDetector))).ToDictionary(type => ((ResDependencyDetector)Activator.CreateInstance(type)).resType, type => type);
        public static Sha1 GetSha1(AssetEntry parEntry)
        {
            if (parEntry.HasModifiedData)
                return parEntry.ModifiedEntry.Sha1;
            return parEntry.Sha1;
        }
        private static DependencyRaw GetRawDependencies(AssetEntry parEntry, bool getResDependencies = true)
        {
            Sha1 sha1 = GetSha1(parEntry);
            if (!dependencies.ContainsKey(sha1))
            {
                cacheNeedsUpdating = true;
                if (parEntry.GetType() == typeof(EbxAssetEntry))
                {
                    EbxDependencyDetector ebxDependencyDetector = new EbxDependencyDetector((EbxAssetEntry)parEntry, App.AssetManager.GetEbx((EbxAssetEntry)parEntry));
                    dependencies.Add(sha1, new DependencyRaw(ebxDependencyDetector.refNames, ebxDependencyDetector.ebxPointerGuids, ebxDependencyDetector.resRids, ebxDependencyDetector.chunkGuids) { });
                }
                else if (parEntry.GetType() == typeof(ResAssetEntry))
                {
                    ResDependencyDetector extension = (ResDependencyDetector)Activator.CreateInstance(resLoggerExtensions[(ResourceType)((ResAssetEntry)parEntry).ResType]);
                    extension.ExtractData(((ResAssetEntry)parEntry).ResRid);
                    dependencies.Add(sha1, new DependencyRaw(extension.refNames, extension.ebxPointerGuids, extension.resRids, extension.chunkGuids) { });
                }
                else
                    throw new Exception("Unknown AssetEntry Type");
            }
            DependencyRaw dependencyData =  new DependencyRaw(dependencies[sha1]);
            void ExtractRes(ulong resRid)
            {
                ResAssetEntry resEntry = App.AssetManager.GetResEntry(resRid);
                if (resEntry != null && resLoggerExtensions.ContainsKey((ResourceType)resEntry.ResType))
                {
                    DependencyRaw resDependencies = GetRawDependencies(resEntry, true);

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
                    foreach (Guid chkGuid in resDependencies.chunkGuids)
                        dependencyData.chunkGuids.Add(chkGuid);
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
        public static DependencyData GetDependencies(AssetEntry parEntry)
        {
            return new DependencyData(GetRawDependencies(parEntry));
        }
        public static void UpdateCache()
        {
            if (!cacheNeedsUpdating)
                return;
            if (!Directory.Exists(Path.GetDirectoryName(cacheFileName)))
                Directory.CreateDirectory(Path.GetDirectoryName(cacheFileName));
            using (NativeWriter writer = new NativeWriter(new FileStream(cacheFileName, FileMode.Create)))
            {
                writer.WriteNullTerminatedString("MopMagicMopMagic"); //Magic
                writer.Write(cacheVersion);
                writer.Write(dependencies.Count);
                foreach(KeyValuePair<Sha1, DependencyRaw> pair in dependencies)
                {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value.refNames);
                    writer.Write(pair.Value.ebxGuids);
                    writer.Write(pair.Value.resRids);
                    writer.Write(pair.Value.chunkGuids);
                }
            }
            cacheNeedsUpdating = false;
        }

        static DependenciesCache()
        {
            //Read Cache
            if (!File.Exists(cacheFileName))
                return;
            using (NativeReader reader = new NativeReader(new FileStream(cacheFileName, FileMode.Open, FileAccess.Read)))
            {
                if (reader.ReadNullTerminatedString() != "MopMagicMopMagic" || reader.ReadInt() != cacheVersion)
                    return;
                int dependencyCount = reader.ReadInt();
                for (int i = 0; i < dependencyCount; i++)
                    dependencies.Add(reader.ReadSha1(), new DependencyRaw(reader.ReadHashSetStrings(), reader.ReadHashSetGuids(), reader.ReadHashSetULongs(), reader.ReadHashSetGuids()));
            }
        }
    }
}
