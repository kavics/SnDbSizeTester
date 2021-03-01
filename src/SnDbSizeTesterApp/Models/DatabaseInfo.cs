namespace SnDbSizeTesterApp.Models
{
    public class DatabaseInfo
    {
        public string Name { get; set; }
        public DatabaseSizeInfo Database { get; set; }
        public TableInfo[] Tables { get; set; }
    }
    public class TableInfo
    {
        public string Name { get; set; }
        public int Rows { get; set; }
        public long Size { get; set; }
    }
    public class DatabaseSizeInfo
    {
        public long DatabaseSize { get; set; }
        public long DataSize { get; set; }
        public long LogSize { get; set; }
        public long Reserved { get; set; }
        public long Data { get; set; }
        public long Index { get; set; }
        public long Unused { get; set; }
        public long UsedLog { get; set; }
        public long UnusedLog { get; set; }
        public float DataPercent { get; set; }
        public float IndexPercent { get; set; }
        public float UnusedPercent { get; set; }
        public float UnallocatedPercent { get; set; }
        public float UsedLogPercent { get; set; }
        public float UnusedLogPercent { get; set; }
    }
}
