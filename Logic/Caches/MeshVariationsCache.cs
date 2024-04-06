using Frosty.Core;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using MeshSetPlugin.Resources;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBundleManagerPlugin
{
    //To be made redundant
    public static class MeshVariationsCache
    {
        private static string cacheFileName = $"{App.FileSystem.CacheName}/AutoBundleManager/MeshVariationsCache.cache";
        private static int cacheVersion = 0;
        private static bool cacheNeedsUpdating = false;

        private static Dictionary<Sha1, AbmMeshVariationDatabaseEntry> cachedMeshVariationDbs = new Dictionary<Sha1, AbmMeshVariationDatabaseEntry>();

        //public static AbmMeshVariationDatabaseEntry GetMeshVariationEntry(EbxAssetEntry meshEntry)
        //{
        //    return GetMeshVariationEntry(meshEntry, null);
        //}

        //public static AbmMeshVariationDatabaseEntry GetMeshVariationEntry(EbxAssetEntry meshEntry, EbxAssetEntry varEntry)
        //{
        //    //Sha1 meshSha1 = meshEntry.GetSha1();
        //    //Sha1 varSha1 = varEntry == null ? Sha1.Zero : varEntry.GetSha1();

        //    ////Check if already cached
        //    //if (cachedMeshVariationDbs.ContainsKey(meshSha1))
        //    //    return cachedMeshVariationDbs[meshSha1];
        //    //if (varEntry != null)
        //    //{
        //    //    Sha1 meshUnmodifiedSha1 = meshEntry.Sha1;
        //    //    uint varHash = (uint)Utils.HashString(varEntry.Name, true);
        //    //    if (AbmMeshVariationDatabasePrecache.MeshMvdbDatabase.ContainsKey((meshEntry.Guid, varHash)))
        //    //        return AbmMeshVariationDatabasePrecache.MeshMvdbDatabase[(meshEntry.Guid, varHash)];
        //    //    else
        //    //        App.Logger.LogError($"You appear to have added a new Mesh Variation pair: {meshEntry.Name} - {varEntry.Name}.\nThis feature is NOT supported by the bundle manager and will cause issues. Please do not try to create custom mesh variation pairs.");
        //    //    return null;
        //    //}
        //    //else
        //    //{
        //    //    if (!meshEntry.HasModifiedData)
        //    //    {
        //    //        if (AbmMeshVariationDatabasePrecache.MeshMvdbDatabase.ContainsKey((meshEntry.Guid, 0)))
        //    //            return AbmMeshVariationDatabasePrecache.MeshMvdbDatabase[(meshEntry.Guid, 0)];
        //    //        else
        //    //            App.Logger.LogError($"{meshEntry.Name} is unmodified but not contained within the abm meshvari precache. Please investigate");
        //    //    }

        //    //    EbxAsset meshAsset = App.AssetManager.GetEbx(meshEntry);
        //    //    dynamic meshRoot = meshAsset.RootObject;
        //    //    AbmMeshVariationDatabaseEntry newMeshVariEntry = new AbmMeshVariationDatabaseEntry(meshEntry, meshAsset, meshRoot);
        //    //    cachedMeshVariationDbs.Add(meshEntry.GetSha1(), newMeshVariEntry);
        //    //    cacheNeedsUpdating = true;
        //    //    return cachedMeshVariationDbs[meshEntry.GetSha1()];

        //    //}
        //}
        //public static AbmMeshVariationDatabaseEntry GetMeshVariationEntry(EbxAssetEntry meshEntry, EbxAssetEntry varEntry)
        //{
        //    Sha1 meshSha1 = meshEntry.GetSha1();
        //    Sha1 varSha1 = varEntry == null ? Sha1.Zero : varEntry.GetSha1();

        //    //Check if already cached
        //    if (cachedMeshVariationDbs.ContainsKey((meshSha1, varSha1)))
        //        return cachedMeshVariationDbs[(meshSha1, varSha1)];

        //    //Check if contained within unmodified precache
        //    if (!meshEntry.HasModifiedData)
        //    {
        //        if (varEntry != null)
        //        {
        //            if (!varEntry.HasModifiedData)
        //            {
        //                uint varHash = (uint)Utils.HashString(varEntry.Name, true);
        //                if (AbmMeshVariationDatabasePrecache.MeshVariationDatabase.ContainsKey((meshEntry.Guid, varHash)))
        //                    return AbmMeshVariationDatabasePrecache.MeshVariationDatabase[(meshEntry.Guid, varHash)];
        //                else
        //                    App.Logger.LogError($"{meshEntry.Name} and {varEntry.Name} are unmodified but not contained within the abm meshvari precache. Please investigate");

        //            }
        //        }
        //        else
        //        {
        //            if (AbmMeshVariationDatabasePrecache.MeshVariationDatabase.ContainsKey((meshEntry.Guid, 0)))
        //                return AbmMeshVariationDatabasePrecache.MeshVariationDatabase[(meshEntry.Guid, 0)];
        //            else
        //                App.Logger.LogError($"{meshEntry.Name} is unmodified but not contained within the abm meshvari precache. Please investigate");
        //        }
        //    }

        //    //Finally cache if we absolutely have to
        //    if (varEntry.HasModifiedData)
        //    {
        //        Sha1 unmodifiedMeshSha1 = meshEntry.Sha1;
        //        Sha1 unmodifiedVarSha1 = meshEntry.Sha1;


        //        EbxAsset meshAsset = App.AssetManager.GetEbx(meshEntry);
        //        dynamic meshRoot = meshAsset.RootObject;

        //        EbxAsset varAsset = App.AssetManager.GetEbx(varEntry);
        //        dynamic varRoot = varAsset.RootObject;


        //        //MeshSet meshSet = App.AssetManager.GetResAs<MeshSet>(App.AssetManager.GetResEntry(meshRoot.MeshSetResource));

        //        //Dictionary<string, dynamic> meshMaterialsNameToMaterial = new Dictionary<string, dynamic>();
        //        //foreach (dynamic classObject in meshAsset.Objects)
        //        //{
        //        //    if (classObject.GetType().Name == "MeshMaterialVariation")
        //        //    {
        //        //        if (classObject.__Id == "MeshMaterialVariation" || meshMaterialsNameToMaterial.ContainsKey(classObject.__Id))
        //        //        {
        //        //            App.Logger.LogError($"{meshEntry.Name} section \"{classObject.__Id}\"\nYou need to rename your MeshMaterialVariations to match the mesh section names of the original mesh in the materials section and they should each be unique");
        //        //            return null;
        //        //        }
        //        //        meshMaterialsNameToMaterial.Add(classObject.__Id, classObject);
        //        //    }
        //        //}

        //        //Dictionary<Guid, dynamic> meshSectionToVariationSection = new Dictionary<Guid, dynamic>();
        //        //foreach (MeshSetLod lod in meshSet.Lods)
        //        //{
        //        //    foreach (MeshSetSection section in lod.Sections)
        //        //    {
        //        //        if (lod.IsSectionRenderable(section) && section.PrimitiveCount > 0)
        //        //        {
        //        //            dynamic material = meshRoot.Materials[section.MaterialId].Internal;
        //        //            if (!meshSectionToVariationSection.ContainsKey(material.__InstanceGuid.ExportedGuid))
        //        //            {
        //        //                if (!meshMaterialsNameToMaterial.ContainsKey(section.Name))
        //        //                {
        //        //                    App.Logger.LogError($"{varEntry.Name} missing mesh material variation \"{section.Name}\"\nYou need to rename your MeshMaterialVariations to match the mesh section names of the original mesh in the materials section and they should each be unique");
        //        //                    return null;
        //        //                }
        //        //                meshSectionToVariationSection.Add(material.__InstanceGuid.ExportedGuid, meshMaterialsNameToMaterial[section.Name]);
        //        //            }
        //        //        }
        //        //    }
        //        //}

        //        AbmMeshVariationDatabaseEntry newMeshVariEntry = new AbmMeshVariationDatabaseEntry(meshEntry, meshAsset, meshRoot, varEntry, varRoot, meshSectionToVariationSection);
        //        cachedMeshVariationDbs.Add((meshEntry.GetSha1(), varEntry.GetSha1()), newMeshVariEntry);
        //        cacheNeedsUpdating = true;
        //        return cachedMeshVariationDbs[(meshEntry.GetSha1(), varEntry.GetSha1())];
        //    }
        //    else
        //    {
        //        EbxAsset meshAsset = App.AssetManager.GetEbx(meshEntry);
        //        dynamic meshRoot = meshAsset.RootObject;
        //        AbmMeshVariationDatabaseEntry newMeshVariEntry = new AbmMeshVariationDatabaseEntry(meshEntry, meshAsset, meshRoot);
        //        cachedMeshVariationDbs.Add((meshEntry.GetSha1(), Sha1.Zero), newMeshVariEntry);
        //        cacheNeedsUpdating = true;
        //        return cachedMeshVariationDbs[(meshEntry.GetSha1(), Sha1.Zero)];
        //    }
        //}

        public static void UpdateCache()
        {
            if (!cacheNeedsUpdating)
                return;
            if (!Directory.Exists(Path.GetDirectoryName(cacheFileName)))
                Directory.CreateDirectory(Path.GetDirectoryName(cacheFileName));
            using (NativeWriter writer = new NativeWriter(new FileStream(cacheFileName, FileMode.Create)))
            {
                writer.WriteNullTerminatedString("MopMagicMeshVari"); //Magic
                writer.Write(cacheVersion);
                writer.Write(cachedMeshVariationDbs.Count);
                foreach (KeyValuePair<Sha1, AbmMeshVariationDatabaseEntry> pair in cachedMeshVariationDbs)
                {
                    writer.Write(pair.Key);
                    pair.Value.WriteBinary(writer);
                }
            }
            cacheNeedsUpdating = false;
        }

        static MeshVariationsCache()
        {
            //Read Cache
            if (!File.Exists(cacheFileName))
                return;
            using (NativeReader reader = new NativeReader(new FileStream(cacheFileName, FileMode.Open, FileAccess.Read)))
            {
                if (reader.ReadNullTerminatedString() != "MopMagicMeshVari" || reader.ReadInt() != cacheVersion)
                    return;
                int meshVariCount = reader.ReadInt();
                for (int i = 0; i < meshVariCount; i++)
                    cachedMeshVariationDbs.Add(reader.ReadSha1(), new AbmMeshVariationDatabaseEntry(reader));
            }

        }
    }
}
