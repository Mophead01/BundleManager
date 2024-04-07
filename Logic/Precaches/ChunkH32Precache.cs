using Frosty.Core;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AutoBundleManagerPlugin.Logic.Precaches
{
    public static class ChunkH32Precache
    {
        public static Dictionary<ChunkAssetEntry, (int, int)> chunkH32Cached = new Dictionary<ChunkAssetEntry, (int, int)>();
        static ChunkH32Precache()
        {
            using (NativeReader reader = new NativeReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("AutoBundleManagerPlugin.Data.Swbf2_H32FirstMip.cache")))
            {
                int chunkCount = reader.ReadInt();
                for (int i = 0; i < chunkCount; i++)
                    chunkH32Cached.Add(App.AssetManager.GetChunkEntry(reader.ReadGuid()), (reader.ReadInt(), reader.ReadInt()));
            }
        }
    }
}
