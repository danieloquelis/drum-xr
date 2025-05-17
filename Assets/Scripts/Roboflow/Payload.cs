using System;
using Newtonsoft.Json;

namespace Roboflow
{
    [Serializable]
    public class Payload
    {
        [JsonProperty("api_key")]
        public string apiKey;
        public Input inputs;

        public Payload(string apiKey, string imageBase64)
        {
            this.apiKey = apiKey;
            inputs = new Input(imageBase64);
        }
    }
}