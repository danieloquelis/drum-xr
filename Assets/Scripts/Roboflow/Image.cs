using System;

namespace Roboflow
{
    [Serializable]
    public class Image
    {
        public string type;
        public string value;

        public Image(string type, string value)
        {
            this.type = type;
            this.value = value;
        }
    }
}