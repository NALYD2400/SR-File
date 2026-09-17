using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using SRFile.Sidecar.Models;

namespace SRFile.Sidecar.Services
{
    public class SystemService
    {
        private readonly RpfService _rpfService;
        private readonly DateTime _startTime = DateTime.UtcNow;

        public SystemService(RpfService rpfService)
        {
            _rpfService = rpfService;
        }

        public SystemMetricsDto GetMetrics()
        {
            using var proc = Process.GetCurrentProcess();
            proc.Refresh();

            long workingSet = proc.WorkingSet64;
            long privateBytes = proc.PrivateMemorySize64;
            long virtualBytes = proc.VirtualMemorySize64;
            long gcTotal = GC.GetTotalMemory(false);

            int gen0 = GC.CollectionCount(0);
            int gen1 = GC.CollectionCount(1);
            int gen2 = GC.CollectionCount(2);

            var gcInfo = GC.GetGCMemoryInfo();
            long heapSize = gcInfo.HeapSizeBytes;

            var cacheStats = _rpfService.GetCacheStats();
            long uptime = (long)(DateTime.UtcNow - _startTime).TotalSeconds;

            return new SystemMetricsDto(
                ProcessWorkingSetBytes: workingSet,
                ProcessWorkingSetMB: Math.Round(workingSet / 1024.0 / 1024.0, 2),
                ProcessPrivateMemoryBytes: privateBytes,
                ProcessPrivateMemoryMB: Math.Round(privateBytes / 1024.0 / 1024.0, 2),
                ProcessVirtualMemoryBytes: virtualBytes,
                GcTotalMemoryBytes: gcTotal,
                GcTotalMemoryMB: Math.Round(gcTotal / 1024.0 / 1024.0, 2),
                GcGen0Collections: gen0,
                GcGen1Collections: gen1,
                GcGen2Collections: gen2,
                HeapSizeBytes: heapSize,

                ProcessId: proc.Id,
                ThreadCount: proc.Threads.Count,
                HandleCount: proc.HandleCount,
                UptimeSeconds: uptime,
                StartTime: _startTime,
                ProcessorCount: Environment.ProcessorCount,
                OsPlatform: RuntimeInformation.OSDescription,
                OsArchitecture: RuntimeInformation.OSArchitecture.ToString(),
                ProcessArchitecture: RuntimeInformation.ProcessArchitecture.ToString(),
                FrameworkDescription: RuntimeInformation.FrameworkDescription,

                OpenArchivesCount: cacheStats.LoadedRpfsCount,
                OpenArchives: cacheStats.OpenArchives,
                TotalIndexedEntries: cacheStats.TotalIndexedEntries,
                CacheHits: cacheStats.CacheHits,
                CacheMisses: cacheStats.CacheMisses
            );
        }
    }
}
