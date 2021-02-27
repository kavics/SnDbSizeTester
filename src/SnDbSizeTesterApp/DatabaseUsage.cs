using System;
using System.Collections.Generic;
using System.Text;

namespace SnDbSizeTesterApp
{
    public class DatabaseUsage
    {
        public Dimensions Content { get; set; }
        public Dimensions OldVersions { get; set; }
        public Dimensions Preview { get; set; }
        public Dimensions System { get; set; }
        public LogDimensions OperationLog { get; set; }
        public long OrphanedBlobs { get; set; }
        public DateTime Executed { get; set; }
        public TimeSpan ExecutionTime { get; set; }
    }
    public class Dimensions
    {
        public int Count { get; set; }
        public long Blob { get; set; }
        public long Metadata { get; set; }
        public long Text { get; set; }
        public long Index { get; set; }

        public static Dimensions Sum(params Dimensions[] items)
        {
            var d = new Dimensions();
            foreach (var item in items)
            {
                d.Count += item.Count;
                d.Blob += item.Blob;
                d.Metadata += item.Metadata;
                d.Text += item.Text;
                d.Index += item.Index;
            }
            return d;
        }

        public Dimensions Clone()
        {
            return new Dimensions()
            {
                Count = Count,
                Blob = Blob,
                Metadata = Metadata,
                Text = Text,
                Index = Index
            };
        }
    }
    public class LogDimensions
    {
        public int Count { get; set; }
        public long Metadata { get; set; }
        public long Text { get; set; }

        public LogDimensions Clone()
        {
            return new LogDimensions()
            {
                Count = Count,
                Metadata = Metadata,
                Text = Text,
            };
        }
    }
}
