using System.Collections.Generic;
using Newtonsoft.Json;

namespace Models
{
    public class RawRecognition
    {
        [JsonProperty("image")]
        public ImageData Image { get; set; }

        [JsonProperty("predictions")]
        public List<Prediction> Predictions { get; set; }
    }
}