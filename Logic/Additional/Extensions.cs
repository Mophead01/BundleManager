using Frosty.Core.Windows;
using FrostySdk;
using FrostySdk.IO;
using FrostySdk.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace AutoBundleManagerPlugin
{
    public static class Extensions
    {
        #region Writers
        public static void Write(this NativeWriter writer, List<string> stringList)
        {
            writer.Write(stringList.Count);
            foreach (string str in stringList)
                writer.WriteNullTerminatedString(str);
        }
        public static void Write(this NativeWriter writer, HashSet<string> stringSet)
        {
            writer.Write(stringSet.Count);
            foreach (string str in stringSet)
                writer.WriteNullTerminatedString(str);
        }
        public static void Write(this NativeWriter writer, List<ulong> ulongList)
        {
            writer.Write(ulongList.Count);
            foreach (ulong num in ulongList)
                writer.Write(num);
        }
        public static void Write(this NativeWriter writer, HashSet<ulong> ulongSet)
        {
            writer.Write(ulongSet.Count);
            foreach (ulong num in ulongSet)
                writer.Write(num);
        }
        public static void Write(this NativeWriter writer, List<Guid> guidList)
        {
            writer.Write(guidList.Count);
            foreach (Guid guid in guidList)
                writer.Write(guid);
        }
        public static void Write(this NativeWriter writer, HashSet<Guid> guidSet)
        {
            writer.Write(guidSet.Count);
            foreach (Guid guid in guidSet)
                writer.Write(guid);
        }
        public static void Write(this NativeWriter writer, HashSet<(Guid, Guid)> guidSet)
        {
            writer.Write(guidSet.Count);
            foreach ((Guid, Guid) guidPair in guidSet)
            {
                writer.Write(guidPair.Item1);
                writer.Write(guidPair.Item2);
            }
        }
        public static void Write(this NativeWriter writer, Dictionary<Guid, int> guidDict)
        {
            writer.Write(guidDict.Count);
            foreach (KeyValuePair<Guid, int> pair in guidDict)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value);
            }
        }
        public static void Write(this NativeWriter writer, List<int> intList)
        {
            writer.Write(intList.Count);
            foreach (int value in intList)
                writer.Write(value);
        }
        public static void Write(this NativeWriter writer, Dictionary<Guid, List<Guid>> guidDict)
        {
            writer.Write(guidDict.Count);
            foreach (KeyValuePair<Guid, List<Guid>> pair in guidDict)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value);
            }
        }
        public static void Write(this NativeWriter writer, Dictionary<string, HashSet<string>> strDict)
        {
            writer.Write(strDict.Count);
            foreach (KeyValuePair<string, HashSet<string>> pair in strDict)
            {
                writer.WriteNullTerminatedString(pair.Key);
                writer.Write(pair.Value.Count());
                pair.Value.ToList().ForEach(item => writer.WriteNullTerminatedString(item));
            }
        }
        #endregion
        #region Readers

        public static HashSet<string> ReadHashSetStrings(this NativeReader reader)
        {
            int count = reader.ReadInt();
            HashSet<string> hashSet = new HashSet<string>();
            for (int i = 0; i < count; i++)
                hashSet.Add(reader.ReadNullTerminatedString());
            return hashSet;
        }

        public static List<Guid> ReadListGuids(this NativeReader reader)
        {
            int count = reader.ReadInt();
            List<Guid> hashSet = new List<Guid>();
            for (int i = 0; i < count; i++)
                hashSet.Add(reader.ReadGuid());
            return hashSet;
        }

        public static HashSet<Guid> ReadHashSetGuids(this NativeReader reader)
        {
            int count = reader.ReadInt();
            HashSet<Guid> hashSet = new HashSet<Guid>();
            for (int i = 0; i < count; i++)
                hashSet.Add(reader.ReadGuid());
            return hashSet;
        }

        public static HashSet<(Guid, Guid)> ReadHashSetGuidPairs(this NativeReader reader)
        {
            int count = reader.ReadInt();
            HashSet<(Guid, Guid)> hashSet = new HashSet<(Guid, Guid)>();
            for (int i = 0; i < count; i++)
                hashSet.Add((reader.ReadGuid(), reader.ReadGuid()));
            return hashSet;
        }

        public static Dictionary<Guid, int> ReadGuidDictionary(this NativeReader reader)
        {
            int count = reader.ReadInt();
            Dictionary<Guid, int> guidDict = new Dictionary<Guid, int>();
            for (int i = 0; i < count; i++)
                guidDict[reader.ReadGuid()] = reader.ReadInt();
            return guidDict;
        }

        public static HashSet<ulong> ReadHashSetULongs(this NativeReader reader)
        {
            int count = reader.ReadInt();
            HashSet<ulong> hashSet = new HashSet<ulong>();
            for (int i = 0; i < count; i++)
                hashSet.Add(reader.ReadULong());
            return hashSet;
        }

        public static List<int> ReadIntList(this NativeReader reader)
        {
            int count = reader.ReadInt();
            List<int> intList = new List<int>();
            for (int i = 0; i < count; i++)
                intList.Add(reader.ReadInt());
            return intList;
        }

        public static Dictionary<string, HashSet<string>> ReadStringToHashsetDictionary(this NativeReader reader)
        {
            int count = reader.ReadInt();
            Dictionary<string, HashSet<string>> strDict = new Dictionary<string, HashSet<string>>();
            for (int i = 0; i < count; i++)
            {
                string key = reader.ReadNullTerminatedString();
                HashSet<string> valStrings = new HashSet<string>();
                int valCount = reader.ReadInt();
                for (int j = 0; j < valCount; j++)
                    valStrings.Add(reader.ReadNullTerminatedString());
                strDict.Add(key, valStrings);
            }
            return strDict;
        }

        #endregion

        public static void ParallelForeach<T>(this FrostyTaskWindow task, string taskName, IEnumerable<T> source, Action<T, int> body)
        {
            task.Update(taskName);
            int forCount = source.Count();
            int forIdx = 0;
            object forLock = new object();

            Parallel.ForEach(source, item =>
            {
                body(item, Interlocked.Increment(ref forCount));

                lock (forLock)
                {
                    task.Update(progress: (float)forIdx++ / forCount * 100);
                }
            });
        }
        public static bool IsInBundleHeap(this AssetEntry assetEntry, int bunId, List<int> parentIds)
        {
            if (assetEntry.IsInBundle(bunId))
                return true;
            foreach (int parId in parentIds)
                if (assetEntry.IsInBundle(parId))
                    return true;
            return false;
        }

        public static Sha1 GetSha1(this AssetEntry parEntry)
        {
            if (parEntry.HasModifiedData)
                return parEntry.ModifiedEntry.Sha1;
            return parEntry.Sha1;
        }
    }
}
