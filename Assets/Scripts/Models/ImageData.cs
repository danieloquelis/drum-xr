using Newtonsoft.Json;

namespace Models
{
    public class ImageData
    {
        [JsonProperty("width")]
        public float? Width { get; set; }

        [JsonProperty("height")]
        public float? Height { get; set; }
    }
}