# Camera Capture

Preview or Export GIF of recent camera frames in Unity Engine. 

# Install

## Unity Package

There is a Unity package available that install the gdk libraries with some Unity setup classes

To install, add a package via git url
```
https://github.com/Unnamed-Studios-LLC/CameraCapture.git
```

## Manual

Download and the source code and copy the `Runtime` folder into your Scripts

# Usage & Components

## CameraCapture
Record Camera frames.

### Properties
`FrameRate`: Amount of frames recorded per second.
`MaxFrames`: The maximum amount of frames to store. *MaxFrames / FrameRate = Max Clip Duration*
`Recording`: If the camera is currently being recorded.
`DownSample`: The downsampling factor. *2 = 50%, 3 = 33%*

### Frame methods
`CopyFramesTo`: Copies frames to a destination span. Frames contain their size and a reference to their backing **RenderTexture**.
`ClearFrames`: Clears all stored frames and cleans up the backing resources.
`ExportGifAsync`: Exports current frames to a gif file into folder in `persistentDataPath`.

## CapturePreview
Display recorded Camera frames in UI.

### Recording
Add **CapturePreview** component to the camera GameObject you wish to record.

### Properties
`Capture`: The **CameraCapture** to read from.
`FitSize`: The maximum amount of frames to store. *MaxFrames / FrameRate = Max Clip Duration*
`Recording`: If the camera is currently being recorded.
`DownSample`: The downsampling factor. *2 = 50%, 3 = 33%*

### Frame methods
`CopyFramesTo`: Copies frames to a destination span. Frames contain their size and a reference to their backing **RenderTexture**.
`ClearFrames`: Clears all stored frames and cleans up the backing resources.
`ExportGifAsync`: Exports current frames to a gif file into folder in `persistentDataPath`.