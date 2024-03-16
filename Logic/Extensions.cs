using Frosty.Core.Windows;
using FrostySdk.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        public static void Write(this NativeWriter writer, List<int> intList)
        {
            writer.Write(intList.Count);
            foreach (int value in intList)
                writer.Write(value);
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

        public static HashSet<Guid> ReadHashSetGuids(this NativeReader reader)
        {
            int count = reader.ReadInt();
            HashSet<Guid> hashSet = new HashSet<Guid>();
            for (int i = 0; i < count; i++)
                hashSet.Add(reader.ReadGuid());
            return hashSet;
        }

        public static HashSet<ulong> ReadHashSetULongs(this NativeReader reader)
        {
            int count = reader.ReadInt();
            HashSet<ulong> hashSet = new HashSet<ulong>();
            for (int i = 0; i < count; i++)
                hashSet.Add(reader.ReadULong());
            return hashSet;
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
    }
}
