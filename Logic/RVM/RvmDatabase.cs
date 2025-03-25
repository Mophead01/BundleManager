using AtlasTexturePlugin;
using AutoBundleManagerPlugin.Rvm;
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
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using static Frosty.Core.Screens.GFSDK_ShadowLib;
using static FrostySdk.GeometryDeclarationDesc;
using Frosty.Hash;
using Frosty.Core.Windows;
using System.Collections;
using static AutoBundleManagerPlugin.RvmEditor;
using System.Drawing;
using System.Security.Policy;
using Frosty.Core.Viewport;

namespace AutoBundleManagerPlugin
{
    public class RvmEditor
    {
        public static class RvmStaticVariables
        {
            public static Dictionary<uint, ushort> rvmSizes = new Dictionary<uint, ushort>();
            static RvmStaticVariables()
            {
                using (BinaryReader reader = new BinaryReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("AutoBundleManagerPlugin.Data.Swbf2_V2ExtractedTypeInfoSizes.cache")))
                {
                    int typeCount = reader.ReadInt32();
                    for (int i = 0; i < typeCount; i++)
                    {
                        string typeName = reader.ReadString();
                        ushort typeSize = reader.ReadUInt16();
                        uint hash = (uint)Utils.HashString(typeName);
                        if (!rvmSizes.ContainsKey(hash))
                            rvmSizes.Add(hash, typeSize);
                        else
                            App.Logger.LogError(typeName);
                    }
                }
            }
        }
        public class RvmViewerMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => "AutoBundleManager";

            public override string SubLevelMenuName => null;

            public override string MenuItemName => "RVM - Viewer";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                RvmDatabase rvmDatabase = new RvmDatabase();
                FrostyTaskWindow.Show("Opening RVM", "", (task) =>
                {
                    string rvmName = "levels/mp/deathstar02_01/deathstar02_01/rvmdatabase_dx11rvmdatabase"; //"s2/levels/cloudcity_01/cloudcity_01/rvmdatabase_dx12pcrvmdatabase";//"levels/sp/rootlevel/rootlevel/rvmdatabase_dx12pcrvmdatabase";//"gameplay/bundles/sharedbundles/sp/vehicles/sharedbundlevehicles_sp/rvmdatabase_dx11nvrvmdatabase";
                    rvmDatabase = new RvmDatabase(App.AssetManager.GetResEntry(rvmName), RvmReadingType.BodyWithRefs, task);
                });
                App.EditorWindow.OpenEditor("RVM Viewer", new RvmViewer(rvmDatabase));
            });

        }
        public class RvmWriterTestMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => "AutoBundleManager";

            public override string SubLevelMenuName => null;

            public override string MenuItemName => "RVM - Rewrite Test";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                FrostyTaskWindow.Show("Opening RVM", "", (task) =>
                {
                    RvmDatabase rvmDatabase = new RvmDatabase();
                    string rvmName = "gameplay/bundles/sharedbundles/sp/vehicles/sharedbundlevehicles_sp/rvmdatabase_dx11nvrvmdatabase"; //"s2/levels/cloudcity_01/cloudcity_01/rvmdatabase_dx12pcrvmdatabase";//"levels/sp/rootlevel/rootlevel/rvmdatabase_dx12pcrvmdatabase";//"gameplay/bundles/sharedbundles/sp/vehicles/sharedbundlevehicles_sp/rvmdatabase_dx11nvrvmdatabase";
                    rvmDatabase = new RvmDatabase(App.AssetManager.GetResEntry(rvmName), RvmReadingType.BodyNoRefs, task);
                    rvmDatabase.WriteToFile(@"E:\C#\Frosty\rvm\Tests\test.res", task);
                });
            });

        }
        public class RvmComparerMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => "AutoBundleManager";

            public override string SubLevelMenuName => null;

            public override string MenuItemName => "RVM - Comparison";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                RvmDatabase editedDatabase = new RvmDatabase(@"E:\C#\Frosty\rvm\bespin_rvmdatabase_dx11nvrvmdatabase.res");
                RvmDatabase baseDatabase = new RvmDatabase(App.AssetManager.GetResEntry("s2/levels/cloudcity_01/cloudcity_01/rvmdatabase_dx11nvrvmdatabase"));
                List<string> types = baseDatabase.Types.Select(type => type.Name).ToList();
                types.AddRange(editedDatabase.Types.Select(type => type.Name));
                types = types.Distinct().ToList();

                Dictionary<string, RvmDataContainer> baseDataContainers = baseDatabase.DataContainer.ToDictionary(dataContainer => dataContainer.TypeName);
                Dictionary<string, RvmDataContainer> editedDataContainers = editedDatabase.DataContainer.ToDictionary(dataContainer => dataContainer.TypeName);

                foreach (string type in types)
                {
                    long baseSize = 0;
                    long editedSize = 0;
                    if (baseDataContainers.ContainsKey(type))
                        baseSize = baseDataContainers[type].ByteSize;
                    if (editedDataContainers.ContainsKey(type))
                        editedSize = editedDataContainers[type].ByteSize;
                    if (baseSize != editedSize)
                        App.Logger.Log($"{type} - {baseSize} - {editedSize}");
                }
            });

        }
        public class RvmExportHeaderTypesMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => "AutoBundleManager";

            public override string SubLevelMenuName => null;

            public override string MenuItemName => "RVM - Export Header Types";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                FrostyTaskWindow.Show("Opening RVMs", "", (task) =>
                {
                    Dictionary<string, uint> Checksums = new Dictionary<string, uint>();
                    foreach (ResAssetEntry resEntry in App.AssetManager.EnumerateRes())
                    {
                        if (resEntry.Name.Contains("rvmdatabase_dx"))
                        {
                            RvmDatabase rvmDatabase = new RvmDatabase(resEntry, RvmReadingType.BodyWithRefs, task);
                            Checksums.Add(resEntry.Name, rvmDatabase.MaybeHash);
                            string levName = resEntry.Name.Substring(resEntry.Name.LastIndexOf('/', resEntry.Name.LastIndexOf('/') - 1) + 1, resEntry.Name.LastIndexOf('/') - resEntry.Name.LastIndexOf('/', resEntry.Name.LastIndexOf('/') - 1) - 1);

                            using (StreamWriter writer = new StreamWriter($@"E:\C#\Frosty\rvm\Headers\{levName}_{resEntry.DisplayName}_{Utils.HashString(resEntry.Name)}.csv"))
                            {
                                writer.WriteLine("Name,NameHash,ActualIndex,WrittenIndex,HasParser,ByteSize"); // CSV header

                                int idx = 0;
                                foreach (var typeData in rvmDatabase.TypesIndexOrdered)
                                {
                                    writer.WriteLine($"{typeData.Name},{typeData.NameHash},{idx++},{typeData.Index},{typeData.HasParser},0x{typeData.ByteSize.ToString("X")}");
                                }
                            }

                            Dictionary<string, Dictionary<string, List<string>>> referencesToExport = new Dictionary<string, Dictionary<string, List<string>>>();
                            foreach(RvmDataContainer dataContainer in rvmDatabase.DataContainer)
                            {
                                string typeName = dataContainer.TypeName;
                                Dictionary<string, List<string>> typeReferences = new Dictionary<string, List<string>>();
                                foreach (RvmArray arrayData in dataContainer.Data)
                                {
                                    foreach (RvmData rvmData in arrayData.Array)
                                    {
                                        foreach (var field in rvmData.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
                                        {
                                            if (field.PropertyType == typeof(RvmReference))
                                            {
                                                string refFieldName = field.Name;
                                                if (((RvmReference)field.GetValue(rvmData)).ReferenceArray.Count == 0)
                                                    continue;
                                                string refName = ((RvmReference)field.GetValue(rvmData)).ReferenceArray[0].GetType().Name;
                                                if (!typeReferences.ContainsKey(refFieldName))
                                                    typeReferences.Add(refFieldName, new List<string>());
                                                if (!typeReferences[refFieldName].Contains(refName))
                                                    typeReferences[refFieldName].Add(refName);
                                            }
                                            else if (field.PropertyType.IsGenericType && field.PropertyType.GetGenericTypeDefinition() == typeof(List<>) && field.PropertyType.GetGenericArguments()[0] == typeof(RvmReference))
                                            {
                                                List<RvmReference> rvmReferences = ((List<RvmReference>)field.GetValue(rvmData));
                                                foreach(RvmReference rvmReference in rvmReferences)
                                                {
                                                    string refFieldName = field.Name;
                                                    if (rvmReference.ReferenceArray.Count == 0)
                                                        continue;
                                                    string refName = rvmReference.ReferenceArray[0].GetType().Name;
                                                    if (!typeReferences.ContainsKey(refFieldName))
                                                        typeReferences.Add(refFieldName, new List<string>());
                                                    if (!typeReferences[refFieldName].Contains(refName))
                                                        typeReferences[refFieldName].Add(refName);
                                                }
                                            }
                                        }
                                    }
                                }
                                referencesToExport.Add(typeName, typeReferences);
                            }
                            using (StreamWriter writer = new StreamWriter($@"E:\C#\Frosty\rvm\References\{levName}_{resEntry.DisplayName}_{Utils.HashString(resEntry.Name)}.csv"))
                            {
                                writer.WriteLine("TypeName,FieldName,ReferenceTypes"); // CSV header

                                foreach (var pair in referencesToExport)
                                {
                                    foreach(var refPair in pair.Value)
                                    {
                                        writer.WriteLine($"{pair.Key},{refPair.Key},{string.Join("$", refPair.Value)}");
                                    }
                                }
                            }
                        }
                    }
                    using (StreamWriter writer = new StreamWriter($@"E:\C#\Frosty\rvm\Headers\RvmMaybeHashes.csv"))
                    {
                        writer.WriteLine("Name,MaybeHash"); // CSV header

                        foreach (var typeData in Checksums)
                        {
                            writer.WriteLine($"{typeData.Key},{typeData.Value.ToString("X")}");
                        }
                    }
                });
            });
        }
        public class RvmVerifyRefTypesMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => "AutoBundleManager";

            public override string SubLevelMenuName => null;

            public override string MenuItemName => "RVM - Verify Ref Types";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                FrostyTaskWindow.Show("Opening RVMs", "", (task) =>
                {
                    foreach (ResAssetEntry resEntry in App.AssetManager.EnumerateRes())
                    {
                        if (resEntry.Name.Contains("rvmdatabase_dx"))
                        {
                            App.Logger.Log("Checking: " + resEntry.Name);
                            RvmDatabase rvmDatabase = new RvmDatabase(resEntry, RvmReadingType.DebugOnlyIdentifyRefs, task);
                        }
                    }
                });
            });
        }
        public class RvmExtractShaderMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => "AutoBundleManager";

            public override string SubLevelMenuName => null;

            public override string MenuItemName => "RVM - Extract Shaders";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                FrostyTaskWindow.Show("Opening RVMs", "", (task) =>
                {
                    foreach (ResAssetEntry resEntry in App.AssetManager.EnumerateRes())
                    {
                        if (resEntry.Name.Contains("rootlevel/rvmdatabase_dx11"))
                        {
                            if (resEntry.Name.Contains("_dx11rvmdatabase"))
                            {
                                App.Logger.Log("Exporting: " + resEntry.Name);
                                RvmDatabase rvmDatabase = new RvmDatabase(resEntry, RvmReadingType.BodyWithRefs, task);
                                rvmDatabase.ExtractShaders(@"E:\C#\Frosty\rvm\Dx11rvmShaders");
                            }
                            else if (resEntry.Name.Contains("_dx11nvrvmdatabase"))
                            {
                                App.Logger.Log("Exporting: " + resEntry.Name);
                                RvmDatabase rvmDatabase = new RvmDatabase(resEntry, RvmReadingType.BodyWithRefs, task);
                                rvmDatabase.ExtractShaders(@"E:\C#\Frosty\rvm\Dx11nvrvmShaders");
                            }
                        }
                    }
                });
            });
        }
        public class RvmTestWriterBespinMenuExtension : MenuExtension
        {
            public override string TopLevelMenuName => "AutoBundleManager";

            public override string SubLevelMenuName => null;

            public override string MenuItemName => "RVM - Test Writer (Bespin)";

            public override ImageSource Icon => new ImageSourceConverter().ConvertFromString("pack://application:,,,/FrostyEditor;component/Images/Compile.png") as ImageSource;

            static List<string> dx11Order = new List<string>()
            {
                "RvmSerializedDb_ns_Dx11SerializedBlendState",
                "Vec",
                "RvmLegacyLightMapInstance",
                "StencilState",
                "RvmSerializedDb_ns_Dx11Sampler",
                "RvmLegacyLightProbes",
                "ViewState",
                "RvmSerializedDb_ns_DefaultValueRef",
                "RvmSerializedDb_ns_InstructionBatchRef",
                "RvmSerializedDb_ns_Dx11ApplyParametersBlock",
                "RvmSerializedDb_ns_ParamDbSerializedReadView",
                "RvmSerializedDb_ns_Dx11ViewStateInstructionData",
                "RvmSerializedDb_ns_RttiType",
                "Uint16",
                "RvmSerializedDb_ns_RvmPermutationRef",
                "RvmSerializedDb_ns_PackLightMapWeightIntoInstanceInstructionData",
                "RvmSerializedDb_ns_PreparedVertexStream",
                "ShaderRenderMode",
                "Uint8",
                "ParamDbHash",
                "RvmSerializedDb_ns_WriteOp",
                "RvmSerializedDb_ns_VectorSubtractInstructionData",
                "RvmSerializedDb_ns_InstructionBatch",
                "Int64",
                "RvmSerializedDb_ns_CpuToGpuMatrixInstructionData",
                "RvmSerializedDb_ns_RvmFunction",
                "RvmSerializedDb_ns_DepthBiasGroupData",
                "RvmSerializedDb_ns_ShaderStreamableExternalTextureRef",
                "RvmSerializedDb_ns_RvmPermutation",
                "RvmSerializedDb_ns_ParamDbSerializedHashViewRef",
                "RvmSerializedDb_ns_DefaultValueStructLegacyData",
                "RvmSerializedDb_ns_BaseShaderState",
                "RvmSerializedDb_ns_SliceCountInstructionData",
                "RvmSerializedDb_ns_Dx11ShaderDispatchDrawInstructionData",
                "RvmSerializedDb_ns_TessellationParametersInstructionData",
                "RvmContextSortKeyInfo",
                "RvmSerializedDb_ns_WriteOpGroup",
                "RvmSerializedDb_ns_DefaultValueSimpleTexture",
                "QualityLevel",
                "ProjectionState",
                "RvmSerializedDb_ns_LodFadeInstructionData",
                "RvmSerializedDb_ns_TableAssemblyData",
                "RvmSerializedDb_ns_TableAssemblyInstructionBatchData",
                "RvmSerializedDb_ns_OffsetTranslationInMatrixInstructionData",
                "RvmSerializedDb_ns_Dx11ByteCodeElement",
                "RvmSerializedDb_ns_Dx11DispatchInstructionData",
                "Boolean",
                "RvmSerializedDb_ns_Dx11LegacyDrawStateBuilderData",
                "RvmSerializedDb_ns_RvmPermutationLookupTable",
                "ShaderGeometrySpace",
                "RvmIndexBufferFormat",
                "Uint32",
                "CharRvm",
                "RvmSerializedDb_ns_RvmPermutationSet",
                "RvmSerializedDb_ns_Dx11PsShader",
                "RvmSerializedDb_ns_DefaultValueZeroMem",
                "RvmSerializedDb_ns_RvmFunctionInputTableIndices",
                "RvmSerializedDb_ns_SerializedParameterBlock",
                "RvmSerializedDb_ns_SurfaceShader",
                "RvmSerializedDb_ns_DefaultValueSimpleBuffer",
                "Float32",
                "RvmSerializedDb_ns_ValueRef",
                "RvmSerializedDb_ns_Dx11TextureConversionInstructionData",
                "RvmSerializedDb_ns_Dx11DsShader",
                "RvmSerializedDb_ns_Dx11HsShader",
                "RvmSerializedDb_ns_SerializedParameterBlockRef",
                "RvmSerializedDb_ns_ShaderStreamableTextureRef",
                "RvmSerializedDb_ns_Dx11BlendStateData",
                "PrimitiveType",
                "RvmSerializedDb_ns_InstanceTableAssemblyInstructionBatchData",
                "RvmSerializedDb_ns_Dx11LegacyVertexBufferConversionInstructionData",
                "RvmSerializedDb_ns_DirectInputInstructionData",
                "ShaderDepthBiasGroup",
                "RvmSerializedDb_ns_Dx11BufferConversionInstructionData",
                "RvmSerializedDb_ns_InstanceTableAssemblyData",
                "ShaderInstancingMethod",
                "RvmSerializedDb_ns_ShaderStreamableTexture",
                "Int32",
                "RvmSerializedDb_ns_Dx11ApplyParametersInstructionData",
                "RvmSerializedDb_ns_Dx11InputElement",
                "RvmSerializedDb_ns_RvmFunctionInstanceRef",
                "RvmLegacyInstructions_ns_LegacyInstructionData",
                "RvmLevelOfDetail",
                "RvmSerializedDb_ns_Settings",
                "RvmLegacyOutdoorLightStatus",
                "RenderDepthMode",
                "RvmSlotHandle",
                "RvmSerializedDb_ns_ParamDbSerializedFilterView",
                "RvmSerializedDb_ns_ParamDbKeyRef",
                "RvmSerializedDb_ns_CombinedSerializedParameterBlock",
                "RvmSerializedDb_ns_ShaderStreamableExternalTexture",
                "RvmSerializedDb_ns_RvmDispatch",
                "RvmSerializedDb_ns_RuntimeInstantiatedType",
                "RvmSerializedDb_ns_Dx11VsShader",
                "ShaderSkinningMethod",
                "RvmSerializedDb_ns_SerializedParamDbKey",
                "RvmSerializedDb_ns_RvmFunctionInstance",
                "RvmSerializedDb_ns_ParamDbSerializedHashView",
            };

            static List<string> nvdx11Order = new List<string>()
            {
                "Vec",
                "RvmSerializedDb_ns_Dx11Sampler",
                "ViewState",
                "RvmSerializedDb_ns_DefaultValueRef",
                "RvmSerializedDb_ns_InstructionBatchRef",
                "RvmSerializedDb_ns_Dx11ApplyParametersBlock",
                "RvmSerializedDb_ns_ParamDbSerializedHashViewRef",
                "RvmLegacyInstructions_ns_LegacyInstructionData",
                "RvmSerializedDb_ns_RvmFunctionInstanceRef",
                "RvmSerializedDb_ns_TessellationParametersInstructionData",
                "RvmSerializedDb_ns_Dx11TextureConversionInstructionData",
                "QualityLevel",
                "RvmSerializedDb_ns_Dx11NvViewStateInstructionData",
                "RvmSerializedDb_ns_InstanceTableAssemblyData",
                "RvmSerializedDb_ns_SliceCountInstructionData",
                "RvmSerializedDb_ns_Dx11InputElement",
                "RvmSerializedDb_ns_CpuToGpuMatrixInstructionData",
                "RvmSlotHandle",
                "RvmSerializedDb_ns_InstructionBatch",
                "Int64",
                "RvmSerializedDb_ns_LodFadeInstructionData",
                "ShaderSkinningMethod",
                "RvmSerializedDb_ns_ParamDbSerializedReadView",
                "RvmSerializedDb_ns_RvmPermutation",
                "RvmSerializedDb_ns_RvmPermutationSet",
                "Uint8",
                "CharRvm",
                "RvmSerializedDb_ns_Dx11NvDrawStateInstructionData",
                "RvmSerializedDb_ns_DefaultValueSimpleTexture",
                "RvmSerializedDb_ns_VectorSubtractInstructionData",
                "RvmSerializedDb_ns_Dx11NvViewStateDepthInstructionData",
                "RvmSerializedDb_ns_Dx11ByteCodeElement",
                "RvmSerializedDb_ns_PreparedVertexStream",
                "RvmSerializedDb_ns_TableAssemblyData",
                "ProjectionState",
                "RvmSerializedDb_ns_Dx11ShaderDispatchDrawInstructionData",
                "PrimitiveType",
                "ParamDbHash",
                "RvmContextSortKeyInfo",
                "RvmSerializedDb_ns_DefaultValueStructLegacyData",
                "RvmSerializedDb_ns_Dx11ApplyParametersInstructionData",
                "Boolean",
                "RvmSerializedDb_ns_DefaultValueZeroMem",
                "RvmSerializedDb_ns_Dx11BufferConversionInstructionData",
                "ShaderRenderMode",
                "ShaderGeometrySpace",
                "ShaderInstancingMethod",
                "RvmSerializedDb_ns_WriteOpGroup",
                "RvmSerializedDb_ns_Dx11DsShader",
                "Uint16",
                "RvmSerializedDb_ns_SurfaceShader",
                "RvmSerializedDb_ns_InstanceTableAssemblyInstructionBatchData",
                "RvmSerializedDb_ns_DefaultValueSimpleBuffer",
                "RvmLevelOfDetail",
                "RvmSerializedDb_ns_RvmFunction",
                "RvmSerializedDb_ns_DirectInputInstructionData",
                "RvmSerializedDb_ns_Dx11HsShader",
                "RvmSerializedDb_ns_SerializedParameterBlockRef",
                "RvmSerializedDb_ns_Dx11LegacyVertexBufferConversionInstructionData",
                "RvmSerializedDb_ns_SerializedParameterBlock",
                "RvmSerializedDb_ns_RvmFunctionInputTableIndices",
                "NvShadowMapRenderType",
                "RvmSerializedDb_ns_Dx11PsShader",
                "RvmLegacyOutdoorLightStatus",
                "RvmSerializedDb_ns_ShaderStreamableTexture",
                "RvmSerializedDb_ns_ShaderStreamableExternalTexture",
                "RvmSerializedDb_ns_ParamDbKeyRef",
                "RvmSerializedDb_ns_ParamDbSerializedFilterView",
                "RvmSerializedDb_ns_RvmDispatch",
                "RvmSerializedDb_ns_RuntimeInstantiatedType",
                "RvmSerializedDb_ns_Dx11DispatchInstructionData",
                "RvmSerializedDb_ns_Settings",
                "RvmSerializedDb_ns_WriteOp",
                "Int32",
                "RvmSerializedDb_ns_CombinedSerializedParameterBlock",
                "RvmSerializedDb_ns_RvmPermutationLookupTable",
                "RvmSerializedDb_ns_TableAssemblyInstructionBatchData",
                "RvmSerializedDb_ns_OffsetTranslationInMatrixInstructionData",
                "RvmSerializedDb_ns_ShaderStreamableTextureRef",
                "RvmSerializedDb_ns_RvmFunctionInstance",
                "RvmSerializedDb_ns_ShaderStreamableExternalTextureRef",
                "RvmSerializedDb_ns_RttiType",
                "RvmSerializedDb_ns_RvmPermutationRef",
                "RvmSerializedDb_ns_ParamDbSerializedHashView",
                "RvmSerializedDb_ns_Dx11VsShader",
                "Float32",
                "RvmSerializedDb_ns_SerializedParamDbKey",
                "RvmSerializedDb_ns_ValueRef",
            };

            static List<string> pcdx12Order = new List<string>()
            {
                "Vec",
                "RvmLegacyLightMapInstance",
                "RvmSerializedDb_ns_SerializedParamDbKey",
                "RvmSerializedDb_ns_Dx12BinaryBlob",
                "ProjectionState",
                "StencilState",
                "RvmLegacyLightProbes",
                "ViewState",
                "RvmSerializedDb_ns_Dx12PcSampler",
                "RvmSerializedDb_ns_RvmDispatch",
                "RvmSerializedDb_ns_PackLightMapWeightIntoInstanceInstructionData",
                "RvmSerializedDb_ns_TessellationParametersInstructionData",
                "RvmSerializedDb_ns_SerializedParameterBlockRef",
                "QualityLevel",
                "RvmSerializedDb_ns_PreparedVertexStream",
                "PrimitiveType",
                "RvmSerializedDb_ns_Settings",
                "RvmSerializedDb_ns_RvmPermutationRef",
                "RvmSerializedDb_ns_SliceCountInstructionData",
                "RvmLegacyInstructions_ns_LegacyInstructionData",
                "RvmSlotHandle",
                "Uint32",
                "RvmSerializedDb_ns_WriteOp",
                "ShaderGeometrySpace",
                "ShaderSkinningMethod",
                "Int32",
                "ShaderRenderMode",
                "RvmSerializedDb_ns_Dx12VertexBufferViewInstructionData",
                "Uint8",
                "RvmSerializedDb_ns_BaseShaderState",
                "RvmSerializedDb_ns_Dx12PcRootSignature",
                "RvmLevelOfDetail",
                "RvmSerializedDb_ns_DefaultValueSimpleBuffer",
                "ShaderInstancingMethod",
                "Boolean",
                "RvmSerializedDb_ns_LodFadeInstructionData",
                "RvmSerializedDb_ns_VectorSubtractInstructionData",
                "RvmContextSortKeyInfo",
                "RvmSerializedDb_ns_WriteOpGroup",
                "RvmIndexBufferFormat",
                "RvmSerializedDb_ns_DefaultValueSimpleTexture",
                "RenderDepthMode",
                "Float32",
                "RvmSerializedDb_ns_CpuToGpuMatrixInstructionData",
                "RvmSerializedDb_ns_RvmPermutationLookupTable",
                "RvmSerializedDb_ns_Dx12RootWriteOp",
                "RvmSerializedDb_ns_DefaultValueStructLegacyData",
                "RvmSerializedDb_ns_Dx12ShaderState",
                "RvmSerializedDb_ns_DefaultValueZeroMem",
                "RvmSerializedDb_ns_TableAssemblyInstructionBatchData",
                "RvmSerializedDb_ns_ShaderStreamableExternalTexture",
                "RvmSerializedDb_ns_RttiType",
                "CharRvm",
                "RvmSerializedDb_ns_Dx12PcRvmDescriptorTableAssemblyInstructionData",
                "RvmSerializedDb_ns_RvmFunction",
                "Uint16",
                "RvmSerializedDb_ns_InstructionBatch",
                "Int64",
                "RvmSerializedDb_ns_InstructionBatchRef",
                "RvmSerializedDb_ns_Dx12PcShaderDispatchDrawInstructionData",
                "RvmSerializedDb_ns_RvmFunctionInstanceRef",
                "RvmSerializedDb_ns_Dx12PcSamplerTableWriterInstructionData",
                "RvmSerializedDb_ns_SerializedParameterBlock",
                "RvmSerializedDb_ns_RvmFunctionInputTableIndices",
                "RvmSerializedDb_ns_Dx12LegacyDrawStateBuilderInstructionBatchData",
                "RvmSerializedDb_ns_InstanceTableAssemblyInstructionBatchData",
                "RvmSerializedDb_ns_Dx12PcSamplerPointer",
                "RvmSerializedDb_ns_Dx12Shader",
                "RvmSerializedDb_ns_ShaderStreamableExternalTextureRef",
                "RvmSerializedDb_ns_DefaultValueRef",
                "RvmSerializedDb_ns_RvmPermutation",
                "RvmSerializedDb_ns_SurfaceShader",
                "RvmSerializedDb_ns_ParamDbSerializedHashViewRef",
                "RvmSerializedDb_ns_ShaderStreamableTexture",
                "RvmSerializedDb_ns_ParamDbSerializedReadView",
                "ParamDbHash",
                "RvmSerializedDb_ns_CombinedSerializedParameterBlock",
                "RvmSerializedDb_ns_RvmFunctionInstance",
                "RvmSerializedDb_ns_Dx12PcPSOPreloadOp",
                "RvmLegacyOutdoorLightStatus",
                "RvmSerializedDb_ns_Dx12ViewStateInstructionData",
                "RvmSerializedDb_ns_InstanceTableAssemblyData",
                "RvmSerializedDb_ns_Dx12RootDescriptorTableAssemblyInstructionData",
                "RvmSerializedDb_ns_DepthBiasGroupData",
                "RvmSerializedDb_ns_RuntimeInstantiatedType",
                "RvmSerializedDb_ns_ParamDbSerializedFilterView",
                "RvmSerializedDb_ns_OffsetTranslationInMatrixInstructionData",
                "RvmSerializedDb_ns_ShaderStreamableTextureRef",
                "RvmSerializedDb_ns_TableAssemblyData",
                "RvmSerializedDb_ns_ParamDbSerializedHashView",
                "RvmSerializedDb_ns_Dx12PcDispatchInstructionData",
                "RvmSerializedDb_ns_Dx12LegacyDrawStateBuilderInstructionData",
                "RvmSerializedDb_ns_Dx12InputElement",
                "RvmSerializedDb_ns_ParamDbKeyRef",
                "ShaderDepthBiasGroup",
                "RvmSerializedDb_ns_ValueRef",
                "RvmSerializedDb_ns_DirectInputInstructionData",
                "RvmSerializedDb_ns_RvmPermutationSet"
            };

            static List<string> nvdx12Order = new List<string>()
            {
                "Vec",
                "RvmSerializedDb_ns_SerializedParamDbKey",
                "RvmSerializedDb_ns_Dx12BinaryBlob",
                "ProjectionState",
                "StencilState",
                "ViewState",
                "RvmSerializedDb_ns_Dx12PcSampler",
                "RvmSerializedDb_ns_ParamDbSerializedHashViewRef",
                "RvmSerializedDb_ns_Dx12NvDescriptorTable",
                "Uint16",
                "RvmSerializedDb_ns_Dx12PcRvmDescriptorTableAssemblyInstructionData",
                "RvmSerializedDb_ns_Dx12ViewStateInstructionData",
                "RvmContextSortKeyInfo",
                "ShaderDepthBiasGroup",
                "RvmSerializedDb_ns_Dx12PcRootSignature",
                "Uint8",
                "RvmSerializedDb_ns_WriteOp",
                "RvmSerializedDb_ns_VectorSubtractInstructionData",
                "ShaderGeometrySpace",
                "RvmSerializedDb_ns_ParamDbSerializedFilterView",
                "RvmSerializedDb_ns_CpuToGpuMatrixInstructionData",
                "RvmSerializedDb_ns_TableAssemblyData",
                "Uint32",
                "RvmSerializedDb_ns_ShaderStreamableTexture",
                "RvmSerializedDb_ns_Dx12InputElement",
                "RvmSerializedDb_ns_DefaultValueStructLegacyData",
                "Boolean",
                "RvmSerializedDb_ns_ShaderStreamableTextureRef",
                "ShaderInstancingMethod",
                "RvmSerializedDb_ns_SliceCountInstructionData",
                "RvmSerializedDb_ns_Dx12NvLegacyDrawStateBuilderInstructionData",
                "RvmSerializedDb_ns_CombinedSerializedParameterBlock",
                "RvmSerializedDb_ns_DefaultValueSimpleTexture",
                "RvmSerializedDb_ns_TessellationParametersInstructionData",
                "RvmSerializedDb_ns_Dx12RootWriteOp",
                "CharRvm",
                "QualityLevel",
                "RvmSerializedDb_ns_PreparedVertexStream",
                "NvShadowMapRenderType",
                "RvmSerializedDb_ns_Dx12RootDescriptorTableAssemblyInstructionData",
                "ShaderSkinningMethod",
                "RvmSerializedDb_ns_LodFadeInstructionData",
                "RvmSerializedDb_ns_Dx12NvConstantBufferAssemblyInstructionData",
                "RvmSerializedDb_ns_OffsetTranslationInMatrixInstructionData",
                "RvmSerializedDb_ns_WriteOpGroup",
                "RvmSerializedDb_ns_Dx12ShaderState",
                "RvmSerializedDb_ns_Dx12PcShaderDispatchDrawInstructionData",
                "RvmLevelOfDetail",
                "RvmSerializedDb_ns_InstanceTableAssemblyData",
                "RvmLegacyOutdoorLightStatus",
                "RvmSerializedDb_ns_BaseShaderState",
                "RvmSerializedDb_ns_Dx12PcSamplerPointer",
                "RvmSerializedDb_ns_InstanceTableAssemblyInstructionBatchData",
                "RvmSerializedDb_ns_DefaultValueZeroMem",
                "RvmSerializedDb_ns_RvmFunction",
                "RvmSerializedDb_ns_InstructionBatch",
                "RvmSerializedDb_ns_RvmPermutation",
                "RvmSerializedDb_ns_ParamDbSerializedReadView",
                "RvmSerializedDb_ns_DefaultValueSimpleBuffer",
                "RvmSerializedDb_ns_RvmPermutationLookupTable",
                "RvmSerializedDb_ns_DefaultValueRef",
                "RvmSerializedDb_ns_SerializedParameterBlockRef",
                "RvmSerializedDb_ns_Dx12PcSamplerTableWriterInstructionData",
                "RvmSerializedDb_ns_Dx12NvDescriptorTableAssemblyInstructionData",
                "RvmSerializedDb_ns_Dx12Shader",
                "RvmSerializedDb_ns_SurfaceShader",
                "RvmSerializedDb_ns_DirectInputInstructionData",
                "RvmSerializedDb_ns_SerializedParameterBlock",
                "RvmSerializedDb_ns_ShaderStreamableExternalTexture",
                "RvmSerializedDb_ns_InstructionBatchRef",
                "Int64",
                "RvmSerializedDb_ns_RvmFunctionInputTableIndices",
                "ShaderRenderMode",
                "RvmSerializedDb_ns_Dx12PcDispatchInstructionData",
                "RvmSerializedDb_ns_TableAssemblyInstructionBatchData",
                "Int32",
                "RvmSerializedDb_ns_RttiType",
                "RvmSerializedDb_ns_RvmFunctionInstanceRef",
                "RvmSerializedDb_ns_RvmDispatch",
                "Float32",
                "RvmLegacyInstructions_ns_LegacyInstructionData",
                "PrimitiveType",
                "RvmSerializedDb_ns_ParamDbKeyRef",
                "RvmIndexBufferFormat",
                "RvmSerializedDb_ns_ShaderStreamableExternalTextureRef",
                "RvmSerializedDb_ns_Settings",
                "RvmSlotHandle",
                "RvmSerializedDb_ns_Dx12NvLegacyDrawStateBuilderInstructionBatchData",
                "RvmSerializedDb_ns_DepthBiasGroupData",
                "RvmSerializedDb_ns_RvmFunctionInstance",
                "ParamDbHash",
                "RvmSerializedDb_ns_RvmPermutationRef",
                "RvmSerializedDb_ns_ParamDbSerializedHashView",
                "RenderDepthMode",
                "RvmSerializedDb_ns_RuntimeInstantiatedType",
                "RvmSerializedDb_ns_RvmPermutationSet",
                "RvmSerializedDb_ns_ValueRef",
                "RvmSerializedDb_ns_Dx12VertexBufferViewInstructionData"
            };

            Dictionary<string, List<string>> indexOrderDict = new Dictionary<string, List<string>>()
            {
                { "_dx11rvmdatabase", dx11Order },
                { "_dx11nvrvmdatabase", nvdx11Order },
                //{ "_dx12pcrvmdatabase", pcdx12Order },
                //{ "_dx12nvrvmdatabase", nvdx12Order }
            };

            public override RelayCommand MenuItemClicked => new RelayCommand((o) =>
            {
                FrostyTaskWindow.Show("Opening RVMs", "", (task) =>
                {

                    foreach (ResAssetEntry resEntry in App.AssetManager.EnumerateRes())
                    {
                        if (resEntry.Name.Contains("rvmdatabase_dx11") && resEntry.Name.Contains("gameplay/bundles/sharedbundles/frontend+mp/abilities/sharedbundleabilities_frontend+mp/rvmdatabase_dx11"))
                        {
                            App.Logger.Log("Checking: " + resEntry.Name);
                            RvmDatabase rvmDatabase = new RvmDatabase(resEntry, RvmReadingType.BodyNoRefs, task);
                            int idx = 0;
                            //rvmDatabase.Types.ForEach(type => type.Index = (ulong)idx++);
                            //ulong val1 = rvmDatabase.Types[0].Index;
                            //ulong val2 = rvmDatabase.Types[1].Index;
                            //rvmDatabase.Types[0].Index = val2;
                            //rvmDatabase.Types[1].Index = val1;
                            //rvmDatabase.MaybeHash = rvmDatabase.MaybeHash - 1000;
                            //rvmDatabase.Hash = ulong.MaxValue;
                            //RvmDataContainer charContainer = rvmDatabase.DataContainer.First(data => data.TypeName == "CharRvm");
                            //RvmArray newArray = new RvmArray();
                            //newArray.Array = "MOPHEAD".Select(cha => (RvmData)(new CharRvm() { originalBytes = System.Text.Encoding.UTF8.GetBytes(new char[] { cha })})).ToList();
                            //newArray.Array.Add((RvmData)(new CharRvm() { originalBytes = new byte[] { 0x00 } }));
                            //charContainer.Data.Add(newArray);

                            //List<string> newIndexOrder = new List<string>(File.ReadAllLines(@"E:\C#\Frosty\rvm\Tests\TypesToImport.txt"));
                            //foreach (var indexType in rvmDatabase.TypesIndexOrdered )
                            //{
                            //    if (!newIndexOrder.Contains(indexType.Name))
                            //    {
                            //        newIndexOrder.Add(indexType.Name);
                            //    }
                            //}

                            bool isModified = false;
                            int changeCount = 0;
                            foreach (var indexPair in indexOrderDict)
                            {
                                if (resEntry.Name.Contains(indexPair.Key))
                                {
                                    Dictionary<string, RvmTypeData> typeToTypeData = rvmDatabase.Types.ToDictionary(pair => pair.Name);
                                    int typeIdx = 0;
                                    foreach (string type in indexPair.Value)
                                    {
                                        if (typeToTypeData.ContainsKey(type))
                                        {
                                            if (typeToTypeData[type].Index != (ushort)typeIdx)
                                            {
                                                typeToTypeData[type].Index = (ushort)typeIdx;
                                                isModified = true;
                                                changeCount++;
                                            }
                                            typeIdx++;
                                        }
                                    }
                                    if (typeIdx != rvmDatabase.Types.Count())
                                        throw new Exception();
                                }
                            }
                            //App.Logger.Log($"Modified:{resEntry.Name}\t{changeCount} changes");

                            //if (resEntry.Name.Contains("_dx11rvmdatabase"))
                            //{
                            //    foreach (string file in Directory.GetFiles(@"E:\C#\Frosty\rvm\Dx11rvmShaders\"))
                            //    {
                            //        rvmDatabase.ImportShader(file);
                            //    }
                            //    //rvmDatabase.ImportShader(@"E:\C#\Frosty\rvm\Dx11rvmShaders\SS_EndorHologram_01_3785612692.bin");
                            //}
                            //else if (resEntry.Name.Contains("_dx11nvrvmdatabase"))
                            //{
                            //    foreach (string file in Directory.GetFiles(@"E:\C#\Frosty\rvm\Dx11nvrvmShaders\"))
                            //    {
                            //        rvmDatabase.ImportShader(file);
                            //    }
                            //    //rvmDatabase.ImportShader(@"E:\C#\Frosty\rvm\Dx11nvrvmShaders\SS_EndorHologram_01_3785612692.bin");
                            //}

                            //if (resEntry.Name.Contains("_dx11rvmdatabase"))
                            //    rvmDatabase.WriteToFile(@"E:\C#\Frosty\rvm\Comparison\mod-bes-rvmdatabase_dx11rvmdatabase.res", task);
                            if (isModified)
                            {
                                rvmDatabase.WriteToRes(task);
                                App.Logger.Log($"Modified:{resEntry.Name}\t{changeCount} changes");
                            }
                        }
                    }
                });
            });
        }

        public class RvmViewer : FrostyBaseEditor
        {
            private const string PART_RvmEdtiorViewer = "PART_RvmEdtiorViewer";
            private FrostyPropertyGrid RvmEditorPropertyGrid;
            private RvmDatabase rvmDatabase;
            static RvmViewer()
            {
                DefaultStyleKeyProperty.OverrideMetadata(typeof(RvmViewer), new FrameworkPropertyMetadata(typeof(RvmViewer)));
            }
            public RvmViewer(RvmDatabase inputDatabase)
            {
                rvmDatabase = inputDatabase;
            }
            public override void OnApplyTemplate()
            {
                base.OnApplyTemplate();
                RvmEditorPropertyGrid = GetTemplateChild(PART_RvmEdtiorViewer) as FrostyPropertyGrid;
                RvmEditorPropertyGrid.SetClass(rvmDatabase);
            }
        }
        [EbxClassMeta(EbxFieldType.Struct)]

        public enum RvmReadingType
        {
            HeaderOnly,
            BodyNoRefs,
            BodyWithRefs,
            DebugOnlyIdentifyRefs,
        }


        static List<string> RvmTypeOrder = new List<string>()
        {
            "RvmSerializedDb_ns_Dx11SerializedBlendState",
            "Vec",
            "RvmLegacyLightMapInstance",
            "RvmSerializedDb_ns_Dx11VsShader",
            "RvmSerializedDb_ns_SerializedParamDbKey",
            "RvmSerializedDb_ns_Dx12BinaryBlob",
            "ProjectionState",
            "StencilState",
            "RvmSerializedDb_ns_Dx11Sampler",
            "RvmLegacyLightProbes",
            "ViewState",
            "RvmSerializedDb_ns_Dx12PcSampler",
            "RvmSerializedDb_ns_DefaultValueRef",
            "RvmSerializedDb_ns_InstructionBatchRef",
            "RvmSerializedDb_ns_Dx11ApplyParametersBlock",
            "RvmSerializedDb_ns_RvmPermutation",
            "Int64",
            "RvmSerializedDb_ns_InstructionBatch",
            "RvmSerializedDb_ns_RvmFunction",
            "RvmSerializedDb_ns_Dx12LegacyDrawStateBuilderInstructionBatchData",
            "RvmSerializedDb_ns_InstanceTableAssemblyInstructionBatchData",
            "RvmSerializedDb_ns_Dx12Shader",
            "RvmSerializedDb_ns_SurfaceShader",
            "RvmSerializedDb_ns_Dx11LegacyVertexBufferConversionInstructionData",
            "RvmSerializedDb_ns_SerializedParameterBlock",
            "RvmSerializedDb_ns_RvmFunctionInputTableIndices",
            "RvmSerializedDb_ns_Dx11PsShader",
            "RvmSerializedDb_ns_Dx12PcSamplerTableWriterInstructionData",
            "RvmSerializedDb_ns_Dx11HsShader",
            "RvmSerializedDb_ns_Dx12NvDescriptorTableAssemblyInstructionData",
            "RvmSerializedDb_ns_SerializedParameterBlockRef",
            "RvmSerializedDb_ns_Dx11BlendStateData",
            "RvmSerializedDb_ns_Dx12PcRvmDescriptorTableAssemblyInstructionData",
            "RvmSerializedDb_ns_Dx12PcSamplerPointer",
            "RvmSerializedDb_ns_DirectInputInstructionData",
            "RvmSerializedDb_ns_Dx11TextureConversionInstructionData",
            "RvmSerializedDb_ns_Dx12LegacyDrawStateBuilderInstructionData",
            "RvmSerializedDb_ns_Dx11DsShader",
            "RvmSerializedDb_ns_ValueRef",
            "RvmSerializedDb_ns_RvmPermutationRef",
            "RvmSerializedDb_ns_RvmPermutationSet",
            "RvmSerializedDb_ns_Dx12PcRootSignature",
            "RvmSerializedDb_ns_Dx12VertexBufferViewInstructionData",
            "RvmSerializedDb_ns_ShaderStreamableTextureRef",
            "RvmSerializedDb_ns_Dx12InputElement",
            "RvmSerializedDb_ns_Dx12NvLegacyDrawStateBuilderInstructionBatchData",
            "RvmSerializedDb_ns_Dx12PcPSOPreloadOp",
            "RvmSerializedDb_ns_ParamDbSerializedHashView",
            "RvmSerializedDb_ns_RvmDispatch",
            "RvmSerializedDb_ns_RvmFunctionInstance",
            "RvmSerializedDb_ns_ParamDbSerializedHashViewRef",
            "RvmSerializedDb_ns_Dx12PcDispatchInstructionData",
            "RvmSerializedDb_ns_ParamDbSerializedFilterView",
            "RvmSerializedDb_ns_RuntimeInstantiatedType",
            "RvmSerializedDb_ns_Dx11InputElement",
            "RvmSerializedDb_ns_Dx12NvLegacyDrawStateBuilderInstructionData",
            "RvmSerializedDb_ns_Dx12RootDescriptorTableAssemblyInstructionData",
            "RvmSerializedDb_ns_CombinedSerializedParameterBlock",
            "RvmSerializedDb_ns_ParamDbSerializedReadView",
            "RvmSerializedDb_ns_Dx11ApplyParametersInstructionData",
            "RvmSerializedDb_ns_RvmFunctionInstanceRef",
            "ParamDbHash",
            "RvmSerializedDb_ns_Dx11BufferConversionInstructionData",
            "RvmSerializedDb_ns_Settings",
            "RvmSerializedDb_ns_ShaderStreamableExternalTextureRef",
            "RvmSerializedDb_ns_Dx11DispatchInstructionData",
            "RvmSerializedDb_ns_ParamDbKeyRef",
            "RvmSerializedDb_ns_RttiType",
            "RvmSerializedDb_ns_TableAssemblyInstructionBatchData",
            "RvmSerializedDb_ns_RvmPermutationLookupTable",
            "Int32",
            "ShaderDepthBiasGroup",
            "RvmSerializedDb_ns_ShaderStreamableExternalTexture",
            "RenderDepthMode",
            "RvmLegacyOutdoorLightStatus",
            "RvmLevelOfDetail",
            "RvmSerializedDb_ns_Dx12NvConstantBufferAssemblyInstructionData",
            "RvmSerializedDb_ns_ShaderStreamableTexture",
            "RvmSerializedDb_ns_InstanceTableAssemblyData",
            "NvShadowMapRenderType",
            "Float32",
            "ShaderInstancingMethod",
            "ShaderRenderMode",
            "PrimitiveType",
            "ShaderSkinningMethod",
            "Uint32",
            "ShaderGeometrySpace",
            "RvmSerializedDb_ns_TableAssemblyData",
            "RvmSerializedDb_ns_DepthBiasGroupData",
            "RvmIndexBufferFormat",
            "QualityLevel",
            "RvmSerializedDb_ns_PackLightMapWeightIntoInstanceInstructionData",
            "RvmSerializedDb_ns_Dx11ViewStateInstructionData",
            "RvmSerializedDb_ns_Dx12ViewStateInstructionData",
            "RvmSerializedDb_ns_Dx12NvDescriptorTable",
            "RvmLegacyInstructions_ns_LegacyInstructionData",
            "RvmSerializedDb_ns_WriteOp",
            "RvmSerializedDb_ns_CpuToGpuMatrixInstructionData",
            "RvmSerializedDb_ns_LodFadeInstructionData",
            "RvmSerializedDb_ns_Dx11ShaderDispatchDrawInstructionData",
            "RvmSerializedDb_ns_DefaultValueSimpleTexture",
            "RvmSerializedDb_ns_PreparedVertexStream",
            "RvmSerializedDb_ns_Dx11NvViewStateInstructionData",
            "RvmSerializedDb_ns_Dx11NvViewStateDepthInstructionData",
            "RvmContextSortKeyInfo",
            "RvmSerializedDb_ns_Dx12RootWriteOp",
            "RvmSerializedDb_ns_OffsetTranslationInMatrixInstructionData",
            "RvmSlotHandle",
            "RvmSerializedDb_ns_VectorSubtractInstructionData",
            "RvmSerializedDb_ns_TessellationParametersInstructionData",
            "Uint16",
            "RvmSerializedDb_ns_DefaultValueZeroMem",
            "RvmSerializedDb_ns_Dx11LegacyDrawStateBuilderData",
            "RvmSerializedDb_ns_Dx12PcShaderDispatchDrawInstructionData",
            "RvmSerializedDb_ns_DefaultValueSimpleBuffer",
            "RvmSerializedDb_ns_Dx11NvDrawStateInstructionData",
            "RvmSerializedDb_ns_SliceCountInstructionData",
            "Uint8",
            "RvmSerializedDb_ns_DefaultValueStructLegacyData",
            "RvmSerializedDb_ns_Dx11ByteCodeElement",
            "RvmSerializedDb_ns_WriteOpGroup",
            "CharRvm",
            "RvmSerializedDb_ns_BaseShaderState",
            "Boolean",
            "RvmSerializedDb_ns_Dx12ShaderState",
        };
        public class RvmDatabase
        {

            #region Meta

            [Category("Res Meta")]
            [EbxFieldMeta(EbxFieldType.UInt32)]
            public uint MaybeVersion { get; set; }

            [Category("Res Meta")]
            [EbxFieldMeta(EbxFieldType.UInt32)]
            public uint HeaderOffset { get; set; }

            [Category("Res Meta")]
            [EbxFieldMeta(EbxFieldType.UInt32)]
            public uint HeaderSize { get; set; }

            [Category("Res Meta")]
            [EbxFieldMeta(EbxFieldType.UInt32)]
            public uint MaybeHash { get; set; }

            #endregion

            #region Header

            [Category("Header")]
            [EbxFieldMeta(EbxFieldType.String)]
            public string Name { get; set; }

            [Category("Header")]
            [EbxFieldMeta(EbxFieldType.Guid)]
            public Guid Guid { get; set; }

            [Category("Header")]
            [EbxFieldMeta(EbxFieldType.UInt64)]
            public UInt64 Hash { get; set; }

            [Category("Header")]
            [EbxFieldMeta(EbxFieldType.UInt32)]
            public uint Unk01 { get; set; }

            [Category("Header")]
            [EbxFieldMeta(EbxFieldType.UInt32)]
            public uint Count { get; set; }

            [Category("Header")]
            [EbxFieldMeta(EbxFieldType.UInt32)]
            public uint Unk03 { get; set; }

            [Category("Header")]
            [EbxFieldMeta(EbxFieldType.UInt16)]
            public UInt16 Unk04 { get; set; }

            [Category("Header")]
            [EbxFieldMeta(EbxFieldType.UInt16)]
            public UInt16 TypeCount { get; set; }

            [Category("Header")]
            [EbxFieldMeta(EbxFieldType.Array)]
            public List<RvmTypeData> Types { get; set; }

            [Category("Header")]
            [EbxFieldMeta(EbxFieldType.Array)]
            public List<RvmTypeData> TypesIndexOrdered { get; set; }

            [Category("Header")]
            [EbxFieldMeta(EbxFieldType.UInt64)]
            public UInt64 Unk06 { get; set; }

            [Category("Header")]
            [EbxFieldMeta(EbxFieldType.Array)]
            public List<UInt16> Counts { get; set; }

            #endregion

            #region RVM Data

            [Category("Rvm Database")]
            [EbxFieldMeta(EbxFieldType.Array)]
            public List<RvmDataContainer> DataContainer { get; set; }


            //[Category("Rvm Database")]
            //[EbxFieldMeta(EbxFieldType.Array)]
            //public List<List<RvmData>> ParentArrays { get; set; }
            [Category("Rvm Database")]
            [EbxFieldMeta(EbxFieldType.Array)]
            public List<RvmData> ParentData { get; set; }

            #endregion

            public RvmDatabase()
            {
            }
            public RvmDatabase(ResAssetEntry resAssetEntry, RvmReadingType readingType = RvmReadingType.BodyWithRefs, FrostyTaskWindow task = null)
            {
                Name = resAssetEntry.Name;
                using (NativeReader reader = new NativeReader(new MemoryStream(resAssetEntry.HasModifiedData ? resAssetEntry.ModifiedEntry.ResMeta : resAssetEntry.ResMeta)))
                    ReadHeader(reader);
                using (NativeReader reader = new NativeReader(App.AssetManager.GetRes(resAssetEntry))) 
                    ReadData(reader, readingType, task:task);
                //using (NativeReader reader = new NativeReader(App.AssetManager.GetRes(resAssetEntry)))
                //    ReadChecksums(reader, readingType, task: task);
            }
            public RvmDatabase(string fileName, RvmReadingType readingType = RvmReadingType.BodyWithRefs, FrostyTaskWindow task = null)
            {
                Name = Path.GetFileNameWithoutExtension(fileName);
                using (NativeReader reader = new NativeReader(new FileStream(fileName, FileMode.Open)))
                {
                    ReadHeader(reader);
                    //ReadChecksums(reader, readingType, 0x10, task: task);
                    ReadData(reader, readingType, 0x10, task: task);
                }
            }
            public void ReadHeader(NativeReader reader)
            {
                MaybeVersion = reader.ReadUInt();
                HeaderOffset = reader.ReadUInt();
                HeaderSize = reader.ReadUInt();
                MaybeHash = reader.ReadUInt();
            }
            public void ReadChecksums(NativeReader reader, RvmReadingType readingType, int startIndex = 0, FrostyTaskWindow task = null)
            {
                uint typeHash = (uint)Utils.HashString("Dx11NvRvmDatabase");
                ulong hashToFind = (Hash & ((1UL << 47) - 1));
                bool BruteTryFindHash(int minValue, int maxValue)
                {
                    int idx = 0;
                    for (int i = minValue; i < maxValue; i++)
                    {
                        task.Update(progress: (float)(i-minValue) / (maxValue-minValue) * 100);
                        for (int y = i; y < maxValue; y++)
                        {
                            idx++;
                            reader.BaseStream.Position = i;
                            byte[] array = reader.ReadBytes(y - i);
                            ulong hashGuess = (CityHash.Hash64(array) & ((1UL << 47) - 1));
                            if (hashGuess == hashToFind)
                            {
                                App.Logger.Log("Found " + hashGuess.ToString());
                                return true;
                            }
                            hashGuess = (CityHash.HashWithSeed64(array, typeHash) & ((1UL << 47) - 1));
                            if (hashGuess == hashToFind)
                            {
                                App.Logger.Log("Found " + hashGuess.ToString());
                                return true;
                            }
                        }
                    }
                    App.Logger.Log($"Made {idx} guesses");
                    return false;
                }
                //bool StrategicTryFindHash()
                //{
                //    //int idx = 0;
                //    //int maxCount = tempStratPos.Count();
                //    //for (int i = 0; i < maxCount; i++)
                //    //{
                //    //    task.Update(progress: (float)(i) / (maxCount) * 100);
                //    //    for (int y = i; y < maxCount; y++)
                //    //    {
                //    //        idx++;
                //    //        reader.BaseStream.Position = tempStratPos[i];
                //    //        int readSize = (int)(tempStratPos[y] - tempStratPos[i]);
                //    //        byte[] array = reader.ReadBytes(readSize);
                //    //        ulong hashGuess = (CityHash.Hash64(array) & ((1UL << 47) - 1));
                //    //        if (hashGuess == hashToFind)
                //    //        {
                //    //            App.Logger.Log("Found " + hashGuess.ToString());
                //    //            return true;
                //    //        }
                //    //        hashGuess = (CityHash.HashWithSeed64(array, typeHash) & ((1UL << 47) - 1));
                //    //        if (hashGuess == hashToFind)
                //    //        {
                //    //            App.Logger.Log("Found " + hashGuess.ToString());
                //    //            return true;
                //    //        }
                //    //    }
                //    //}
                //    //App.Logger.Log($"Made {idx} guesses");
                //    //return false;
                //}

                App.Logger.Log((Hash & ((1UL << 47) - 1)).ToString());
                void CheckChecksum(int startPos, int size)
                {
                    reader.BaseStream.Position = startPos;
                    byte[] bytes = reader.ReadBytes(size);
                    App.Logger.Log((CityHash.Hash64(bytes) & ((1UL << 47) - 1)).ToString());
                    foreach (uint key in RvmStaticVariables.rvmSizes.Keys)
                    {
                        ulong hashGuess = (CityHash.HashWithSeed64(bytes, key) & ((1UL << 47) - 1));
                        if (hashGuess == hashToFind)
                            throw new Exception();
                    }
                }
                CheckChecksum(startIndex, (int)reader.BaseStream.Length);
                CheckChecksum((int)HeaderOffset, (int)HeaderSize);
                CheckChecksum(startIndex, (int)HeaderOffset);
                CheckChecksum((int)temp1, (int)(temp1Max - temp1));
                CheckChecksum((int)temp2, (int)(temp2Max - temp2));

                CheckChecksum((int)temp1, (int)(temp1Max - temp1));

                if (!BruteTryFindHash((int)HeaderOffset, (int)(HeaderSize + HeaderOffset)))
                    App.Logger.Log("Could not brute force hash");

                //if (!StrategicTryFindHash())
                //    App.Logger.Log("Could not strat hash");
            }
            long temp1 { get; set; }
            long temp1Max { get; set; }
            long temp2 { get; set; }
            long temp2Max { get; set; }
            public void ReadData(NativeReader reader,RvmReadingType readingType, int startIndex = 0, FrostyTaskWindow task = null)
            {
                #region Header
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                Dictionary<uint, Type> types = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(RvmData))).ToDictionary(type => (uint)Utils.HashString(type.Name), type => type);
                types.Add((uint)Utils.HashString("char"), typeof(CharRvm));
                reader.BaseStream.Position = HeaderOffset + startIndex;
                Guid = reader.ReadGuid();
                Hash = reader.ReadULong();
                ulong unknownHash = Hash & ((1UL << 47) - 1);
                Unk01 = reader.ReadUInt();
                Count = reader.ReadUInt();
                Unk03 = reader.ReadUInt();
                Unk04 = reader.ReadUShort();
                TypeCount = reader.ReadUShort();
                Types = new List<RvmTypeData>();
                temp1 = reader.BaseStream.Position;
                for (int i = 0; i < TypeCount; i++)
                    Types.Add(new RvmTypeData(reader, types));
                temp1Max = reader.BaseStream.Position;
                TypesIndexOrdered = Types.OrderBy(type => type.Index).ToList();
                //Types = Types.OrderBy(type => type.Index).ToList();

                Unk06 = reader.ReadULong();
                Counts = new List<ushort>();

                for (int i = 0; i < Count; i++)
                    Counts.Add(reader.ReadUShort());
                //App.Logger.Log(types.Count().ToString());
                stopwatch.Stop();
                //App.Logger.Log($"Read Header: {stopwatch.ElapsedMilliseconds} ms");
                if (readingType == RvmReadingType.HeaderOnly)
                    return;

                #endregion

                #region Body Reading
                stopwatch.Restart();

                reader.BaseStream.Position = startIndex;
                DataContainer = new List<RvmDataContainer>();
                RvmCountManager rvmCountManager = new RvmCountManager(Counts);
                foreach (var type in Types)
                {
                    //DataContainer.Add(new RvmDataContainer(type.NameHash.ToString(), (int)type.Count, null, reader, rvmCountManager, type.NameHash));
                    Type rvmType = types.ContainsKey(type.NameHash) ? types[type.NameHash] : typeof(UnknownRvmType);
                    DataContainer.Add(new RvmDataContainer(type.Name, (int)type.Count, rvmType, reader, rvmCountManager, type.NameHash));
                }

                stopwatch.Stop();
                //App.Logger.Log($"Read Body: {stopwatch.ElapsedMilliseconds} ms");
                if (readingType == RvmReadingType.BodyNoRefs    )
                    return;

                #endregion


                #region Setting References
                stopwatch.Restart(); 
                bool bPrintMissingRefs = readingType == RvmReadingType.DebugOnlyIdentifyRefs;

                //Dictionary<ulong, (ulong, ulong, uint, string, int, int)> tempCityHashReadValues = new Dictionary<ulong, (ulong, ulong, uint, string, int, int)>();

                Dictionary<ulong, RvmArray> cityHashDict = new Dictionary<ulong, RvmArray>();
                foreach (RvmDataContainer dataContainer in DataContainer)
                {
                    for (int i = 0; i < dataContainer.Data.Count; i++)
                    {
                        cityHashDict.Add(dataContainer.CityHashes[i] & ((1UL << 47) - 1), dataContainer.Data[i]);
                        //tempCityHashReadValues.Add(dataContainer.CityHashes[i] & ((1UL << 47) - 1), (dataContainer.TempOriginalCityHashes[i], 0, dataContainer.typeHash, dataContainer.TypeName, dataContainer.Data[i].Array.Count(), dataContainer.Data.Count()));
                    }
                }
                HashSet<string> errorsToReport = new HashSet<string>();
                int containerIdx = 0;
                int containerMax = DataContainer.Count;
                Dictionary<Guid, RvmSerializedDb_ns_RvmPermutationSet> permutationSets = new Dictionary<Guid, RvmSerializedDb_ns_RvmPermutationSet>();
                foreach (RvmDataContainer dataContainer in DataContainer)
                {
                    task.Update(progress: (float)containerIdx++ / containerMax * 100);
                    foreach (RvmArray rvmArray in dataContainer.Data)
                    {
                        foreach (RvmData rvmData in rvmArray.Array)
                        {
                            string rvmDataTypeName = rvmData.GetType().Name;
                            if (rvmDataTypeName == "UnknownRvmType")
                                rvmDataTypeName = ((UnknownRvmType)rvmData).TypeRealName;
                            if (rvmDataTypeName == "RvmSerializedDb_ns_RvmPermutationSet")
                            {
                                rvmArray.IsChild = true;
                                permutationSets.Add(((RvmSerializedDb_ns_RvmPermutationSet)rvmData).Guid, (RvmSerializedDb_ns_RvmPermutationSet)rvmData);
                            }

                            //if (rvmDataTypeName != "RvmSerializedDb_ns_RvmPermutationSet")
                            //    continue;
                            var fields = rvmData.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);

                            List<ulong> foundRefs = new List<ulong>();

                            foreach (var field in fields)
                            {
                                // Check if the field is of type RvmReference
                                if (field.PropertyType == typeof(RvmReference))
                                {
                                    ulong refHash = ((RvmReference)field.GetValue(rvmData)).referenceHash & ((1UL << 47) - 1);
                                    //ulong origHash = ((RvmReference)field.GetValue(rvmData)).referenceHash;
                                    if (!cityHashDict.ContainsKey(refHash) && refHash != 0)
                                        errorsToReport.Add($"Warning: Recognised Ref is a Float: {rvmDataTypeName}\t{field.Name}: {refHash}\t{cityHashDict.ContainsKey(refHash)}");
                                    else if (cityHashDict.ContainsKey(refHash))
                                    {
                                        ((RvmReference)field.GetValue(rvmData)).ReferenceArray = cityHashDict[refHash].Array;
                                            foundRefs.Add(refHash);
                                        cityHashDict[refHash].IsChild = true;
                                        //if (tempCityHashReadValues[refHash].Item2 == 0)
                                        //    tempCityHashReadValues[refHash] = (tempCityHashReadValues[refHash].Item1, origHash, tempCityHashReadValues[refHash].Item3, tempCityHashReadValues[refHash].Item4, tempCityHashReadValues[refHash].Item5, tempCityHashReadValues[refHash].Item6);
                                        //else if (tempCityHashReadValues[refHash].Item2 != origHash)
                                        //    throw new Exception($"UNRECOGNISED REFERENCE: {rvmDataTypeName}\t{field.Name}: {refHash}\t{tempCityHashReadValues[refHash].Item1}\t{tempCityHashReadValues[refHash].Item2}");
                                    }
                                }
                                else if (field.PropertyType.IsGenericType && field.PropertyType.GetGenericTypeDefinition() == typeof(List<>) && field.PropertyType.GetGenericArguments()[0] == typeof(RvmReference))
                                {
                                    List<RvmReference> rvmReferences = ((List<RvmReference>)field.GetValue(rvmData));
                                    foreach(RvmReference rvmRef in rvmReferences)
                                    {
                                        ulong refHash = rvmRef.referenceHash & ((1UL << 47) - 1);
                                        if (!cityHashDict.ContainsKey(refHash) && refHash != 0)
                                            errorsToReport.Add($"Warning: Recognised Ref is a Float: {rvmDataTypeName}\t{field.Name}: {refHash}\t{cityHashDict.ContainsKey(refHash)}");
                                        else if (cityHashDict.ContainsKey(refHash))
                                        {
                                            rvmRef.ReferenceArray = cityHashDict[refHash].Array;
                                            foundRefs.Add(refHash);
                                            cityHashDict[refHash].IsChild = true;
                                        }
                                    }
                                }
                                else if (field.PropertyType == typeof(ulong) && bPrintMissingRefs)
                                {
                                    ulong refHash = (ulong)field.GetValue(rvmData) & ((1UL << 47) - 1);
                                    if (cityHashDict.ContainsKey(refHash) && refHash != 0 && field.Name != "cityHash")
                                        errorsToReport.Add($"Warning: Unrecognised Float is a Ref: {rvmDataTypeName}\t{field.Name}");
                                }
                            }


                            //Check all refs have been discovered
                            if (bPrintMissingRefs) //rvmDataTypeName != "RvmSerializedDb_ns_RvmPermutationSet"
                            {
                                List<ulong> possiblyUnfoundRefs = new List<ulong>();
                                Dictionary<ulong, List<int>> unfoundLocations = new Dictionary<ulong, List<int>>();
                                using (NativeReader rvmReader = new NativeReader(new MemoryStream(rvmData.originalBytes)))
                                {
                                    for (int i = 0; rvmData.originalBytes.Length + 16> i; i++)
                                    {
                                        rvmReader.BaseStream.Position = i;
                                        ulong refHash = rvmReader.ReadULong() & ((1UL << 47) - 1);
                                        if (cityHashDict.ContainsKey(refHash))
                                        {
                                            possiblyUnfoundRefs.Add(refHash);
                                            if (!unfoundLocations.ContainsKey(refHash))
                                                unfoundLocations.Add(refHash, new List<int>());
                                            unfoundLocations[refHash].Add(i);
                                        }
                                    }
                                };

                                foreach (ulong refHash in foundRefs.Union(possiblyUnfoundRefs).Distinct())
                                {
                                    int foundCount = foundRefs.Count(r => r == refHash);
                                    int possiblyUnfoundCount = possiblyUnfoundRefs.Count(r => r == refHash);
                                    if (foundCount != possiblyUnfoundCount)
                                    {
                                        foreach(int location in unfoundLocations[refHash])
                                            errorsToReport.Add($"Unrecognised Reference Somewhere: {rvmDataTypeName}:\t 0x{location.ToString("X")}\t(Total: 0x{rvmData.expectedSize.ToString("X")})");
                                        //App.Logger.LogError($"UNRECOGNISED REFERENCE SOMEWHERE: {rvmDataTypeName}:\t {refHash}\t{foundCount}\t{possiblyUnfoundCount}");
                                    }
                                }
                            }
                        }
                    }
                }
                if (bPrintMissingRefs)
                    App.Logger.LogError("Errors:\n" + string.Join("\n", errorsToReport.OrderByDescending(a => a)));

                //ParentArrays = new List<List<RvmData>>();
                //foreach (RvmDataContainer dataContainer in DataContainer)
                //{
                //    foreach (RvmArray rvmArray in dataContainer.Data.Where(array => !array.IsChild))
                //        ParentArrays.Add(rvmArray.Array);
                //}

                ParentData = new List<RvmData>();
                foreach (RvmDataContainer dataContainer in DataContainer)
                {
                    foreach (RvmArray rvmArray in dataContainer.Data.Where(array => !array.IsChild))
                    {
                        if (rvmArray.Array.Count > 1)
                        {
                            App.Logger.LogError("MAJOR READING ERROR");
                            return;
                        }
                        ParentData.Add(rvmArray.Array[0]);
                        if (rvmArray.Array[0].GetType() == typeof(RvmSerializedDb_ns_SurfaceShader))
                        {
                            RvmSerializedDb_ns_SurfaceShader shader = (RvmSerializedDb_ns_SurfaceShader)rvmArray.Array[0];
                            shader.PermutationSet = permutationSets[shader.Guid];
                        }
                    }
                }

                stopwatch.Stop();
                App.Logger.Log($"Aligned References: {stopwatch.ElapsedMilliseconds} ms");

                #endregion

                //string filePath = @"E:\C#\Frosty\FrostyToolSuite\FrostyEditor\bin\Developer\Debug\Data\RvmCityHashComparison.csv";
                //using (StreamWriter writer = new StreamWriter(filePath))
                //{
                //    writer.WriteLine("Key,RealCityHash,ReadCityHash,TypeHash,TypeName,ArrayLength,TypeCount"); // CSV header

                //    foreach (var kvp in tempCityHashReadValues.Where(kvp => kvp.Value.Item2 != 0))
                //    {
                //        writer.WriteLine($"{kvp.Key},{kvp.Value.Item1},{kvp.Value.Item2},{kvp.Value.Item3},{kvp.Value.Item4},{kvp.Value.Item5},{kvp.Value.Item6}");
                //    }
                //}
                Debug.Assert(reader.BaseStream.Position == HeaderOffset + startIndex);
            }

            public void WriteToRes(FrostyTaskWindow task = null, string writeToAsset = null)
            {
                if (task != null)
                    task.Update($"Writing { (writeToAsset == null ? Name : writeToAsset)}");
                    MemoryStream memoryStream = new MemoryStream();
                using (NativeWriter writer = new NativeWriter(memoryStream))
                {
                    WriteData(writer, task);
                }
                MemoryStream metaMemoryStream = new MemoryStream();
                using (NativeWriter writer = new NativeWriter(metaMemoryStream))
                {
                    writer.Write(MaybeVersion);
                    writer.Write(HeaderOffset);
                    writer.Write(HeaderSize);
                    writer.Write(MaybeHash);
                }
                
                App.AssetManager.ModifyRes(writeToAsset == null ? Name : writeToAsset, memoryStream.ToArray(), metaMemoryStream.ToArray());
            }

            public void WriteToFile(string fileName, FrostyTaskWindow task = null)
            {
                MemoryStream bodyMemoryStream = new MemoryStream();
                using (NativeWriter writer = new NativeWriter(bodyMemoryStream))
                {
                    WriteData(writer, task);
                }
                MemoryStream metaMemoryStream = new MemoryStream();
                using (NativeWriter writer = new NativeWriter(metaMemoryStream))
                {
                    writer.Write(MaybeVersion);
                    writer.Write(HeaderOffset);
                    writer.Write(HeaderSize);
                    writer.Write(MaybeHash);
                }
                using (NativeWriter writer = new NativeWriter(new FileStream(fileName, FileMode.Create)))
                {
                    writer.Write(metaMemoryStream.ToArray());
                    writer.Write(bodyMemoryStream.ToArray());
                }
            }

            public void WriteData(NativeWriter writer, FrostyTaskWindow task = null)
            {
                Dictionary<string, uint> types = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(RvmData))).ToDictionary(type => type.Name, type => (uint)Utils.HashString(type.Name));
                types["CharRvm"] = (uint)Utils.HashString("char");

                int containerIdx = 0;
                int containerMax = DataContainer.Count;

                MemoryStream typeMemoryStream = new MemoryStream();
                MemoryStream countMemoryStream = new MemoryStream();

                NativeWriter typeStreamWriter = new NativeWriter(typeMemoryStream);
                NativeWriter countStreamWriter = new NativeWriter(countMemoryStream);

                //ulong typeIndex = 0;
                int arrayCount = 0;

                Dictionary<uint, ulong> indexOrderTEMP = Types.ToDictionary(type => type.NameHash, type => type.Index); //NOT CORRECT, NEEDS TO BE FIXED
                foreach (RvmDataContainer dataContainer in DataContainer)
                {
                    string typeName = dataContainer.TypeName;
                    if (!types.ContainsKey(typeName))
                        types.Add(typeName, (uint)Utils.HashString(typeName));

                    //Writing Type Data
                    typeStreamWriter.Write(types[typeName]);
                    typeStreamWriter.Write((uint)dataContainer.Data.Count);
                    // typeStreamWriter.Write(typeIndex++); //NOT CORRECT, NEEDS TO BE FIXED
                    typeStreamWriter.Write(indexOrderTEMP[types[typeName]]); //NOT CORRECT, NEEDS TO BE FIXED


                    foreach (RvmArray rvmArray in dataContainer.Data)
                    {
                        //Writing Count Data
                        arrayCount++;
                        countStreamWriter.Write((ushort)rvmArray.Array.Count);

                        foreach (RvmData rvmData in rvmArray.Array)
                            writer.Write(rvmData.originalBytes);
                    }
                    task.Update(progress: (float)containerIdx++ / containerMax * 100);
                }

                HeaderOffset = (uint)writer.BaseStream.Position;
                writer.Write(Guid);
                writer.Write(Hash);
                writer.Write(Unk01);
                writer.Write(arrayCount);
                writer.Write(Unk03);
                writer.Write(Unk04);
                writer.Write(TypeCount);
                writer.Write(typeMemoryStream.ToArray());
                writer.Write(Unk06);
                writer.Write(countMemoryStream.ToArray());

                typeMemoryStream.Dispose();
                countMemoryStream.Dispose();
                typeStreamWriter.Dispose();
                countStreamWriter.Dispose();
                HeaderSize = (uint)writer.BaseStream.Position - HeaderOffset;
            }


            public void ExtractShaders(string folderName)
            {
                foreach (RvmDataContainer dataContainer in DataContainer)
                {
                    if (dataContainer.TypeName == "RvmSerializedDb_ns_SurfaceShader")
                    {
                        foreach (RvmArray rvmArray in dataContainer.Data)
                        {
                            RvmSerializedDb_ns_SurfaceShader shader = (RvmSerializedDb_ns_SurfaceShader)rvmArray.Array[0];
                            //if (!shader.Name.ToLower().Contains("SS_EndorHologram_01".ToLower()))
                            //    continue;
                            string targFile = $@"{folderName}\{shader.Name.Split('/').Last()}_{shader.NameHash}.bin";
                            if (File.Exists(targFile))
                                continue;
                            using (NativeWriter writer = new NativeWriter(new FileStream(targFile, FileMode.Create)))
                            {
                                WriteShader(writer, shader);
                            }
                        }
                        break;
                    }
                }
            }
            public void WriteShader(NativeWriter writer, RvmSerializedDb_ns_SurfaceShader surfaceShader)
            {
                Dictionary<string, List<ulong>> typesToHahes = new Dictionary<string, List<ulong>>();
                Dictionary<ulong, List<RvmData>> arraysToExtract = new Dictionary<ulong, List<RvmData>>();
                void ExtractFromReference(RvmReference rvmRef)
                {
                    ulong referenceHash = rvmRef.referenceHash & ((1UL << 47) - 1);

                    if (referenceHash != 0 && !arraysToExtract.ContainsKey(referenceHash))
                    {
                        string refType = rvmRef.ReferenceArray[0].GetType().Name;
                        if (typesToHahes.ContainsKey(refType))
                            typesToHahes[refType].Add(referenceHash);
                        else
                            typesToHahes.Add(refType, new List<ulong>() { referenceHash });
                        arraysToExtract.Add(referenceHash, rvmRef.ReferenceArray);
                        foreach (RvmData rvmDataRef in rvmRef.ReferenceArray)
                            FindRvmDataToExtract(rvmDataRef);
                    }
                }
                void FindRvmDataToExtract(RvmData rvmData)
                {
                    var fields = rvmData.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        // Check if the field is of type RvmReference
                        if (field.PropertyType == typeof(RvmReference))
                        {
                            RvmReference rvmRef = ((RvmReference)field.GetValue(rvmData));
                            ExtractFromReference(rvmRef);
                        }
                        else if (field.PropertyType.IsGenericType && field.PropertyType.GetGenericTypeDefinition() == typeof(List<>) && field.PropertyType.GetGenericArguments()[0] == typeof(RvmReference))
                        {
                            List<RvmReference> rvmReferences = ((List<RvmReference>)field.GetValue(rvmData));
                            foreach (RvmReference rvmRef in rvmReferences)
                            {
                                ExtractFromReference(rvmRef);
                            }
                        }
                    }
                }

                FindRvmDataToExtract(surfaceShader);
                FindRvmDataToExtract(surfaceShader.PermutationSet);

                writer.WriteNullTerminatedString("MopMagicRVMShad"); //Magic
                writer.Write(0); //Version


                MemoryStream headerMemoryStream = new MemoryStream();
                MemoryStream bodyMemoryStream = new MemoryStream();

                NativeWriter headerStreamWriter = new NativeWriter(headerMemoryStream);
                NativeWriter bodyStreamWriter = new NativeWriter(bodyMemoryStream);

                int typesToExport = 0;
                foreach(string typeName in RvmTypeOrder)
                {
                    if (!typesToHahes.ContainsKey(typeName))
                        continue;
                    typesToExport++;

                    headerStreamWriter.WriteNullTerminatedString(typeName);
                    headerStreamWriter.Write(arraysToExtract[typesToHahes[typeName][0]][0].originalBytes.Length); //Byte size of each type
                    headerStreamWriter.Write(typesToHahes[typeName].Count);
                    foreach(ulong hash in typesToHahes[typeName])
                    {
                        headerStreamWriter.Write(hash);
                        headerStreamWriter.Write((uint)bodyStreamWriter.Position);
                        headerStreamWriter.Write(arraysToExtract[hash].Count());
                        foreach(RvmData rvmData in arraysToExtract[hash])
                            bodyStreamWriter.Write(rvmData.originalBytes);
                    }
                }
                if (typesToExport != typesToHahes.Keys.Count())
                    throw new Exception();

                writer.Write(typesToExport);
                long bodyStartWrittenOffset = writer.BaseStream.Position;
                writer.Write(0XDEADBEEF);
                writer.Write(headerMemoryStream.ToArray());
                writer.Write(surfaceShader.originalBytes);
                writer.Write(surfaceShader.PermutationSet.originalBytes);
                ulong bodyStart = (ulong)writer.BaseStream.Position;
                writer.Write(bodyMemoryStream.ToArray());
                writer.BaseStream.Position = bodyStartWrittenOffset;
                writer.Write((uint)bodyStart);


                headerMemoryStream.Dispose();
                bodyMemoryStream.Dispose();
                headerStreamWriter.Dispose();
                bodyStreamWriter.Dispose();
            }
            public void ImportShader(string shaderName)
            {

                Dictionary<string, Type> types = Assembly.GetExecutingAssembly().GetTypes().Where(type => type.IsSubclassOf(typeof(RvmData))).ToDictionary(type => type.Name, type => type);
                types.Add("char", typeof(CharRvm));

                RvmDataContainer shaderContainer = DataContainer.First(DataContainer => DataContainer.TypeName == "RvmSerializedDb_ns_SurfaceShader");
                List<string> existingShaders = shaderContainer.Data.Select(shaderArray => $"{((RvmSerializedDb_ns_SurfaceShader)shaderArray.Array[0]).Name.Split('/').Last()}_{((RvmSerializedDb_ns_SurfaceShader)shaderArray.Array[0]).NameHash}").ToList(); //{shader.Name.Split('/').Last()}_{shader.NameHash}.bin"

                if (existingShaders.Contains(Path.GetFileNameWithoutExtension(shaderName)))
                    return;

                Dictionary<ulong, RvmArray> cityHashDict = new Dictionary<ulong, RvmArray>();
                foreach (RvmDataContainer dataContainer in DataContainer)
                {
                    for (int i = 0; i < dataContainer.CityHashes.Count; i++)
                    {
                        cityHashDict.Add(dataContainer.CityHashes[i] & ((1UL << 47) - 1), dataContainer.Data[i]);
                        //tempCityHashReadValues.Add(dataContainer.CityHashes[i] & ((1UL << 47) - 1), (dataContainer.TempOriginalCityHashes[i], 0, dataContainer.typeHash, dataContainer.TypeName, dataContainer.Data[i].Array.Count(), dataContainer.Data.Count()));
                    }
                }

                using (NativeReader reader = new NativeReader(new FileStream(shaderName, FileMode.Open, FileAccess.Read)))
                {
                    if (reader.ReadNullTerminatedString() != "MopMagicRVMShad" || reader.ReadInt() != 0)
                        return;
                    int typesToImport = reader.ReadInt();
                    int bodyStartPos = reader.ReadInt();
                    for (int i = 0; i < typesToImport; i++)
                    {
                        string typeName = reader.ReadNullTerminatedString();
                        Type type = types[typeName];

                        RvmDataContainer dataContainer = DataContainer.First(DataContainer => DataContainer.TypeName == typeName);
                        //List<ulong> alreadyAddedHashes = dataContainer.CityHashes;



                        int typeSize = reader.ReadInt();
                        int typeCount = reader.ReadInt();
                        for (int y = 0; y < typeCount; y++)
                        {
                            ulong hash = reader.ReadULong();
                            int goToOffset = reader.ReadInt() + bodyStartPos;
                            int arrayCount = reader.ReadInt();

                            if (cityHashDict.ContainsKey(hash))
                                continue;
                            long curOffset = reader.BaseStream.Position;
                            reader.BaseStream.Position = goToOffset;
                            List<RvmData> rvmArray = new List<RvmData>();
                            for (int z = 0; z < arrayCount; z++)
                            {
                                byte[] bytes = reader.ReadBytes(typeSize);
                                RvmData rvmData = (RvmData)Activator.CreateInstance(type);
                                rvmData.originalBytes = bytes;
                                rvmArray.Add(rvmData);
                                //rvmData.ReadStruct(new NativeReader(new MemoryStream(bytes)));
                            }
                            reader.BaseStream.Position = goToOffset;
                            ulong hash2 = CityHash.HashWithSeed64(reader.ReadBytes((int)(typeSize * arrayCount)), (uint)Utils.HashString(typeName)) & ((1UL << 47) - 1);
                            if (hash2 != hash)
                                throw new Exception();


                            reader.BaseStream.Position = curOffset;
                            dataContainer.CityHashes.Add(hash);
                            dataContainer.Data.Add(new RvmArray(rvmArray));
                        }
                    }
                    {
                        RvmDataContainer dataContainer = DataContainer.First(DataContainer => DataContainer.TypeName == "RvmSerializedDb_ns_SurfaceShader");
                        RvmSerializedDb_ns_SurfaceShader shader = new RvmSerializedDb_ns_SurfaceShader();
                        shader.originalBytes = reader.ReadBytes(0x50);
                        reader.BaseStream.Position = reader.BaseStream.Position - 0x50;
                        shader.ReadStruct(reader);
                        dataContainer.Data.Add(new RvmArray() { Array = new List<RvmData>() { shader } });
                    }
                    {
                        RvmDataContainer dataContainer = DataContainer.First(DataContainer => DataContainer.TypeName == "RvmSerializedDb_ns_RvmPermutationSet");
                        RvmSerializedDb_ns_RvmPermutationSet permSet = new RvmSerializedDb_ns_RvmPermutationSet();
                        permSet.originalBytes = reader.ReadBytes(0x68);
                        dataContainer.Data.Add(new RvmArray() { Array = new List<RvmData>() { permSet } });
                    }
                }
            }
        }
        [EbxClassMeta(EbxFieldType.Struct)]
        public class RvmTypeData
        {
            [EbxFieldMeta(EbxFieldType.String)]
            public string Name { get; set; }
            [EbxFieldMeta(EbxFieldType.UInt32)]
            public uint NameHash { get; set; }
            [EbxFieldMeta(EbxFieldType.UInt32)]
            public uint Count { get; set; }
            [EbxFieldMeta(EbxFieldType.UInt64)]
            public UInt64 Index { get; set; }
            [EbxFieldMeta(EbxFieldType.Boolean)]
            public bool HasParser { get; set; }
            [EbxFieldMeta(EbxFieldType.UInt32)]
            public uint ByteSize { get; set; }
            public RvmTypeData()
            {

            }
            public RvmTypeData(NativeReader reader, Dictionary<uint, Type> types)
            {
                NameHash = reader.ReadUInt();
                Count = reader.ReadUInt();
                Index = reader.ReadULong();
                Name = types.ContainsKey(NameHash) ? types[NameHash].Name : (TypeLibrary.GetType(NameHash) != null ? TypeLibrary.GetType(NameHash).Name : "");
                HasParser = types.ContainsKey(NameHash);
                ByteSize = RvmStaticVariables.rvmSizes[NameHash];

            }


        }
        public class RvmTypeDataPrIdExtension : PrIdExtension
        {
            public override string GetOverrideString(dynamic assetData)
            {
                if (assetData.HasParser)
                    return $"{assetData.Name} ({assetData.NameHash}) - {assetData.Count} - {assetData.Index} - 0x{assetData.ByteSize.ToString("X")}";
                else
                    return $"{assetData.Name} ({assetData.NameHash}) - {assetData.Count} - {assetData.Index} - 0x{assetData.ByteSize.ToString("X")} - No Parser";
            }
        }
        [EbxClassMeta(EbxFieldType.Struct)]
        public class RvmDataContainer
        {
            [EbxFieldMeta(EbxFieldType.String)]
            public string TypeName { get; set; }
            [IsHidden]
            public uint typeHash { get; set; }
            [EbxFieldMeta(EbxFieldType.Int64)]
            public long ByteSize { get; set; }

            [IsHidden]
            public List<ulong> CityHashes { get; set; }

            [IsHidden]
            public List<ulong> TempOriginalCityHashes { get; set; }

            [EbxFieldMeta(EbxFieldType.Array)]
            public List<RvmArray> Data { get; set; }

            public RvmDataContainer()
            {
                Data = new List<RvmArray>();
            }
            public RvmDataContainer(string typeName, int count, Type rvmDataType, NativeReader reader, RvmCountManager countManager, uint typeHash)
            {
                typeHash = typeHash;
                TypeName = typeName;
                Data = new List<RvmArray>();
                CityHashes = new List<ulong>();
                TempOriginalCityHashes = new List<ulong>();
                long startOffset = reader.BaseStream.Position;
                //if (TypeLibrary.GetType(typeHash) != null)
                //    App.Logger.Log($"{TypeLibrary.GetType(typeHash).Name}\t{RvmTypeSizesPreCache.rvmSizes[typeHash]}");
                for (int i = 0; i < count; i++)
                {
                    List<RvmData> rvmDatas = new List<RvmData>();
                    int arrayCount = countManager.GetArrayCount();
                    long preReadArrayOffset = reader.BaseStream.Position;
                    for (int y = 0; y < arrayCount; y++)
                    {
                        RvmData rvmData = (RvmData)Activator.CreateInstance(rvmDataType);
                        rvmData.expectedSize = RvmStaticVariables.rvmSizes[typeHash];

                        long preReadOffset = reader.BaseStream.Position;
                        if (rvmData.GetType().Name == "UnknownRvmType")
                        {
                            ((UnknownRvmType)rvmData).ReadFixed(reader, RvmStaticVariables.rvmSizes[typeHash]);
                            ((UnknownRvmType)rvmData).TypeRealName = typeName;
                        }
                        else
                            rvmData.ReadStruct(reader);
                        if ((int)(reader.BaseStream.Position - preReadOffset) != rvmData.expectedSize)
                            throw new Exception($"Read {typeName} incorrectly");



                        reader.BaseStream.Position = preReadOffset;
                        rvmData.cityHash = CityHash.HashWithSeed64(reader.ReadBytes(rvmData.expectedSize), typeHash) & 0x000000ffffffffff;
                        ByteSize = reader.BaseStream.Position - startOffset;

                        reader.BaseStream.Position = preReadOffset;
                        rvmData.originalBytes = reader.ReadBytes(rvmData.expectedSize);

                        rvmDatas.Add(rvmData);

                    }
                    int readArraySize = (int)(reader.BaseStream.Position - preReadArrayOffset);
                    reader.BaseStream.Position = preReadArrayOffset;

                    ulong hash = CityHash.HashWithSeed64(reader.ReadBytes(readArraySize), typeHash);
                    if (CityHashes.Contains(hash))
                        throw new Exception();
                    else
                        CityHashes.Add(hash);
                    TempOriginalCityHashes.Add(hash);
                    //if (typeName == "RvmSerializedDb_ns_OffsetTranslationInMatrixInstructionData")
                    //{
                    //    reader.BaseStream.Position = preReadArrayOffset;
                    //    App.Logger.Log(BitConverter.ToString(reader.ReadBytes(readArraySize)).Replace("-", ""));
                    //}
                    Data.Add(new RvmArray(rvmDatas));
                }
                ByteSize = reader.BaseStream.Position - startOffset;
            }
        }

        [EbxClassMeta(EbxFieldType.Struct)]
        public class RvmArray
        {
            [EbxFieldMeta(EbxFieldType.Boolean)]
            public bool IsChild { get; set; }

            [EbxFieldMeta(EbxFieldType.Array)]
            public List<RvmData> Array { get; set; }
            public RvmArray()
            {
                Array = new List<RvmData>();
            }
            public RvmArray(List<RvmData> array) 
            {
                Array = array;
            }
        }
        public class RvmDataContainerPrIdExtension : PrIdExtension
        {
            public override string GetOverrideString(dynamic assetData)
            {
                return $"{assetData.TypeName} - {assetData.Data.Count}";
            }
        }
        public class RvmCountManager
        {
            List<ushort> counts = new List<ushort>();
            int idx = 0;

            public ushort GetArrayCount()
            {
                return counts[idx++];
            }
            public RvmCountManager(List<ushort> countsToUse)
            {
                counts = countsToUse;
            }
        }
    }
}
