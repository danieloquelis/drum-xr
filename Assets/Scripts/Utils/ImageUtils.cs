using UnityEngine;

namespace Utils
{
    public static class ImageUtils
    {
        public static Texture2D Resize(Texture2D source, int width, int height)
        {
            var rt = RenderTexture.GetTemporary(width, height);
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);

            var result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);
            return result;
        }
        
        public static Texture2D FlipTextureVertically(Texture2D original)
        {
            var flipped = new Texture2D(original.width, original.height, original.format, false);
            for (var y = 0; y < original.height; y++)
            {
                flipped.SetPixels(0, y, original.width, 1, original.GetPixels(0, original.height - y - 1, original.width, 1));
            }
            
            flipped.Apply();
            return flipped;
        }
    }
}