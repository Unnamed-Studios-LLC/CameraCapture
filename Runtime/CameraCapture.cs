using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace UnnamedStudios
{
    [RequireComponent(typeof(Camera))]
    public class CameraCapture : MonoBehaviour
    {
        /// <summary>
        /// Amount of frames recorded per second.
        /// </summary>
        public int FrameRate = 30;
        /// <summary>
        /// The maximum amount of frames to store. MaxFrames / FrameRate = Max Clip Duration
        /// </summary>
        public int MaxFrames = 150;
        /// <summary>
        /// If the camera is currently being recorded.
        /// </summary>
        public bool Recording = false;
        /// <summary>
        /// The downscaling factor. 0.5 = 50%, 0.3 = 33%, etc.
        /// </summary>
        public float DownScale = 0.5f;
        /// <summary>
        /// The filtering mode to use for the backing RenderTextures.
        /// </summary>
        public FilterMode FilterMode = FilterMode.Point;

        private float _elapsedTime;
        private CaptureFrame[] _frames;
        private int _frameIndex = 0;
        private int _frameCount = 0;

        private float TargetFrameDuration => 1f / FrameRate;

        /// <summary>
        /// Clears all stored frames and cleans up the backing resources.
        /// </summary>
        public void ClearFrames()
        {
            if (_frameCount > 0)
            {
                foreach (ref var frame in _frames.AsSpan(0, _frameCount))
                {
                    if (frame.Texture != null) Destroy(frame.Texture);
                    frame = default;
                }
                _frameCount = 0;
            }
            _frameIndex = 0;
            _elapsedTime = 0;
        }

        /// <summary>
        /// Copies frames to a destination span. Frames contain their size and a reference to their backing RenderTexture.
        /// </summary>
        /// <returns>The amount of frames copied</returns>
        public int CopyFramesTo(Span<CaptureFrame> destination, int? frameCount = default)
        {
            if (_frameCount <= 0) return 0;
            var source = _frames.AsSpan(0, Mathf.Min(_frames.Length, frameCount ?? _frameCount));
            return CopyFramesTo(source, destination);
        }

        /// <summary>
        /// Exports current frames to a gif file in a folder found at persistentDataPath.
        /// </summary>
        /// <returns>Filepath of the created gif</returns>
        public async Task<string> ExportGifAsync(int playbackFrameRate)
        {
            const string randomCharacterSource = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            const int randomLength = 10;

            var folder = Path.Combine(Application.persistentDataPath, "Clips");
            Directory.CreateDirectory(folder);

            string filePath;
            var stringBuilder = new StringBuilder();
            do
            {
                stringBuilder.Append(Application.productName);
                stringBuilder.Replace(' ', '_');
                stringBuilder.Append('_');
                for (int i = 0; i < randomLength; i++)
                {
                    stringBuilder.Append(randomCharacterSource[UnityEngine.Random.Range(0, randomCharacterSource.Length)]);
                }
                stringBuilder.Append(".gif");
                filePath = Path.Combine(folder, stringBuilder.ToString());
            }
            while (File.Exists(filePath));

            var frames = new CaptureFrame[_frames.Length];
            var frameCount = CopyFramesTo(frames.AsSpan());

            var maxSize = Vector2Int.zero;
            for (int i = 0; i < frameCount; i++)
            {
                var sourceFrame = frames[i];
                maxSize = Vector2Int.Max(maxSize, new Vector2Int(sourceFrame.Width, sourceFrame.Height));
            }

            var readTexture = new Texture2D(maxSize.x, maxSize.y, TextureFormat.ARGB32, false, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var renderedFrames = new RenderedFrame[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                var sourceFrame = frames[i];
                RenderTexture.active = sourceFrame.Texture;
                var offset = new Vector2Int(maxSize.x - sourceFrame.Width, maxSize.y - sourceFrame.Height) / 2;
                readTexture.ReadPixels(new Rect(0, 0, sourceFrame.Width, sourceFrame.Height), offset.x, offset.y);
                renderedFrames[i] = new RenderedFrame(maxSize.x, maxSize.y, readTexture.GetPixels32());
            }

            RenderTexture.active = null;

            var gifEncoder = new GifEncoder(0, 20);
            gifEncoder.SetFrameRate(playbackFrameRate);

            await Task.Run(() =>
            {
                var startTimestamp = DateTime.Now;
                gifEncoder.Start(filePath);

                // pass all frames to encoder to build a palette out of a subset of them
                gifEncoder.BuildPalette(renderedFrames, frameCount);

                for (int i = 0; i < frameCount; i++)
                {
                    gifEncoder.AddFrame(renderedFrames[i]);

                }
                gifEncoder.Finish();
                Debug.Log("Gif export finished in " + (DateTime.Now - startTimestamp).TotalMilliseconds + " ms");
            });

            return filePath;
        }

        private void CaptureFrame(RenderTexture source)
        {
            DownScale = Mathf.Min(1, DownScale);
            var targetSize = Vector2Int.Max(Vector2Int.one, new Vector2Int(Mathf.RoundToInt(source.width * DownScale), Mathf.RoundToInt(source.height * DownScale)));

            ref var current = ref _frames[_frameIndex++];
            if (current.Texture == null ||
                current.Texture.width != targetSize.x ||
                current.Texture.height != targetSize.y ||
                current.Texture.filterMode != FilterMode)
            {
                if (current.Texture != null) Destroy(current.Texture);
                current = new CaptureFrame(targetSize.x, targetSize.y, FilterMode);
            }

            Graphics.Blit(source, current.Texture);

            _frameIndex %= MaxFrames;
            _frameCount = Mathf.Min(_frames.Length, _frameCount + 1);
        }

        private void CreateFramesArray()
        {
            var frames = new CaptureFrame[MaxFrames];
            if (_frames != null && _frameCount > 0)
            {
                // copy previous frame data
                var copyCount = CopyFramesTo(_frames.AsSpan(0, _frameCount), frames.AsSpan());

                _frameIndex = copyCount % MaxFrames;
                _frameCount = copyCount;
            }
            _frames = frames;
        }

        private int CopyFramesTo(Span<CaptureFrame> source, Span<CaptureFrame> destination)
        {
            var lastIndex = mod(_frameIndex - 1, source.Length); // fixes c# negative mod
            var largerOffset = Mathf.Max(0, destination.Length - source.Length);

            var framesCopied = 0;
            var firstChunkSize = Mathf.Min(destination.Length, lastIndex + 1);
            if (firstChunkSize > 0)
            {
                var firstChunk = source.Slice(lastIndex + 1 - firstChunkSize, firstChunkSize);
                firstChunk.CopyTo(destination.Slice(destination.Length - firstChunkSize - largerOffset, firstChunkSize));
                framesCopied += firstChunkSize;
            }

            var secondChunkSize = Mathf.Min(destination.Length - firstChunkSize, source.Length - lastIndex - 1);
            if (secondChunkSize > 0)
            {
                var secondChunk = source.Slice(lastIndex + 1, secondChunkSize);
                secondChunk.CopyTo(destination);
                framesCopied += secondChunkSize;
            }

            return framesCopied;
        }

        private void OnDestroy()
        {
            ClearFrames();
        }

        private void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (Recording)
            {
                if (_frames == null ||
                    MaxFrames != _frames.Length)
                {
                    CreateFramesArray();
                }

                _elapsedTime += Time.unscaledDeltaTime;
                if (_elapsedTime >= TargetFrameDuration)
                {
                    CaptureFrame(source);
                    _elapsedTime = mod(_elapsedTime, TargetFrameDuration);
                }
            }

            Graphics.Blit(source, destination);
        }

        int mod(int x, int m)
        {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        float mod(float x, float m)
        {
            float r = x % m;
            return r < 0 ? r + m : r;
        }
    }
}