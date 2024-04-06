using Frosty.Core;
using Frosty.Core.Windows;
using Frosty.Hash;
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
    public class AbmTextureShaderParameter
    {
        public PointerRef Value;
        public CString ParameterName;

        public AbmTextureShaderParameter(NativeReader reader, Boolean Test)
        {
            ParameterName = reader.ReadNullTerminatedString();
            Value = new PointerRef(new EbxImportReference() { FileGuid = reader.ReadGuid(), ClassGuid = reader.ReadGuid() });
        }

        public AbmTextureShaderParameter(dynamic texparam)
        {
            Value = texparam.Value;
            ParameterName = texparam.ParameterName;
        }

        public dynamic WriteToGameTexParam()
        {
            dynamic mvdbTexParam = TypeLibrary.CreateObject("TextureShaderParameter");
            mvdbTexParam.Value = Value;
            mvdbTexParam.ParameterName = ParameterName;
            return mvdbTexParam;
        }
    }

    public class AbmMeshVariationDatabaseMaterial
    {
        public PointerRef Material;
        public PointerRef MaterialVariation;
        public Int64 MaterialId;
        public Guid SurfaceShaderGuid;
        public UInt32 SurfaceShaderId;
        public List<AbmTextureShaderParameter> TextureParameters = new List<AbmTextureShaderParameter>();
        public AbmMeshVariationDatabaseMaterial(NativeReader reader, PointerRef mesh, UInt32 var)
        {
            Material = new PointerRef(new EbxImportReference() { FileGuid = mesh.External.FileGuid, ClassGuid = reader.ReadGuid() });
            if (var != 0)
                MaterialVariation = new PointerRef(new EbxImportReference() { FileGuid = reader.ReadGuid(), ClassGuid = reader.ReadGuid() });
            SurfaceShaderId = reader.ReadUInt();
            SurfaceShaderGuid = reader.ReadGuid();
            MaterialId = reader.ReadLong();
            int texParamCount = reader.ReadInt();
            for (int i = 0; i < texParamCount; i++)
                TextureParameters.Add(new AbmTextureShaderParameter(reader, true));
        }

        public AbmMeshVariationDatabaseMaterial(dynamic material)
        {
            Material = material.Material;
            MaterialVariation = material.MaterialVariation;
            if (ProfilesLibrary.IsLoaded(ProfileVersion.StarWarsBattlefrontII))
            {
                MaterialId = material.MaterialId;
                SurfaceShaderGuid = material.SurfaceShaderGuid;
                SurfaceShaderId = material.SurfaceShaderId;
            }
            foreach (dynamic texparam in material.TextureParameters)
                TextureParameters.Add(new AbmTextureShaderParameter(texparam));
        }

        public AbmMeshVariationDatabaseMaterial(EbxAssetEntry parEntry, dynamic material)
        {
            Material = new PointerRef(new EbxImportReference() { FileGuid = parEntry.Guid, ClassGuid = material.__InstanceGuid.ExportedGuid });
            dynamic shader = material.Shader;
            if (shader.Shader.Type == PointerRefType.External)
            {
                SurfaceShaderGuid = shader.Shader.External.FileGuid;
                EbxAssetEntry shaderEntry = App.AssetManager.GetEbxEntry(shader.Shader.External.FileGuid);
                if (shaderEntry != null)
                    SurfaceShaderId = (uint)Utils.HashString(shaderEntry.Name.ToLower());
            }
            foreach (dynamic texparam in shader.TextureParameters)
                TextureParameters.Add(new AbmTextureShaderParameter(texparam));
        }

        public AbmMeshVariationDatabaseMaterial(EbxAssetEntry parEntry, dynamic material, EbxAssetEntry varEntry, dynamic variationMaterial)
        {
            Material = new PointerRef(new EbxImportReference() { FileGuid = parEntry.Guid, ClassGuid = material.__InstanceGuid.ExportedGuid });
            MaterialVariation = new PointerRef(new EbxImportReference() { FileGuid = varEntry.Guid, ClassGuid = variationMaterial.__InstanceGuid.ExportedGuid });

            dynamic shader = variationMaterial.Shader.Shader.Type == PointerRefType.External ? variationMaterial.Shader : material.Shader;
            if (shader.Shader.Type == PointerRefType.External)
            {
                SurfaceShaderGuid = shader.Shader.External.FileGuid;

                EbxAssetEntry shaderEntry = App.AssetManager.GetEbxEntry(shader.Shader.External.FileGuid);
                if (shaderEntry != null)
                    SurfaceShaderId = (uint)Utils.HashString(shaderEntry.Name.ToLower());
            }
            foreach (dynamic texparam in variationMaterial.Shader.TextureParameters)
                TextureParameters.Add(new AbmTextureShaderParameter(texparam));
        }

        public dynamic WriteToGameMaterial()
        {
            dynamic mvdbMaterial = TypeLibrary.CreateObject("MeshVariationDatabaseMaterial");
            mvdbMaterial.Material = Material;
            mvdbMaterial.MaterialVariation = MaterialVariation;
            mvdbMaterial.MaterialId = MaterialId;
            mvdbMaterial.SurfaceShaderGuid = SurfaceShaderGuid;
            mvdbMaterial.SurfaceShaderId = SurfaceShaderId;
            foreach (AbmTextureShaderParameter BM_TexParam in TextureParameters)
                mvdbMaterial.TextureParameters.Add(BM_TexParam.WriteToGameTexParam());

            return mvdbMaterial;
        }
    }
    public class AbmMeshVariationDatabaseEntry
    {
        public PointerRef Mesh;
        public List<AbmMeshVariationDatabaseMaterial> Materials = new List<AbmMeshVariationDatabaseMaterial>();
        public uint VariationAssetNameHash;

        //Constructor from raw binary
        public AbmMeshVariationDatabaseEntry(NativeReader reader)
        {
            Mesh = new PointerRef(new EbxImportReference() {FileGuid = reader.ReadGuid(), ClassGuid = reader.ReadGuid() });
            VariationAssetNameHash = reader.ReadUInt();
            int matCount = reader.ReadInt();
            for (int i = 0; i < matCount; i++)
                Materials.Add(new AbmMeshVariationDatabaseMaterial(reader, Mesh, VariationAssetNameHash));
        }

        //Constructor from base game MeshVariationDatabaseEntry 
        public AbmMeshVariationDatabaseEntry(dynamic mvdbEntry)
        {

            Mesh = mvdbEntry.Mesh;
            VariationAssetNameHash = mvdbEntry.VariationAssetNameHash;
            foreach (dynamic material in mvdbEntry.Materials)
                Materials.Add(new AbmMeshVariationDatabaseMaterial(material));
        }

        //Constructor from modified mesh asset
        public AbmMeshVariationDatabaseEntry(EbxAssetEntry parEntry, EbxAsset parAsset, dynamic parRoot)
        {
            Mesh = new PointerRef(new EbxImportReference() { FileGuid = parEntry.Guid, ClassGuid = parAsset.RootInstanceGuid });
            if (TypeLibrary.IsSubClassOf(parRoot.GetType(), "MeshAsset"))
                VariationAssetNameHash = 0;

            foreach (dynamic pr in parRoot.Materials)
            {
                if (pr.Type == PointerRefType.Internal)
                    Materials.Add(new AbmMeshVariationDatabaseMaterial(parEntry, pr.Internal));
            }

        }

        //Constructor from modified mesh and variation asset
        public AbmMeshVariationDatabaseEntry(EbxAssetEntry meshEntry, EbxAsset meshAsset, dynamic meshRoot, EbxAssetEntry varEntry, dynamic varRoot, Dictionary<Guid, dynamic> meshSectionToVariationSection)
        {
            Mesh = new PointerRef(new EbxImportReference() { FileGuid = meshEntry.Guid, ClassGuid = meshAsset.RootInstanceGuid });
            VariationAssetNameHash = (uint)Utils.HashString(varEntry.Name.ToLower());

            foreach (dynamic pr in meshRoot.Materials)
            {
                if (pr.Type == PointerRefType.Internal)
                {
                    Materials.Add(new AbmMeshVariationDatabaseMaterial(meshEntry, pr.Internal, varEntry, meshSectionToVariationSection[pr.Internal.__InstanceGuid.ExportedGuid]));
                }
            }
        }

        public dynamic WriteToGameEntry()
        {
            dynamic mvdbObject = TypeLibrary.CreateObject("MeshVariationDatabaseEntry");
            mvdbObject.Mesh = Mesh;
            mvdbObject.VariationAssetNameHash = VariationAssetNameHash;
            foreach (AbmMeshVariationDatabaseMaterial BM_Material in Materials)
                mvdbObject.Materials.Add(BM_Material.WriteToGameMaterial());
            return mvdbObject;
        }

        public bool CheckMeshNeedsUpdating(EbxAsset parAsset, dynamic parRoot)
        {

            if (Materials.Count != parRoot.Materials.Count)
                return true;

            foreach (AbmMeshVariationDatabaseMaterial material in Materials)
            {
                dynamic matObj = parAsset.GetObject(material.Material.External.ClassGuid);
                if (matObj == null)
                    return true;

                if (matObj.Shader.Shader.External.FileGuid != material.SurfaceShaderGuid)
                    return true;

                if (material.TextureParameters.Count != matObj.Shader.TextureParameters.Count)
                    return true;

                for (int i = 0; i < material.TextureParameters.Count; i++)
                {
                    if ((material.TextureParameters[i].ParameterName != matObj.Shader.TextureParameters[i].ParameterName)
                        || (material.TextureParameters[i].Value.External.FileGuid != matObj.Shader.TextureParameters[i].Value.External.FileGuid))
                        return true;
                }
            }
            return false;
        }
        public static class AbmMeshVariationDatabasePrecache
        {
            //public static Dictionary<Guid, List<Guid>> NetworkRegistryReferences = new Dictionary<Guid, List<Guid>>();
            //public static HashSet<string> NetworkRegistryTypes = new HashSet<string>();

            //static AbmMeshVariationDatabasePrecache()
            //{
            //    using (NativeReader reader = new NativeReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("AutoBundleManager.Data.Swbf2_NetworkRegistryObjects.cache")))
            //    {
            //        NetworkRegistryTypes = reader.ReadHashSetStrings();
            //        int dictLength = reader.ReadInt();
            //        for (int i = 0; i < dictLength; i++)
            //            NetworkRegistryReferences.Add(reader.ReadGuid(), reader.ReadListGuids());
            //    }
            //}

            public static void WriteToCache(FrostyTaskWindow task)
            {

            }
        }
        public void WriteBinary(NativeWriter writer)
        {
            writer.Write(Mesh.External.FileGuid);
            writer.Write(Mesh.External.ClassGuid);
            writer.Write(VariationAssetNameHash);
            writer.Write(Materials.Count);
            foreach (AbmMeshVariationDatabaseMaterial mvMat in Materials)
            {
                writer.Write(mvMat.Material.External.ClassGuid);
                if (VariationAssetNameHash != 0)
                {
                    writer.Write(mvMat.MaterialVariation.External.FileGuid);
                    writer.Write(mvMat.MaterialVariation.External.ClassGuid);
                }
                writer.Write(mvMat.SurfaceShaderId);
                writer.Write(mvMat.SurfaceShaderGuid);
                writer.Write(mvMat.MaterialId);
                writer.Write(mvMat.TextureParameters.Count);
                foreach (AbmTextureShaderParameter texParam in mvMat.TextureParameters)
                {
                    writer.WriteNullTerminatedString(texParam.ParameterName);
                    writer.Write(texParam.Value.External.FileGuid);
                    writer.Write(texParam.Value.External.ClassGuid);
                }
            }
        }
    }

    public static class AbmMeshVariationDatabasePrecache
    {
        public static Dictionary<Guid, AbmMeshVariationDatabaseEntry> MeshMvdbDatabase;
        public static Dictionary<string, AbmMeshVariationDatabaseEntry> VariationMvdbDatabase;

        static AbmMeshVariationDatabasePrecache()
        {
            MeshMvdbDatabase = new Dictionary<Guid, AbmMeshVariationDatabaseEntry>();
            VariationMvdbDatabase = new Dictionary<string, AbmMeshVariationDatabaseEntry>();
            using (NativeReader reader = new NativeReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("AutoBundleManager.Data.Swbf2_MeshVariationDatabasePrecache.cache")))
            {
                int meshMvdbEntryCount = reader.ReadInt();
                int varMvdbEntryCount = reader.ReadInt();
                for (int i = 0; i < meshMvdbEntryCount; i++)
                {
                    AbmMeshVariationDatabaseEntry mvEntry = new AbmMeshVariationDatabaseEntry(reader);
                    MeshMvdbDatabase.Add(mvEntry.Mesh.External.FileGuid, mvEntry);
                }
                for (int i = 0; i < varMvdbEntryCount; i++)
                {
                    string blocksName = reader.ReadNullTerminatedString();
                    AbmMeshVariationDatabaseEntry mvEntry = new AbmMeshVariationDatabaseEntry(reader);
                    VariationMvdbDatabase.Add(blocksName, mvEntry);
                }
            }
        }
        public static void WriteToCache(FrostyTaskWindow task)
        {
            FileInfo fi = new FileInfo($"{App.FileSystem.CacheName}/AutoBundleManager/Swbf2_MeshVariationDatabasePrecache.cache");
            if (!Directory.Exists(fi.DirectoryName))
                Directory.CreateDirectory(fi.DirectoryName);

            MeshMvdbDatabase = new Dictionary<Guid, AbmMeshVariationDatabaseEntry>();
            VariationMvdbDatabase = new Dictionary<string, AbmMeshVariationDatabaseEntry>();
            dynamic forLock = new object();
            Dictionary<uint, EbxAssetEntry> hashesToObjectVariations = App.AssetManager.EnumerateEbx(type: "ObjectVariation").ToDictionary(refEntry => (uint)Utils.HashString(refEntry.Name, true), refEntry => refEntry);
            task.ParallelForeach("Caching MeshVariationDatabases", App.AssetManager.EnumerateEbx(type: "MeshVariationDatabase"), (parEntry, index) =>
            {
                dynamic parRoot = App.AssetManager.GetEbx(parEntry, true).RootObject;

                lock (forLock)
                {
                    foreach (dynamic mvdbEntry in parRoot.Entries)
                    {
                        Guid refGuid = mvdbEntry.Mesh.External.FileGuid;
                        uint varHash = mvdbEntry.VariationAssetNameHash;

                        if (varHash == 0)
                        {
                            if (!MeshMvdbDatabase.ContainsKey(refGuid))
                                MeshMvdbDatabase.Add(refGuid, new AbmMeshVariationDatabaseEntry(mvdbEntry));
                        }
                        else
                        {
                            EbxAssetEntry meshEntry = App.AssetManager.GetEbxEntry(refGuid);
                            EbxAssetEntry varEntry = hashesToObjectVariations[varHash];
                            string blocksName = $"{varEntry.Name}/{meshEntry.DisplayName}_{(uint)Utils.HashString(meshEntry.Name)}/shaderblocks_variation/blocks".ToLower();
                            if (!VariationMvdbDatabase.ContainsKey(blocksName))
                                VariationMvdbDatabase.Add(blocksName, new AbmMeshVariationDatabaseEntry(mvdbEntry));
                        }
                    }
                    if (parRoot.RedirectEntries.Count > 0)
                        App.Logger.Log($"{parEntry.Name}:\tRedirect Entries:\t{parRoot.RedirectEntries.Count}");
                }
            });

            using (NativeWriter writer = new NativeWriter(new FileStream(fi.FullName, FileMode.Create)))
            {
                writer.Write(MeshMvdbDatabase.Count());
                writer.Write(VariationMvdbDatabase.Count());
                foreach (KeyValuePair<Guid, AbmMeshVariationDatabaseEntry> pair in MeshMvdbDatabase.OrderBy(pair => pair.Key))
                    pair.Value.WriteBinary(writer);
                foreach (KeyValuePair<string, AbmMeshVariationDatabaseEntry> pair in VariationMvdbDatabase.OrderBy(pair => pair.Key))
                {
                    writer.WriteNullTerminatedString(pair.Key);
                    pair.Value.WriteBinary(writer);
                }
            }
        }
    }
}