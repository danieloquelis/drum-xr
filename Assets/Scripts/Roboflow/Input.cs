using System;

namespace Roboflow
{   
    [Serializable]
    public class Input
    {
        public Image image;
        
        public Input(string imageBase64)
        {
            // As of now we don't require url type. 
            // We are going to work with base64.
            image = new Image("base64", imageBase64);
        }
    }
}