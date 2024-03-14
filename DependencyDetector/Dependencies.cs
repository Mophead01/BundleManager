using Frosty.Core;
using Frosty.Core.Controls;
using FrostySdk.Attributes;
using FrostySdk.Ebx;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace AutoBundleManager.DependencyDetector
{
    public class Dependencies
    {
        public EbxAssetEntry parEntry;
        public EbxAsset parAsset;
        private const BindingFlags PropertyBindingFlags = BindingFlags.Public | BindingFlags.Instance;
        public HashSet<string> refNames = new HashSet<string>();
        public HashSet<Guid> ebxPointerGuids = new HashSet<Guid>();
        public HashSet<ulong> resRids = new HashSet<ulong>();
        public HashSet<Guid> chunkGuids = new HashSet<Guid>();
        public Dependencies(EbxAssetEntry parEntry, EbxAsset parAsset)
        {
            this.parEntry = parEntry;
            this.parAsset = parAsset;
            foreach (object obj in parAsset.Objects)
                ExtractClass(obj);
        }

        void ExtractClass(object obj)
        {
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
                    case Type t2 when t2 == typeof(byte):
                        string refName = Value.ToString();
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
                        if (App.AssetManager.GetEbxEntry(refName) != null || refName.Contains("/"))
                            refNames.Add(refName);
                        break;
                    case Type t2 when t2 == typeof(BoxedValueRef):
                    case Type t3 when t3.Namespace != "FrostySdk.Ebx":
                        return;
                    default:
                        ExtractClass(Value);
                        break;
                }
                //if (FieldType.Namespace == "FrostySdk.Ebx" && FieldType.BaseType != typeof(Enum))
                //{
                //    if (FieldType == typeof(CString)) App.Logger.Log(Value.ToString());
                //    else if (FieldType == typeof(ResourceRef)) App.Logger.Log(Value.ToString());
                //    else if (FieldType == typeof(FileRef)) App.Logger.Log(Value.ToString());
                //    else if (FieldType == typeof(TypeRef)) App.Logger.Log(Value.ToString());
                //    else if (FieldType == typeof(BoxedValueRef)) App.Logger.Log(Value.ToString());
                //    else if (FieldType == typeof(PointerRef))
                //    {
                //        PointerRef Reference = (PointerRef)Value;
                //        if (Reference.Type == PointerRefType.External)
                //        {
                //            EbxAssetEntry entry = App.AssetManager.GetEbxEntry(Reference.External.FileGuid);
                //            if (entry != null)
                //                App.Logger.Log(entry.Name);
                //            else
                //                App.Logger.Log("BAD REF");
                //        }
                //    }
                //    else
                //        ExtractClass(Value);
                //}
            }
        }
    }
}
