using Newtonsoft.Json;

namespace Models
{
    public class Prediction
    {
        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonProperty("confidence")]
        public float Confidence { get; set; }

        [JsonProperty("class_id")]
        public int ClassId { get; set; }

        [JsonProperty("class")]
        public string Class { get; set; }

        [JsonProperty("detection_id")]
        public string DetectionId { get; set; }

        [JsonProperty("parent_id")]
        public string ParentId { get; set; }
    }
}