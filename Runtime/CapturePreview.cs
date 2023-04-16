using System;
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

        private RawImage _rawImage;
        private CaptureFrame[] _frames;
        private int _frameCount;
        private int _displayedIndex = -1;
        private float _startTime;
        private int _indexOffset;
        private int _syncedFrameRate;

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
            _startTime = Time.time;

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
                _rawImage == null) return;

            if (PlaybackFrameRate != _syncedFrameRate)
            {
                _syncedFrameRate = PlaybackFrameRate;
                _indexOffset = Mathf.Max(0, _displayedIndex);
                _startTime = Time.time;
            }

            var index = (_indexOffset + Mathf.FloorToInt((Time.time - _startTime) * PlaybackFrameRate)) % _frameCount;
            SetFrame(index);
        }
    }
}