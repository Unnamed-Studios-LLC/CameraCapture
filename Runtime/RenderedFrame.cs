using UnityEngine;

namespace UnnamedStudios
{
    public struct RenderedFrame
    {
        public int Width;
        public int Height;
        public Color32[] Colors;

        public RenderedFrame(int width, int height, Color32[] colors)
        {
            Width = width;
            Height = height;
            Colors = colors;
        }
    }
}