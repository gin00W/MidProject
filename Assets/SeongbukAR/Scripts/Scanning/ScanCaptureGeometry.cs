using System;

namespace SeongbukAR.Scanning
{
    // Shared by the camera crop and full-screen aiming frame. The AR camera
    // background uses a centered aspect-fill image; safe-area/UI offsets must
    // not move the aiming frame away from the sensor center.
    public static class ScanCaptureGeometry
    {
        public const float FrameCoverage = 0.62f;

        public readonly struct Crop
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Side;

            public Crop(int x, int y, int side)
            {
                X = x;
                Y = y;
                Side = side;
            }
        }

        public static Crop CenteredVisibleSquare(int imageWidth, int imageHeight,
            int screenWidth, int screenHeight, int exifOrientation)
        {
            if (imageWidth <= 0 || imageHeight <= 0 || screenWidth <= 0 || screenHeight <= 0)
                throw new ArgumentOutOfRangeException(nameof(imageWidth), "Image and screen dimensions must be positive.");
            if (exifOrientation != 1 && exifOrientation != 3 &&
                exifOrientation != 6 && exifOrientation != 8)
                throw new ArgumentException("Unsupported rear-camera orientation.", nameof(exifOrientation));

            bool portraitSensor = exifOrientation == 6 || exifOrientation == 8;
            int orientedWidth = portraitSensor ? imageHeight : imageWidth;
            int orientedHeight = portraitSensor ? imageWidth : imageHeight;
            double fillScale = Math.Max((double)screenWidth / orientedWidth,
                (double)screenHeight / orientedHeight);
            double frameSide = Math.Min(screenWidth, screenHeight) * (double)FrameCoverage;
            int side = Math.Max(1, Math.Min(Math.Min(imageWidth, imageHeight),
                (int)Math.Floor(frameSide / fillScale)));
            return new Crop((imageWidth - side) / 2, (imageHeight - side) / 2, side);
        }
    }
}
