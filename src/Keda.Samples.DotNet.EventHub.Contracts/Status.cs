using System;
using Newtonsoft.Json;

namespace Keda.Samples.DotNet.EventHub.Contracts
{
    public class Status
    {
        [JsonProperty]
        public long MessageCount { get; set; }
    }
}
