using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace SnDbSizeTesterApp
{
    public class DashboardData
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        [JsonProperty("host")]
        public string Host { get; set; }

        [JsonProperty("pending")]
        public bool? Pending { get; set; }

        [JsonProperty("version")]
        public VersionData Version { get; set; }

        private SubscriptionData _subscription;
        [JsonProperty("subscription")]
        public SubscriptionData Subscription
        {
            get => _subscription;
            set
            {
                _subscription = value;
                UpdateUsage();
            }
        }

        private UsageData _usage;
        [JsonProperty("usage")]
        public UsageData Usage
        {
            get => _usage;
            set
            {
                _usage = value;
                UpdateUsage();
            }
        }

        private void UpdateUsage()
        {
            if (_usage == null)
                return;
            var storageSizeLimitInMb = _subscription?.Plan.Limitations.StorageSizeInMb ?? decimal.Zero;
            _usage.Storage.Available = Convert.ToInt64(storageSizeLimitInMb * 1024 * 1024);
        }
    }
    public class VersionData
    {
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("date")]
        public DateTime Date { get; set; }
        [JsonProperty("latest")]
        public bool IsLatest { get; set; }
    }
    public class SubscriptionData
    {
        [JsonProperty("plan")]
        public SubscriptionPlanData Plan { get; set; }
        [JsonProperty("expirationDate")]
        public DateTime ExpirationDate { get; set; }
    }
    public class SubscriptionPlanData
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
        [JsonProperty("baseprice")]
        public decimal BasePrice { get; set; }
        [JsonProperty("limitations")]
        public UsageLimit Limitations { get; set; }
    }
    public class UsageData
    {
        [JsonProperty("content")]
        public int ContentCount { get; set; }
        [JsonProperty("user")]
        public int UserCount { get; set; }
        [JsonProperty("group")]
        public int GroupCount { get; set; }
        [JsonProperty("workspace")]
        public int WorkspaceCount { get; set; }
        [JsonProperty("contentType")]
        public int ContentTypeCount { get; set; }
        [JsonProperty("storage")]
        public DatabaseUsageView Storage { get; set; }
    }
    public class UsageLimit
    {
        [JsonProperty("content")]
        public int ContentCount { get; set; }
        [JsonProperty("user")]
        public int UserCount { get; set; }
        [JsonProperty("group")]
        public int GroupCount { get; set; }
        [JsonProperty("workspace")]
        public int WorkspaceCount { get; set; }
        [JsonProperty("contentType")]
        public int ContentTypeCount { get; set; }
        [JsonProperty("storage")]
        public decimal StorageSizeInMb { get; set; }
    }
    public class DatabaseUsageView
    {
        [JsonProperty("available")]
        public long Available { get; set; }
        [JsonProperty("files")]
        public long Files { get; set; }
        [JsonProperty("content")]
        public long Content { get; set; }
        [JsonProperty("oldVersions")]
        public long OldVersions { get; set; }
        [JsonProperty("log")]
        public long Log { get; set; }
        [JsonProperty("system")]
        public long System { get; set; }
    }
}
