using Newtonsoft.Json;

namespace Models
{
    public class DetectionResult
    {
        [JsonProperty("raw_recognition")]
        public RawRecognition RawRecognition { get; set; }
    }
}