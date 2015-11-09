using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Akka.TestKit;

namespace Akka.Interfaced.Persistence.Tests
{
    internal class CleanupLocalSnapshots : IDisposable
    {
        internal List<DirectoryInfo> StorageLocations;
        private static readonly object _syncRoot = new object();

        public CleanupLocalSnapshots(TestKitBase testKit)
        {
            StorageLocations = new[]
            {
                "akka.persistence.snapshot-store.local.dir"
            }.Select(s => new DirectoryInfo(testKit.Sys.Settings.Config.GetString(s))).ToList();
        }

        public void Initialize()
        {
            DeleteStorageLocations();
        }

        private void DeleteStorageLocations()
        {
            StorageLocations.ForEach(fi =>
            {
                lock (_syncRoot)
                {
                    try
                    {
                        if (fi.Exists) fi.Delete(true);
                    }
                    catch (IOException) { }
                }
            });
        }

        public void Dispose()
        {
            DeleteStorageLocations();
        }
    }
}
