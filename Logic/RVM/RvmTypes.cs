using AtlasTexturePlugin;
using Frosty.Core;
using Frosty.Core.Controls;
using Frosty.Hash;
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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using static FrostySdk.GeometryDeclarationDesc;

namespace AutoBundleManagerPlugin.Rvm
{
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmReference
    {
        [IsHidden]
        public ulong referenceHash { get; set; }

        [EbxFieldMeta(EbxFieldType.Array)]
        public List<RvmData> ReferenceArray { get; set; }
        public RvmReference(NativeReader reader)
        {
            referenceHash = reader.ReadULong(); // & 0x000000ffffffffff;
            ReferenceArray = new List<RvmData>();
        }
    }
    [EbxClassMeta(EbxFieldType.Struct)]
    abstract public class RvmData
    {
        [IsHidden]
        public ulong cityHash { get; set; }
        [IsHidden]
        public int expectedSize { get; set; }
        [IsHidden]
        public byte[] originalBytes { get; set; }

        public static Dictionary<uint, string> textureHashToNames = App.AssetManager.EnumerateEbx().Where(ebxEntry => TypeLibrary.IsSubClassOf(ebxEntry.Type, "TextureBaseAsset")).ToDictionary(ebxEntry => (uint)Utils.HashString(ebxEntry.Name, true), ebxEntry => ebxEntry.Name);
        public static Dictionary<uint, string> shaderHashToNames = App.AssetManager.EnumerateEbx().Where(ebxEntry => TypeLibrary.IsSubClassOf(ebxEntry.Type, "SurfaceShaderBaseAsset")).ToDictionary(ebxEntry => (uint)Utils.HashString(ebxEntry.Name, true), ebxEntry => ebxEntry.Name);
        public abstract void ReadStruct(NativeReader reader);
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11SerializedBlendState : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }
        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x80); //Might be 0x30
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class Vec : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Float32)]
        public float X { get; set; }
        [EbxFieldMeta(EbxFieldType.Float32)]
        public float Y { get; set; }
        [EbxFieldMeta(EbxFieldType.Float32)]
        public float Z { get; set; }
        [EbxFieldMeta(EbxFieldType.Float32)]
        public float W { get; set; }
        public override void ReadStruct(NativeReader reader)
        {
            X = reader.ReadUInt();
            Y = reader.ReadUInt();
            Z = reader.ReadUInt();
            W = reader.ReadUInt();
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmLegacyLightMapInstance : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }
        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x20);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11VsShader : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        [IsHidden]
        public RvmReference _dx11ByteCodeElement { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx11_byte_code_element { get { return _dx11ByteCodeElement == null ? new List<RvmData>() : _dx11ByteCodeElement.ReferenceArray; } }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        [IsHidden]
        public RvmReference _dx11InputElement { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx11_input_element { get { return _dx11InputElement == null ? new List<RvmData>() : _dx11InputElement.ReferenceArray; } }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk3 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x20);
            _dx11ByteCodeElement = new RvmReference(reader);
            Unk2 = reader.ReadULong();
            _dx11InputElement = new RvmReference(reader);
            Unk3 = reader.ReadULong();
        }
    }



    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_SerializedParamDbKey : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Hash { get; set; }
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint TypeHash { get; set; }
        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort ElementCount { get; set; }
        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort LegacyHigh { get; set; }
        public override void ReadStruct(NativeReader reader)
        {
            Hash = reader.ReadULong();
            TypeHash = reader.ReadUInt();
            ElementCount = reader.ReadUShort();
            LegacyHigh = reader.ReadUShort();
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class ProjectionState : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Matrix { get; set; }
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Rest { get; set; }
        public override void ReadStruct(NativeReader reader)
        {
            Matrix = reader.ReadBytes(0x40);
            Rest = reader.ReadBytes(0x50);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class StencilState : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }
        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x10);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11Sampler : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }
        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x40);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmLegacyLightProbes : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }
        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x90);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class ViewState : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }
        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x140); // 4x4 matrix and some other stuff
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_DefaultValueRef : RvmData
    {
        [IsHidden]
        public RvmReference _defaultValue { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> DefaultValue { get { return _defaultValue == null ? new List<RvmData>() : _defaultValue.ReferenceArray; } }

        public override void ReadStruct(NativeReader reader)
        {
            _defaultValue = new RvmReference(reader);
        }
    }



    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_InstructionBatchRef : RvmData
    {
        [IsHidden]
        public RvmReference _instructionBatch { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> InstructionBatch { get { return _instructionBatch == null ? new List<RvmData>() : _instructionBatch.ReferenceArray; } }

        public override void ReadStruct(NativeReader reader)
        {
            _instructionBatch = new RvmReference(reader);
        }
    }



    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11ApplyParametersBlock : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }

        [IsHidden]
        public RvmReference _rvmSlotHandle { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RvmSlotHandle { get { return _rvmSlotHandle == null ? new List<RvmData>() : _rvmSlotHandle.ReferenceArray; } }

        public override void ReadStruct(NativeReader reader)
        {
            Unk1 = reader.ReadULong();
            _rvmSlotHandle = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RvmPermutation : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Guid)]
        public Guid Guid { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort Index1 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort Index2 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt8)]
        public byte Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt8)]
        public byte Unk2 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort Unk3 { get; set; }

        [IsHidden]
        public RvmReference _rvmFunctionInstanceRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RvmFunctionInstanceRef { get { return _rvmFunctionInstanceRef == null ? new List<RvmData>() : _rvmFunctionInstanceRef.ReferenceArray; } }

        [IsHidden]
        public RvmReference _rvmContextSortKeyInfo { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RvmContextSortKeyInfo { get { return _rvmContextSortKeyInfo == null ? new List<RvmData>() : _rvmContextSortKeyInfo.ReferenceArray; } }

        [IsHidden]
        public RvmReference _rvmFunctionInputTableIndices { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RvmFunctionInputTableIndices { get { return _rvmFunctionInputTableIndices == null ? new List<RvmData>() : _rvmFunctionInputTableIndices.ReferenceArray; } }

        [IsHidden]
        public RvmReference _rvmDispatch { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RvmDispatch { get { return _rvmDispatch == null ? new List<RvmData>() : _rvmDispatch.ReferenceArray; } }

        public override void ReadStruct(NativeReader reader)
        {
            Guid = reader.ReadGuid();
            Index1 = reader.ReadUShort();
            Index2 = reader.ReadUShort();
            Unk1 = reader.ReadByte();
            Unk2 = reader.ReadByte();
            Unk3 = reader.ReadUShort();
            _rvmFunctionInstanceRef = new RvmReference(reader);
            _rvmContextSortKeyInfo = new RvmReference(reader);
            _rvmFunctionInputTableIndices = new RvmReference(reader);
            _rvmDispatch = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class Int64 : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Int { get; set; }
        public override void ReadStruct(NativeReader reader)
        {
            Int = reader.ReadULong();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_InstructionBatch : RvmData
    {
        [IsHidden]
        public RvmReference _runtimeInstantiatedType { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RuntimeInstantiatedType { get { return _runtimeInstantiatedType == null ? new List<RvmData>() : _runtimeInstantiatedType.ReferenceArray; } }

        /// <summary>
        /// This can be a ref to any "InstructionData" or "InstructionBatch" type
        /// </summary>
        [IsHidden]
        public RvmReference _instructionRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> InstructionRef { get { return _instructionRef == null ? new List<RvmData>() : _instructionRef.ReferenceArray; } }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            _runtimeInstantiatedType = new RvmReference(reader);
            _instructionRef = new RvmReference(reader);
            Unk2 = reader.ReadULong();
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RvmFunction : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }

        [IsHidden]
        public RvmReference _instructionBatchRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> InstructionBatchRef { get { return _instructionBatchRef == null ? new List<RvmData>() : _instructionBatchRef.ReferenceArray; } }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk3 { get; set; }

        [IsHidden]
        public RvmReference _paramDbSerializedHashView { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> ParamDbSerializedHashView { get { return _paramDbSerializedHashView == null ? new List<RvmData>() : _paramDbSerializedHashView.ReferenceArray; } }

        [IsHidden]
        public RvmReference _paramDbSerializedFilterView { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> ParamDbSerializedFilterView { get { return _paramDbSerializedFilterView == null ? new List<RvmData>() : _paramDbSerializedFilterView.ReferenceArray; } }

        public override void ReadStruct(NativeReader reader)
        {
            Unk1 = reader.ReadULong();
            _instructionBatchRef = new RvmReference(reader);
            Unk2 = reader.ReadULong();
            Unk3 = reader.ReadULong();
            _paramDbSerializedHashView = new RvmReference(reader);
            _paramDbSerializedFilterView = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_InstanceTableAssemblyInstructionBatchData : RvmData
    {
        [IsHidden]
        public RvmReference _tableAssemblyData { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> TableAssemblyData { get { return _tableAssemblyData == null ? new List<RvmData>() : _tableAssemblyData.ReferenceArray; } }

        [IsHidden]
        public RvmReference _writeOp { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> WriteOp { get { return _writeOp == null ? new List<RvmData>() : _writeOp.ReferenceArray; } }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            _tableAssemblyData = new RvmReference(reader);
            _writeOp = new RvmReference(reader);
            Unk1 = reader.ReadULong();
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_SurfaceShader : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong SharedStreamableTextureRef { get; set; }

        [EbxFieldMeta(EbxFieldType.String)]
        public string Name { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint NameHash { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Padding { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk4 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Unk2 { get; set; }

        [IsHidden]
        public RvmReference _shaderStreamableTextureRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> ShaderStreamableTextureRef => _shaderStreamableTextureRef?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _shaderStreamableExternalTextureRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> ShaderStreamableExternalTextureRef => _shaderStreamableExternalTextureRef?.ReferenceArray ?? new List<RvmData>();

        /// <summary>
        /// Found in SBD next to Guid
        /// </summary>
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Unk3 { get; set; }

        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Padding2 { get; set; } = new byte[12];

        [EbxFieldMeta(EbxFieldType.Guid)]
        public Guid Guid { get; set; }


        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmSerializedDb_ns_RvmPermutationSet PermutationSet { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            SharedStreamableTextureRef = reader.ReadULong();
            NameHash = reader.ReadUInt();
            Padding = reader.ReadUInt();
            Unk4 = reader.ReadULong();
            Unk1 = reader.ReadUInt();
            Unk2 = reader.ReadUInt();
            _shaderStreamableTextureRef = new RvmReference(reader);
            _shaderStreamableExternalTextureRef = new RvmReference(reader);
            Unk3 = reader.ReadUInt();
            Padding2 = reader.ReadBytes(12);
            Guid = reader.ReadGuid();
            if (shaderHashToNames.ContainsKey(NameHash))
                Name = shaderHashToNames[NameHash];
            else
                Name = "Unknown Shader";
            PermutationSet = new RvmSerializedDb_ns_RvmPermutationSet();
            //else
            //{
            //    Dictionary<uint, string> temp = new Dictionary<uint, string>();

            //    foreach (var ebxEntry in App.AssetManager.EnumerateEbx())
            //    {
            //        uint hash = (uint)Utils.HashString(ebxEntry.Name, true);

            //        // Check if the key already exists before adding
            //        if (!temp.ContainsKey(hash))
            //        {
            //            temp.Add(hash, ebxEntry.Name);
            //        }
            //    }
            //    if (temp.ContainsKey(NameHash))
            //    {
            //        App.Logger.LogError($"Could not find SurfaceShader NameHash:\t{temp[NameHash]}");
            //    }
            //    else
            //    {
            //        App.Logger.LogError($"Could not find SurfaceShader NameHash:\t{NameHash}");
            //    }
            //    Name = "Undiscovered";
            //}
        }
    }

    public class RvmSerializedDb_ns_SurfaceShaderPrIdExtension : PrIdExtension
    {
        public override string GetOverrideString(dynamic assetData)
        {
            return $"{assetData.Name}";
        }
    }



    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11LegacyVertexBufferConversionInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        [IsHidden]
        public RvmReference _preparedVertexStreamRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> PreparedVertexStream => _preparedVertexStreamRef?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk3 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk4 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Unk1 = reader.ReadULong();
            Unk2 = reader.ReadULong();
            _preparedVertexStreamRef = new RvmReference(reader);
            Unk3 = reader.ReadULong();
            Unk4 = reader.ReadULong();
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_SerializedParameterBlock : RvmData
    {
        [IsHidden]
        public RvmReference _paramDbKeyRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> ParamDbKeyRef => _paramDbKeyRef?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _defaultValueRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> DefaultValueRef => _defaultValueRef?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            _paramDbKeyRef = new RvmReference(reader);
            _defaultValueRef = new RvmReference(reader);
            Unk1 = reader.ReadULong();
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RvmFunctionInputTableIndices : RvmData
    {
        [IsHidden]
        public RvmReference _uint16 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Uint16 => _uint16?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _uint16 = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11PsShader : RvmData
    {
        [IsHidden]
        public RvmReference _unk1 { get; set; }

        [IsHidden]
        public RvmReference _dx11ByteCodeElement { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Unk1 => _unk1?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx11ByteCodeElement => _dx11ByteCodeElement?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            _unk1 = new RvmReference(reader);
            _dx11ByteCodeElement = new RvmReference(reader);
            Unk2 = reader.ReadULong();
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11HsShader : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }

        [IsHidden]
        public RvmReference _hash { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Hash => _hash?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Unk1 = reader.ReadULong();
            _hash = new RvmReference(reader);
            Unk2 = reader.ReadULong();
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_SerializedParameterBlockRef : RvmData
    {
        [IsHidden]
        public RvmReference _serializedParameterBlock { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Hash => _serializedParameterBlock?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _serializedParameterBlock = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11BlendStateData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Array)]
        public List<RvmReference> Dx11SerializedBlendStates { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Dx11SerializedBlendStates = new List<RvmReference>();
            for (int i = 0; i < 512; i++)
                Dx11SerializedBlendStates.Add(new RvmReference(reader));
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_DirectInputInstructionData : RvmData
    {
        [IsHidden]
        public RvmReference _paramDbSerializedReadView { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> ParamDbSerializedReadView => _paramDbSerializedReadView?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            _paramDbSerializedReadView = new RvmReference(reader);
            Unk1 = reader.ReadULong();
            Unk2 = reader.ReadULong();
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11TextureConversionInstructionData : RvmData
    {
        [IsHidden]
        public RvmReference _rvmSlotHandle1 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RvmSlotHandle1 => _rvmSlotHandle1?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _rvmSlotHandle2 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RvmSlotHandle2 => _rvmSlotHandle2?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _rvmSlotHandle1 = new RvmReference(reader);
            _rvmSlotHandle2 = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11DsShader : RvmData
    {
        [IsHidden]
        public RvmReference _unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Unk1 => _unk1?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _dx11ByteCodeElement { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx11ByteCodeElement => _dx11ByteCodeElement?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            _unk1 = new RvmReference(reader);
            _dx11ByteCodeElement = new RvmReference(reader);
            Unk2 = reader.ReadULong();
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ValueRef : RvmData
    {
        [IsHidden]
        public RvmReference _hash { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Hash => _hash?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            _hash = new RvmReference(reader);
            Unk1 = reader.ReadULong();
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RvmPermutationRef : RvmData
    {
        [IsHidden]
        public RvmReference _rvmPermutation { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RvmPermutation => _rvmPermutation?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _rvmPermutation = new RvmReference(reader);
        }
    }




    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RvmPermutationSet : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Guid)]
        public Guid Guid { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort Unk2 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort Unk3 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort PermutationCount { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort Unk4 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort Unk5 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint TempUnknown { get; set; }

        [IsHidden]
        public RvmReference _unk6 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Unk6 => _unk6?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _permutationRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> PermutationRef => _permutationRef?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _permutationLookupTable { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> PermutationLookupTable => _permutationLookupTable?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _paramDbHash { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> ParamDbHash => _paramDbHash?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _uint16 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Uint16 => _uint16?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _unk7 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Unk7 => _unk7?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _unk8 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Unk8 => _unk8?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _serializedParameterBlock { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> SerializedParameterBlock => _serializedParameterBlock?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _paramDbSerializedHashViewRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> ParamDbSerializedHashViewRef => _paramDbSerializedHashViewRef?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            Guid = reader.ReadGuid();
            Unk1 = reader.ReadUShort();
            Unk2 = reader.ReadUShort();
            Unk3 = reader.ReadUShort();
            PermutationCount = reader.ReadUShort();
            Unk4 = reader.ReadUShort();
            Unk5 = reader.ReadUShort();
            TempUnknown = reader.ReadUInt();
            _unk6 = new RvmReference(reader);
            _permutationRef = new RvmReference(reader);
            _permutationLookupTable = new RvmReference(reader);
            _paramDbHash = new RvmReference(reader);
            _uint16 = new RvmReference(reader);
            _unk7 = new RvmReference(reader);
            _unk8 = new RvmReference(reader);
            _serializedParameterBlock = new RvmReference(reader);
            _paramDbSerializedHashViewRef = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ShaderStreamableTextureRef : RvmData
    {
        [IsHidden]
        public RvmReference _shaderStreamableTexture { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> ShaderStreamableTexture => _shaderStreamableTexture?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _shaderStreamableTexture = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ParamDbSerializedHashView : RvmData
    {
        [IsHidden]
        public RvmReference _paramDbKeyRef { get; set; }

        [IsHidden]
        public RvmReference _paramDbKeyRef2 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> ParamDbKeyRef => _paramDbKeyRef?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> ParamDbKeyRef2 => _paramDbKeyRef2?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk3 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            _paramDbKeyRef = new RvmReference(reader);
            _paramDbKeyRef2 = new RvmReference(reader);
            Unk3 = reader.ReadULong();
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RvmDispatch : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }

        [IsHidden]
        public RvmReference _instructionBatchRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> InstructionBatchRef => _instructionBatchRef?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk3 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Unk1 = reader.ReadULong();
            _instructionBatchRef = new RvmReference(reader);
            Unk2 = reader.ReadULong();
            Unk3 = reader.ReadULong();
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RvmFunctionInstance : RvmData
    {
        [IsHidden]
        public RvmReference _unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Unk1 => _unk1?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _rvmFunction { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RvmFunction => _rvmFunction?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _combinedSerializedParameterBlock { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> CombinedSerializedParameterBlock => _combinedSerializedParameterBlock?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _unk1 = new RvmReference(reader);
            _rvmFunction = new RvmReference(reader);
            _combinedSerializedParameterBlock = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ParamDbSerializedHashViewRef : RvmData
    {
        [IsHidden]
        public RvmReference _paramDbSerializedHashView { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> ParamDbSerializedHashView => _paramDbSerializedHashView?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _paramDbSerializedHashView = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ParamDbSerializedFilterView : RvmData
    {
        [IsHidden]
        public RvmReference _paramDbKeyRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> ParamDbKeyRef => _paramDbKeyRef?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            _paramDbKeyRef = new RvmReference(reader);
            Unk2 = reader.ReadULong();
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RuntimeInstantiatedType : RvmData
    {
        [IsHidden]
        public RvmReference _rttiType { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RttiType => _rttiType?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _rttiType = new RvmReference(reader);
        }
    }


    public enum DXGI_FORMAT : uint
    {
        DXGI_FORMAT_UNKNOWN = 0,
        DXGI_FORMAT_R32G32B32A32_TYPELESS = 1,
        DXGI_FORMAT_R32G32B32A32_FLOAT = 2,
        DXGI_FORMAT_R32G32B32A32_UINT = 3,
        DXGI_FORMAT_R32G32B32A32_SINT = 4,
        DXGI_FORMAT_R32G32B32_TYPELESS = 5,
        DXGI_FORMAT_R32G32B32_FLOAT = 6,
        DXGI_FORMAT_R32G32B32_UINT = 7,
        DXGI_FORMAT_R32G32B32_SINT = 8,
        DXGI_FORMAT_R16G16B16A16_TYPELESS = 9,
        DXGI_FORMAT_R16G16B16A16_FLOAT = 10,
        DXGI_FORMAT_R16G16B16A16_UNORM = 11,
        DXGI_FORMAT_R16G16B16A16_UINT = 12,
        DXGI_FORMAT_R16G16B16A16_SNORM = 13,
        DXGI_FORMAT_R16G16B16A16_SINT = 14,
        DXGI_FORMAT_R32G32_TYPELESS = 15,
        DXGI_FORMAT_R32G32_FLOAT = 16,
        DXGI_FORMAT_R32G32_UINT = 17,
        DXGI_FORMAT_R32G32_SINT = 18,
        DXGI_FORMAT_R32G8X24_TYPELESS = 19,
        DXGI_FORMAT_D32_FLOAT_S8X24_UINT = 20,
        DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS = 21,
        DXGI_FORMAT_X32_TYPELESS_G8X24_UINT = 22,
        DXGI_FORMAT_R10G10B10A2_TYPELESS = 23,
        DXGI_FORMAT_R10G10B10A2_UNORM = 24,
        DXGI_FORMAT_R10G10B10A2_UINT = 25,
        DXGI_FORMAT_R11G11B10_FLOAT = 26,
        DXGI_FORMAT_R8G8B8A8_TYPELESS = 27,
        DXGI_FORMAT_R8G8B8A8_UNORM = 28,
        DXGI_FORMAT_R8G8B8A8_UNORM_SRGB = 29,
        DXGI_FORMAT_R8G8B8A8_UINT = 30,
        DXGI_FORMAT_R8G8B8A8_SNORM = 31,
        DXGI_FORMAT_R8G8B8A8_SINT = 32,
        DXGI_FORMAT_R16G16_TYPELESS = 33,
        DXGI_FORMAT_R16G16_FLOAT = 34,
        DXGI_FORMAT_R16G16_UNORM = 35,
        DXGI_FORMAT_R16G16_UINT = 36,
        DXGI_FORMAT_R16G16_SNORM = 37,
        DXGI_FORMAT_R16G16_SINT = 38,
        DXGI_FORMAT_R32_TYPELESS = 39,
        DXGI_FORMAT_D32_FLOAT = 40,
        DXGI_FORMAT_R32_FLOAT = 41,
        DXGI_FORMAT_R32_UINT = 42,
        DXGI_FORMAT_R32_SINT = 43,
        DXGI_FORMAT_R24G8_TYPELESS = 44,
        DXGI_FORMAT_D24_UNORM_S8_UINT = 45,
        DXGI_FORMAT_R24_UNORM_X8_TYPELESS = 46,
        DXGI_FORMAT_X24_TYPELESS_G8_UINT = 47,
        DXGI_FORMAT_R8G8_TYPELESS = 48,
        DXGI_FORMAT_R8G8_UNORM = 49,
        DXGI_FORMAT_R8G8_UINT = 50,
        DXGI_FORMAT_R8G8_SNORM = 51,
        DXGI_FORMAT_R8G8_SINT = 52,
        DXGI_FORMAT_R16_TYPELESS = 53,
        DXGI_FORMAT_R16_FLOAT = 54,
        DXGI_FORMAT_D16_UNORM = 55,
        DXGI_FORMAT_R16_UNORM = 56,
        DXGI_FORMAT_R16_UINT = 57,
        DXGI_FORMAT_R16_SNORM = 58,
        DXGI_FORMAT_R16_SINT = 59,
        DXGI_FORMAT_R8_TYPELESS = 60,
        DXGI_FORMAT_R8_UNORM = 61,
        DXGI_FORMAT_R8_UINT = 62,
        DXGI_FORMAT_R8_SNORM = 63,
        DXGI_FORMAT_R8_SINT = 64,
        DXGI_FORMAT_A8_UNORM = 65,
        DXGI_FORMAT_R1_UNORM = 66,
        DXGI_FORMAT_R9G9B9E5_SHAREDEXP = 67,
        DXGI_FORMAT_R8G8_B8G8_UNORM = 68,
        DXGI_FORMAT_G8R8_G8B8_UNORM = 69,
        DXGI_FORMAT_BC1_TYPELESS = 70,
        DXGI_FORMAT_BC1_UNORM = 71,
        DXGI_FORMAT_BC1_UNORM_SRGB = 72,
        DXGI_FORMAT_BC2_TYPELESS = 73,
        DXGI_FORMAT_BC2_UNORM = 74,
        DXGI_FORMAT_BC2_UNORM_SRGB = 75,
        DXGI_FORMAT_BC3_TYPELESS = 76,
        DXGI_FORMAT_BC3_UNORM = 77,
        DXGI_FORMAT_BC3_UNORM_SRGB = 78,
        DXGI_FORMAT_BC4_TYPELESS = 79,
        DXGI_FORMAT_BC4_UNORM = 80,
        DXGI_FORMAT_BC4_SNORM = 81,
        DXGI_FORMAT_BC5_TYPELESS = 82,
        DXGI_FORMAT_BC5_UNORM = 83,
        DXGI_FORMAT_BC5_SNORM = 84,
        DXGI_FORMAT_B5G6R5_UNORM = 85,
        DXGI_FORMAT_B5G5R5A1_UNORM = 86,
        DXGI_FORMAT_B8G8R8A8_UNORM = 87,
        DXGI_FORMAT_B8G8R8X8_UNORM = 88,
        DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM = 89,
        DXGI_FORMAT_B8G8R8A8_TYPELESS = 90,
        DXGI_FORMAT_B8G8R8A8_UNORM_SRGB = 91,
        DXGI_FORMAT_B8G8R8X8_TYPELESS = 92,
        DXGI_FORMAT_B8G8R8X8_UNORM_SRGB = 93,
        DXGI_FORMAT_BC6H_TYPELESS = 94,
        DXGI_FORMAT_BC6H_UF16 = 95,
        DXGI_FORMAT_BC6H_SF16 = 96,
        DXGI_FORMAT_BC7_TYPELESS = 97,
        DXGI_FORMAT_BC7_UNORM = 98,
        DXGI_FORMAT_BC7_UNORM_SRGB = 99,
        DXGI_FORMAT_AYUV = 100,
        DXGI_FORMAT_Y410 = 101,
        DXGI_FORMAT_Y416 = 102,
        DXGI_FORMAT_NV12 = 103,
        DXGI_FORMAT_P010 = 104,
        DXGI_FORMAT_P016 = 105,
        DXGI_FORMAT_420_OPAQUE = 106,
        DXGI_FORMAT_YUY2 = 107,
        DXGI_FORMAT_Y210 = 108,
        DXGI_FORMAT_Y216 = 109,
        DXGI_FORMAT_NV11 = 110,
        DXGI_FORMAT_AI44 = 111,
        DXGI_FORMAT_IA44 = 112,
        DXGI_FORMAT_P8 = 113,
        DXGI_FORMAT_A8P8 = 114,
        DXGI_FORMAT_B4G4R4A4_UNORM = 115,
        DXGI_FORMAT_P208 = 130,
        DXGI_FORMAT_V208 = 131,
        DXGI_FORMAT_V408 = 132,
        DXGI_FORMAT_SAMPLER_FEEDBACK_MIN_MIP_OPAQUE = 0x00000100,
        DXGI_FORMAT_SAMPLER_FEEDBACK_MIP_REGION_USED_OPAQUE = 0x00000101,
        DXGI_FORMAT_FORCE_UINT = 0xffffffff
    }

    public enum D3D11_INPUT_CLASSIFICATION : uint
    {
        D3D11_INPUT_PER_VERTEX_DATA = 0,
        D3D11_INPUT_PER_INSTANCE_DATA = 1
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11InputElement : RvmData
    {
        [IsHidden]
        public RvmReference _semanticName { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> SemanticName => _semanticName?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint SemanticIndex { get; set; }

        [EbxFieldMeta(EbxFieldType.Enum)]
        public DXGI_FORMAT Format { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint InputSlot { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint AlignedByteOffset { get; set; }

        [EbxFieldMeta(EbxFieldType.Enum)]
        public D3D11_INPUT_CLASSIFICATION InputSlotClass { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint InstanceDataStepRate { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            _semanticName = new RvmReference(reader);
            SemanticIndex = reader.ReadUInt();
            Format = (DXGI_FORMAT)reader.ReadUInt();
            InputSlot = reader.ReadUInt();
            AlignedByteOffset = reader.ReadUInt();
            InputSlotClass = (D3D11_INPUT_CLASSIFICATION)reader.ReadUInt();
            InstanceDataStepRate = reader.ReadUInt();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_CombinedSerializedParameterBlock : RvmData
    {
        [IsHidden]
        public RvmReference _serializedParameterBlockRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> SerializedParameterBlockRef => _serializedParameterBlockRef?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _probablyUnused { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> ProbablyUnused => _probablyUnused?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _serializedParameterBlockRef = new RvmReference(reader);
            _probablyUnused = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ParamDbSerializedReadView : RvmData
    {
        [IsHidden]
        public RvmReference _paramDbKeyRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> ParamDbKeyRef => _paramDbKeyRef?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            _paramDbKeyRef = new RvmReference(reader);
            Unk2 = reader.ReadULong();
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11ApplyParametersInstructionData : RvmData
    {
        [IsHidden]
        public RvmReference _dx11ApplyParametersBlock { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx11ApplyParametersBlock => _dx11ApplyParametersBlock?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _rvmSlotHandle { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RvmSlotHandle => _rvmSlotHandle?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _dx11ApplyParametersBlock = new RvmReference(reader);
            _rvmSlotHandle = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RvmFunctionInstanceRef : RvmData
    {
        [IsHidden]
        public RvmReference _rvmFunctionInstance { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RvmFunctionInstance => _rvmFunctionInstance?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _rvmFunctionInstance = new RvmReference(reader);
        }
    }



    [EbxClassMeta(EbxFieldType.Struct)]
    public class ParamDbHash : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Hash { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Hash = reader.ReadULong();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11BufferConversionInstructionData : RvmData
    {
        [IsHidden]
        public RvmReference _rvmSlotHandle1 { get; set; }

        [IsHidden]
        public RvmReference _rvmSlotHandle2 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RvmSlotHandle1 => _rvmSlotHandle1?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RvmSlotHandle2 => _rvmSlotHandle2?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _rvmSlotHandle1 = new RvmReference(reader);
            _rvmSlotHandle2 = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Settings : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Unk2 { get; set; }

        [IsHidden]
        public RvmReference _databaseNameRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> DataBaseName { get { if (_databaseNameRef == null) return new List<RvmData>(); else return _databaseNameRef.ReferenceArray; } }

        [IsHidden]
        public RvmReference _dataBaseShortName { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> DataBaseShortName { get { if (_dataBaseShortName == null) return new List<RvmData>(); else return _dataBaseShortName.ReferenceArray; } }

        public override void ReadStruct(NativeReader reader)
        {
            Unk1 = reader.ReadUInt();
            Unk2 = reader.ReadUInt();
            _databaseNameRef = new RvmReference(reader);
            _dataBaseShortName = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ShaderStreamableExternalTextureRef : RvmData
    {
        [IsHidden]
        public RvmReference _shaderStreamableExternalTexture { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> ShaderStreamableExternalTexture => _shaderStreamableExternalTexture?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _shaderStreamableExternalTexture = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11DispatchInstructionData : RvmData
    {
        [IsHidden]
        public RvmReference _rvmSlotHandle1 { get; set; }

        [IsHidden]
        public RvmReference _rvmSlotHandle2 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RvmSlotHandle1 => _rvmSlotHandle1?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RvmSlotHandle2 => _rvmSlotHandle2?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _rvmSlotHandle1 = new RvmReference(reader);
            _rvmSlotHandle2 = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ParamDbKeyRef : RvmData
    {
        [IsHidden]
        public RvmReference _serializedParamDbKey { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> SerializedParamDbKey => _serializedParamDbKey?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _serializedParamDbKey = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RttiType : RvmData
    {
        [IsHidden]
        public RvmReference _moduleName { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> ModuleName => _moduleName?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _name { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Name => _name?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _moduleName = new RvmReference(reader);
            _name = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_TableAssemblyInstructionBatchData : RvmData
    {
        [IsHidden]
        public RvmReference _tableAssemblyData { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> TableAssemblyData => _tableAssemblyData?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _writeOp { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> WriteOp => _writeOp?.ReferenceArray ?? new List<RvmData>();

        [IsHidden]
        public RvmReference _writeOpGroup { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> WriteOpGroup => _writeOpGroup?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _tableAssemblyData = new RvmReference(reader);
            _writeOp = new RvmReference(reader);
            _writeOpGroup = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RvmPermutationLookupTable : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort Unk2 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort Unk3 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort Unk4 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Unk1 = reader.ReadUShort();
            Unk2 = reader.ReadUShort();
            Unk3 = reader.ReadUShort();
            Unk4 = reader.ReadUShort();
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class Int32 : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Int32)]
        public int Int { get; set; }
        public override void ReadStruct(NativeReader reader)
        {
            Int = reader.ReadInt();
        }
    }
    public enum ShaderDepthBiasGroupEnum : uint
    {
        ShaderDepthBiasGroup_Default = 0,
        ShaderDepthBiasGroup_Decal = 1,
        ShaderDepthBiasGroup_EmitterOcclusion = 2,
        ShaderDepthBiasGroup_EdgeModel = 3,
        ShaderDepthBiasGroup_TerrainDecal = 4,
        ShaderDepthBiasGroup_TerrainDecalZPass = 5,
        ShaderDepthBiasGroup_DistantShadowCache_LowestBias = 6,
        ShaderDepthBiasGroup_DistantShadowCache_LowerBias = 7,
        ShaderDepthBiasGroup_DistantShadowCache_NormalBias = 8,
        ShaderDepthBiasGroup_DistantShadowCache_HigherBias = 9,
        ShaderDepthBiasGroup_DistantShadowCache_HighestBias = 10,
        ShaderDepthBiasGroup_Shadow16Bit = 11,
        ShaderDepthBiasGroup_Shadow24Bit = 12,
        ShaderDepthBiasGroup_Shadow32Bit = 13,
        ShaderDepthBiasGroup_ZPass = 14,
        ShaderDepthBiasGroup_Emissive = 15,
        ShaderDepthBiasGroup_VelocityVector = 16,
        ShaderDepthBiasGroupCount = 17
    }
    [EbxClassMeta(EbxFieldType.Struct)]
    public class ShaderDepthBiasGroup : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Enum)]
        public ShaderDepthBiasGroupEnum Enum { get; set; }
        public override void ReadStruct(NativeReader reader)
        {
            Enum = (ShaderDepthBiasGroupEnum)reader.ReadInt();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ShaderStreamableExternalTexture : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Float32)]
        public float TexCoordsPerMeter { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort TextureType { get; set; }

        [EbxFieldMeta(EbxFieldType.String)]
        public string Name { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint NameHash { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint ExternalHash { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            TexCoordsPerMeter = reader.ReadFloat();
            Unk1 = reader.ReadUShort();
            TextureType = reader.ReadUShort();
            NameHash = reader.ReadUInt();
            ExternalHash = reader.ReadUInt();

            if (textureHashToNames.ContainsKey(NameHash))
                Name = textureHashToNames[NameHash];
            else
                Name = "Unknown Texture";
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RenderDepthMode : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmLegacyOutdoorLightStatus : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmLevelOfDetail : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ShaderStreamableTexture : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Float32)]
        public float TexCoordsPerMeter { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort TextureType { get; set; }

        [EbxFieldMeta(EbxFieldType.String)]
        public string Name { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint NameHash { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint ExternalHash { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            TexCoordsPerMeter = reader.ReadFloat();
            Unk1 = reader.ReadUShort();
            TextureType = reader.ReadUShort();
            NameHash = reader.ReadUInt();
            ExternalHash = reader.ReadUInt(); /// Guessing this has something to do with external since the non-externals have 0xFFFF here

            if (textureHashToNames.ContainsKey(NameHash))
                Name = textureHashToNames[NameHash];
            else
                Name = "Unknown Texture";
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_InstanceTableAssemblyData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x0C);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class Float : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Float32)]
        public float Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = reader.ReadFloat();
        }
    }

    // RvmSerializedDb_ns_ShaderInstancingMethod
    [EbxClassMeta(EbxFieldType.Struct)]
    public class ShaderInstancingMethod : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    // RvmSerializedDb_ns_ShaderRenderMode
    [EbxClassMeta(EbxFieldType.Struct)]
    public class ShaderRenderMode : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    // RvmSerializedDb_ns_PrimitiveType
    [EbxClassMeta(EbxFieldType.Struct)]
    public class PrimitiveType : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    // RvmSerializedDb_ns_ShaderSkinningMethod
    [EbxClassMeta(EbxFieldType.Struct)]
    public class ShaderSkinningMethod : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    // RvmSerializedDb_ns_Uint32
    [EbxClassMeta(EbxFieldType.Struct)]
    public class Uint32 : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    // RvmSerializedDb_ns_ShaderGeometrySpace
    [EbxClassMeta(EbxFieldType.Struct)]
    public class ShaderGeometrySpace : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    // RvmSerializedDb_ns_TableAssemblyData
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_TableAssemblyData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x14);
        }
    }

    // RvmSerializedDb_ns_DepthBiasGroupData
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_DepthBiasGroupData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x88);
        }
    }


    // RvmSerializedDb_ns_IndexBufferFormat
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmIndexBufferFormat : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    // RvmSerializedDb_ns_QualityLevel
    [EbxClassMeta(EbxFieldType.Struct)]
    public class QualityLevel : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    // RvmSerializedDb_ns_PackLightMapWeightIntoInstanceInstructionData
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_PackLightMapWeightIntoInstanceInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x0C);
        }
    }

    // RvmSerializedDb_ns_Dx11ViewStateInstructionData
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11ViewStateInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x14);
        }
    }

    // RvmLegacyInstructions_ns_LegacyInstructionData
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmLegacyInstructions_ns_LegacyInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x14);
        }
    }

    // RvmSerializedDb_ns_WriteOp
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_WriteOp : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(8);
        }
    }

    // RvmSerializedDb_ns_CpuToGpuMatrixInstructionData
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_CpuToGpuMatrixInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(8);
        }
    }

    // RvmSerializedDb_ns_LodFadeInstructionData
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_LodFadeInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x14);
        }
    }

    // RvmSerializedDb_ns_Dx11ShaderDispatchDrawInstructionData
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11ShaderDispatchDrawInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x24);
        }
    }

    // RvmSerializedDb_ns_DefaultValueSimpleTexture
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_DefaultValueSimpleTexture : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(4);
        }
    }

    // RvmSerializedDb_ns_PreparedVertexStream
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_PreparedVertexStream : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x0C);
        }
    }

    // RvmSerializedDb_ns_RvmContextSortKeyInfo
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmContextSortKeyInfo : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Index { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Index = reader.ReadUInt();
        }
    }

    // RvmSerializedDb_ns_OffsetTranslationInMatrixInstructionData
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_OffsetTranslationInMatrixInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x0C);
        }
    }

    // RvmSerializedDb_ns_RvmSlotHandle
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSlotHandle : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Unk1 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Unk1 = reader.ReadUInt();
        }
    }

    // RvmSerializedDb_ns_VectorSubtractInstructionData
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_VectorSubtractInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x0C);
        }
    }

    // RvmSerializedDb_ns_TessellationParametersInstructionData
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_TessellationParametersInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x38);
        }
    }

    // RvmSerializedDb_ns_Uint16
    [EbxClassMeta(EbxFieldType.Struct)]
    public class uint16 : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt16)]
        public ushort Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = reader.ReadUShort();
        }
    }

    // RvmSerializedDb_ns_DefaultValueZeroMem
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_DefaultValueZeroMem : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(2);
        }
    }

    // RvmSerializedDb_ns_Dx11LegacyDrawStateBuilderData
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11LegacyDrawStateBuilderData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x54);
        }
    }

    // RvmSerializedDb_ns_DefaultValueSimpleBuffer
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_DefaultValueSimpleBuffer : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(2);
        }
    }

    // RvmSerializedDb_ns_SliceCountInstructionData
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_SliceCountInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Unk1 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Unk1 = reader.ReadUInt();
        }
    }

    // RvmSerializedDb_ns_Uint8
    [EbxClassMeta(EbxFieldType.Struct)]
    public class uint8 : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt8)]
        public byte Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = reader.ReadByte();
        }
    }

    // RvmSerializedDb_ns_DefaultValueStructLegacyData
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_DefaultValueStructLegacyData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt8)]
        public byte Unk1 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Unk1 = reader.ReadByte();
        }
    }

    // RvmSerializedDb_ns_Dx11ByteCodeElement
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11ByteCodeElement : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x100);
        }
    }

    // RvmSerializedDb_ns_WriteOpGroup
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_WriteOpGroup : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Unk1 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Unk1 = reader.ReadBytes(3);
        }
    }

    // RvmSerializedDb_ns_Char
    [EbxClassMeta(EbxFieldType.Struct)]
    public class Char : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt8)]
        public byte Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = reader.ReadByte();
        }
    }

    // RvmSerializedDb_ns_BaseShaderState
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_BaseShaderState : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(6);
        }
    }

    // RvmSerializedDb_ns_Boolean
    [EbxClassMeta(EbxFieldType.Struct)]
    public class Boolean : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Boolean)]
        public bool Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = reader.ReadBoolean();
        }
    }

    // RvmSerializedDb_ns_NvShadowMapRenderType
    [EbxClassMeta(EbxFieldType.Struct)]
    public class NvShadowMapRenderType : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class CharRvm : RvmData
    {
        [EbxFieldMeta(EbxFieldType.String)]
        public char Value { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Value = (char)reader.ReadByte();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12VertexBufferViewInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data1 { get; set; }

        [IsHidden]
        public RvmReference _preparedVertexStream { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> PreparedVertexStream => _preparedVertexStream?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data2 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data1 = reader.ReadBytes(0x10);
            _preparedVertexStream = new RvmReference(reader);
            Data2 = reader.ReadBytes(0x08);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12Shader : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data1 { get; set; }

        [IsHidden]
        public RvmReference _dx12BinaryBlob { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx12BinaryBlob => _dx12BinaryBlob?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _dx12BinaryBlob = new RvmReference(reader);
            Data1 = reader.ReadBytes(0x20);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12RootDescriptorTableAssemblyInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data1 { get; set; }

        [IsHidden]
        public RvmReference _dx12RootWriteOp { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx12RootWriteOp => _dx12RootWriteOp?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _dx12RootWriteOp = new RvmReference(reader);
            Data1 = reader.ReadBytes(0x10);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12PcSamplerTableWriterInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data1 { get; set; }

        [IsHidden]
        public RvmReference _dx12PcSamplerPointer { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx12PcSamplerPointer => _dx12PcSamplerPointer?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _dx12PcSamplerPointer = new RvmReference(reader);
            Data1 = reader.ReadBytes(0x10);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12PcSamplerPointer : RvmData
    {
        [IsHidden]
        public RvmReference _dx12PcSampler { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx12PcSampler => _dx12PcSampler?.ReferenceArray ?? new List<RvmData>();

        public override void ReadStruct(NativeReader reader)
        {
            _dx12PcSampler = new RvmReference(reader);
        }
    }


    public class RvmSerializedDb_ns_Dx12PcRvmDescriptorTableAssemblyInstructionData : RvmData
    {
        [IsHidden]
        public RvmReference _tableAssemblyData { get; set; }

        [IsHidden]
        public RvmReference _writeOp { get; set; }

        [IsHidden]
        public RvmReference _writeOpGroup { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> TableAssemblyData => _tableAssemblyData?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> WriteOp => _writeOp?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> WriteOpGroup => _writeOpGroup?.ReferenceArray ?? new List<RvmData>();

        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data1 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            _tableAssemblyData = new RvmReference(reader);
            _writeOp = new RvmReference(reader);
            _writeOpGroup = new RvmReference(reader);
            Data1 = reader.ReadBytes(0x08);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12PcPSOPreloadOp : RvmData
    {
        [IsHidden]
        public RvmReference _dx12PcRootSignature { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx12PcRootSignature => _dx12PcRootSignature == null ? new List<RvmData>() : _dx12PcRootSignature.ReferenceArray;

        [IsHidden]
        public RvmReference _dx12InputElement { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx12InputElement => _dx12InputElement == null ? new List<RvmData>() : _dx12InputElement.ReferenceArray;

        [IsHidden]
        public RvmReference _dx12Shader1 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx12Shader1 => _dx12Shader1 == null ? new List<RvmData>() : _dx12Shader1.ReferenceArray;

        [IsHidden]
        public RvmReference _dx12Shader2 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx12Shader2 => _dx12Shader2 == null ? new List<RvmData>() : _dx12Shader2.ReferenceArray;

        [IsHidden]
        public RvmReference _dx12Shader3 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx12Shader3 => _dx12Shader3 == null ? new List<RvmData>() : _dx12Shader3.ReferenceArray;

        [IsHidden]
        public RvmReference _dx12Shader4 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx12Shader4 => _dx12Shader4 == null ? new List<RvmData>() : _dx12Shader4.ReferenceArray;

        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data1 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            _dx12PcRootSignature = new RvmReference(reader);
            _dx12InputElement = new RvmReference(reader);
            _dx12Shader1 = new RvmReference(reader);
            _dx12Shader2 = new RvmReference(reader);
            _dx12Shader3 = new RvmReference(reader);
            _dx12Shader4 = new RvmReference(reader);
            Data1 = reader.ReadBytes(0x08);
        }
    }



    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12PcRootSignature : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference _dx12BinaryBlob { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx12BinaryBlob => _dx12BinaryBlob == null ? new List<RvmData>() : _dx12BinaryBlob.ReferenceArray;

        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data1 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            _dx12BinaryBlob = new RvmReference(reader);
            Data1 = reader.ReadBytes(0x08);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12PcDispatchInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data1 { get; set; }

        [IsHidden]
        public RvmReference _rvmSlotHandle { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> RvmSlotHandle => _rvmSlotHandle == null ? new List<RvmData>() : _rvmSlotHandle.ReferenceArray;

        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data2 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data1 = reader.ReadBytes(0x08);
            _rvmSlotHandle = new RvmReference(reader);
            Data2 = reader.ReadBytes(0x08);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12LegacyDrawStateBuilderInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data1 { get; set; }

        [IsHidden]
        public RvmReference _dx12InputElement { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx12InputElement => _dx12InputElement == null ? new List<RvmData>() : _dx12InputElement.ReferenceArray;

        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data2 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data1 = reader.ReadBytes(0x50);
            _dx12InputElement = new RvmReference(reader);
            Data2 = reader.ReadBytes(0x30);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12LegacyDrawStateBuilderInstructionBatchData : RvmData
    {
        [IsHidden]
        public RvmReference _dx12LegacyDrawStateBuilderInstructionData { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx12LegacyDrawStateBuilderInstructionData => _dx12LegacyDrawStateBuilderInstructionData == null ? new List<RvmData>() : _dx12LegacyDrawStateBuilderInstructionData.ReferenceArray;

        public override void ReadStruct(NativeReader reader)
        {
            _dx12LegacyDrawStateBuilderInstructionData = new RvmReference(reader);
        }
    }



    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12InputElement : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data1 { get; set; }

        [IsHidden]
        public RvmReference _charRvm { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> CharRvm => _charRvm == null ? new List<RvmData>() : _charRvm.ReferenceArray;

        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data2 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data1 = reader.ReadBytes(0x08);
            _charRvm = new RvmReference(reader);
            Data2 = reader.ReadBytes(0x18);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12NvLegacyDrawStateBuilderInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data1 { get; set; }

        [IsHidden]
        public RvmReference _dx12InputElement { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx12InputElement => _dx12InputElement == null ? new List<RvmData>() : _dx12InputElement.ReferenceArray;

        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data2 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data1 = reader.ReadBytes(0x50);
            _dx12InputElement = new RvmReference(reader);
            Data2 = reader.ReadBytes(0x40);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12NvLegacyDrawStateBuilderInstructionBatchData : RvmData
    {
        [IsHidden]
        public RvmReference _dx12NvLegacyDrawStateBuilderInstructionData { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx12NvLegacyDrawStateBuilderInstructionData => _dx12NvLegacyDrawStateBuilderInstructionData == null ? new List<RvmData>() : _dx12NvLegacyDrawStateBuilderInstructionData.ReferenceArray;

        public override void ReadStruct(NativeReader reader)
        {
            _dx12NvLegacyDrawStateBuilderInstructionData = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12NvDescriptorTableAssemblyInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference _dx12NvDescriptorTable { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public List<RvmData> Dx12NvDescriptorTable => _dx12NvDescriptorTable == null ? new List<RvmData>() : _dx12NvDescriptorTable.ReferenceArray;

        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data1 { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            _dx12NvDescriptorTable = new RvmReference(reader);
            Data1 = reader.ReadBytes(0x18);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12BinaryBlob : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x100);
        }
    }
    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12PcSampler : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x40);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RenderDepthMode : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x4);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12NvConstantBufferAssemblyInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x14);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class Float32 : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x4);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12ViewStateInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x14);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12NvDescriptorTable : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x8);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12RootWriteOp : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x8);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class Uint16 : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x2);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12PcShaderDispatchDrawInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x24);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class Uint8 : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x1);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx12ShaderState : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x1);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11NvViewStateInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x8);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11NvViewStateDepthInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x10);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11NvDrawStateInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0x18);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class UnknownRvmType : RvmData
    {
        [EbxFieldMeta(EbxFieldType.String)]
        public string TypeRealName { get; set; }
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void ReadStruct(NativeReader reader)
        {
            Data = reader.ReadBytes(0);
        }
        public void ReadFixed(NativeReader reader, int size)
        {
            Data = reader.ReadBytes(size);
            //ulong hash = CityHash.CityHash64WithSeed(inputBytes, seed);
        }
    }
}