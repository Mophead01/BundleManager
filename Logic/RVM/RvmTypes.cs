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
        public byte[] originalBytes { get; set; }
        public abstract void Read(NativeReader reader);
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11SerializedBlendState : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }
        public override void Read(NativeReader reader)
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
        public override void Read(NativeReader reader)
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
        public override void Read(NativeReader reader)
        {
            Data = reader.ReadBytes(0x20);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11VsShader : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Dx11_byte_code_element { get; set; }
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Dx11_input_element { get; set; }
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk3 { get; set; }
        public override void Read(NativeReader reader)
        {
            Data = reader.ReadBytes(0x20);
            Dx11_byte_code_element = new RvmReference(reader);
            Unk2 = reader.ReadULong();
            Dx11_input_element = new RvmReference(reader);
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
        public override void Read(NativeReader reader)
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
        public override void Read(NativeReader reader)
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
        public override void Read(NativeReader reader)
        {
            Data = reader.ReadBytes(0x10);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11Sampler : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }
        public override void Read(NativeReader reader)
        {
            Data = reader.ReadBytes(0x40);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmLegacyLightProbes : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }
        public override void Read(NativeReader reader)
        {
            Data = reader.ReadBytes(0x90);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class ViewState : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }
        public override void Read(NativeReader reader)
        {
            Data = reader.ReadBytes(0x140); // 4x4 matrix and some other stuff
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_DefaultValueRef : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash { get; set; }
        public override void Read(NativeReader reader)
        {
            Hash = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_InstructionBatchRef : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash { get; set; }
        public override void Read(NativeReader reader)
        {
            Hash = new RvmReference(reader);
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11ApplyParametersBlock : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference RvmSlotHandle { get; set; }
        public override void Read(NativeReader reader)
        {
            Unk1 = reader.ReadULong();
            RvmSlotHandle = new RvmReference(reader);
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

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference RvmFunctionInstanceRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference RvmContextSortKeyInfo { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash3 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference RvmDispatch { get; set; }

        public override void Read(NativeReader reader)
        {
            Guid = reader.ReadGuid();
            Index1 = reader.ReadUShort();
            Index2 = reader.ReadUShort();
            Unk1 = reader.ReadByte();
            Unk2 = reader.ReadByte();
            Unk3 = reader.ReadUShort();
            RvmFunctionInstanceRef = new RvmReference(reader);
            RvmContextSortKeyInfo = new RvmReference(reader);
            Hash3 = new RvmReference(reader);
            RvmDispatch = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class Int64 : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Int { get; set; }
        public override void Read(NativeReader reader)
        {
            Int = reader.ReadULong();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_InstructionBatch : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference RuntimeInstantiatedType { get; set; }

        /// <summary>
        /// This can be a ref to any "InstructionData" or "InstructionBatch" type
        /// </summary>
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference InstructionRef { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        public override void Read(NativeReader reader)
        {
            RuntimeInstantiatedType = new RvmReference(reader);
            InstructionRef = new RvmReference(reader);
            Unk2 = reader.ReadULong();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RvmFunction : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference InstructionBatchRef { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk3 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference ParamDbSerializedHashView { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference ParamDbSerializedFilterView { get; set; }

        public override void Read(NativeReader reader)
        {
            Unk1 = reader.ReadULong();
            InstructionBatchRef = new RvmReference(reader);
            Unk2 = reader.ReadULong();
            Unk3 = reader.ReadULong();
            ParamDbSerializedHashView = new RvmReference(reader);
            ParamDbSerializedFilterView = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_InstanceTableAssemblyInstructionBatchData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference TableAssemblyData { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference WriteOp { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }

        public override void Read(NativeReader reader)
        {
            TableAssemblyData = new RvmReference(reader);
            WriteOp = new RvmReference(reader);
            Unk1 = reader.ReadULong();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_SurfaceShader : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong SharedStreamableTextureRef { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint NameHash { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Padding { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong ShaderStreamableExternalTextureRef { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Unk2 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Hash3 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Hash4 { get; set; }

        /// <summary>
        /// Found in SBD next to Guid
        /// </summary>
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Unk3 { get; set; }

        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Padding2 { get; set; } = new byte[12];

        [EbxFieldMeta(EbxFieldType.Guid)]
        public Guid Guid { get; set; }

        public override void Read(NativeReader reader)
        {
            SharedStreamableTextureRef = reader.ReadULong();
            NameHash = reader.ReadUInt();
            Padding = reader.ReadUInt();
            ShaderStreamableExternalTextureRef = reader.ReadULong();
            Unk1 = reader.ReadUInt();
            Unk2 = reader.ReadUInt();
            Hash3 = reader.ReadULong();
            Hash4 = reader.ReadULong();
            Unk3 = reader.ReadUInt();
            Padding2 = reader.ReadBytes(12);
            Guid = reader.ReadGuid();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11LegacyVertexBufferConversionInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference PreparedVertexStream { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk3 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk4 { get; set; }

        public override void Read(NativeReader reader)
        {
            Unk1 = reader.ReadULong();
            Unk2 = reader.ReadULong();
            PreparedVertexStream = new RvmReference(reader);
            Unk3 = reader.ReadULong();
            Unk4 = reader.ReadULong();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_SerializedParameterBlock : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference ParamDbKeyRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash1 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }

        public override void Read(NativeReader reader)
        {
            ParamDbKeyRef = new RvmReference(reader);
            Hash1 = new RvmReference(reader);
            Unk1 = reader.ReadULong();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RvmFunctionInputTableIndices : RvmData
    {
        /// <summary>
        /// Points to a UInt16
        /// </summary>
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash { get; set; }

        public override void Read(NativeReader reader)
        {
            Hash = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11PsShader : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Dx11ByteCodeElement { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        public override void Read(NativeReader reader)
        {
            Unk1 = new RvmReference(reader);
            Dx11ByteCodeElement = new RvmReference(reader);
            Unk2 = reader.ReadULong();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11HsShader : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        public override void Read(NativeReader reader)
        {
            Unk1 = reader.ReadULong();
            Hash = new RvmReference(reader);
            Unk2 = reader.ReadULong();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_SerializedParameterBlockRef : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash { get; set; }

        public override void Read(NativeReader reader)
        {
            Hash = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11BlendStateData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Dx11SerializedBlendStates { get; set; }

        public override void Read(NativeReader reader)
        {
            Dx11SerializedBlendStates = reader.ReadBytes(4096);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_DirectInputInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference ParamDbSerializedReadView { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        public override void Read(NativeReader reader)
        {
            ParamDbSerializedReadView = new RvmReference(reader);
            Unk1 = reader.ReadULong();
            Unk2 = reader.ReadULong();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11TextureConversionInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference RvmSlotHandle1 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference RvmSlotHandle2 { get; set; }

        public override void Read(NativeReader reader)
        {
            RvmSlotHandle1 = new RvmReference(reader);
            RvmSlotHandle2 = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11DsShader : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        public override void Read(NativeReader reader)
        {
            Unk1 = new RvmReference(reader);
            Hash = new RvmReference(reader);
            Unk2 = reader.ReadULong();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ValueRef : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }

        public override void Read(NativeReader reader)
        {
            Hash = new RvmReference(reader);
            Unk1 = reader.ReadULong();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RvmPermutationRef : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash { get; set; }

        public override void Read(NativeReader reader)
        {
            Hash =  new RvmReference(reader);
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
        public uint TempUnknown { get; set; } //MISSING IN BATTLEDASH'S CODE - I THINK HE MIGHT HAVE MISSED IT BUT JONAH'S MIGHT BE WRONG

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Unk6 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference PermutationRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference PermutationLookupTable { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference ParamDbHash { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash4 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Unk7 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Unk8 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash5 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference ParamDbSerializedHashViewRef { get; set; }

        //[EbxFieldMeta(EbxFieldType.Struct)]
        //public ulong ParamDbSerializedHashViewRef { get; set; }


        public override void Read(NativeReader reader)
        {
            Guid = reader.ReadGuid();
            Unk1 = reader.ReadUShort();
            Unk2 = reader.ReadUShort();
            Unk3 = reader.ReadUShort();
            PermutationCount = reader.ReadUShort();
            Unk4 = reader.ReadUShort();
            Unk5 = reader.ReadUShort();
            TempUnknown = reader.ReadUInt();
            Unk6 = new RvmReference(reader);
            PermutationRef = new RvmReference(reader);
            PermutationLookupTable = new RvmReference(reader);
            ParamDbHash = new RvmReference(reader);
            Hash4 = new RvmReference(reader);
            Unk7 = new RvmReference(reader);
            Unk8 = new RvmReference(reader);
            Hash5 = new RvmReference(reader);
            //ParamDbSerializedHashViewRef = reader.ReadULong();
            ParamDbSerializedHashViewRef = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ShaderStreamableTextureRef : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash { get; set; }

        public override void Read(NativeReader reader)
        {
            Hash = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ParamDbSerializedHashView : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference ParamDbKeyRef { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference ParamDbKeyRef2 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk3 { get; set; }

        public override void Read(NativeReader reader)
        {
            ParamDbKeyRef = new RvmReference(reader);
            ParamDbKeyRef2 = new RvmReference(reader);
            Unk3 = reader.ReadULong();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RvmDispatch : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference InstructionBatchRef { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk3 { get; set; }

        public override void Read(NativeReader reader)
        {
            Unk1 = reader.ReadULong();
            InstructionBatchRef = new RvmReference(reader);
            Unk2 = reader.ReadULong();
            Unk3 = reader.ReadULong();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RvmFunctionInstance : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Unk2 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference CombinedSerializedParameterBlock { get; set; }

        public override void Read(NativeReader reader)
        {
            Unk1 = new RvmReference(reader);
            Unk2 = new RvmReference(reader);
            CombinedSerializedParameterBlock = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ParamDbSerializedHashViewRef : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash { get; set; }

        public override void Read(NativeReader reader)
        {
            Hash = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ParamDbSerializedFilterView : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference ParamDbKeyRef { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        public override void Read(NativeReader reader)
        {
            ParamDbKeyRef = new RvmReference(reader);
            Unk2 = reader.ReadULong();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RuntimeInstantiatedType : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference RttiType { get; set; }

        public override void Read(NativeReader reader)
        {
            RttiType = new RvmReference(reader);
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
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference SemanticName { get; set; }

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

        public override void Read(NativeReader reader)
        {
            SemanticName = new RvmReference(reader);
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
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash1 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash2 { get; set; }

        public override void Read(NativeReader reader)
        {
            Hash1 = new RvmReference(reader);
            Hash2 = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ParamDbSerializedReadView : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference ParamDbKeyRef { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Unk2 { get; set; }

        public override void Read(NativeReader reader)
        {
            ParamDbKeyRef = new RvmReference(reader);
            Unk2 = reader.ReadULong();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11ApplyParametersInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash1 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash2 { get; set; }

        public override void Read(NativeReader reader)
        {
            Hash1 = new RvmReference(reader);
            Hash2 = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RvmFunctionInstanceRef : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash { get; set; }

        public override void Read(NativeReader reader)
        {
            Hash = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class ParamDbHash : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt64)]
        public ulong Hash { get; set; }

        public override void Read(NativeReader reader)
        {
            Hash = reader.ReadULong();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11BufferConversionInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference RvmSlotHandle1 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference RvmSlotHandle2 { get; set; }

        public override void Read(NativeReader reader)
        {
            RvmSlotHandle1 = new RvmReference(reader);
            RvmSlotHandle2 = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Settings : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Unk1 { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Unk2 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference DatabaseName { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash1 { get; set; }

        public override void Read(NativeReader reader)
        {
            Unk1 = reader.ReadUInt();
            Unk2 = reader.ReadUInt();
            DatabaseName = new RvmReference(reader);
            Hash1 = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ShaderStreamableExternalTextureRef : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash { get; set; }

        public override void Read(NativeReader reader)
        {
            Hash = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_Dx11DispatchInstructionData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference RvmSlotHandle1 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference  RvmSlotHandle2 { get; set; }

        public override void Read(NativeReader reader)
        {
            RvmSlotHandle1 = new RvmReference(reader);
            RvmSlotHandle2 = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_ParamDbKeyRef : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash { get; set; }

        public override void Read(NativeReader reader)
        {
            Hash = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RttiType : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference ModuleName { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Name { get; set; }

        public override void Read(NativeReader reader)
        {
            ModuleName = new RvmReference(reader);
            Name = new RvmReference(reader);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_TableAssemblyInstructionBatchData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference TableAssemblyData { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference Hash1 { get; set; }

        [EbxFieldMeta(EbxFieldType.Struct)]
        public RvmReference WriteOpGroup { get; set; }

        public override void Read(NativeReader reader)
        {
            TableAssemblyData = new RvmReference(reader);
            Hash1 = new RvmReference(reader);
            WriteOpGroup = new RvmReference(reader);
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

        public override void Read(NativeReader reader)
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
        public override void Read(NativeReader reader)
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
        public override void Read(NativeReader reader)
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

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint NameHash { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint ExternalHash { get; set; }

        public override void Read(NativeReader reader)
        {
            TexCoordsPerMeter = reader.ReadFloat();
            Unk1 = reader.ReadUShort();
            TextureType = reader.ReadUShort();
            NameHash = reader.ReadUInt();
            ExternalHash = reader.ReadUInt();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_RenderDepthMode : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Value { get; set; }

        public override void Read(NativeReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmLegacyOutdoorLightStatus : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Value { get; set; }

        public override void Read(NativeReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmLevelOfDetail : RvmData
    {
        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint Value { get; set; }

        public override void Read(NativeReader reader)
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

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint NameHash { get; set; }

        [EbxFieldMeta(EbxFieldType.UInt32)]
        public uint ExternalHash { get; set; }

        public override void Read(NativeReader reader)
        {
            TexCoordsPerMeter = reader.ReadFloat();
            Unk1 = reader.ReadUShort();
            TextureType = reader.ReadUShort();
            NameHash = reader.ReadUInt();
            ExternalHash = reader.ReadUInt(); /// Guessing this has something to do with external since the non-externals have 0xFFFF here
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class RvmSerializedDb_ns_InstanceTableAssemblyData : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void Read(NativeReader reader)
        {
            Data = reader.ReadBytes(0x0C);
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class Float : RvmData
    {
        [EbxFieldMeta(EbxFieldType.Float32)]
        public float Value { get; set; }

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
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

        public override void Read(NativeReader reader)
        {
            Value = reader.ReadUInt();
        }
    }

    [EbxClassMeta(EbxFieldType.Struct)]
    public class CharRvm : RvmData
    {
        [EbxFieldMeta(EbxFieldType.String)]
        public char Value { get; set; }

        public override void Read(NativeReader reader)
        {
            Value = (char)reader.ReadByte();
        }
    }


    [EbxClassMeta(EbxFieldType.Struct)]
    public class UnknownRvmType : RvmData
    {
        [EbxFieldMeta(EbxFieldType.DbObject)]
        public byte[] Data { get; set; }

        public override void Read(NativeReader reader)
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