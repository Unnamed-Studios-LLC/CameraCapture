using UnityEngine;

namespace UnnamedStudios
{
    public struct CaptureFrame
    {
        public int Width;
        public int Height;
        public RenderTexture Texture;

        public CaptureFrame(int width, int height, FilterMode filterMode)
        {
            Width = width;
            Height = height;
            Texture = new RenderTexture(width, height, 0, RenderTextureFormat.Default)
            {
                filterMode = filterMode,
                wrapMode = TextureWrapMode.Clamp
            };
        }
    }
}