using AutoBundleManager.Logic;
using AutoBundleManagerPlugin;
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
[assembly: RegisterTabExtension(typeof(DependencyViewer))]


[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AbmOptionsMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AbmCacheBundleHierarchyMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AbmCacheNetworkRegistriesMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AutoBundleManagerTestBlueprintBundlesMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AutoBundleManagerTestBlueprintBundlesParentsMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AutoBundleManagerTestSublevelsMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AutoBundleManagerTestSublevelsParentMenuExtension))]
[assembly: RegisterMenuExtension(typeof(AbmMenuExtensions.AutoBundleManagerTestChunkFirstMipH32MenuExtension))]
