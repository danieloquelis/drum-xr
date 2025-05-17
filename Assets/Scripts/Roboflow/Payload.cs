using System;

namespace Roboflow
{
    [Serializable]
    public class Payload
    {
        public string api_key;
        public Input inputs;

        public Payload(string apiKey, string imageBase64)
        {
            this.api_key = apiKey;
            inputs = new Input(imageBase64);
        }
    }
}