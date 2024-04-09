using Frosty.Core;
using Frosty.Core.IO;
using Frosty.Core.Mod;
using Frosty.Hash;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace AutoBundleManagerPlugin.Logic.Operations
{
    public class FbmodParsing
    {
        private static int cacheVersion = 1;
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
        private List<ParsedModifiedEbx> ParsedEbx = new List<ParsedModifiedEbx>();
        private List<ParsedModifiedRes> ParsedRes = new List<ParsedModifiedRes>();
        private List<ParsedModifiedChunk> ParsedChunks = new List<ParsedModifiedChunk>();
        private class ParsedCustomBundle
        {
            public string Name;
            public BundleType Type;
            public int SuperBundleId;
            public void WriteToCache(FbmodParsing parser, NativeWriter writer)
            {
                writer.WriteNullTerminatedString(Name);
                writer.WriteNullTerminatedString(Type.ToString());
                writer.Write(SuperBundleId);
                parser.adaptiveBundleHashesToIds.Add(HashBundle(Name), Name);
            }
            public ParsedCustomBundle(FbmodParsing parser, BaseModResource resource)
            {
                BundleEntry bEntry = new BundleEntry();
                resource.FillAssetEntry(bEntry);
                Type = bEntry.Type;
                if (bEntry.Name.EndsWith("_bpb"))
                    Type = BundleType.BlueprintBundle;
                SuperBundleId = bEntry.SuperBundleId;
                parser.adaptiveBundleHashesToIds.Add(HashBundle(Name), Name);
            }
            public ParsedCustomBundle(NativeReader reader)
            {
                Name = reader.ReadNullTerminatedString();
                Type = (BundleType)Enum.Parse(typeof(BundleType), reader.ReadNullTerminatedString());
                SuperBundleId = reader.ReadInt();
            }
        }
        private class ParsedModifiedEbx
        {
            public string Name;
            public string Type;
            public bool IsImaginary = false;
            public bool HasHandler = false;
            public Guid FileGuid;
            public Sha1 Hash;
            public HashSet<string> AddedBundles = new HashSet<string>();


            public EbxAsset modifiedAsset; //Only when parsing the fbmod
            public byte[] data; //Only when parsing the fbmod

            public void WriteToCache(NativeWriter writer)
            {
                writer.WriteNullTerminatedString(Name);
                writer.WriteNullTerminatedString(Type);
                writer.Write(IsImaginary);
                writer.Write(HasHandler);
                writer.Write(FileGuid);
                writer.Write(Hash != null);
                if (Hash != null)
                    writer.Write(Hash);
                writer.Write(AddedBundles);
            }
            public ParsedModifiedEbx(NativeReader reader)
            {
                Name = reader.ReadNullTerminatedString();
                Type = reader.ReadNullTerminatedString();
                IsImaginary = reader.ReadBoolean();
                HasHandler = reader.ReadBoolean();
                FileGuid = reader.ReadGuid();
                if (reader.ReadBoolean())
                    Hash = reader.ReadSha1();
                AddedBundles = reader.ReadHashSetStrings();
            }

            public ParsedModifiedEbx(FbmodParsing parser, BaseModResource resource, FrostyModReader fbmodReader, ResourceManager rm)
            {
                Name = resource.Name;
                Object dataObject = null;

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
                        Name = ((dynamic)((EbxAsset)dataObject).RootObject).Name;
                        Type = ((dynamic)((EbxAsset)dataObject).RootObject).GetType().Name;
                        FileGuid = ((EbxAsset)dataObject).FileGuid;
                        modifiedAsset = ((EbxAsset)dataObject);
                    }
                    else
                    {
                        dataObject = data;
                        Type = typeHashesToNames[(uint)resource.Handler];
                        if (swbf2GameplayHandlerTypes.Contains(Type))
                        {
                            ICustomActionHandler handler = App.PluginManager.GetCustomHandler((uint)resource.Handler);
                            if (handler == null)
                                throw new Exception($"Missing custom handler capable of reading asset type \"{typeHashesToNames[(uint)resource.Handler]}\"");

                            HandlerExtraData extraData = new HandlerExtraData();

                            extraData.Handler = App.PluginManager.GetCustomHandler((uint)Utils.HashString(Type, true)); ;
                            extraData.Data = extraData.Handler.Load(extraData.Data, (byte[])dataObject);
                            RuntimeResources runtimeResources = new RuntimeResources();
                            AssetEntry newEntry = new AssetEntry() { Name = Name };
                            extraData.Handler.Modify(resource.IsAdded ? newEntry : App.AssetManager.GetEbxEntry(Name), App.AssetManager, runtimeResources, extraData.Data, out byte[] ebxData);
                            using (EbxReader reader = EbxReader.CreateReader(new ResourceManager(App.FileSystem).GetResourceData(ebxData), App.FileSystem))
                            {
                                modifiedAsset = reader.ReadAsset<EbxAsset>();
                                Name = ((dynamic)((EbxAsset)modifiedAsset).RootObject).Name;
                                Type = ((dynamic)((EbxAsset)modifiedAsset).RootObject).GetType().Name;
                                reader.BaseStream.Position = 0;
                                Hash = Utils.GenerateSha1(reader.ReadToEnd());
                            }
                        }

                    }
                }
                else if (!resource.IsAdded)
                {
                    Name = App.AssetManager.GetEbxEntry(Name).Name; //Fix capitalisaiton
                    Type = App.AssetManager.GetEbxEntry(Name).Type;
                }
                else
                    IsImaginary = true;
                AddedBundles = parser.GetResourceBundles(resource);
            }
        }
        private class ParsedModifiedRes
        {
            public string Name;
            public ResourceType ResType; //Need to figure out way to work this out
            public bool IsImaginary = false; //Need to figure out way to work this out
            public bool HasHandler = false;
            public ulong ResRid; //Need to figure out way to work this out
            public byte[] ResMeta; //Need to figure out way to work this out
            public Sha1 Hash;
            public long OriginalSize;
            public HashSet<string> AddedBundles = new HashSet<string>();

            public Object dataObject; //Only when parsing the fbmod

            public ParsedModifiedRes(FbmodParsing parser, BaseModResource resource, FrostyModReader fbmodReader, ResourceManager rm)
            {
                Name = resource.Name;
                byte[] data = null;

                ResAssetEntry resEntry = new ResAssetEntry();
                resource.FillAssetEntry(resEntry);
                bool isAdded = App.AssetManager.GetResEntry(Name) == null || App.AssetManager.GetResEntry(Name).IsAdded;
                HasHandler = resource.HasHandler;

                if (resource.IsModified)
                {
                    data = fbmodReader.GetResourceData(resource);
                    if (resource.HasHandler)
                    {
                        // // Don't need to care about res handlers yet
                        //ICustomActionHandler handler = App.PluginManager.GetCustomHandler(resType);
                        //if (handler == null)
                        //    throw new Exception($"Missing custom handler capable of reading asset type \"{resType}\"");
                    }
                    else
                    {
                        using (NativeReader reader = new NativeReader(rm.GetResourceData(data)))
                            data = reader.ReadToEnd();
                    }
                    Hash = Utils.GenerateSha1(data);
                }
                ResMeta = resEntry.ResMeta;

            }
        }
        private class ParsedModifiedChunk //No handlers here so only need to care about what needs to be written to the fbmod in terms of imaginary assets
        {
            public Guid ChunkGuid;
            public int FirstMip;
            public int H32;
            public HashSet<string> AddedBundles = new HashSet<string>();

            public void WriteToCache(NativeWriter writer)
            {
                writer.Write(ChunkGuid);
                writer.Write(FirstMip);
                writer.Write(H32);
                writer.Write(AddedBundles);
            }
            public ParsedModifiedChunk(NativeReader reader)
            {
                ChunkGuid = reader.ReadGuid();
                FirstMip = reader.ReadInt();
                H32 = reader.ReadInt();
                AddedBundles = reader.ReadHashSetStrings();
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
                EbxAssetEntry refEntry = App.AssetManager.GetEbxEntry(parsedEbx.Name);
                if (refEntry == null)
                    App.AssetManager.AddImaginaryEbx(parsedEbx.Name, parsedEbx.FileGuid, parsedEbx.Hash, parsedEbx.Type, parsedEbx.AddedBundles.Select(bunName => App.AssetManager.GetBundleId(bunName)).ToArray());
                else
                    refEntry.AddToBundles(parsedEbx.AddedBundles.Select(bunName => App.AssetManager.GetBundleId(bunName)));
            }

            foreach (ParsedModifiedRes parsedRes in ParsedRes)
            {
                ResAssetEntry resEntry = App.AssetManager.GetResEntry(parsedRes.Name);
                //Fix this to be res
                if (resEntry == null)
                    App.AssetManager.AddImaginaryRes(parsedRes.Name, parsedRes.ResType, parsedRes.OriginalSize, parsedRes.Hash, parsedRes.AddedBundles.Select(bunName => App.AssetManager.GetBundleId(bunName)).ToArray()); //App.AssetManager.AddImaginaryEbx(parsedRes.Name, parsedEbx.FileGuid, parsedEbx.Hash, parsedEbx.Type, parsedEbx.AddedBundles.Select(bunName => App.AssetManager.GetBundleId(bunName)).ToArray());
                else
                    resEntry.AddToBundles(parsedRes.AddedBundles.Select(bunName => App.AssetManager.GetBundleId(bunName)));
            }
            foreach (ParsedModifiedChunk parsedChk in ParsedChunks)
            {
                ChunkAssetEntry chkEntry = App.AssetManager.GetChunkEntry(parsedChk.ChunkGuid);
                // Fix this to be chk and don't forget FirstMip/H32
                if (chkEntry == null)
                    App.AssetManager.AddImaginaryChunk(0, Sha1.Zero, parsedChk.ChunkGuid, parsedChk.AddedBundles.Select(bunName => App.AssetManager.GetBundleId(bunName)).ToArray()); //App.AssetManager.AddImaginaryEbx(parsedChk.Name, parsedEbx.FileGuid, parsedEbx.Hash, parsedEbx.Type, parsedEbx.AddedBundles.Select(bunName => App.AssetManager.GetBundleId(bunName)).ToArray());
                else
                {
                    chkEntry.AddToBundles(parsedChk.AddedBundles.Select(bunName => App.AssetManager.GetBundleId(bunName)));
                    chkEntry.H32 = parsedChk.H32;
                    chkEntry.FirstMip = parsedChk.FirstMip;
                }
            }
        }
        public bool TryReadCache(string cacheFullPath, string checksum)
        {
            if (!File.Exists(cacheFullPath))
                return false;

            using (NativeReader reader = new NativeReader(new FileStream(cacheFullPath, FileMode.Open, FileAccess.Read)))
            {
                if (reader.ReadNullTerminatedString() != "MopMagicFbmodCac" || reader.ReadInt() != cacheVersion)
                    return false;
                if (reader.ReadNullTerminatedString() != checksum)
                    return false;
                int parsedBundlesCount = reader.ReadInt();
                int parsedEbxCount = reader.ReadInt();
                int parsedResCount = reader.ReadInt();
                int parsedChkCount = reader.ReadInt();
                for (int i = 0; i < parsedBundlesCount; i++)
                    ParsedBundles.Add(new ParsedCustomBundle(reader));
                for (int i = 0; i < parsedEbxCount; i++)
                {
                    ParsedModifiedEbx parsedEbx = new ParsedModifiedEbx(reader);
                    if (parsedEbx.Hash != null && !AbmDependenciesCache.HasSha1(parsedEbx.Hash))
                        return false;
                    ParsedEbx.Add(parsedEbx);
                }
                for (int i = 0; i < parsedResCount; i++)
                {
                    //ParsedModifiedRes parsedRes = new ParsedModifiedRes(reader);
                    //if (parsedRes.Hash != null && !AbmDependenciesCache.HasSha1(parsedRes.Hash))
                    //    return false;
                    //ParsedRes.Add(parsedRes);
                }
                for (int i = 0; i < parsedEbxCount; i++)
                    ParsedChunks.Add(new ParsedModifiedChunk(reader));

            }
            AddImaginaryAssetsToAssetManager();

            return true;
        }
        private HashSet<string> GetResourceBundles(BaseModResource resource)
        {
            foreach (int bunHash in resource.AddedBundles.Where(bunHash => !adaptiveBundleHashesToIds.ContainsKey(bunHash)))
                App.Logger.LogWarning($"{resource.Name}\t{bunHash}");
            return new HashSet<string>(resource.AddedBundles.Where(bunHash => adaptiveBundleHashesToIds.ContainsKey(bunHash)).Select(bunHash => adaptiveBundleHashesToIds[bunHash]).ToList());
        }
        public FbmodParsing(string fbmodFullPath)
        {
            string checksum = CalculateChecksum(fbmodFullPath, new SHA256Managed());
            string fbmodShortName = fbmodFullPath.Split('/').Last();
            string cacheFullPath = $"{App.FileSystem.CacheName}/AutoBundleManager/Fbmods/{fbmodShortName.Replace(".fbmod", "")}.cache";
            //App.Logger.Log(fbmodShortName);
            //App.Logger.Log(cacheFullPath);

            bool parseFbmod = !TryReadCache(cacheFullPath, checksum);


            //App.Logger.Log("SHA256 Checksum: " + checksum);


            if (parseFbmod)
            {
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
                        AbmDependenciesCache.CreateRawEbxDependency(parsedEbx.Hash, parEntry, (parsedEbx.modifiedAsset));
                    else if (parsedEbx.Hash != null)
                        AbmDependenciesCache.CreateRawEbxDependency(parsedEbx.Hash, parEntry, isEmpty: true);
                }

                if (!Directory.Exists(Path.GetDirectoryName(cacheFullPath)))
                    Directory.CreateDirectory(Path.GetDirectoryName(cacheFullPath));
                using (NativeWriter writer = new NativeWriter(new FileStream(cacheFullPath, FileMode.Create)))
                {
                    writer.WriteNullTerminatedString("MopMagicFbmodCac"); //Magic
                    writer.Write(cacheVersion);
                    writer.WriteNullTerminatedString(checksum);
                    writer.Write(ParsedBundles.Count);
                    writer.Write(ParsedEbx.Count);
                    writer.Write(ParsedRes.Count);
                    writer.Write(ParsedChunks.Count);
                    foreach (ParsedCustomBundle parsedBundle in ParsedBundles)
                        parsedBundle.WriteToCache(this, writer);
                    foreach (ParsedModifiedEbx parsedEbx in ParsedEbx)
                        parsedEbx.WriteToCache(writer);
                    //foreach (ParsedModifiedRes parsedRes in ParsedRes)
                    //    parsedRes.WriteToCache(writer);
                    //foreach (ParsedModifiedChunks parsedChk in ParsedChunks)
                    //    parsedChk.WriteToCache(writer);
                }
            }
        }
    }
}
