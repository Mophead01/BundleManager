using Frosty.Core;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoBundleManagerPlugin
{
    public class BundleCompleter
    {
        private Stopwatch stopWatch = new Stopwatch();
        private long lastTimestamp = 0;
        private StringBuilder LogList = new StringBuilder();
        private void AddToLog(string type, string description, string parent, string child)
        {
            LogList.AppendLine(type + "," + description + "," + parent + "," + child + "," + (stopWatch.ElapsedMilliseconds - lastTimestamp).ToString());
            lastTimestamp = stopWatch.ElapsedMilliseconds;
        }
        public BundleCompleter(AssetManager AM, string fbmodName,List<string> loadOrder) 
        {
            stopWatch.Start();

            if (!loadOrder.Contains(fbmodName))
                loadOrder.Add(fbmodName);

            //
            //  Clear Existing Bundle Edits from primary project
            //

            //
            // Get data from modified files and check to see if they modify the bundle heap, if so make changes
            //
            foreach (EbxAssetEntry parEntry in AM.EnumerateEbx()) 
            {
                if (!parEntry.HasModifiedData)
                    continue;
                AddToLog("Caching", "Getting Dependencies", parEntry.Name, parEntry.GetSha1().ToString());
                DependencyActiveData dependencies = AbmDependenciesCache.GetDependencies(parEntry);
                foreach(KeyValuePair<string, HashSet<string>> bunParents in dependencies.bundleReferences)
                {
                    AddToLog("Caching", "Modifying Bundle Hierarchy", bunParents.Key, string.Join(", ", bunParents.Value));
                }
            }

            //
            // Post completion operations
            //

            try
            {
                using (NativeWriter writer = new NativeWriter(new FileStream($"{App.FileSystem.CacheName}/AutoBundleManager/Logger.csv", FileMode.Create)))
                {
                    writer.WriteLine("Type, Description, Parent, Child, Time Elapsed (MS)");
                    writer.WriteLine(LogList.ToString());
                }
            }
            catch
            {
                App.Logger.Log("Could not export file " + App.FileSystem.CacheName + "_BundleManager_LogList.csv");
            }

            AbmDependenciesCache.UpdateCache();
            stopWatch.Stop();
            App.Logger.Log(string.Format("Bundle Manager Completed in {0} seconds.", stopWatch.Elapsed));
        }

    }
}
