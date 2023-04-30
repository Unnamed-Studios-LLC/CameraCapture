using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace UnnamedStudios
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(RawImage))]
    public class CapturePreview : MonoBehaviour
    {
        /// <summary>
        /// The CameraCapture to read from.
        /// </summary>
        public CameraCapture Capture;
        /// <summary>
        /// The frame rate to playback frames at.
        /// </summary>
        public int PlaybackFrameRate = 30;
        /// <summary>
        /// If enabled, the RectTransform's sizeDelta will be adjusted to fit inside the given FitSize.
        /// </summary>
        public bool AutoFit = true;
        /// <summary>
        /// The size to fit to if AutoFit is enabled.
        /// </summary>
        public Vector2 FitSize = new Vector2(100, 50);
        /// <summary>
        /// If playback should increment frames at the given PlaybackFrameRate.
        /// </summary>
        public bool Playing = true;

        private RawImage _rawImage;
        private CaptureFrame[] _frames;
        private int _frameCount;
        private int _displayedIndex = -1;
        private float _timeRemaining;

        private float TargetFrameDuration => 1f / Mathf.Max(1, PlaybackFrameRate);

        /// <summary>
        /// Exports currently displayed frame to a png file in a folder found at persistentDataPath.
        /// </summary>
        /// <returns>Filepath of the created gif</returns>
        public async Task<string> ExportPngAsync(int playbackFrameRate)
        {
            if (_displayedIndex < 0 ||
                _frameCount <= 0) return string.Empty;

            var filePath = FileHelper.GetRandomApplicationFileName("Clips", "png");

            var maxSize = Vector2Int.zero;
            for (int i = 0; i < frameCount; i++)
            {
                var sourceFrame = frames[i];
                maxSize = Vector2Int.Max(maxSize, new Vector2Int(sourceFrame.Width, sourceFrame.Height));
            }

            var frame = _frames[_displayedIndex];
            var readTexture = new Texture2D(frame.Width, frame.Height, TextureFormat.ARGB32, false, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            RenderTexture.active = frame.Texture;
            readTexture.ReadPixels(new Rect(0, 0, frame.Width, frame.Height), 0, 0);
            var bytes = readTexture.EncodeToPNG();

            RenderTexture.active = null;
            Destroy(readTexture);

            await File.WriteAllBytesAsync(filePath, bytes);

            return filePath;
        }

        /// <summary>
        /// Loads frames from the assigned CameraCapture. It is recommended to set Recording = false in the CameraCapture before calling this method.
        /// </summary>
        public void LoadFrames(int? frameCount = default)
        {
            if (Capture == null)
            {
                Debug.LogError("CapturePreview.Capture is null");
                return;
            }

            _frames = new CaptureFrame[frameCount ?? Capture.MaxFrames];
            var framesSpan = _frames.AsSpan();
            _frameCount = Capture.CopyFramesTo(framesSpan);
            _displayedIndex = -1;

            if (_frameCount <= 0)
            {
                _rawImage.texture = null;
                return;
            }

            var maxSize = Vector2Int.zero;
            foreach (ref var frame in framesSpan)
            {
                maxSize = Vector2Int.Max(maxSize, new Vector2Int(frame.Width, frame.Height));
            }

            SetFrame(0);
        }

        /// <summary>
        /// Immediately displays the next frame of playback.
        /// </summary>
        public void NextFrame()
        {
            if (_displayedIndex < 0 ||
                _frameCount <= 0 ||
                _rawImage == null) return;

            SetFrame(mod(_displayedIndex + 1, _frameCount));
        }

        /// <summary>
        /// Immediately displays the previous frame of playback.
        /// </summary>
        public void PreviousFrame()
        {
            if (_displayedIndex < 0 ||
                _frameCount <= 0 ||
                _rawImage == null) return;

            SetFrame(mod(_displayedIndex - 1, _frameCount));
        }

        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();
        }

        private void LateUpdate()
        {
            UpdateFrame();
            UpdateFit();
        }

        private void SetFrame(int index)
        {
            if (index == _displayedIndex) return;
            _displayedIndex = index;
            ref var frame = ref _frames[index];
            _rawImage.texture = frame.Texture;
        }

        private void UpdateFit()
        {
            if (_rawImage == null ||
                _rawImage.texture == null ||
                !AutoFit) return;

            var textureSize = new Vector2(_rawImage.texture.width, _rawImage.texture.height);
            var aspectRatio = textureSize.x / textureSize.y;
            var targetRatio = FitSize.x / FitSize.y;

            if (aspectRatio > targetRatio)
            {
                // fit width
                _rawImage.rectTransform.sizeDelta = new Vector2(FitSize.x, FitSize.x / aspectRatio);
            }
            else
            {
                // fit height
                _rawImage.rectTransform.sizeDelta = new Vector2(FitSize.y * aspectRatio, FitSize.y);
            }
        }

        private void UpdateFrame()
        {
            if (_frameCount <= 0 ||
                _rawImage == null ||
                !Playing) return;

            _timeRemaining = Mathf.Min(_timeRemaining, TargetFrameDuration);
            _timeRemaining -= Time.deltaTime;
            while (_timeRemaining <= 0)
            {
                NextFrame();
                _timeRemaining += TargetFrameDuration;
            }
        }

        int mod(int x, int m) // negative mod
        {
            return (x % m + m) % m;
        }
    }
}