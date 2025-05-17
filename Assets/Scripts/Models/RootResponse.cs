using System.Collections.Generic;
using Newtonsoft.Json;

namespace Models
{
    public class RootResponse
    {
        [JsonProperty("outputs")]
        public List<DetectionResult> Outputs { get; set; }
    }
}