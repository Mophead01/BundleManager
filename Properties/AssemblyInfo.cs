using AutoBundleManagerPlugin;
using AutoBundleManagerPlugin.Logic.Operations;
using AutoBundleManagerPlugin.Rvm;
using Frosty.Core.Attributes;
using FrostySdk;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

// Setting ComVisible to false makes the types in this assembly not visible
// to COM components.  If you need to access a type in this assembly from
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

[assembly: ThemeInfo(
    ResourceDictionaryLocation.None, //where theme specific resource dictionaries are located
                                     //(used if a resource is not found in the page, 
                                     // or application resource dictionaries)
    ResourceDictionaryLocation.SourceAssembly //where the generic resource dictionary is located
                                              //(used if a resource is not found in the page, 
                                              // app, or any theme specific resource dictionaries)
)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("4b612468-9b6a-4304-88a5-055c3575eb3d")]

[assembly: PluginDisplayName("Auto Bundle Manager")]
[assembly: PluginAuthor("Mophead")]
[assembly: PluginVersion("0.0.0.1")]

[assembly: PluginValidForProfile((int)ProfileVersion.StarWarsBattlefrontII)]
[assembly: PluginValidForProfile((int)ProfileVersion.Battlefield1)]
[assembly: RegisterTabExtension(typeof(DependencyViewer))]


[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AbmOptionsMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AutoBundleManagerCompleteBundlesBundlesMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AutoBundleManagerCompleteGlobalBundlesBundlesMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AutoBundleManagerClearBundlesBundlesMenuExtension))]
[assembly: RegisterExportAction(typeof(BundleManagerLaunchExportOverride), ExportType.ExportOnly, 15)]

#if false
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AbmCacheBundleHierarchyMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AbmCacheNetworkRegistriesMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AbmCacheMeshVariationDatabasesMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AutoBundleManagerTestBlueprintBundlesMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AutoBundleManagerTestBlueprintBundlesParentsMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AutoBundleManagerTestSublevelsMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AutoBundleManagerTestSublevelsParentMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AutoBundleManagerTestChunkFirstMipH32MenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AbmDependenciesToXmlMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AbmCurrentProjectToCsvMenuExtension))]
[assembly: RegisterMenuExtension(typeof(RvmEditor.RvmViewerMenuExtension))]
[assembly: RegisterMenuExtension(typeof(RvmEditor.RvmWriterTestMenuExtension))]
[assembly: RegisterMenuExtension(typeof(RvmEditor.RvmComparerMenuExtension))]
[assembly: RegisterMenuExtension(typeof(RvmEditor.RvmExportHeaderTypesMenuExtension))]
[assembly: RegisterMenuExtension(typeof(RvmEditor.RvmVerifyRefTypesMenuExtension))]
[assembly: RegisterMenuExtension(typeof(RvmEditor.RvmExtractShaderMenuExtension))]
[assembly: RegisterMenuExtension(typeof(RvmEditor.RvmTestWriterBespinMenuExtension))]
[assembly: RegisterPointerRefIdOverride("RvmTypeData", typeof(RvmEditor.RvmTypeDataPrIdExtension), true, 0)]
[assembly: RegisterPointerRefIdOverride("RvmDataContainer", typeof(RvmEditor.RvmDataContainerPrIdExtension), true, 0)]
[assembly: RegisterPointerRefIdOverride("RvmSerializedDb_ns_SurfaceShader", typeof(RvmSerializedDb_ns_SurfaceShaderPrIdExtension), true, 0)]
#endif