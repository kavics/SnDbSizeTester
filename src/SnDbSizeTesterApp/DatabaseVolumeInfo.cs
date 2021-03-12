using System;
using System.Collections.Generic;
using System.Text;

namespace SnDbSizeTesterApp
{
    public class DatabaseVolumeInfo
    {
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public double TempDbFillPercent { get; set; }
        public long TempDbSizeKb { get; set; }
        public long DiskSizeBytes { get; set; }
        public long DiskFreeBytes { get; set; }
    }
}
