using Frosty.Core;
using Frosty.Core.IO;
using Frosty.Core.Mod;
using Frosty.Core.Windows;
using Frosty.Hash;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using FrostySdk.Resources;
using MeshSetPlugin.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;

namespace AutoBundleManagerPlugin.Logic.Operations
{
    public class FbmodParsing
    {
        private static int cacheVersion = 5;
        protected static int HashBundle(BundleEntry bentry)
        {
            return HashBundle(bentry.Name);
        }
        protected static int HashBundle(string bName)
        {
            int hash = Fnv1.HashString(bName.ToLower());

            if (bName.Length == 8 && int.TryParse(bName, System.Globalization.NumberStyles.HexNumber, null, out int tmp))
                hash = tmp;

            return hash;
        }
        protected static Dictionary<int, int> superBundleHashesToIds = App.AssetManager.EnumerateSuperBundles().ToDictionary(supBundle => Fnv1a.HashString(supBundle.Name.ToLower()), supBundle => App.AssetManager.GetSuperBundleId(supBundle));
        protected static Dictionary<uint, string> typeHashesToNames = TypeLibrary.GetConcreteTypes().ToDictionary(type => (uint)Utils.HashString(type.Name, true), type => type.Name);
        protected static Dictionary<int, string> bundleHashesToNames = App.AssetManager.EnumerateBundles().Where(bEntry => !bEntry.Added && !bEntry.Imaginary).ToDictionary(bunEntry => HashBundle(bunEntry), bunEntry => bunEntry.Name);
        private Dictionary<int, string> adaptiveBundleHashesToIds = new Dictionary<int, string>(bundleHashesToNames) { };
        private static List<string> swbf2GameplayHandlerTypes = new List<string> { "WSTeamData" };

        private List<ParsedCustomBundle> ParsedBundles = new List<ParsedCustomBundle>();
        public List<ParsedModifiedEbx> ParsedEbx = new List<ParsedModifiedEbx>();
        private List<ParsedModifiedRes> ParsedRes = new List<ParsedModifiedRes>();
        private List<ParsedModifiedChunk> ParsedChunks = new List<ParsedModifiedChunk>();
        private List<string> BundleIndexOrder = new List<string>(); //List for storing bundle IDs so that the cache doesn't have to repeatedly read strings
        private class ParsedCustomBundle
        {
            public string Name;
            public BundleType Type;
            public int SuperBundleId;
            private int indexOfBundleInOrder;
            public void WriteToCache(NativeWriter writer)
            {
                writer.Write(indexOfBundleInOrder);
                writer.WriteNullTerminatedString(Type.ToString());
                writer.Write(SuperBundleId);
                //parser.adaptiveBundleHashesToIds.Add(HashBundle(Name), Name);
            }
            public ParsedCustomBundle(FbmodParsing parser, BaseModResource resource)
            {
                BundleEntry bEntry = new BundleEntry();
                resource.FillAssetEntry(bEntry);
                Name = bEntry.Name;
                Type = bEntry.Type;
                if (bEntry.Name.EndsWith("_bpb"))
                    Type = BundleType.BlueprintBundle;
                SuperBundleId = bEntry.SuperBundleId;
                if (!parser.adaptiveBundleHashesToIds.ContainsKey(HashBundle(Name)))
                    parser.adaptiveBundleHashesToIds.Add(HashBundle(Name), Name);
                if (!parser.BundleIndexOrder.Contains(Name))
                    parser.BundleIndexOrder.Add(Name);
                indexOfBundleInOrder = parser.BundleIndexOrder.IndexOf(Name);
            }
            public ParsedCustomBundle(FbmodParsing parser, NativeReader reader)
            {
                Name = parser.BundleIndexOrder[reader.ReadInt()]; //Name = reader.ReadNullTerminatedString();
                Type = (BundleType)Enum.Parse(typeof(BundleType), reader.ReadNullTerminatedString());
                SuperBundleId = reader.ReadInt();
                if (!parser.adaptiveBundleHashesToIds.ContainsKey(HashBundle(Name)))
                    parser.adaptiveBundleHashesToIds.Add(HashBundle(Name), Name);
            }
        }
        public class ParsedModifiedEbx
        {
            public string Name;

            public bool ContainsModifiedData;
            public string Type;
            public bool HasHandler = false;
            public Guid FileGuid;
            public Sha1 Hash;
            public HashSet<string> AddedBundles = new HashSet<string>();


            public EbxAsset modifiedAsset; //Only when parsing the fbmod
            public byte[] data; //Only when parsing the fbmod

            public void WriteToCache(FbmodParsing parser, NativeWriter writer)
            {
                writer.WriteNullTerminatedString(Name);
                writer.Write(ContainsModifiedData);
                if (ContainsModifiedData)
                {
                    writer.Write((uint)Utils.HashString(Type, true));
                    writer.Write(HasHandler);
                    writer.Write(FileGuid);
                    writer.Write(Hash);
                }
                parser.WriteAddedBundles(writer, AddedBundles);
            }
            public ParsedModifiedEbx(FbmodParsing parser, NativeReader reader)
            {
                Name = reader.ReadNullTerminatedString();
                ContainsModifiedData = reader.ReadBoolean();
                if (ContainsModifiedData)
                {
                    Type = typeHashesToNames[(uint)reader.ReadUInt()];
                    HasHandler = reader.ReadBoolean();
                    FileGuid = reader.ReadGuid();
                    Hash = reader.ReadSha1();
                }

                AddedBundles = parser.ReadAddedBundles(reader);
            }

            public ParsedModifiedEbx(FbmodParsing parser, BaseModResource resource, FrostyModReader fbmodReader, ResourceManager rm)
            {
                Name = resource.Name;
                Object dataObject = null;

                ContainsModifiedData = resource.IsModified;

                if (resource.IsModified)
                {
                    data = fbmodReader.GetResourceData(resource);
                    Hash = Utils.GenerateSha1(data);
                    HasHandler = resource.HasHandler;
                    if (!resource.HasHandler)
                    {
                        Stream ebxStream = rm.GetResourceData(data);
                        using (EbxReader reader = EbxReader.CreateReader(rm.GetResourceData(data), App.FileSystem))
                            dataObject = reader.ReadAsset<EbxAsset>();
                        string rootName = ((dynamic)((EbxAsset)dataObject).RootObject).Name;
                        if (rootName.ToLower() == Name.ToLower())
                            Name = rootName;
                        Type = ((dynamic)((EbxAsset)dataObject).RootObject).GetType().Name;
                        FileGuid = ((EbxAsset)dataObject).FileGuid;
                        modifiedAsset = ((EbxAsset)dataObject);
                    }
                    else
                    {
                        dataObject = data;
                        Type = typeHashesToNames[(uint)resource.Handler];
                        //if (swbf2GameplayHandlerTypes.Contains(Type))
                        //{
                        ICustomActionHandler handler = App.PluginManager.GetCustomHandler((uint)resource.Handler);
                        if (handler == null)
                            throw new Exception($"Missing custom handler capable of reading asset type \"{typeHashesToNames[(uint)resource.Handler]}\"");

                        HandlerExtraData extraData = new HandlerExtraData();

                        extraData.Handler = App.PluginManager.GetCustomHandler((uint)Utils.HashString(Type, true)); ;
                        extraData.Data = extraData.Handler.Load(extraData.Data, (byte[])dataObject);
                        RuntimeResources runtimeResources = new RuntimeResources();
                        AssetEntry newEntry = new AssetEntry() { Name = Name };

                        EbxAssetEntry origEntry = App.AssetManager.GetEbxEntry(newEntry.Name);
                        Sha1 origSha1 = origEntry.Sha1;
                        long origSize = origEntry.Size;


                        extraData.Handler.Modify(resource.IsAdded ? newEntry : App.AssetManager.GetEbxEntry(Name), App.AssetManager, runtimeResources, extraData.Data, out byte[] ebxData);
                        using (EbxReader reader = EbxReader.CreateReader(rm.GetResourceData(ebxData), App.FileSystem))
                        {
                            modifiedAsset = reader.ReadAsset<EbxAsset>();
                            Name = ((dynamic)((EbxAsset)modifiedAsset).RootObject).Name;
                            Type = ((dynamic)((EbxAsset)modifiedAsset).RootObject).GetType().Name;
                            reader.BaseStream.Position = 0;
                            Hash = Utils.GenerateSha1(reader.ReadToEnd());
                        }
                        origEntry.Sha1 = origSha1;
                        origEntry.Size = origSize;
                        //}

                    }
                }
                else if (!resource.IsAdded)
                    Name = App.AssetManager.GetEbxEntry(Name).Name; //Fix capitalisaiton
                AddedBundles = parser.GetResourceBundles(resource);
            }
        }
        private class ParsedModifiedRes
        {
            public string Name;

            public bool ContainsModifiedData; 

            public ResourceType ResType; 
            public bool HasHandler = false; //Important to know if we need to merge between mods
            public ulong ResRid; //Need to figure out way to work this out
            public Sha1 Hash;
            public HashSet<string> AddedBundles = new HashSet<string>();

            public byte[] ResMeta; //Only when parsing the fbmod
            public byte[] data; //Only when parsing the fbmod

            public void WriteToCache(FbmodParsing parser, NativeWriter writer)
            {
                writer.WriteNullTerminatedString(Name);
                writer.Write(ContainsModifiedData);
                if (ContainsModifiedData)
                {
                    writer.Write(HasHandler);
                    writer.Write((uint)ResType);
                    writer.Write(ResRid);
                    writer.Write(Hash);
                }
                parser.WriteAddedBundles(writer, AddedBundles);
            }

            public ParsedModifiedRes(FbmodParsing parser, NativeReader reader)
            {
                Name = reader.ReadNullTerminatedString();
                ContainsModifiedData = reader.ReadBoolean();
                if (ContainsModifiedData)
                {
                    HasHandler = reader.ReadBoolean();
                    ResType = (ResourceType)reader.ReadUInt();
                    ResRid = reader.ReadULong();
                    Hash = reader.ReadSha1();
                }
                AddedBundles = parser.ReadAddedBundles(reader);
            }

            public ParsedModifiedRes(FbmodParsing parser, BaseModResource resource, FrostyModReader fbmodReader, ResourceManager rm)
            {
                Name = resource.Name;
                ContainsModifiedData = resource.IsModified;

                if (resource.IsModified)
                {
                    ResAssetEntry resEntry = new ResAssetEntry();
                    resource.FillAssetEntry(resEntry);

                    HasHandler = resource.HasHandler;
                    ResRid = resEntry.ResRid;
                    ResType = (ResourceType)resEntry.ResType;
                    ResMeta = resEntry.ResMeta;
                    data = fbmodReader.GetResourceData(resource);
                    if (resource.HasHandler)
                    {
                        // Don't need to care about res handlers yet
                        ICustomActionHandler handler = App.PluginManager.GetCustomHandler(ResType);
                        if (handler == null)
                            throw new Exception($"Missing custom handler capable of reading asset type \"{ResType}\"");

                        HandlerExtraData extraData = new HandlerExtraData();

                        extraData.Handler = App.PluginManager.GetCustomHandler(ResType);
                        extraData.Data = extraData.Handler.Load(extraData.Data, data);
                        RuntimeResources runtimeResources = new RuntimeResources();
                        AssetEntry newEntry = new AssetEntry() { Name = Name };

                        ResAssetEntry oldEntry = App.AssetManager.GetResEntry(Name);

                        Sha1 oldSha1 = oldEntry.Sha1;
                        long oldOriginalSize = oldEntry.OriginalSize;
                        long oldSize = oldEntry.Size;
                        byte[] oldMeta = oldEntry.ResMeta;


                        extraData.Handler.Modify(oldEntry, App.AssetManager, runtimeResources, extraData.Data, out byte[] resData);
                        using (NativeReader reader = new NativeReader(rm.GetResourceData(resData)))
                            data = reader.ReadToEnd();

                        oldEntry.Sha1 = oldSha1;
                        oldEntry.OriginalSize = oldOriginalSize;
                        oldEntry.Size = oldSize;
                        oldEntry.ResMeta = oldMeta;


                        //extraData.Handler.Modify(resource.IsAdded ? newEntry : App.AssetManager.GetResEntry(Name), App.AssetManager, runtimeResources, extraData.Data, out byte[] resData);
                        //using (NativeReader reader = new NativeReader(rm.GetResourceData(resData)))
                        //    data = reader.ReadToEnd();

                    }
                    else
                    {
                        using (NativeReader reader = new NativeReader(rm.GetResourceData(data)))
                            data = reader.ReadToEnd();
                    }
                    Hash = Utils.GenerateSha1(data);
                }

                AddedBundles = parser.GetResourceBundles(resource);
            }
        }
        private class ParsedModifiedChunk //No handlers here so only need to care about what needs to be written to the fbmod in terms of imaginary assets
        {
            public Guid ChunkGuid;
            public int FirstMip;
            public int H32;
            public HashSet<string> AddedBundles = new HashSet<string>();

            public void WriteToCache(FbmodParsing parser, NativeWriter writer)
            {
                writer.Write(ChunkGuid);
                writer.Write(FirstMip);
                writer.Write(H32);
                parser.WriteAddedBundles(writer, AddedBundles);
            }
            public ParsedModifiedChunk(FbmodParsing parser, NativeReader reader)
            {
                ChunkGuid = reader.ReadGuid();
                FirstMip = reader.ReadInt();
                H32 = reader.ReadInt();
                AddedBundles = parser.ReadAddedBundles(reader);
            }

            public ParsedModifiedChunk(FbmodParsing parser, BaseModResource resource, FrostyModReader fbmodReader, ResourceManager rm)
            {
                ChunkGuid = new Guid(resource.Name);
                ChunkAssetEntry chkReadEntry = new ChunkAssetEntry();
                resource.FillAssetEntry(chkReadEntry);
                FirstMip = chkReadEntry.FirstMip;
                H32 = chkReadEntry.H32;
                AddedBundles = parser.GetResourceBundles(resource);
            }
        }

        static string CalculateChecksum(string filePath, HashAlgorithm algorithm)
        {
            using (var stream = File.OpenRead(filePath))
            {
                byte[] checksumBytes = algorithm.ComputeHash(stream);
                return BitConverter.ToString(checksumBytes).Replace("-", String.Empty);
            }
        }
        void AddImaginaryAssetsToAssetManager()
        {
            foreach (ParsedCustomBundle customBundle in ParsedBundles.Where(customBundle => App.AssetManager.GetBundleId(customBundle.Name) == -1))
                App.AssetManager.AddImaginaryBundle(customBundle.Name, customBundle.Type, superBundleHashesToIds[customBundle.SuperBundleId]);

            foreach (ParsedModifiedEbx parsedEbx in ParsedEbx)
            {
                EbxAssetEntry refEntry = App.AssetManager.GetEbxEntry(parsedEbx.FileGuid);
                int[] bunIds = { };// parsedEbx.AddedBundles.Select(bunName => App.AssetManager.GetBundleId(bunName)).ToArray();
                if (refEntry == null)
                    App.AssetManager.AddImaginaryEbx(parsedEbx.Name, parsedEbx.FileGuid, parsedEbx.ContainsModifiedData ? parsedEbx.Hash : Sha1.Zero, parsedEbx.ContainsModifiedData ? parsedEbx.Type : null, bunIds);
                else
                {
                    if (parsedEbx.ContainsModifiedData && refEntry.IsImaginary)
                    {
                        if (refEntry.IsTotallyImaginary)
                            refEntry.Type = parsedEbx.Type;
                        refEntry.Sha1 = parsedEbx.Hash;

                    }
                    refEntry.AddToBundles(bunIds);
                }
            }

            foreach (ParsedModifiedRes parsedRes in ParsedRes)
            {
                parsedRes.AddedBundles.Clear();
                ResAssetEntry resEntry = App.AssetManager.GetResEntry(parsedRes.Name);
                int[] bunIds = { };// = (int[])parsedRes.AddedBundles.Select(bunName => App.AssetManager.GetBundleId(bunName)).ToArray();
                if (resEntry == null)
                {
                    if (parsedRes.ContainsModifiedData)
                        App.AssetManager.AddImaginaryRes(parsedRes.Name, parsedRes.ResRid, parsedRes.ResType, parsedRes.Hash, bunIds);
                    else
                        App.AssetManager.AddImaginaryRes(parsedRes.Name, 0, ResourceType.Invalid, Sha1.Zero, bunIds);
                }
                else
                {
                    if (parsedRes.ContainsModifiedData && resEntry.IsImaginary)
                    {
                        if (App.AssetManager.GetResEntry(parsedRes.ResRid) == null)
                            App.AssetManager.UpdateImaginaryResRid(resEntry, resEntry.ResRid);
                        if (resEntry.IsTotallyImaginary)
                            resEntry.ResType = (uint)parsedRes.ResType;
                        resEntry.Sha1 = parsedRes.Hash;

                    }
                    resEntry.AddToBundles(bunIds);
                }
            }
            foreach (ParsedModifiedChunk parsedChk in ParsedChunks)
            {
                parsedChk.AddedBundles.Clear();
                ChunkAssetEntry chkEntry = App.AssetManager.GetChunkEntry(parsedChk.ChunkGuid);
                // Fix this to be chk and don't forget FirstMip/H32
                int[] bunIds = { };// = (int[])parsedChk.AddedBundles.Select(bunName => App.AssetManager.GetBundleId(bunName)).ToArray();
                if (chkEntry == null)
                    App.AssetManager.AddImaginaryChunk(parsedChk.ChunkGuid, parsedChk.H32, parsedChk.FirstMip, bunIds); //App.AssetManager.AddImaginaryEbx(parsedChk.Name, parsedEbx.FileGuid, parsedEbx.Hash, parsedEbx.Type, parsedEbx.AddedBundles.Select(bunName => App.AssetManager.GetBundleId(bunName)).ToArray());
                else
                {
                    chkEntry.AddToBundles(bunIds);
                    chkEntry.H32 = parsedChk.H32;
                    chkEntry.FirstMip = parsedChk.FirstMip;
                }
            }
        }
        public bool TryReadCache(string cacheFullPath, string checksum, FrostyTaskWindow task, string fbmodShortName)
        {
            if (!File.Exists(cacheFullPath))
                return false;

            using (NativeReader reader = new NativeReader(new FileStream(cacheFullPath, FileMode.Open, FileAccess.Read)))
            {
                task.Update($"ABM: Loading {fbmodShortName}");
                if (reader.ReadNullTerminatedString() != "MopMagicFbmodCac" || reader.ReadInt() != cacheVersion)
                    return false;
                if (reader.ReadNullTerminatedString() != checksum)
                    return false;
                int bundleOrderCount = reader.ReadInt();
                int parsedBundlesCount = reader.ReadInt();
                int parsedEbxCount = reader.ReadInt();
                int parsedResCount = reader.ReadInt();
                int parsedChkCount = reader.ReadInt();
                for (int i = 0; i < bundleOrderCount; i++)
                    BundleIndexOrder.Add(reader.ReadNullTerminatedString());
                for (int i = 0; i < parsedBundlesCount; i++)
                    ParsedBundles.Add(new ParsedCustomBundle(this, reader));
                for (int i = 0; i < parsedEbxCount; i++)
                {
                    ParsedModifiedEbx parsedEbx = new ParsedModifiedEbx(this, reader);
                    if (parsedEbx.Hash != null &&  parsedEbx.Hash != Sha1.Zero && !AbmDependenciesCache.HasSha1(parsedEbx.Hash))
                        return false;
                    ParsedEbx.Add(parsedEbx);
                }
                for (int i = 0; i < parsedResCount; i++)
                {
                    ParsedModifiedRes parsedRes = new ParsedModifiedRes(this, reader);
                    if (parsedRes.Hash != null && parsedRes.Hash != Sha1.Zero && !AbmDependenciesCache.HasSha1(parsedRes.Hash))
                        return false;
                    ParsedRes.Add(parsedRes);
                }
                for (int i = 0; i < parsedChkCount; i++)
                    ParsedChunks.Add(new ParsedModifiedChunk(this, reader));

            }
            task.Update($"ABM: Adding Imaginary {fbmodShortName}");
            AddImaginaryAssetsToAssetManager();
            task.Update($"ABM: Compoleted {fbmodShortName}");

            return true;
        }
        private HashSet<string> GetResourceBundles(BaseModResource resource)
        {
            foreach (int bunHash in resource.AddedBundles.Where(bunHash => !adaptiveBundleHashesToIds.ContainsKey(bunHash) && bunHash != 1548502573)) //1548502573 is chunk bundle
                App.Logger.LogWarning($"{resource.Name}\t{bunHash}");
            HashSet<string> bundles = new HashSet<string>(resource.AddedBundles.Where(bunHash => adaptiveBundleHashesToIds.ContainsKey(bunHash)).Select(bunHash => adaptiveBundleHashesToIds[bunHash]).ToList());
            foreach(string addedBundle in bundles)
                if (!BundleIndexOrder.Contains(addedBundle))
                    BundleIndexOrder.Add(addedBundle);
            return bundles;
        }
        public Dictionary<AssetEntry, Sha1> GetModifiedSha1s()
        {
            Dictionary<AssetEntry, Sha1> modifiedSha1s = new Dictionary<AssetEntry, Sha1>();
            foreach (ParsedModifiedEbx parsedEbx in ParsedEbx.Where(parsedEbx => parsedEbx.ContainsModifiedData))
            {
                EbxAssetEntry modifEbxEntry = App.AssetManager.GetEbxEntry(parsedEbx.Name);
                if (modifEbxEntry == null)
                    modifEbxEntry = App.AssetManager.GetEbxEntry(parsedEbx.FileGuid);
                if (modifiedSha1s.ContainsKey(modifEbxEntry))
                    modifiedSha1s[modifEbxEntry] = parsedEbx.Hash;
                else
                    modifiedSha1s.Add(modifEbxEntry, parsedEbx.Hash);
            }
            foreach (ParsedModifiedRes parsedRes in ParsedRes.Where(parsedRes => parsedRes.ContainsModifiedData))
            {
                ResAssetEntry modifiedResEntry = App.AssetManager.GetResEntry(parsedRes.Name);
                if (modifiedSha1s.ContainsKey(modifiedResEntry))
                    modifiedSha1s[modifiedResEntry] = parsedRes.Hash;
                else
                    modifiedSha1s.Add(modifiedResEntry, parsedRes.Hash);
            }
            return modifiedSha1s;
        }

        public void WriteAddedBundles(NativeWriter writer, HashSet<string> AddedBundles)
        {
            writer.Write(AddedBundles.Count);
            foreach (string addedBundle in AddedBundles)
            {
                writer.Write(BundleIndexOrder.IndexOf(addedBundle));
            }
        }
        public HashSet<string> ReadAddedBundles(NativeReader reader)
        {
            HashSet<string> AddedBundles = new HashSet<string>();
            int addedBundleCount = reader.ReadInt();
            for (int i = 0; i < addedBundleCount; i++)
            {
                //reader.ReadInt();
                //AddedBundles.Add("win32/A3/Levels/SP/M2PIL/DS02/StreamingZones");
                AddedBundles.Add(BundleIndexOrder[reader.ReadInt()]);
            }
                //AddedBundles.Add(BundleIndexOrder[reader.ReadInt()]);
            return AddedBundles;
        }
        public FbmodParsing(string fbmodFullPath, FrostyTaskWindow task)
        {
            string checksum = CalculateChecksum(fbmodFullPath, new SHA256Managed());
            string fbmodShortName = fbmodFullPath.Split('/').Last();
            string cacheFullPath = $"{App.FileSystem.CacheName}/AutoBundleManager/Fbmods/{fbmodShortName.Replace(".fbmod", "")}.cache";
            //App.Logger.Log(fbmodShortName);
            //App.Logger.Log(cacheFullPath);

            bool parseFbmod = !TryReadCache(cacheFullPath, checksum, task, fbmodShortName);


            //App.Logger.Log("SHA256 Checksum: " + checksum);


            if (parseFbmod)
            {
                task.Update($"ABM: Caching {fbmodShortName}");
                adaptiveBundleHashesToIds = new Dictionary<int, string>(bundleHashesToNames);
                ParsedBundles.Clear();
                ParsedEbx.Clear();
                ParsedRes.Clear();
                ParsedChunks.Clear();


                ResourceManager rm = new ResourceManager(App.FileSystem);
                rm.Initialize();
                using (FrostyModReader reader = new FrostyModReader(new FileStream(fbmodFullPath, FileMode.Open, FileAccess.Read)))
                {
                    if (reader.IsValid)
                    {
                        reader.ReadModDetails();

                        BaseModResource[] resources = reader.ReadResources();
                        foreach (BaseModResource resource in resources)
                        {
                            switch (resource.Type)
                            {
                                case ModResourceType.Bundle:
                                    {
                                        ParsedBundles.Add(new ParsedCustomBundle(this, resource));
                                        break;
                                    }
                                case ModResourceType.Ebx:
                                    {
                                        ParsedEbx.Add(new ParsedModifiedEbx(this, resource, reader, rm));
                                        break;
                                    }
                                case ModResourceType.Res:
                                    {
                                        ParsedRes.Add(new ParsedModifiedRes(this, resource, reader, rm));
                                        break;
                                    }
                                case ModResourceType.Chunk:
                                    {
                                        ParsedChunks.Add(new ParsedModifiedChunk(this, resource, reader, rm));
                                        break;
                                    }
                            }
                        }
                    }
                }
                AddImaginaryAssetsToAssetManager();
                foreach (ParsedModifiedEbx parsedEbx in ParsedEbx.Where(parsedEbx => parsedEbx.modifiedAsset != null))
                {
                    EbxAssetEntry parEntry = new EbxAssetEntry()
                    {
                        Name = parsedEbx.Name,
                        Type = parsedEbx.Type,
                        Guid = parsedEbx.FileGuid,
                        ModifiedEntry = new ModifiedAssetEntry() { Data = parsedEbx.data }
                    };
                    if (parsedEbx.modifiedAsset != null)
                        AbmDependenciesCache.CreateRawEbxDependency(parsedEbx.Hash, parEntry, parsedEbx.modifiedAsset);
                    else if (parsedEbx.Hash != null) //Handler case - To be removed?
                        AbmDependenciesCache.CreateRawEbxDependency(parsedEbx.Hash, parEntry, isEmpty: true);
                }
                foreach (ParsedModifiedRes parsedRes in ParsedRes.Where(parsedRes => parsedRes.data != null))
                {
                    ResAssetEntry parEntry = new ResAssetEntry()
                    {
                        Name = parsedRes.Name,
                        ResType = (uint)parsedRes.ResType,
                        ResMeta = parsedRes.ResMeta,
                        ModifiedEntry = new ModifiedAssetEntry() { Data = parsedRes.data }
                    };
                    if (parsedRes.data != null)
                        AbmDependenciesCache.CreateRawResDependency(parsedRes.Hash, parEntry, new MemoryStream(parsedRes.data));
                    else if (parsedRes.Hash != null) //Handler case - To be removed?
                        AbmDependenciesCache.CreateRawResDependency(parsedRes.Hash, parEntry, isEmpty: true);
                }

                if (!Directory.Exists(Path.GetDirectoryName(cacheFullPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(cacheFullPath));
                using (NativeWriter writer = new NativeWriter(new FileStream(cacheFullPath, FileMode.Create)))
                {
                    writer.WriteNullTerminatedString("MopMagicFbmodCac"); //Magic
                    writer.Write(cacheVersion);
                    writer.WriteNullTerminatedString(checksum);
                    writer.Write(BundleIndexOrder.Count);
                    writer.Write(ParsedBundles.Count);
                    writer.Write(ParsedEbx.Count);
                    writer.Write(ParsedRes.Count);
                    writer.Write(ParsedChunks.Count);
                    foreach(string bundleName in BundleIndexOrder)
                        writer.WriteNullTerminatedString(bundleName);
                    foreach (ParsedCustomBundle parsedBundle in ParsedBundles)
                        parsedBundle.WriteToCache(writer);
                    foreach (ParsedModifiedEbx parsedEbx in ParsedEbx)
                        parsedEbx.WriteToCache(this, writer);
                    foreach (ParsedModifiedRes parsedRes in ParsedRes)
                        parsedRes.WriteToCache(this, writer);
                    foreach (ParsedModifiedChunk parsedChk in ParsedChunks)
                        parsedChk.WriteToCache(this, writer);
                }
            }
        }
    }
}
