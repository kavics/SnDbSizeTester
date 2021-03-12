using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace SnDbSizeTesterApp
{
    public class ClientInfo
    {
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("clientId")]
        public string ClientId { get; set; }
        [JsonProperty("secret")]
        public string Secret { get; set; }
        [JsonProperty("connectionString")]
        public string ConnectionStrting { get; set; }
    }
}
