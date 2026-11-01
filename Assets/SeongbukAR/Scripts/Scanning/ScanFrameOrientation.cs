using System;

namespace SeongbukAR.Scanning
{
    // Input: unmirrored camera RGBA with top-left row order.
    // Output: an upright image in Unity Texture2D's bottom-left row order.
    public static class ScanFrameOrientation
    {
        public static byte[] ToUnityThumbnail(byte[] rgba, int width, int height, int exifOrientation)
        {
            if (rgba == null || width <= 0 || height <= 0 ||
                rgba.Length != checked(width * height * 4))
                throw new ArgumentException("Invalid camera pixel buffer.");
            if (exifOrientation != 1 && exifOrientation != 3 &&
                exifOrientation != 6 && exifOrientation != 8)
                throw new ArgumentException("Only rear-camera image orientations are supported.");

            int outputWidth = exifOrientation == 6 || exifOrientation == 8 ? height : width;
            int outputHeight = exifOrientation == 6 || exifOrientation == 8 ? width : height;
            var output = new byte[rgba.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int ox, oy;
                    switch (exifOrientation)
                    {
                        case 3: ox = width - 1 - x; oy = height - 1 - y; break;
                        case 6: ox = height - 1 - y; oy = x; break;
                        case 8: ox = y; oy = width - 1 - x; break;
                        default: ox = x; oy = y; break;
                    }
                    int source = (y * width + x) * 4;
                    int destination = ((outputHeight - 1 - oy) * outputWidth + ox) * 4;
                    Buffer.BlockCopy(rgba, source, output, destination, 4);
                }
            }
            return output;
        }
    }
}
