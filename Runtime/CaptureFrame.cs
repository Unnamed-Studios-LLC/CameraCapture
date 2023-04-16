using UnityEngine;

namespace UnnamedStudios
{
    public struct CaptureFrame
    {
        public int Width;
        public int Height;
        public RenderTexture Texture;

        public CaptureFrame(int width, int height)
        {
            Width = width;
            Height = height;
            Texture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
        }
    }
}